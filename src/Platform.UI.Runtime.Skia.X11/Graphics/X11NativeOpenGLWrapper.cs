using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

internal class X11NativeOpenGLWrapper : INativeOpenGLWrapper
{
	// GL_SHADING_LANGUAGE_VERSION. The shader sources GLCanvasElement subclasses compile are the
	// thing that actually fails on a too-old context, so this - not GL_VERSION - is what decides
	// whether the GLX context we just built is usable. (A V3D context reports GL_VERSION 3.1,
	// which clears GLCanvasElement's 3.0 floor, and only then fails on "#version 330 core".)
	private const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;

	// The GLSL level a "#version 330 core" shader needs. Matches the floor CreateContext asks
	// GLX_ARB_create_context for.
	private const double RequiredGlslVersion = 3.30;

	private IntPtr _display;
	private IntPtr _glContext;
	private IntPtr _pBuffer;

	// Non-zero only when the GLX context turned out to be too old for modern shaders and we
	// rebuilt this wrapper on top of EGL/GLES instead (see TrySwitchToEglIfGlslTooOld).
	private IntPtr _eglDisplay;

	private bool UsingEgl => _eglDisplay != IntPtr.Zero;

	public unsafe X11NativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		if (XamlRootMap.GetHostForRoot(xamlRoot) is not X11XamlRootHost xamlRootHost)
		{
			throw new InvalidOperationException($"The XamlRoot and its XamlRootHost must be initialized on the element before constructing an {nameof(X11NativeOpenGLWrapper)}.");
		}

		_display = xamlRootHost.RootX11Window.Display;

		using var lockDisposable = X11Helper.XLock(_display);

		var glxAttribs = new int[]{
			GlxConsts.GLX_DRAWABLE_TYPE   , GlxConsts.GLX_PBUFFER_BIT,
			GlxConsts.GLX_RED_SIZE        , 8,
			GlxConsts.GLX_GREEN_SIZE      , 8,
			GlxConsts.GLX_BLUE_SIZE       , 8,
			GlxConsts.GLX_ALPHA_SIZE      , 8,
			GlxConsts.GLX_DEPTH_SIZE      , 8,
			GlxConsts.GLX_STENCIL_SIZE    , 8,
			(int)X11Helper.None
		};

