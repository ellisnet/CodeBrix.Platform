using CodeBrix.SkiaSvg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

#pragma warning disable IDE0130

namespace CodeBrix.Platform.WinUI.Controls;

/// <summary>
/// A control that displays an image loaded from an embedded resource via the
/// embedded://AssemblyName/ResourceName URI scheme, as well as standard URIs
/// (ms-appx:///, https://). Supports PNG, JPEG, BMP, GIF and other image formats,
/// plus SVG.
/// <para>
/// In native WinUI 3 <see cref="Image"/> is <c>sealed</c>, so this control hosts
/// an internal <see cref="Image"/> rather than subclassing it (the CodeBrix.Platform
/// version subclasses <c>Image</c>, which is only possible under Uno).
/// </para>
/// <para>
/// Embedded SVGs are rasterised with CodeBrix.SkiaSvg (Skia) rather than the
/// built-in <see cref="SvgImageSource"/>: the Direct2D SVG renderer does not
/// reliably honour the CSS <c>&lt;style&gt;</c> class selectors these icons use.
/// Rasterising via Skia matches how the CodeBrix.Platform Skia heads render them.
/// </para>
/// </summary>
public sealed class EmbeddedImage : ContentControl
{
    // Pixel size of the longest edge of the rasterised SVG. Rendered large and
    // downscaled by the inner Image (Stretch=Uniform) so icons stay crisp at any
    // display size / DPI.
    private const float SvgRasterTargetPixels = 256f;

    private readonly Image _image = new() { Stretch = Stretch.Uniform };

    public EmbeddedImage()
    {
        Content = _image;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    public static readonly DependencyProperty UriSourceProperty =
        DependencyProperty.Register(
            nameof(UriSource), typeof(string), typeof(EmbeddedImage),
            new PropertyMetadata(null, OnUriSourceChanged));

    /// <summary>
    /// The URI of the image source. Supports embedded://AssemblyName/ResourceName
    /// for embedded resources, or standard URIs (ms-appx:///, https://).
    /// </summary>
    public string UriSource
    {
        get => (string)GetValue(UriSourceProperty);
        set => SetValue(UriSourceProperty, value);
    }

    private static void OnUriSourceChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
        => _ = LoadImageAsync((EmbeddedImage)d, e.NewValue as string);

    private static async Task LoadImageAsync(EmbeddedImage control, string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            control._image.Source = null;
            return;
        }

        try
        {
            var isSvg = uri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

            if (uri.StartsWith("embedded://", StringComparison.OrdinalIgnoreCase))
            {
                // Parse: embedded://AssemblyName/Fully.Qualified.Resource.Name
                var path = uri["embedded://".Length..];
                var separatorIndex = path.IndexOf('/');
                if (separatorIndex < 0)
                    throw new ArgumentException(
                        $"Invalid embedded resource URI: {uri}. "
                        + "Expected: embedded://AssemblyName/Resource.Name");

                var assemblyName = path[..separatorIndex];
                var resourceName = path[(separatorIndex + 1)..];

                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName)
                    ?? throw new InvalidOperationException(
                        $"Assembly '{assemblyName}' is not loaded.");

                await using var resourceStream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException(
                        $"Resource '{resourceName}' not found in '{assemblyName}'.");

                control._image.Source = isSvg
                    ? await RasterizeSvgAsync(resourceStream)
                    : await CreateBitmapFromStreamAsync(resourceStream);
            }
            else
            {
                // Standard URI. SVGs still rasterise through Skia for fidelity;
                // everything else is a normal BitmapImage.
                if (isSvg)
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
                    await using var fileStream = await file.OpenStreamForReadAsync();
                    control._image.Source = await RasterizeSvgAsync(fileStream);
                }
                else
                {
                    control._image.Source = new BitmapImage { UriSource = new Uri(uri) };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EmbeddedImage] Failed to load from '{uri}': {ex.Message}");
        }
    }

    /// <summary>
    /// Rasterises an SVG stream to a transparent PNG via CodeBrix.SkiaSvg and
    /// returns it as a <see cref="BitmapImage"/>.
    /// </summary>
    private static async Task<BitmapImage> RasterizeSvgAsync(Stream svgStream)
    {
        // Copy to a buffer so the (heavier) parse + render can run off the UI thread.
        using var buffer = new MemoryStream();
        await svgStream.CopyToAsync(buffer);
        var svgBytes = buffer.ToArray();

        var pngBytes = await Task.Run(() =>
        {
            using var input = new MemoryStream(svgBytes);
            using var svg = SKSvg.CreateFromStream(input);
            var picture = svg.Picture;
            if (picture is null) return null;

            var bounds = picture.CullRect;
            var longestEdge = Math.Max(bounds.Width, bounds.Height);
            var scale = longestEdge > 0 ? SvgRasterTargetPixels / longestEdge : 1f;

            using var output = new MemoryStream();
            svg.Save(output, SKColors.Transparent, SKEncodedImageFormat.Png, 100, scale, scale);
            return output.ToArray();
        });

        if (pngBytes is not { Length: > 0 }) return null;

        var ras = new InMemoryRandomAccessStream();
        var writeStream = ras.AsStreamForWrite();
        await writeStream.WriteAsync(pngBytes, 0, pngBytes.Length);
        await writeStream.FlushAsync();
        ras.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(ras);
        return bitmap;
    }

    private static async Task<BitmapImage> CreateBitmapFromStreamAsync(Stream stream)
    {
        var ras = new InMemoryRandomAccessStream();
        var writeStream = ras.AsStreamForWrite();
        await stream.CopyToAsync(writeStream);
        await writeStream.FlushAsync();
        ras.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(ras);
        return bitmap;
    }
}
