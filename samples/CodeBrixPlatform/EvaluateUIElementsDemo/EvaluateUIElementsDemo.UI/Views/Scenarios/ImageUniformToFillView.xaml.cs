using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views.Scenarios;

/// <summary>
/// Scenario 6: one two-colour picture under each Stretch mode. The picture is drawn at run time
/// with SkiaSharp and encoded as a PNG, so what the page expects and what the picture holds cannot
/// drift apart; it is the same picture the frame review used.
/// </summary>
public sealed partial class ImageUniformToFillView : UserControl
{
    private const int PictureWidth = 40;
    private const int PictureHeight = 20;

    public ImageUniformToFillView()
    {
        InitializeComponent();

        var png = EncodeTwoColourPng(SKColors.Red, SKColors.Blue);
        UniformToFillImage.Source = Decode(png);
        UniformImage.Source = Decode(png);
        FillImage.Source = Decode(png);
        NoneImage.Source = Decode(png);

        PictureNote.Text = $"Picture encoded at run time: {PictureWidth} x {PictureHeight} PNG, {png.Length} bytes.";
    }

    private static BitmapImage Decode(byte[] png)
    {
        var picture = new BitmapImage();
        using (var stream = new MemoryStream(png, false))
        {
            picture.SetSource(stream);
        }

        return picture;
    }

    private static byte[] EncodeTwoColourPng(SKColor left, SKColor right)
    {
        using var bitmap = new SKBitmap(PictureWidth, PictureHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            using var leftPaint = new SKPaint { Color = left };
            using var rightPaint = new SKPaint { Color = right };
            canvas.DrawRect(SKRect.Create(0, 0, PictureWidth / 2, PictureHeight), leftPaint);
            canvas.DrawRect(SKRect.Create(PictureWidth / 2, 0, PictureWidth / 2, PictureHeight), rightPaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The two-colour picture could not be encoded as a PNG.");

        return encoded.ToArray();
    }
}
