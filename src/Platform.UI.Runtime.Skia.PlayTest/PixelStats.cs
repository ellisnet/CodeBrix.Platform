using System;
using System.Collections.Generic;
using SkiaSharp;
using Windows.UI;

namespace CodeBrix.Platform.PlayTest;

/// <summary>One half of a <see cref="PixelStats"/> region, for <see cref="PixelStats.Half"/>.</summary>
public enum PixelHalf
{
    /// <summary>The left half (the middle column, in an odd width, belongs to neither half).</summary>
    Left,
    /// <summary>The right half.</summary>
    Right,
    /// <summary>The top half (the middle row, in an odd height, belongs to neither half).</summary>
    Top,
    /// <summary>The bottom half.</summary>
    Bottom,
}

/// <summary>The mirror axis for <see cref="PixelStats.MirrorSymmetry"/>.</summary>
public enum PixelAxis
{
    /// <summary>A vertical axis through the middle: left mirrors right.</summary>
    Vertical,
    /// <summary>A horizontal axis through the middle: top mirrors bottom.</summary>
    Horizontal,
}

/// <summary>A pixel rectangle inside a <see cref="PixelStats"/> region, in whole pixels from its top-left corner.</summary>
/// <param name="X">The left column.</param>
/// <param name="Y">The top row.</param>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
public readonly record struct PixelBounds(int X, int Y, int Width, int Height);

/// <summary>How two captures of the same size differ (<see cref="PixelStats.Difference"/>).</summary>
/// <param name="ChangedFraction">The fraction of pixels (0 to 1) whose largest channel change exceeds the tolerance.</param>
/// <param name="MaxChannelDelta">The largest change of any channel of any pixel (0 to 255).</param>
public readonly record struct PixelDifference(double ChangedFraction, int MaxChannelDelta);

/// <summary>
/// Measurements over pixels captured in the running test - a <see cref="Locator.ScreenshotAsync"/> or
/// <see cref="Page.ScreenshotAsync"/> result - for content that has no visual tree to assert on, such as an
/// OpenGL canvas. It reads only the bytes it is given: there are no files, no saved images and no baselines.
/// Colours are compared per channel (red, green, blue, alpha), unpremultiplied; a tolerance is the largest
/// allowed absolute difference of any one channel. Luminance is Rec. 709 (0 to 255).
/// </summary>
public sealed class PixelStats
{
    // BGRA8888, unpremultiplied, rows top to bottom.
    private readonly byte[] _pixels;

    private PixelStats(byte[] pixels, int width, int height)
    {
        _pixels = pixels;
        Width = width;
        Height = height;
    }

    /// <summary>The region's width in pixels.</summary>
    public int Width { get; }

    /// <summary>The region's height in pixels.</summary>
    public int Height { get; }

    /// <summary>Measures a PNG captured in this test (the bytes a screenshot call returned).</summary>
    /// <param name="png">The PNG bytes.</param>
    /// <returns>The measurements.</returns>
    /// <exception cref="ArgumentException">The bytes are not a PNG image.</exception>
    public static PixelStats FromPng(byte[] png)
    {
        ArgumentNullException.ThrowIfNull(png);
        using var image = SKImage.FromEncodedData(png) ?? throw new ArgumentException("The bytes are not a PNG image.", nameof(png));
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var pixels = new byte[info.BytesSize];
        unsafe
        {
            fixed (byte* pointer = pixels)
                if (!image.ReadPixels(info, (nint)pointer, info.RowBytes, 0, 0))
                    throw new ArgumentException("The PNG image could not be decoded.", nameof(png));
        }
        return new PixelStats(pixels, image.Width, image.Height);
    }

    // Unpremultiplied BGRA8888 rows; the head's tests build synthetic regions with it.
    internal static PixelStats FromBgra(byte[] pixels, int width, int height)
    {
        if (width < 0 || height < 0 || pixels.Length != width * height * 4) throw new ArgumentException("The pixel buffer does not match the size.");
        return new PixelStats(pixels, width, height);
    }

