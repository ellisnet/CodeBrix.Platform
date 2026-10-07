using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.PlayTest;

// Cropping, PNG encoding and stable capture behind the screenshot APIs.
internal static class Screenshots
{
    // The renderer's premultiplied BGRA pixels as a PNG.
    internal static byte[] Encode(byte[] pixels, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    // Whole device pixels covering the logical bounds, clipped to the virtual screen.
    internal static (int X, int Y, int Width, int Height) PixelRegion(Rect bounds, int screenWidth, int screenHeight)
    {
        var left = Math.Clamp((int)Math.Floor(bounds.X), 0, screenWidth);
        var top = Math.Clamp((int)Math.Floor(bounds.Y), 0, screenHeight);
        var right = Math.Clamp((int)Math.Ceiling(bounds.X + bounds.Width), 0, screenWidth);
        var bottom = Math.Clamp((int)Math.Ceiling(bounds.Y + bounds.Height), 0, screenHeight);
        return (left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    internal static byte[] Crop(VirtualFrame frame, (int X, int Y, int Width, int Height) region)
    {
        if (region.X == 0 && region.Y == 0 && region.Width == frame.Width && region.Height == frame.Height) return frame.Pixels;
        var pixels = new byte[region.Width * region.Height * 4];
        for (var row = 0; row < region.Height; row++)
            Buffer.BlockCopy(frame.Pixels, ((region.Y + row) * frame.Width + region.X) * 4, pixels, row * region.Width * 4, region.Width * 4);
        return pixels;
    }

    // Captures the region (null: the whole screen; the probe runs on the UI thread after each frame).
    // Stable: repeat until two consecutive captures are identical, or fail at the deadline.
    internal static async Task<byte[]> CaptureAsync(PlayTestApplication app, Func<Rect?>? region, bool stable, Stopwatch elapsed, float timeout, string description)
    {
        byte[]? previous = null;
        (int X, int Y, int Width, int Height) previousRegion = default;
        while (true)
        {
            var frame = await app.Host.CaptureAsync().ConfigureAwait(false);
            var pixelRegion = (0, 0, frame.Width, frame.Height);
            if (region != null)
            {
                var bounds = await app.Host.OnUI(region).ConfigureAwait(false)
                    ?? throw await app.FailureAsync($"{description} is no longer visible.").ConfigureAwait(false);
                pixelRegion = PixelRegion(bounds, frame.Width, frame.Height);
                if (pixelRegion.Item3 == 0 || pixelRegion.Item4 == 0)
                    throw await app.FailureAsync($"{description} is outside the virtual screen.").ConfigureAwait(false);
            }
            var pixels = Crop(frame, pixelRegion);
            if (!stable || (previous != null && previousRegion == pixelRegion && previous.AsSpan().SequenceEqual(pixels)))
                return Encode(pixels, pixelRegion.Item3, pixelRegion.Item4);
            previous = pixels;
            previousRegion = pixelRegion;
            if (elapsed.Elapsed.TotalMilliseconds >= timeout)
                throw await app.FailureAsync($"Timeout {timeout}ms: {description} kept changing; two consecutive captures never matched.").ConfigureAwait(false);
        }
    }

    internal static async Task WriteAsync(string path, byte[] png)
    {
        var full = Path.GetFullPath(path);
        if (Path.GetDirectoryName(full) is { Length: > 0 } directory) Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(full, png).ConfigureAwait(false);
    }
}
