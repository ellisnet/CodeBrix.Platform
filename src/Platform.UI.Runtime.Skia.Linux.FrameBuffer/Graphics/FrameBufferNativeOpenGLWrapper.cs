#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Runtime.Skia.Native;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer;

/// <summary>
/// Offscreen EGL <see cref="INativeOpenGLWrapper"/> backing GLCanvasElement on the FrameBuffer
/// head, which has no windowing system. Two context sources are tried in order:
/// <list type="number">
/// <item>A DRM/GBM device (render nodes first — they need no DRM master, so this works even
/// while the DRMRenderer scans out on the same GPU; card nodes as a fallback). The context is
/// made current with no EGL surface (EGL_KHR_surfaceless_context) since GLCanvasElement
/// renders into its own framebuffer object.</item>
/// <item>The Mesa EGL surfaceless platform (EGL_MESA_platform_surfaceless), which selects a
/// hardware render node when one exists and otherwise falls back to the llvmpipe software
/// rasterizer. On GPU-less systems this requires Mesa's software GL to be installed
/// (e.g. the libegl1 and libgl1-mesa-dri packages on Debian/Ubuntu).</item>
/// </list>
/// </summary>
internal sealed class FrameBufferNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const int EGL_PLATFORM_GBM_KHR = 0x31D7;
	private const int EGL_PLATFORM_SURFACELESS_MESA = 0x31DD;
	private const int EGL_SURFACE_TYPE = 0x3033;
	private const int EGL_PBUFFER_BIT = 0x0001;
	private const int EGL_WINDOW_BIT = 0x0004;

	[DllImport("libgbm.so.1")]
	private static extern void gbm_device_destroy(IntPtr gbm);

	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface; // stays EGL_NO_SURFACE on the GBM (surfaceless-context) path
	private IntPtr _gbmDevice;
	private int _deviceFd = -1;

	public FrameBufferNativeOpenGLWrapper()
	{
		if (!TryInitializeGbm() && !TryInitializeSurfaceless())
		{
			throw new InvalidOperationException(
				"Could not create an offscreen EGL context: no usable DRM device for GBM, and the EGL surfaceless " +
				"platform is unavailable. On systems without a GPU, install Mesa's llvmpipe software renderer " +
				"(e.g. the libegl1 and libgl1-mesa-dri packages on Debian/Ubuntu).");
		}
	}

	private bool TryInitializeGbm()
	{
		foreach (var path in DriDevicePaths())
		{
			var fd = Libc.open(path, Libc.O_RDWR, 0);
			if (fd < 0)
			{
				continue;
			}

			var gbmDevice = LibDrm.gbm_create_device(fd);
			if (gbmDevice == IntPtr.Zero)
			{
				_ = Libc.close(fd);
				continue;
			}

			var display = GetPlatformDisplay(EGL_PLATFORM_GBM_KHR, gbmDevice);
			// GBM configs are window-bit (gbm_surface) configs; the context is used surface-less.
			if (display != IntPtr.Zero && TryCreateContext(display, EGL_WINDOW_BIT, out var context, out var surface))
			{
				_deviceFd = fd;
				_gbmDevice = gbmDevice;
				_eglDisplay = display;
				_glContext = context;
				_pBufferSurface = surface;
				this.LogInfo()?.Info($"Created a {nameof(FrameBufferNativeOpenGLWrapper)} instance on DRM/GBM device '{path}'.");
				return true;
			}

			gbm_device_destroy(gbmDevice);
			_ = Libc.close(fd);
		}

		return false;
	}

	private bool TryInitializeSurfaceless()
	{
		var display = GetPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA, IntPtr.Zero);
		if (display == IntPtr.Zero || !TryCreateContext(display, EGL_PBUFFER_BIT, out var context, out var surface))
		{
			return false;
		}

		_eglDisplay = display;
		_glContext = context;
		_pBufferSurface = surface;
		this.LogInfo()?.Info($"Created a {nameof(FrameBufferNativeOpenGLWrapper)} instance on the EGL surfaceless platform.");
		return true;
	}

	private bool TryCreateContext(IntPtr display, int surfaceTypeBit, out IntPtr context, out IntPtr surface)
	{
		context = IntPtr.Zero;
		surface = IntPtr.Zero; // EGL_NO_SURFACE

		if (!EglHelper.EglInitialize(display, out _, out _))
		{
			return false;
		}

		int[] attribs =
		{
			EglHelper.EGL_RED_SIZE, 8,
			EglHelper.EGL_GREEN_SIZE, 8,
			EglHelper.EGL_BLUE_SIZE, 8,
			EglHelper.EGL_ALPHA_SIZE, 8,
			EglHelper.EGL_DEPTH_SIZE, 8,
			EglHelper.EGL_STENCIL_SIZE, 1,
			EGL_SURFACE_TYPE, surfaceTypeBit,
			EglHelper.EGL_RENDERABLE_TYPE, EglHelper.EGL_OPENGL_ES2_BIT,
			EglHelper.EGL_NONE
		};
		var configs = new IntPtr[1];
		if (!EglHelper.EglChooseConfig(display, attribs, configs, configs.Length, out var numConfig) || numConfig < 1)
		{
			return false;
		}

		// GLCanvasElement requires GL(ES) 3.0+, so ask for a 3.x context first and only then
		// fall back to 2 (Mesa usually hands back the highest supported version either way).
		context = EglHelper.EglCreateContext(display, configs[0], IntPtr.Zero, [EglHelper.EGL_CONTEXT_CLIENT_VERSION, 3, EglHelper.EGL_NONE]);
		if (context == IntPtr.Zero)
		{
			context = EglHelper.EglCreateContext(display, configs[0], IntPtr.Zero, [EglHelper.EGL_CONTEXT_CLIENT_VERSION, 2, EglHelper.EGL_NONE]);
		}
		if (context == IntPtr.Zero)
		{
			return false;
		}

		if (surfaceTypeBit == EGL_PBUFFER_BIT)
		{
			// May legitimately fail and stay EGL_NO_SURFACE — the make-current below then
			// validates the surfaceless-context path instead.
			surface = EglHelper.EglCreatePbufferSurface(display, configs[0], [EglHelper.EGL_NONE]);
		}

		// Validate with a test make-current (with EGL_NO_SURFACE this needs
		// EGL_KHR_surfaceless_context, which Mesa has supported for a very long time).
		var previousContext = EglHelper.EglGetCurrentContext();
		var previousDisplay = EglHelper.EglGetCurrentDisplay();
		var previousRead = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
		var previousDraw = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
		var success = EglHelper.EglMakeCurrent(display, surface, surface, context);
		if (previousDisplay != IntPtr.Zero)
		{
			EglHelper.EglMakeCurrent(previousDisplay, previousDraw, previousRead, previousContext);
		}
		else
		{
			EglHelper.EglMakeCurrent(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		}

		if (!success)
		{
			if (surface != IntPtr.Zero)
			{
				EglHelper.EglDestroySurface(display, surface);
				surface = IntPtr.Zero;
			}
			EglHelper.EglDestroyContext(display, context);
			context = IntPtr.Zero;
		}

		return success;
	}

	private static IEnumerable<string> DriDevicePaths()
	{
		// Render nodes first: offscreen rendering needs no DRM master, so these work even
		// while another process (or this head's DRMRenderer) owns the display.
		for (var i = 128; i <= 135; i++)
		{
			yield return $"/dev/dri/renderD{i}";
		}
		for (var i = 0; i <= 7; i++)
		{
			yield return $"/dev/dri/card{i}";
		}
	}

	private static IntPtr GetPlatformDisplay(int platform, IntPtr nativeDisplay)
	{
		try
		{
			var display = EglHelper.EglGetPlatformDisplay(platform, nativeDisplay, null);
			if (display != IntPtr.Zero)
			{
				return display;
			}
		}
		catch (Exception)
		{
			// eglGetPlatformDisplay entry point missing (EGL < 1.5); try the EXT variant below.
		}

		try
		{
			return EglHelper.EglGetPlatformDisplayEXT(platform, nativeDisplay, null);
		}
		catch (Exception)
		{
			return IntPtr.Zero;
		}
	}

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"A procedure named {proc} was not found in libEGL");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = EglHelper.EglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = EglHelper.EglGetCurrentContext();
		var display = EglHelper.EglGetCurrentDisplay();
		var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
		var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
		if (!EglHelper.EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			// There is frequently no previous context on this head; restore to "nothing
			// current" via our own display rather than passing EGL_NO_DISPLAY.
			var restoreDisplay = display != IntPtr.Zero ? display : _eglDisplay;
			if (!EglHelper.EglMakeCurrent(restoreDisplay, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			}
		});
	}

	public void Dispose()
	{
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			EglHelper.EglDestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			EglHelper.EglDestroyContext(_eglDisplay, _glContext);
		}
		if (_gbmDevice != IntPtr.Zero)
		{
			gbm_device_destroy(_gbmDevice);
		}
		if (_deviceFd >= 0)
		{
			_ = Libc.close(_deviceFd);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
		_gbmDevice = IntPtr.Zero;
		_deviceFd = -1;
	}
}