    /// <summary>The colour of one pixel.</summary>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <returns>The unpremultiplied colour.</returns>
    public Color GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(x < 0 || x >= Width ? nameof(x) : nameof(y));
        var i = (y * Width + x) * 4;
        return Color.FromArgb(_pixels[i + 3], _pixels[i + 2], _pixels[i + 1], _pixels[i]);
    }

    /// <summary>A rectangle of this region, measured on its own.</summary>
    /// <param name="x">The left column.</param>
    /// <param name="y">The top row.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The measurements of the rectangle.</returns>
    public PixelStats Region(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width < 0 || height < 0 || x + width > Width || y + height > Height)
            throw new ArgumentOutOfRangeException(nameof(width), "The rectangle must lie inside the region.");
        var pixels = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
            Buffer.BlockCopy(_pixels, ((y + row) * Width + x) * 4, pixels, row * width * 4, width * 4);
        return new PixelStats(pixels, width, height);
    }

    /// <summary>One half of this region, measured on its own - for example to compare the mean luminance of
    /// the left and right halves of a lit object.</summary>
    /// <param name="half">Which half.</param>
    /// <returns>The measurements of that half.</returns>
    public PixelStats Half(PixelHalf half) => half switch
    {
        PixelHalf.Left => Region(0, 0, Width / 2, Height),
        PixelHalf.Right => Region(Width - Width / 2, 0, Width / 2, Height),
        PixelHalf.Top => Region(0, 0, Width, Height / 2),
        PixelHalf.Bottom => Region(0, Height - Height / 2, Width, Height / 2),
        _ => throw new ArgumentOutOfRangeException(nameof(half)),
    };

    /// <summary>The fraction of pixels (0 to 1) within <paramref name="tolerance"/> of <paramref name="color"/>.</summary>
    /// <param name="color">The colour to count.</param>
    /// <param name="tolerance">The largest allowed difference of any channel (0 to 255).</param>
    /// <returns>The covered fraction; 0 for an empty region.</returns>
    public double Coverage(Color color, int tolerance = 8)
    {
        var count = 0;
        for (var i = 0; i < _pixels.Length; i += 4)
            if (Near(i, color, tolerance)) count++;
        return PixelCount == 0 ? 0 : (double)count / PixelCount;
    }

    /// <summary>The number of different colours in the region.</summary>
    public int DistinctColorCount
    {
        get
        {
            var colors = new HashSet<uint>();
            for (var i = 0; i < _pixels.Length; i += 4) colors.Add(BitConverter.ToUInt32(_pixels, i));
            return colors.Count;
        }
    }

    /// <summary>True when every pixel has the same colour - for example an all-black or all-magenta canvas.</summary>
    public bool IsUniform => DistinctColorCount <= 1;

    /// <summary>True when every pixel is fully transparent or opaque black: what an OpenGL canvas shows when
    /// nothing was drawn or read back.</summary>
    public bool IsBlank
    {
        get
        {
            for (var i = 0; i < _pixels.Length; i += 4)
            {
                var transparent = _pixels[i + 3] == 0;
                var black = _pixels[i] == 0 && _pixels[i + 1] == 0 && _pixels[i + 2] == 0;
                if (!transparent && !black) return false;
            }
            return true;
        }
    }

    /// <summary>The smallest rectangle holding every pixel that differs from <paramref name="background"/> by
    /// more than <paramref name="tolerance"/>.</summary>
    /// <param name="background">The background colour.</param>
    /// <param name="tolerance">The largest difference of any channel still counted as background.</param>
    /// <returns>The bounds, or null when every pixel is background.</returns>
    public PixelBounds? Bounds(Color background, int tolerance = 8)
    {
        int left = Width, top = Height, right = -1, bottom = -1;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                if (Near((y * Width + x) * 4, background, tolerance)) continue;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        return right < 0 ? null : new PixelBounds(left, top, right - left + 1, bottom - top + 1);
    }

    /// <summary>The mean position of every pixel that differs from <paramref name="background"/> by more than
    /// <paramref name="tolerance"/>, in pixels from the region's top-left corner (pixel centres at +0.5).</summary>
    /// <param name="background">The background colour.</param>
    /// <param name="tolerance">The largest difference of any channel still counted as background.</param>
    /// <returns>The centroid, or null when every pixel is background.</returns>
    public (double X, double Y)? Centroid(Color background, int tolerance = 8)
    {
        double sumX = 0, sumY = 0;
        long count = 0;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                if (Near((y * Width + x) * 4, background, tolerance)) continue;
                sumX += x + 0.5;
                sumY += y + 0.5;
                count++;
            }
        return count == 0 ? null : (sumX / count, sumY / count);
    }

    /// <summary>The mean Rec. 709 luminance (0 to 255). With <paramref name="background"/> set, only the pixels
    /// that differ from it by more than <paramref name="tolerance"/> are averaged - the object, not its backdrop.</summary>
    /// <param name="background">The background colour to leave out, or null to average every pixel.</param>
    /// <param name="tolerance">The largest difference of any channel still counted as background.</param>
    /// <returns>The mean luminance, or NaN when no pixel is averaged.</returns>
    public double MeanLuminance(Color? background = null, int tolerance = 8)
    {
        double sum = 0;
        long count = 0;
        for (var i = 0; i < _pixels.Length; i += 4)
        {
            if (background is { } b && Near(i, b, tolerance)) continue;
            sum += Luminance(i);
            count++;
        }
        return count == 0 ? double.NaN : sum / count;
    }

    /// <summary>The variance of the Rec. 709 luminance over every pixel: zero for a flat fill, larger for
    /// texture, shading and edges.</summary>
    /// <returns>The variance, or NaN for an empty region.</returns>
    public double LuminanceVariance()
    {
        if (PixelCount == 0) return double.NaN;
        var mean = MeanLuminance();
        double sum = 0;
        for (var i = 0; i < _pixels.Length; i += 4)
        {
            var d = Luminance(i) - mean;
            sum += d * d;
        }
        return sum / PixelCount;
    }

    /// <summary>How mirror-symmetric the region is: the fraction of pixels (0 to 1) within
    /// <paramref name="tolerance"/> of their mirror image across the middle.</summary>
    /// <param name="axis">The mirror axis.</param>
    /// <param name="tolerance">The largest allowed difference of any channel.</param>
    /// <returns>1 for a perfect mirror image; 0 for an empty region.</returns>
    public double MirrorSymmetry(PixelAxis axis, int tolerance = 8)
    {
        if (!Enum.IsDefined(axis)) throw new ArgumentOutOfRangeException(nameof(axis));
        var matching = 0;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var mirror = axis == PixelAxis.Vertical ? (y * Width + (Width - 1 - x)) * 4 : ((Height - 1 - y) * Width + x) * 4;
                if (MaxDelta(_pixels, (y * Width + x) * 4, _pixels, mirror) <= tolerance) matching++;
            }
        return PixelCount == 0 ? 0 : (double)matching / PixelCount;
    }

    /// <summary>Compares this capture with another of the same size taken in the same test - for example
    /// before and after an input that should change the picture.</summary>
    /// <param name="other">The other capture.</param>
    /// <param name="tolerance">The largest channel change not counted as a change.</param>
    /// <returns>The changed fraction and the largest channel change.</returns>
    /// <exception cref="ArgumentException">The captures differ in size.</exception>
    public PixelDifference Difference(PixelStats other, int tolerance = 0)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Width != Width || other.Height != Height)
            throw new ArgumentException($"The captures differ in size ({Width}x{Height} and {other.Width}x{other.Height}).", nameof(other));
        var changed = 0;
        var max = 0;
        for (var i = 0; i < _pixels.Length; i += 4)
        {
            var delta = MaxDelta(_pixels, i, other._pixels, i);
            if (delta > tolerance) changed++;
            max = Math.Max(max, delta);
        }
        return new PixelDifference(PixelCount == 0 ? 0 : (double)changed / PixelCount, max);
    }

    /// <summary>Describes the region for assertion messages.</summary>
    /// <returns>The size, distinct-colour count and mean luminance.</returns>
    public override string ToString() => $"{Width}x{Height}, {DistinctColorCount} colours, mean luminance {MeanLuminance():0.0}";

    private int PixelCount => Width * Height;

    private bool Near(int i, Color color, int tolerance) =>
        Math.Abs(_pixels[i] - color.B) <= tolerance
        && Math.Abs(_pixels[i + 1] - color.G) <= tolerance
        && Math.Abs(_pixels[i + 2] - color.R) <= tolerance
        && Math.Abs(_pixels[i + 3] - color.A) <= tolerance;

    private double Luminance(int i) => 0.2126 * _pixels[i + 2] + 0.7152 * _pixels[i + 1] + 0.0722 * _pixels[i];

    private static int MaxDelta(byte[] a, int i, byte[] b, int j) => Math.Max(
        Math.Max(Math.Abs(a[i] - b[j]), Math.Abs(a[i + 1] - b[j + 1])),
        Math.Max(Math.Abs(a[i + 2] - b[j + 2]), Math.Abs(a[i + 3] - b[j + 3])));
}
