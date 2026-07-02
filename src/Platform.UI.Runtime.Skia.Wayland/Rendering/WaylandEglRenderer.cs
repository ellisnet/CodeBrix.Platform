using System;
using SkiaSharp;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// GPU renderer (P7, opt-in): a wl_egl_window wraps the content wl_surface, EGL/GLES provides
/// the GL context, and Skia renders through a <see cref="GRContext"/> — the same shape as the
/// X11 head's EGL path. Enabled with the CODEBRIX_WAYLAND_USE_GPU=1 environment variable;
/// otherwise the software <see cref="WaylandShmRenderer"/> is used.
/// </summary>
/// <remarks>
/// EGL_PLATFORM_WAYLAND_KHR (0x31D8) tells eglGetPlatformDisplay the native display is a
/// wl_display*. eglSwapBuffers implicitly attaches+commits the buffer to the wl_surface and
/// blocks for the compositor's frame, so this path does not use wl_shm buffers or the
/// wl_callback frame dance.
/// </remarks>
internal sealed class WaylandEglRenderer : IWaylandRenderer
{
	private const int EGL_PLATFORM_WAYLAND_KHR = 0x31D8;
	private const uint DefaultFramebuffer = 0;

	private readonly object _gate = new();
	private readonly IXamlRootHost _host;
	private readonly WaylandConnection _connection;
	private readonly WlSurface _wlSurface;

	private SKColor _background = SKColors.White;
	private IntPtr _eglDisplay;
	private IntPtr _eglWindow;
	private IntPtr _eglSurface;
	private IntPtr _glContext;
	private GRGlInterface? _glInterface;
	private GRContext? _grContext;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private int _samples;
	private int _stencil;
	private int _width;
	private int _height;
	private int _bufferScale = 1;
	private bool _initialized;
	private bool _disposed;
	private int _renderCount;

	public WaylandEglRenderer(IXamlRootHost host, WaylandConnection connection, WlSurface wlSurface)
	{
		_host = host;
		_connection = connection;
		_wlSurface = wlSurface;
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	private void EnsureContext(int width, int height)
	{
		if (_initialized)
		{
			return;
		}

		_bufferScale = Math.Max(1, _connection.PrimaryOutput.Scale);
		_eglWindow = LibWaylandEgl.wl_egl_window_create(_wlSurface.Handle, width, height);
		if (_eglWindow == IntPtr.Zero)
		{
			throw new InvalidOperationException("wl_egl_window_create failed.");
		}

		_eglDisplay = EglHelper.EglGetPlatformDisplay(EGL_PLATFORM_WAYLAND_KHR, _connection.Display.Handle, null);
		if (_eglDisplay == IntPtr.Zero)
		{
			_eglDisplay = EglHelper.EglGetPlatformDisplayEXT(EGL_PLATFORM_WAYLAND_KHR, _connection.Display.Handle, null);
		}
		if (_eglDisplay == IntPtr.Zero)
		{
			throw new InvalidOperationException($"eglGetPlatformDisplay(EGL_PLATFORM_WAYLAND) failed: {Enum.GetName(EglHelper.EglGetError())}");
		}

		(_eglSurface, _glContext, var major, var minor, _samples, _stencil)
			= EglHelper.InitializeGles2Context(_eglDisplay, _eglWindow);

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Wayland GPU path: EGL {major}.{minor}.");
		}

		MakeCurrent();
		_glInterface = GRGlInterface.CreateGles(EglHelper.EglGetProcAddress)
			?? throw new NotSupportedException("OpenGL ES is not supported on this system.");
		_grContext = GRContext.CreateGl(_glInterface)
			?? throw new NotSupportedException("Failed to create a GL GRContext.");

		_initialized = true;
	}

	public void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		if (_host is WaylandXamlRootHost { IsClosed: true })
		{
			return;
		}

		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}

			// Bind the context on the current (thread-pool) thread before touching GL. On the
			// very first frame the context does not exist yet — it is created lazily in Resize
			// (via the frame-requested callback below), which binds it itself.
			if (_initialized)
			{
				MakeCurrent();
			}

			_surface?.Canvas.Clear(_background);
			_ = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
			{
				Resize((int)size.Width, (int)size.Height);
				_surface!.Canvas.Clear(_background);
				return _surface.Canvas;
			});

			if (_surface == null)
			{
				return;
			}

			_grContext!.Flush();
			_wlSurface.SetBufferScale(_bufferScale);

			// eglSwapBuffers attaches+commits the GL buffer to the wl_surface and blocks for
			// the compositor's frame.
			if (!EglHelper.EglSwapBuffers(_eglDisplay, _eglSurface) && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"eglSwapBuffers failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			// Release the context from this thread. The render timer (System.Timers.Timer)
			// raises Elapsed on varying thread-pool threads, and an EGL context is thread-
			// affine — leaving it bound would make the next frame's MakeCurrent on a different
			// thread fail with BAD_ACCESS. Unbinding here lets the next frame re-bind anywhere.
			ReleaseCurrent();
		}

		_connection.Flush();
	}

	// Must hold _gate.
	private void Resize(int width, int height)
	{
		EnsureContext(width, height);

		if (width == _width && height == _height && _surface != null)
		{
			return;
		}

		_width = width;
		_height = height;
		LibWaylandEgl.wl_egl_window_resize(_eglWindow, width, height, 0, 0);

		_surface?.Dispose();
		_renderTarget?.Dispose();

		const SKColorType colorType = SKColorType.Rgba8888;
		var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, colorType.ToGlSizedFormat());
		_renderTarget = new GRBackendRenderTarget(width, height, _samples, _stencil, glInfo);
		_surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, colorType);
	}

	private void MakeCurrent()
	{
		if (!EglHelper.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _glContext)
			&& this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"eglMakeCurrent failed: {Enum.GetName(EglHelper.EglGetError())}");
		}
	}

	private void ReleaseCurrent()
		=> _ = EglHelper.EglMakeCurrent(_eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

	public void Dispose()
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;

			_surface?.Dispose();
			_renderTarget?.Dispose();
			_grContext?.Dispose();
			_glInterface?.Dispose();

			if (_eglDisplay != IntPtr.Zero)
			{
				if (_eglSurface != IntPtr.Zero)
				{
					_ = EglHelper.EglDestroySurface(_eglDisplay, _eglSurface);
				}
				if (_glContext != IntPtr.Zero)
				{
					_ = EglHelper.EglDestroyContext(_eglDisplay, _glContext);
				}
				_ = EglHelper.EglTerminate(_eglDisplay);
			}

			if (_eglWindow != IntPtr.Zero)
			{
				LibWaylandEgl.wl_egl_window_destroy(_eglWindow);
				_eglWindow = IntPtr.Zero;
			}
		}
	}
}
