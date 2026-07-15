using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Window = Microsoft.UI.Xaml.Window;

#if !CODEBRIX_UWP_BUILD
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

#if WINAPPSDK
using System.Runtime.InteropServices.WindowsRuntime;
#else
using Microsoft.UI.Composition;
using CodeBrix.Platform.UI.Dispatching;
using Buffer = Windows.Storage.Streams.Buffer;
#endif

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

/// <summary>
/// A <see cref="FrameworkElement"/> that draws GPU-accelerated Skia — the reusable "draw GPU Skia in
/// a CodeBrix.Platform view" primitive, i.e. the real, functional equivalent of SkiaSharp's
/// <c>SKGLView</c>. It is the Skia sibling of <see cref="GLCanvasElement"/>: where that element hands
/// you a raw OpenGL <see cref="OpenGL.GL"/>, this one hands you a GPU-backed <see cref="SKSurface"/>
/// and its <see cref="GRContext"/>, so you draw with the familiar SkiaSharp canvas API and let this
/// element read the pixels back and composite them on screen.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here is specific to any one GPU-Skia consumer: a data-viz control, an image/effects
/// pipeline, a custom shader-driven control, an off-screen rasterizer or a game engine can all use it.
/// It owns an <see cref="OffscreenGLContext"/> and its <see cref="GRContext"/> (which owns the
/// per-head desktop-GL-vs-GLES branch — see <see cref="OffscreenGLContext.CreateGrContext"/>), renders
/// each frame into a GPU <see cref="SKSurface"/>, reads the result back to CPU pixels in a single
/// copy and paints them into a <see cref="WriteableBitmap"/> shown as the element's background.
/// </para>
/// <para>
/// Handle <see cref="PaintSurface"/> (or override <see cref="OnPaintSurface"/>) to draw. Call
/// <see cref="Invalidate"/> to request a repaint; call it from inside your paint handler to animate.
/// This is available on the CodeBrix.Platform Skia-based heads that provide a native OpenGL context:
/// Windows (Win32-Skia and WPF-Skia), Linux (X11, Wayland and Frame Buffer) and macOS; and on WinUI.
/// </para>
/// <para>
/// All work stays on the UI thread: <see cref="OffscreenGLContext.MakeCurrent"/> saves and restores
/// the head's own current context, so the off-screen context never disturbs the head's renderer even
/// though they share the thread.
/// </para>
/// </remarks>
public partial class SkiaGLCanvasElement : Grid
{
#if WINAPPSDK
	private const int BytesPerPixel = 4;
#endif

	private readonly Func<Window>? _getWindowFunc;

	// Valid once loaded on a head that provides OpenGL; null otherwise.
	private OffscreenGLContext? _context;
	private GRContext? _grContext;
	private SKSurface? _surface;
	private SKImageInfo _surfaceInfo;
	private WriteableBitmap? _backBuffer;
#if WINAPPSDK
	private IntPtr _pixels;
#endif

	/// <summary>Creates a GPU-Skia canvas element.</summary>
	/// <param name="getWindowFunc">
	/// A function returning the <see cref="Window"/> this element belongs to. Required on WinUI; on
	/// CodeBrix.Platform heads it may be <see langword="null"/>.
	/// </param>
#if WINAPPSDK
	public SkiaGLCanvasElement(Func<Window> getWindowFunc)
#else
	public SkiaGLCanvasElement(Func<Window>? getWindowFunc = null)
#endif
	{
		_getWindowFunc = getWindowFunc;

		// No vertical flip here (unlike GLCanvasElement): a GPU SKSurface uses a top-left origin, so
		// its read-back rows already run top-to-bottom like the WriteableBitmap.
		Background = new ImageBrush();

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += (_, _) => UpdateSurface();
	}

	/// <summary>
	/// Raised once per frame with a GPU-backed <see cref="SKSurface"/> to draw into. The GL context is
	/// current for the duration of the handler; the surface's pixels are read back and shown after it
	/// returns. Call <see cref="Invalidate"/> from within the handler to drive an animation.
	/// </summary>
	public event EventHandler<SkiaGLPaintSurfaceEventArgs>? PaintSurface;

	/// <summary>
	/// Whether this element loaded successfully, including creating the off-screen OpenGL context and
	/// its <see cref="GRContext"/>. Valid only while the element is loaded; <see langword="null"/> when
	/// it is not in the visual tree.
	/// </summary>
	public bool? IsGpuInitialized { get; private set; }

