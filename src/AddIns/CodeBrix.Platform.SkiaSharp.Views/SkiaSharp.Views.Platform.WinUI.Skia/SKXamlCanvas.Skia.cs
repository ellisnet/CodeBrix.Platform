using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CodeBrix.Platform.UI.Hosting;

#if WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#endif

#if WINDOWS || WINUI
namespace SkiaSharp.Views.Windows
#else
namespace SkiaSharp.Views.UWP
#endif
{
	public partial class SKXamlCanvas : Canvas
	{
		private byte[] pixels;
		private GCHandle pixelsHandle;
		private int pixelWidth;
		private int pixelHeight;
		private WriteableBitmap bitmap;

		public SKXamlCanvas()
		{
			Initialize();
		}

		partial void DoUnloaded() =>
			FreeBitmap();

		private void DoInvalidate()
		{
			if (designMode)
				return;

			if (!isVisible)
				return;

			if (ActualWidth <= 0 || ActualHeight <= 0)
			{
				CanvasSize = SKSize.Empty;
				return;
			}

			var info = CreateBitmap(out var unscaledSize, out var dpi);

			// Opt-in (app-wide, via UseDirectSkiaCanvasMode()): draw straight into the on-screen
			// WriteableBitmap buffer, skipping the staging buffer and the per-frame copy below. When
			// the mode is off, everything below runs exactly as before.
			if (DirectSkiaCanvasMode.IsEnabled)
			{
				DoInvalidateDirect(info, unscaledSize, dpi);
				return;
			}

			using (var surface = SKSurface.Create(info, pixelsHandle.AddrOfPinnedObject(), info.RowBytes))
			{
				var userVisibleSize = IgnorePixelScaling ? unscaledSize : info.Size;
				CanvasSize = userVisibleSize;

				if (IgnorePixelScaling)
				{
					var canvas = surface.Canvas;
					canvas.Scale(dpi);
					canvas.Save();
				}

				OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info.WithSize(userVisibleSize), info));
			}

			// Copy the staging buffer into the WriteableBitmap. (The direct path above avoids this
			// copy by drawing into the bitmap's own buffer.)
			using (var data = bitmap.PixelBuffer.AsStream())
			{
				data.Write(pixels, 0, pixels.Length);
				data.Flush();
			}

			bitmap.Invalidate();
		}

		// Direct present path used only when DirectSkiaCanvasMode.IsEnabled. Renders the frame straight
		// into the WriteableBitmap's own pixel buffer, which is pinned only for the duration of the
		// paint (so the whole draw happens inside the callback), then presents it — one fewer
		// full-frame copy than the default path.
		private void DoInvalidateDirect(SKImageInfo info, SKSizeI unscaledSize, float dpi)
		{
			var userVisibleSize = IgnorePixelScaling ? unscaledSize : info.Size;
			CanvasSize = userVisibleSize;

			global::Windows.Storage.Streams.Buffer.Cast(bitmap.PixelBuffer).ApplyActionOnRawBufferPtr(ptr =>
			{
				using var surface = SKSurface.Create(info, ptr, info.RowBytes);

				if (IgnorePixelScaling)
				{
					var canvas = surface.Canvas;
					canvas.Scale(dpi);
					canvas.Save();
				}

				OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info.WithSize(userVisibleSize), info));
			});

			bitmap.PixelBuffer.Length = (uint)info.BytesSize;
			bitmap.Invalidate();
		}

		private SKImageInfo CreateBitmap(out SKSizeI unscaledSize, out float dpi)
		{
			var size = CreateSize(out unscaledSize, out dpi);
			var info = new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

			if (bitmap?.PixelWidth != info.Width || bitmap?.PixelHeight != info.Height)
				FreeBitmap();

			if (bitmap == null && info.Width > 0 && info.Height > 0)
			{
				bitmap = new WriteableBitmap(info.Width, info.Height);

				var brush = new ImageBrush
				{
					ImageSource = bitmap,
					AlignmentX = AlignmentX.Left,
					AlignmentY = AlignmentY.Top,
					Stretch = Stretch.Fill
				};

				Background = brush;
			}

			// Direct mode draws into the bitmap's own buffer, so the staging `pixels` array is never
			// needed. When the mode is off, this condition is exactly the original.
			if (!DirectSkiaCanvasMode.IsEnabled && (pixels == null || pixelWidth != info.Width || pixelHeight != info.Height))
			{
				FreeBitmap();

				pixels = new byte[info.BytesSize];
				pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

				pixelWidth = info.Width;
				pixelHeight = info.Height;
			}

			return info;
		}

		private void FreeBitmap()
		{
			if (pixels != null)
			{
				pixelsHandle.Free();
				pixels = null;
				bitmap = null;
			}
			else if (DirectSkiaCanvasMode.IsEnabled)
			{
				// Direct mode never allocates `pixels`, so drop the bitmap here too (e.g. on resize).
				// Inert when the mode is off — the branch above is then the only one that runs.
				bitmap = null;
			}
		}
	}
}