		var fbConfigs = GlxInterface.glXChooseFBConfig(_display, XLib.XDefaultScreen(_display), glxAttribs, out var count);
		if (fbConfigs == null || *fbConfigs == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXChooseFBConfig)} failed to retrieve GLX framebuffer configurations.");
		}
		using var fbConfigsDisposable = new DisposableStruct<IntPtr>(static aa => { _ = XLib.XFree(aa); }, (IntPtr)fbConfigs);

		IntPtr bestFbc = IntPtr.Zero;
		for (var c = 0; c < count; c++)
		{
			XVisualInfo* visual = GlxInterface.glXGetVisualFromFBConfig(_display, fbConfigs[c]);
			using var visualDisposable = new DisposableStruct<IntPtr>(static aa => { _ = XLib.XFree(aa); }, (IntPtr)visual);
			if (visual->depth == 32) // 24bit color + 8bit stencil as requested above
			{
				bestFbc = fbConfigs[c];
				break;
			}
		}

		if (bestFbc == IntPtr.Zero)
		{
			throw new InvalidOperationException("Could not find a suitable framebuffer config.\n");
		}

		_glContext = GlxInterface.CreateContext(_display, bestFbc, out var usedLegacyFallback);
		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.CreateContext)} failed.");
		}
		_pBuffer = GlxInterface.glXCreatePbuffer(_display, bestFbc, new[] { (int)X11Helper.None });
		if (_pBuffer == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXCreatePbuffer)} failed.");
		}

		// Only when GLX could not give us the 3.3 context we asked for is there any question about
		// what this context can compile. Drivers that honoured the request are left completely
		// alone - not even the version query below runs on them.
		if (usedLegacyFallback)
		{
			TrySwitchToEglIfGlslTooOld();
		}
	}

	/// <summary>
	/// Reads GL_SHADING_LANGUAGE_VERSION off the legacy GLX context and, if it is genuinely below
	/// what modern shaders need, replaces this wrapper's context with an EGL/GLES one.
	/// </summary>
	/// <remarks>
	/// Mesa's V3D driver (Raspberry Pi 4/5) caps desktop GL at 3.1 / GLSL 1.40 while supporting
	/// GLES 3.x, so a GLX context there can never compile "#version 330 core" no matter what is
	/// requested - but the very same GPU compiles "#version 300 es" happily through EGL. That is
	/// the context shape the Wayland head already uses on this hardware.
	/// <para/>
	/// Deliberately best-effort: any failure to determine the version, and any failure to build the
	/// EGL replacement, leaves the GLX context in place and untouched. A machine we cannot make a
	/// confident judgement about behaves exactly as it did before this method existed.
	/// </remarks>
	private void TrySwitchToEglIfGlslTooOld()
	{
		double glslVersion;
		try
		{
			glslVersion = QueryGlslVersion();
		}
		catch (Exception e)
		{
			// Could not tell - assume the driver is fine and keep the context we have.
			this.Log().Debug($"Could not determine the GLSL version of the legacy GLX context; keeping it. ({e.Message})");
			return;
		}

		if (double.IsNaN(glslVersion))
		{
			this.Log().Debug("GL_SHADING_LANGUAGE_VERSION was empty or unparseable; keeping the GLX context.");
			return;
		}

		if (glslVersion >= RequiredGlslVersion)
		{
			return;
		}

		this.LogInfo()?.Info(
			$"The GLX context only provides GLSL {glslVersion.ToString("0.00", CultureInfo.InvariantCulture)} " +
			$"(< {RequiredGlslVersion.ToString("0.00", CultureInfo.InvariantCulture)}), which cannot compile modern shaders. " +
			"Replacing it with an EGL/GLES context.");

		try
		{
			SwitchToEgl();
		}
		catch (Exception e)
		{
			// Keep the GLX context: it renders an empty pane and reports a clear failure, whereas a
			// half-built EGL one would be worse than what we started with.
			this.Log().Error($"Could not create an EGL/GLES replacement context; keeping the GLX context. ({e.Message})");
		}
	}

	/// <summary>
	/// Returns the context's GLSL version as a double (e.g. 1.40, 3.30), or <see cref="double.NaN"/>
	/// when it cannot be determined. Only ever called with a context we have just made current.
	/// </summary>
	private unsafe double QueryGlslVersion()
	{
		using var _ = MakeCurrent();

		var glGetStringPtr = GlxInterface.glXGetProcAddress("glGetString");
		if (glGetStringPtr == IntPtr.Zero)
		{
			return double.NaN;
		}

		var glGetString = (delegate* unmanaged[Cdecl]<int, byte*>)glGetStringPtr;
		var versionBytePtr = glGetString(GL_SHADING_LANGUAGE_VERSION);
		if (versionBytePtr == null)
		{
			return double.NaN;
		}

		var versionString = Marshal.PtrToStringUTF8((IntPtr)versionBytePtr);
		if (string.IsNullOrWhiteSpace(versionString))
		{
			return double.NaN;
		}

		this.LogInfo()?.Info($"The legacy GLX context reports GL_SHADING_LANGUAGE_VERSION = '{versionString}'.");

		// Desktop GLSL reports "<major>.<minor>[.<release>] [vendor detail]", e.g. "1.40" or
		// "3.30 NVIDIA via Cg compiler". Take only the leading numeric token.
		var firstToken = versionString.Trim().Split(' ')[0];
		var parts = firstToken.Split('.');
		if (parts.Length < 2
			|| !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
			|| !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
		{
			return double.NaN;
		}

		// "1.40" is minor 40, "3.3" is minor 3 - normalise both to hundredths.
		return major + (minor < 10 ? minor / 10.0 : minor / 100.0);
	}

	/// <summary>
	/// Builds an offscreen EGL/GLES context for this X11 display and swaps it in, tearing down the
	/// GLX one only once the replacement is known to be good.
	/// </summary>
	private void SwitchToEgl()
	{
		// Same display acquisition the head's own X11EGLRenderer uses.
		var eglDisplay = EglHelper.EglGetDisplay(_display);
		if (eglDisplay == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(EglHelper.EglGetDisplay)} failed for the X11 display: {Enum.GetName(EglHelper.EglGetError())}");
		}

		// Bringing up EGL talks X11, and an X protocol error runs Xlib's default handler, which
		// terminates the process outright - a failure mode no try/catch here could contain. Install
		// a handler that just records the error for the duration, exactly as GlxInterface's own
		// context creation does, so a hostile EGL implementation degrades into "keep the GLX
		// context" instead of taking the application down.
		var errorOccurred = false;
		XErrorHandler errorHandler = (IntPtr _, ref XErrorEvent errorEvent) =>
		{
			errorOccurred = true;
			return 0;
		};

		IntPtr surface, glContext;
		int major, minor;

		IntPtr previousErrorHandler = XLib.XSetErrorHandler(errorHandler);
		try
		{
			// No window handle, and prefer a surfaceless context over a pbuffer: this renders only
			// into FBOs, and Mesa's X11 platform turns the zero-sized pbuffer request into a 0x0
			// X_CreatePixmap, which is a fatal BadValue rather than a recoverable EGL failure.
			(surface, glContext, major, minor, _, _) = EglHelper.InitializeGles2Context(eglDisplay, window: null, preferSurfaceless: true);
			_ = XLib.XSync(_display, false);
		}
		finally
		{
			_ = XLib.XSetErrorHandlerPtr(previousErrorHandler);
			// Xlib keeps using the raw function pointer until it is replaced, but nothing reads
			// errorHandler after it is installed, so without this the JIT is free to treat it as
			// dead and let the GC collect the native thunk early.
			GC.KeepAlive(errorHandler);
		}

		if (errorOccurred)
		{
			throw new InvalidOperationException("An X protocol error occurred while creating the EGL/GLES context.");
		}

		// Only now that the replacement is known to be good is it safe to drop the GLX objects.
		DisposeGlxObjects();

		_eglDisplay = eglDisplay;
		_pBuffer = surface;
		_glContext = glContext;

		this.LogInfo()?.Info($"Replaced the GLX context with an EGL {major}.{minor} GLES context (GL_VERSION: '{SafeGetGlVersionString()}').");
	}

	private string SafeGetGlVersionString()
	{
		try
		{
			using var _ = MakeCurrent();
			return EglHelper.GetGlVersionString();
		}
		catch (Exception e)
		{
			return $"(unavailable: {e.Message})";
		}
	}

	private void DisposeGlxObjects()
	{
		if (_display != IntPtr.Zero && _pBuffer != IntPtr.Zero)
		{
			GlxInterface.glXDestroyPbuffer(_display, _pBuffer);
		}
		if (_display != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			GlxInterface.glXDestroyContext(_display, _glContext);
		}

		_pBuffer = IntPtr.Zero;
		_glContext = IntPtr.Zero;
	}

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_display);

		if (UsingEgl)
		{
			// Deliberately no EglTerminate: the EGLDisplay for this X11 display is shared with any
			// other EGL user in the process (e.g. the opt-in X11EGLRenderer).
			if (_pBuffer != IntPtr.Zero)
			{
				EglHelper.EglDestroySurface(_eglDisplay, _pBuffer);
			}
			if (_glContext != IntPtr.Zero)
			{
				EglHelper.EglDestroyContext(_eglDisplay, _glContext);
			}

			_eglDisplay = default;
			_pBuffer = default;
			_glContext = default;
		}
		else
		{
			DisposeGlxObjects();
		}

		_display = default;
	}

	public IDisposable MakeCurrent()
	{
		if (UsingEgl)
		{
			var previousContext = EglHelper.EglGetCurrentContext();
			var previousDisplay = EglHelper.EglGetCurrentDisplay();
			var previousRead = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
			var previousDraw = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
			if (!EglHelper.EglMakeCurrent(_eglDisplay, _pBuffer, _pBuffer, _glContext))
			{
				this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
			}

			// Captured by value: Dispose may have cleared the field by the time this is unwound.
			var eglDisplay = _eglDisplay;
			return Disposable.Create(() =>
			{
				// Nothing was current before, so there is nothing to restore. Asking to restore
				// it anyway means eglMakeCurrent(EGL_NO_DISPLAY, ...), which is itself an error
				// (EGL_BAD_DISPLAY) and only ever logged a failure for doing nothing wrong.
				// Release our context instead, which is the actual intent.
				var restored = previousDisplay == IntPtr.Zero
					? EglHelper.EglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero)
					: EglHelper.EglMakeCurrent(previousDisplay, previousDraw, previousRead, previousContext);

				if (!restored)
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			});
		}

		var glContext = GlxInterface.glXGetCurrentContext();
		var drawable = GlxInterface.glXGetCurrentDrawable();
		GlxInterface.glXMakeCurrent(_display, _pBuffer, _glContext);
		return Disposable.Create(() => GlxInterface.glXMakeCurrent(_display, drawable, glContext));
	}

	// glXGetProcAddress never returns null: glvnd/Mesa return a lazily-resolved dispatch stub for
	// any requested name, including EGL entry points a GLX context can never service. Callers
	// null-check the result and then call it (e.g. Skia probes eglQueryString/eglGetCurrentDisplay
	// when assembling a GRGlInterface), so forwarding those garbage stubs causes native crashes.
	// The EGL path has no such problem - eglGetProcAddress is the right resolver for it.
	public IntPtr GetProcAddress(string proc) =>
		UsingEgl
			? EglHelper.EglGetProcAddress(proc)
			: proc.StartsWith("egl", StringComparison.Ordinal) ? IntPtr.Zero : GlxInterface.glXGetProcAddress(proc);

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = GetProcAddress(proc);
		return addr != IntPtr.Zero;
	}
}
