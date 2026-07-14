using System;
using System.Diagnostics.CodeAnalysis;
using CodeBrix.Platform.OpenGL;
using Microsoft.UI.Xaml;

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
