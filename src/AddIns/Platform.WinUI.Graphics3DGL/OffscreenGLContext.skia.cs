using System;
using System.Diagnostics.CodeAnalysis;
using CodeBrix.Platform.OpenGL;
using Microsoft.UI.Xaml;
using SkiaSharp;

#if !WINAPPSDK
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;
#endif

#if WINAPPSDK
using Window = Microsoft.UI.Xaml.Window;
#endif

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

/// <summary>
/// A headless, cross-platform OpenGL context for <b>off-screen</b> rendering — the
/// graphics-API-neutral counterpart to <see cref="GLCanvasElement"/>.
/// </summary>
/// <remarks>
/// <para>
/// Where <see cref="GLCanvasElement"/> both renders <i>and</i> displays itself (it is a
/// <see cref="FrameworkElement"/> that owns its on-screen surface), this class hands back only a
/// current-able <see cref="OpenGL.GL"/>. The caller renders into its own framebuffer object, reads
/// the pixels back, and composites them however it likes — for example onto an ordinary Skia
/// canvas that also displays frames produced by a different graphics API. That makes it possible
/// to build one preview surface whose backend can switch between OpenGL and, say, Vulkan at
/// runtime, without an OpenGL-specific UI element in the application.
/// </para>
/// <para>
/// The context is obtained through the same per-head native OpenGL machinery that backs
/// <see cref="GLCanvasElement"/>, so it works on every CodeBrix.Platform head that provides a
/// native OpenGL context. <see cref="MakeCurrent"/> saves and restores whatever context was
/// current, so an off-screen renderer built on this never disturbs the head's own renderer even
/// when they share a thread.
/// </para>
/// </remarks>
public sealed class OffscreenGLContext : IDisposable
{
	private readonly INativeOpenGLWrapper _wrapper;
	private bool _disposed;

	private OffscreenGLContext(INativeOpenGLWrapper wrapper, GL gl)
	{
		_wrapper = wrapper;
		Gl = gl;
	}

	/// <summary>
	/// The OpenGL API bound to this context. Only issue GL calls on it while the context is current
	/// (inside a <c>using</c> of <see cref="MakeCurrent"/>).
	/// </summary>
	public GL Gl { get; }

#if WINAPPSDK
	/// <summary>
	/// Tries to create a headless OpenGL context associated with the given XAML root.
	/// </summary>
	/// <param name="xamlRoot">The XAML root of the window the context is associated with.</param>
	/// <param name="getWindowFunc">A function returning the <see cref="Window"/> the context belongs to (WinUI only).</param>
	/// <param name="context">On success, the created context; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when a context was created; otherwise <see langword="false"/>.</returns>
	public static bool TryCreate(XamlRoot xamlRoot, Func<Window> getWindowFunc, [NotNullWhen(true)] out OffscreenGLContext? context)
	{
		context = null;
		if (xamlRoot is null || getWindowFunc is null)
		{
			return false;
		}

		var wrapper = new WinUINativeOpenGLWrapper(xamlRoot, getWindowFunc);
		return TryFinishCreate(wrapper, out context);
	}
#else
	/// <summary>
	/// Tries to create a headless OpenGL context associated with the given XAML root.
	/// </summary>
	/// <param name="xamlRoot">The XAML root of the window the context is associated with.</param>
	/// <param name="context">
	/// On success, the created context; otherwise <see langword="null"/> (for example when the
	/// running head provides no native OpenGL context).
	/// </param>
	/// <returns><see langword="true"/> when a context was created; otherwise <see langword="false"/>.</returns>
	public static bool TryCreate(XamlRoot xamlRoot, [NotNullWhen(true)] out OffscreenGLContext? context)
	{
		context = null;
		if (xamlRoot is null)
		{
			return false;
		}

		// The same per-head native OpenGL wrapper GLCanvasElement resolves; every CodeBrix.Platform
		// head that supports OpenGL registers one for XamlRoot via ApiExtensibility.
		if (!ApiExtensibility.CreateInstance<INativeOpenGLWrapper>(xamlRoot, out var wrapper) || wrapper is null)
		{
			return false;
		}

		return TryFinishCreate(wrapper, out context);
	}
#endif

