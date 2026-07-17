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

		// Staging-path caches, valid while the staging buffer is unchanged: repainting at game
		// rates (60-70+ Hz) must not allocate a fresh surface + event args per frame.
		private SKSurface stagingSurface;
		private SKPaintSurfaceEventArgs stagingArgs;
		private Action<IntPtr> copyStagingToBitmapAction;

		// Direct-path caches (DirectSkiaCanvasMode), revalidated against the bitmap's raw
		// buffer pointer every frame; the per-frame values reach the cached callback via
		// these fields because its Action<IntPtr> signature is fixed.
		private SKSurface directSurface;
		private IntPtr directSurfacePtr;
		private SKImageInfo directSurfaceInfo;
		private SKPaintSurfaceEventArgs directArgs;
		private Action<IntPtr> directPaintAction;
		private SKImageInfo directInfo;
		private SKSizeI directUserVisibleSize;
		private float directDpi;

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

			{
				var userVisibleSize = IgnorePixelScaling ? unscaledSize : info.Size;
				CanvasSize = userVisibleSize;

				// The surface and event args are cached while the staging buffer is unchanged
				// (see CreateBitmap/FreeBitmap); allocating them per paint causes measurable
				// GC churn at game frame rates.
				stagingSurface ??= SKSurface.Create(info, pixelsHandle.AddrOfPinnedObject(), info.RowBytes);
				if (stagingArgs is null || stagingArgs.Info.Size != userVisibleSize)
				{
					stagingArgs = new SKPaintSurfaceEventArgs(stagingSurface, info.WithSize(userVisibleSize), info);
				}
				var surface = stagingSurface;

				// The disposable-per-frame surface used to discard canvas state implicitly;
				// the cached surface must reset it explicitly or the scale would compound.
				var canvas = surface.Canvas;
				canvas.RestoreToCount(1);
				canvas.ResetMatrix();

				if (IgnorePixelScaling)
				{
					canvas.Scale(dpi);
					canvas.Save();
				}

				OnPaintSurface(stagingArgs);

				surface.Flush();
			}

			// Copy the staging buffer into the WriteableBitmap without a per-frame stream
			// wrapper. (The direct path above avoids this copy entirely by drawing into the
			// bitmap's own buffer.)
			copyStagingToBitmapAction ??= ptr => Marshal.Copy(pixels, 0, ptr, pixels.Length);
			global::Windows.Storage.Streams.Buffer.Cast(bitmap.PixelBuffer).ApplyActionOnRawBufferPtr(copyStagingToBitmapAction);
			bitmap.PixelBuffer.Length = (uint)pixels.Length;

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

			directInfo = info;
			directUserVisibleSize = userVisibleSize;
			directDpi = dpi;
			directPaintAction ??= DirectPaint;
			global::Windows.Storage.Streams.Buffer.Cast(bitmap.PixelBuffer).ApplyActionOnRawBufferPtr(directPaintAction);

			bitmap.PixelBuffer.Length = (uint)info.BytesSize;
			bitmap.Invalidate();
		}

		private void DirectPaint(IntPtr ptr)
		{
			// The cached surface is only trusted while the bitmap's raw buffer pointer and
			// format are unchanged — both are revalidated on every frame.
			if (directSurface == null || directSurfacePtr != ptr || !directSurfaceInfo.Equals(directInfo))
			{
				directSurface?.Dispose();
				directSurface = SKSurface.Create(directInfo, ptr, directInfo.RowBytes);
				directSurfacePtr = ptr;
				directSurfaceInfo = directInfo;
				directArgs = null;
			}

			if (directArgs == null || directArgs.Info.Size != directUserVisibleSize)
			{
				directArgs = new SKPaintSurfaceEventArgs(directSurface, directInfo.WithSize(directUserVisibleSize), directInfo);
			}

			// The disposable-per-frame surface used to discard canvas state implicitly; the
			// cached surface must reset it explicitly or the scale would compound.
			var canvas = directSurface.Canvas;
			canvas.RestoreToCount(1);
			canvas.ResetMatrix();

			if (IgnorePixelScaling)
			{
				canvas.Scale(directDpi);
				canvas.Save();
			}

			OnPaintSurface(directArgs);

			directSurface.Flush();
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
			stagingSurface?.Dispose();
			stagingSurface = null;
			stagingArgs = null;
			directSurface?.Dispose();
			directSurface = null;
			directSurfacePtr = IntPtr.Zero;
			directArgs = null;

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