	/// <summary>
	/// Draws a frame. The base implementation raises <see cref="PaintSurface"/>; override to draw
	/// without subscribing. The GL context is current and <paramref name="args"/> carries the surface,
	/// its <see cref="GRContext"/> and pixel geometry.
	/// </summary>
	/// <param name="args">The surface and context for this frame.</param>
	protected virtual void OnPaintSurface(SkiaGLPaintSurfaceEventArgs args)
		=> PaintSurface?.Invoke(this, args);

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!TryCreateContext(out _context))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(SkiaGLCanvasElement)} could not create an off-screen OpenGL context. Make sure you are running on a platform with OpenGL support.");
				}
				IsGpuInitialized = false;
				return;
			}

			using (_context.MakeCurrent())
			{
				_grContext = _context.CreateGrContext();
			}

			UpdateSurface();

			var window = GetWindow();
			if (window is not null)
			{
				window.Closed += OnClosed;
			}
			else if (XamlRoot?.Content is FrameworkElement fe) // for islands
			{
				fe.Unloaded += OnClosed;
			}

			IsGpuInitialized = true;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(SkiaGLCanvasElement)} initialization failed.", ex);
			}
			DisposeGpuResources();
			IsGpuInitialized = false;
		}
	}

	private bool TryCreateContext([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out OffscreenGLContext? context)
	{
#if WINAPPSDK
		return OffscreenGLContext.TryCreate(XamlRoot!, _getWindowFunc!, out context);
#else
		return OffscreenGLContext.TryCreate(XamlRoot!, out context);
#endif
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		IsGpuInitialized = null;

		var window = GetWindow();
		if (window is not null)
		{
			window.Closed -= OnClosed;
		}
		else if (XamlRoot?.Content is FrameworkElement fe)
		{
			fe.Unloaded -= OnClosed;
		}

		DisposeGpuResources();
	}

	private void OnClosed(object sender, object args) => DisposeGpuResources();

	private void DisposeGpuResources()
	{
		if (_context is not null && (_surface is not null || _grContext is not null))
		{
			// Surface and GRContext must be released with the GL context current and before the
			// context itself is destroyed.
			using (_context.MakeCurrent())
			{
				_surface?.Dispose();
				_grContext?.Dispose();
			}
		}

		_surface = null;
		_grContext = null;

		_context?.Dispose();
		_context = null;

#if WINAPPSDK
		if (_pixels != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_pixels);
			_pixels = IntPtr.Zero;
		}
#endif

		_backBuffer = null;
	}

	private Window? GetWindow()
	{
#if WINAPPSDK
		return _getWindowFunc?.Invoke();
#else
		return XamlRoot?.HostWindow;
#endif
	}

	private void UpdateSurface()
	{
		if (!IsLoaded || _context is null || _grContext is null)
		{
			return;
		}

		// A zero-sized element (collapsed or not yet arranged) can't back a surface; SizeChanged
		// rebuilds it once there is a real size.
		if (RenderSize.Width <= 0 || RenderSize.Height <= 0)
		{
			return;
		}

		var width = (int)RenderSize.Width;
		var height = (int)RenderSize.Height;

		using (_context.MakeCurrent())
		{
			_surface?.Dispose();

			// Prefer BGRA8888 so read-back into the BGRA WriteableBitmap is a straight copy; fall back
			// to RGBA8888 (universally renderable) and let read-back convert if the GPU won't render BGRA.
			_surfaceInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			_surface = SKSurface.Create(_grContext, true, _surfaceInfo);
			if (_surface is null)
			{
				_surfaceInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
				_surface = SKSurface.Create(_grContext, true, _surfaceInfo);
			}

			if (_surface is null)
			{
				throw new InvalidOperationException($"Could not create a {width}x{height} GPU SKSurface on the off-screen GRContext.");
			}
		}

#if WINAPPSDK
		if (_pixels != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_pixels);
		}
		_pixels = Marshal.AllocHGlobal(width * height * BytesPerPixel);
#endif

		_backBuffer = new WriteableBitmap(width, height);
		((ImageBrush)Background).ImageSource = _backBuffer;

		Invalidate();
	}

	private unsafe void Render()
	{
		if (!IsLoaded || _context is null || _grContext is null || _surface is null || _backBuffer is null)
		{
			return;
		}

		using (_context.MakeCurrent())
		{
			OnPaintSurface(new SkiaGLPaintSurfaceEventArgs(_surface, _grContext, _surfaceInfo));

			_surface.Flush();
			_grContext.Flush();

			// The WriteableBitmap is BGRA8888; read the GPU surface straight into its pixel buffer in a
			// single copy (SkiaSharp converts if the surface happens to be RGBA8888).
			var dstInfo = new SKImageInfo((int)RenderSize.Width, (int)RenderSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

#if WINAPPSDK
			_surface.ReadPixels(dstInfo, _pixels, dstInfo.RowBytes, 0, 0);
			using (var stream = _backBuffer.PixelBuffer.AsStream())
			{
				stream.Write(new ReadOnlySpan<byte>((void*)_pixels, dstInfo.BytesSize));
			}
#else
			Buffer.Cast(_backBuffer.PixelBuffer).ApplyActionOnRawBufferPtr(ptr =>
			{
				_surface.ReadPixels(dstInfo, ptr, dstInfo.RowBytes, 0, 0);
			});
			_backBuffer.PixelBuffer.Length = (uint)dstInfo.BytesSize;
#endif
			_backBuffer.Invalidate();
		}
	}

	/// <summary>
	/// Invalidates the rendering and queues a single call to the paint path. Call this whenever the
	/// content needs to update; call it from inside your <see cref="PaintSurface"/> handler to
	/// continuously animate.
	/// </summary>
#if WINAPPSDK
	public void Invalidate() => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Render);
#else
	public void Invalidate() => Compositor.GetSharedCompositor().InvalidateRender(Visual);

	private protected override ContainerVisual CreateElementVisual()
		=> new SkiaGLVisual(this, Compositor.GetSharedCompositor());

	private sealed class SkiaGLVisual(SkiaGLCanvasElement owner, Compositor compositor) : BorderVisual(compositor)
	{
		internal override void Paint(in PaintingSession session)
		{
			NativeDispatcher.Main.Enqueue(owner.Render, NativeDispatcherPriority.High);
			base.Paint(session);
		}
	}
#endif
}
