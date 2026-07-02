using CodeBrix.Platform.WinUI.Skia;
using CodeBrix.SkiaSvg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
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
/// its rendering element rather than subclassing <c>Image</c> (the CodeBrix.Platform
/// version subclasses <c>Image</c>, which is only possible under Uno).
/// </para>
/// <para>
/// SVGs are rendered vector-direct with CodeBrix.SkiaSvg (Skia) onto an
/// <see cref="SKXamlCanvas"/> at the final display resolution — the same engine
/// and drawing path the CodeBrix.Platform Skia heads use for SvgImageSource —
/// rather than the built-in <see cref="SvgImageSource"/>: the Direct2D SVG renderer
/// does not reliably honour CSS <c>&lt;style&gt;</c> class selectors, and an
/// intermediate rasterisation would not match the CodeBrix.Platform output
/// pixel-for-pixel.
/// </para>
/// </summary>
public sealed class EmbeddedImage : ContentControl
{
    private readonly Image _image = new() { Stretch = Stretch.Uniform };
    private SvgCanvasElement _svgCanvas;

    /// <summary>Initializes a new <see cref="EmbeddedImage"/>.</summary>
    public EmbeddedImage()
    {
        Content = _image;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    /// <summary>Identifies the <see cref="UriSource"/> dependency property.</summary>
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

    /// <summary>Identifies the <see cref="Stretch"/> dependency property.</summary>
    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch), typeof(Stretch), typeof(EmbeddedImage),
            new PropertyMetadata(Stretch.Uniform, OnStretchChanged));

    /// <summary>
    /// How the image is resized to fill its allocated space.
    /// Defaults to <see cref="Stretch.Uniform"/>.
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    private static void OnUriSourceChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
        => _ = LoadImageAsync((EmbeddedImage)d, e.NewValue as string);

    private static void OnStretchChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EmbeddedImage)d;
        control._image.Stretch = (Stretch)e.NewValue;
        control._svgCanvas?.InvalidateMeasure();
        control._svgCanvas?.Invalidate();
    }

    private static async Task LoadImageAsync(EmbeddedImage control, string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            control._image.Source = null;
            control.ClearSvg();
            control.Content = control._image;
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

                if (isSvg)
                {
                    await control.ShowSvgAsync(resourceStream);
                }
                else
                {
                    control.ShowBitmap(await CreateBitmapFromStreamAsync(resourceStream));
                }
            }
            else
            {
                // Standard URI. SVGs still render through Skia for fidelity;
                // everything else is a normal BitmapImage.
                if (isSvg)
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
                    await using var fileStream = await file.OpenStreamForReadAsync();
                    await control.ShowSvgAsync(fileStream);
                }
                else
                {
                    control.ShowBitmap(new BitmapImage { UriSource = new Uri(uri) });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EmbeddedImage] Failed to load from '{uri}': {ex.Message}");
        }
    }

    private void ShowBitmap(BitmapImage bitmap)
    {
        ClearSvg();
        _image.Source = bitmap;
        Content = _image;
    }

    /// <summary>
    /// Parses an SVG stream with CodeBrix.SkiaSvg (off the UI thread) and hosts a
    /// Skia canvas that draws the vector picture at the final display resolution.
    /// </summary>
    private async Task ShowSvgAsync(Stream svgStream)
    {
        // Copy to a buffer so the (heavier) parse can run off the UI thread.
        using var buffer = new MemoryStream();
        await svgStream.CopyToAsync(buffer);
        var svgBytes = buffer.ToArray();

        var svg = await Task.Run(() =>
        {
            var skSvg = new SKSvg();
            try
            {
                using var input = new MemoryStream(svgBytes);
                skSvg.Load(input);
                return skSvg;
            }
            catch
            {
                skSvg.Dispose();
                throw;
            }
        });

        if (svg.Picture is null)
        {
            svg.Dispose();
            throw new InvalidOperationException("Failed to parse SVG image.");
        }

        _image.Source = null;
        _svgCanvas ??= new SvgCanvasElement(this);
        _svgCanvas.SetSvg(svg);
        Content = _svgCanvas;
    }

    private void ClearSvg()
    {
        _svgCanvas?.SetSvg(null);
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

    /// <summary>
    /// The Skia render surface for SVG sources. Measures like an Image (natural size =
    /// the SVG's CullRect, adjusted by Stretch) and paints the vector picture scaled and
    /// centered in physical pixels, matching the CodeBrix.Platform SvgCanvas rendering.
    /// </summary>
    private sealed class SvgCanvasElement : SKXamlCanvas
    {
        private readonly EmbeddedImage _owner;
        private SKSvg _svg;

        public SvgCanvasElement(EmbeddedImage owner)
        {
            _owner = owner;
            PaintSurface += OnPaintSurfaceCore;
        }

        public void SetSvg(SKSvg svg)
        {
            if (ReferenceEquals(_svg, svg))
            {
                return;
            }

            _svg?.Dispose();
            _svg = svg;
            InvalidateMeasure();
            Invalidate();
        }

        private Size SourceSize
            => _svg?.Picture?.CullRect is { Width: > 0, Height: > 0 } rect
                ? new Size(rect.Width, rect.Height)
                : default;

        protected override Size MeasureOverride(Size availableSize)
        {
            var sourceSize = SourceSize;
            if (sourceSize == default)
            {
                return default;
            }

            return ImageSizeHelper.AdjustSize(_owner.Stretch, availableSize, sourceSize);
        }

        private void OnPaintSurfaceCore(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var picture = _svg?.Picture;
            var sourceSize = SourceSize;
            if (picture is null || sourceSize == default)
            {
                return;
            }

            // e.Info is in physical pixels, so the vector renders at full display
            // resolution — same as the CodeBrix.Platform Skia heads.
            var localSize = new Size(e.Info.Width, e.Info.Height);
            var (x, y) = ImageSizeHelper.BuildScale(_owner.Stretch, localSize, sourceSize);
            var scaledSize = new Size(sourceSize.Width * x, sourceSize.Height * y);

            canvas.Save();
            canvas.Translate(
                (float)((localSize.Width - scaledSize.Width) / 2),
                (float)((localSize.Height - scaledSize.Height) / 2));
            canvas.Scale((float)x, (float)y);
            canvas.DrawPicture(picture);
            canvas.Restore();
        }
    }
}
