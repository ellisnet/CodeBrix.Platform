using System;
using System.Diagnostics;
using CodeBrix.Imaging.Drawing;
using CodeBrix.Imaging.Drawing.Models;
using CodeBrix.Platform.Simple;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;

namespace EmulateFrameBufferDemo.Views;

/// <summary>
/// The sample's only page: a strip of buttons over three panes — OpenGL, a
/// WebView and a drawing surface — chosen so that one screen exercises the
/// three things a frame-buffer application is most likely to do.
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>The page the browser pane opens on, and returns to.</summary>
    const string HomeUrl = "https://en.wikipedia.org/wiki/Astarte";

    static readonly SKColor[] BrushColors =
    {
        new(0x20, 0x20, 0x20), // near-black
        new(0xD3, 0x2F, 0x2F), // red
        new(0x19, 0x76, 0xD2), // blue
        new(0x38, 0x8E, 0x3C), // green
        new(0xF5, 0x7C, 0x00), // orange
    };

    DrawingSession drawing;
    readonly DrawingLayer[] brushLayers = new DrawingLayer[BrushColors.Length];
    int brushColorIndex;

    public MainPage()
    {
        DataContextChanged += (_, _) =>
        {
            //Give the view model's SimpleDialog helpers a XamlRoot to attach dialogs to
            (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);
        };

        drawing = CreateSession(DrawingSessionOptions.DefaultCalibrationSize);
        Loaded += OnLoaded;

        this.InitializeComponent(); //Leave this line last
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigateHome();
        Sketch.SizeChanged += OnSketchSizeChanged;
        MatchCalibrationToPane();
    }

    /// <summary>
    /// Keeps the drawing's calibrated space the same shape as the pane. A press
    /// outside the calibrated area is ignored by design, so a square calibration
    /// in a tall pane would leave dead bands top and bottom. Only done while the
    /// drawing is empty — once there are strokes, changing calibration would
    /// move them.
    /// </summary>
    void OnSketchSizeChanged(object sender, SizeChangedEventArgs e) => MatchCalibrationToPane();

    void MatchCalibrationToPane()
    {
        var width = Sketch.ActualWidth;
        var height = Sketch.ActualHeight;
        if (width <= 0 || height <= 0 || drawing.HasStrokes)
            return;

        // Long side 1000, short side to match the pane's proportions.
        var calibration = width >= height
            ? new CodeBrix.Imaging.Size(1000, (int) Math.Max(1, Math.Round(1000 * height / width)))
            : new CodeBrix.Imaging.Size((int) Math.Max(1, Math.Round(1000 * width / height)), 1000);
        if (calibration.Width == drawing.CalibrationSize.Width
            && calibration.Height == drawing.CalibrationSize.Height)
        {
            return;
        }

        drawing.Dispose();
        drawing = CreateSession(calibration);
        Sketch.Invalidate();
    }

    DrawingSession CreateSession(CodeBrix.Imaging.Size calibrationSize)
    {
        var session = new DrawingSession(new DrawingSessionOptions
        {
            CalibrationSize = calibrationSize,
            StrokeWidth = 6f,
            // The defaults (100 / 200 of 255) are tuned for annotating a
            // photo; here the strokes are the content, so draw them solid.
            LayerOpacity = 255,
            ActiveStrokeOpacity = 255,
        });
        // Colour lives on the layer, not the stroke, so each colour gets its own
        // layer — otherwise recolouring would repaint everything drawn so far.
        // A session starts with no layers; the first one added becomes active.
        for (var i = 0; i < BrushColors.Length; i++)
            brushLayers[i] = session.AddLayer($"Brush {i + 1}", BrushColors[i]);
        session.ActiveLayer = brushLayers[brushColorIndex];
        // The session redraws itself as strokes grow; the canvas just repaints.
        session.RedrawRequested += (_, _) => Sketch?.Invalidate();
        return session;
    }

    #region | Button handlers |

    void OnToggleSpin(object sender, RoutedEventArgs e) => ModelView.ToggleSpin();

    void OnResetModel(object sender, RoutedEventArgs e) => ModelView.ResetView();

    void OnBrowserHome(object sender, RoutedEventArgs e) => NavigateHome();

    void OnClearDrawing(object sender, RoutedEventArgs e)
    {
        drawing.Clear();
        Sketch.Invalidate();
    }

    void OnUndoStroke(object sender, RoutedEventArgs e)
    {
        drawing.UndoLastStroke();
        Sketch.Invalidate();
    }

    void OnCycleBrushColor(object sender, RoutedEventArgs e)
    {
        brushColorIndex = (brushColorIndex + 1) % BrushColors.Length;
        var color = BrushColors[brushColorIndex];
        drawing.ActiveLayer = brushLayers[brushColorIndex];
        // The button wears the colour it will draw in.
        ColorButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0xFF, color.Red, color.Green, color.Blue));
    }

    void NavigateHome()
    {
        try
        {
            Browser.Source = new Uri(HomeUrl);
        }
        catch (Exception ex)
        {
            // A head without a working WebView must not take the page down.
            Debug.WriteLine($"WebView navigation failed: {ex.Message}");
        }
    }

    #endregion

    #region | Drawing pane |

    void OnSketchPaintSurface(object sender, SKPaintSurfaceEventArgs e) =>
        drawing.Render(e.Surface, e.Info);

    void OnSketchPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Sketch).Position;
        if (drawing.PointerPressed(new SKPoint((float) point.X, (float) point.Y), ViewSize))
        {
            Sketch.CapturePointer(e.Pointer);
            Sketch.Invalidate();
        }
        e.Handled = true;
    }

    void OnSketchPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!drawing.IsPointerActive)
            return;
        var point = e.GetCurrentPoint(Sketch).Position;
        if (drawing.PointerMoved(new SKPoint((float) point.X, (float) point.Y), ViewSize))
            Sketch.Invalidate();
        e.Handled = true;
    }

    void OnSketchPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (drawing.PointerReleased())
            Sketch.Invalidate();
        Sketch.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    void OnSketchPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        drawing.PointerCanceled();
        Sketch.Invalidate();
    }

    SKSize ViewSize => new((float) Sketch.ActualWidth, (float) Sketch.ActualHeight);

    #endregion
}