	private static bool TryFinishCreate(INativeOpenGLWrapper wrapper, [NotNullWhen(true)] out OffscreenGLContext? context)
	{
		context = null;
		try
		{
			// GL.GetApi loads function pointers lazily through the wrapper, so the context does not
			// need to be current here; the caller makes it current per frame via MakeCurrent().
			var gl = GL.GetApi(wrapper.GetProcAddress);
			context = new OffscreenGLContext(wrapper, gl);
			return true;
		}
		catch
		{
			wrapper.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Makes this context current on the calling thread and returns a disposable that restores the
	/// previously-current context when disposed. Wrap each frame's GL work in a single <c>using</c>
	/// of the returned value.
	/// </summary>
	/// <returns>A disposable that restores the previously-current context.</returns>
	public IDisposable MakeCurrent() => _wrapper.MakeCurrent();

	/// <summary>
	/// Returns the address of the named native GL function, for building a SkiaSharp
	/// <see cref="GRGlInterface"/> (or resolving any other GL entry point). Call inside a
	/// <see cref="MakeCurrent"/> scope.
	/// </summary>
	/// <param name="name">The name of the native GL function to resolve.</param>
	/// <returns>
	/// The function's address, or <see cref="IntPtr.Zero"/> when the running GL implementation does
	/// not expose it.
	/// </returns>
	/// <remarks>
	/// Deliberately non-throwing. Skia's <c>gr_glinterface_assemble_interface</c> probes for entry points
	/// that a given implementation legitimately does not have (on WGL, for instance, it still asks for the
	/// EGL functions such as <c>eglQueryString</c>) and requires a zero return for those. The per-head
	/// <see cref="INativeOpenGLWrapper.GetProcAddress"/> throws instead, and that exception would escape
	/// through the native callback and abort interface assembly, so the probe is routed through
	/// <see cref="INativeOpenGLWrapper.TryGetProcAddress"/> here.
	/// </remarks>
	public IntPtr GetProcAddress(string name)
		=> _wrapper.TryGetProcAddress(name, out var address) ? address : IntPtr.Zero;

	/// <summary>
	/// Builds a SkiaSharp <see cref="GRContext"/> on this off-screen OpenGL context, so GPU-accelerated
	/// Skia can render into an <see cref="SKSurface"/> backed by this context. This is the generic
	/// GPU-Skia entry point: a data-viz control, an image/effects pipeline, an off-screen rasterizer,
	/// or a game engine can all draw into a GPU <see cref="SKSurface"/>, read the pixels back and
	/// composite them onto an ordinary on-screen canvas.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method owns the per-head desktop-GL-vs-GLES branch (X11/Win32/WPF are desktop GL;
	/// Wayland/macOS/Frame Buffer are GLES) so no consumer has to re-implement it. The head is
	/// detected from the loaded head-runtime assembly; whichever flavor that implies is tried first
	/// and, if it fails, the other flavor is tried, so the result is robust even on an unrecognized
	/// host.
	/// </para>
	/// <para>
	/// The context is made current for the duration of creation (<see cref="MakeCurrent"/>
	/// saves/restores the head's own context, so this never disturbs the compositor even when they
	/// share the UI thread). Keep the returned <see cref="GRContext"/> on the thread it was created on
	/// and make this context current again before drawing with it. Dispose the <see cref="GRContext"/>
	/// before disposing this <see cref="OffscreenGLContext"/>.
	/// </para>
	/// </remarks>
	/// <returns>A GPU <see cref="GRContext"/> bound to this off-screen OpenGL context.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when neither a desktop-GL nor a GLES <see cref="GRGlInterface"/>/<see cref="GRContext"/>
	/// could be created on this context.
	/// </exception>
	public GRContext CreateGrContext()
	{
		// GRGlInterface/GRContext creation reads the current GL context, so make ours current here.
		// This is safe to nest inside a caller's own MakeCurrent() (save/restore is balanced).
		using (_wrapper.MakeCurrent())
		{
			var useGles = Graphics3DGLHeadDetection.CurrentHeadUsesGles();

			// Try the flavor the head implies first, then fall back to the other so a misdetected or
			// unrecognized host still succeeds where a context genuinely exists.
			var context = TryCreateGrContext(useGles) ?? TryCreateGrContext(!useGles);
			if (context is null)
			{
				throw new InvalidOperationException(
					"Failed to create a GRContext on the off-screen OpenGL context: both the desktop-GL "
					+ "and the GLES GRGlInterface/GRContext creation paths returned null. Ensure this is "
					+ "called on a head that provides a native OpenGL context, with the context current.");
			}

			return context;
		}
	}

	// Assembles a GRGlInterface for the requested flavor and, from it, a GRContext. Returns null (and
	// disposes any partially-created interface) when this GL flavor is not the one this context speaks.
	private GRContext? TryCreateGrContext(bool useGles)
	{
		var glInterface = useGles
			? GRGlInterface.CreateGles(GetProcAddress)
			: GRGlInterface.Create(GetProcAddress);
		if (glInterface is null)
		{
			return null;
		}

		var context = GRContext.CreateGl(glInterface);
		if (context is null)
		{
			glInterface.Dispose();
		}

		return context;
	}

	/// <summary>Destroys the context and releases its native resources.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		using (_wrapper.MakeCurrent())
		{
			Gl.Dispose();
		}
		_wrapper.Dispose();
	}
}
