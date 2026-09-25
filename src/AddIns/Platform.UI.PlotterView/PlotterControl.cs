#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.PlotterView.Engine;
using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Platform.UI.PlotterView.Internal;
using CodeBrix.Platform.UI.PlotterView.Rendering;
using CodeBrix.Plotter;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace CodeBrix.Platform.UI.PlotterView;

//New code written for this add-in against the CodeBrix.Plotter view contract (IPlotView +
//IPlotController); the interaction semantics - which handler feeds which controller method,
//what the tracker and zoom rectangle mean - follow the upstream OxyPlot view controls that
//CodeBrix.Plotter's controller was ported from, but no view code was ported. The hosting
//surface is the family's internal RenderCanvas (the AdvancedTextEdit/TerminalView pattern).
//WPE1 C7: the model attach, the rendering, the tracker/zoom/cursor state, the gesture trackers,
//the typefaces and the controller calls moved into the WinUI-free engine (Engine/PlotHost);
//this control wraps it and keeps only the XAML half (surface, input mapping, focus, cursor
//shapes, clipboard, the Model dependency property, the dispatcher).

/// <summary>
/// A chart view: hosts a CodeBrix.Plotter <see cref="PlotModel"/> on a Skia surface with the
/// full CodeBrix.Plotter interaction model wired in - pan (right-drag, arrow keys), zoom
/// (mouse wheel, +/- keys, middle-drag zoom rectangle), a data-point tracker (left-click),
/// reset (double-middle-click, A or Home), and touch (single-finger pan, two-finger pinch
/// zoom). Set <see cref="Model"/> and the control renders and re-renders it; after changing
/// the model's data from any thread, call <c>PlotModel.InvalidatePlot</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every piece of chart text renders through the application's own fonts - never the host
/// system's. Font family names in the model resolve as follows: an application font URI
/// (<c>ms-appx:///...</c>) loads that font; any bare family name, including the model
/// default, becomes the control's plot font, which is the application's default font unless
/// <see cref="PlotFontFamily"/> says otherwise.
/// </para>
/// <para>
/// Interaction is customizable through <see cref="Controller"/>: bind or unbind gestures on a
/// <see cref="PlotController"/> to change what the mouse, keyboard and touch do. Give the
/// control a bounded size (a Grid star cell is ideal) and keyboard focus lands on it when it
/// is clicked, which is what makes the key bindings work.
/// </para>
/// </remarks>
public sealed partial class PlotterControl : Control, IPlotView
{
    private readonly FrameworkElement _canvas;
    private readonly PlotHost _host;
    private readonly Dictionary<InputSystemCursorShape, InputSystemCursor> _cursors = new();

    /// <summary>Creates the control. Assign <see cref="Model"/> to show a plot.</summary>
    public PlotterControl()
    {
        IsTabStop = true;               //Required for key events

        //The engine runs font-load repaints on this control's UI thread (read when a load completes, from any thread)
        _host = new PlotHost(this, InvalidateCanvas, action => DispatcherQueue?.TryEnqueue(() => action()) ?? false);

        _canvas = RenderCanvasSupply.Create();
        RenderCanvasSupply.AddPaintHandler(_canvas, _host.Paint);
        _canvas.SizeChanged += (_, _) => InvalidatePlot(false);
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerEntered += OnCanvasPointerEntered;
        _canvas.PointerExited += OnCanvasPointerExited;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        _canvas.PointerCanceled += OnCanvasPointerLost;
        _canvas.PointerCaptureLost += OnCanvasPointerLost;

        Template = new ControlTemplate(CreateTemplateRoot);
    }

    /// <summary>Identifies the <see cref="Model"/> dependency property.</summary>
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model),
        typeof(PlotModel),
        typeof(PlotterControl),
        new PropertyMetadata(null, static (d, e) =>
            ((PlotterControl)d).OnModelChanged((PlotModel?)e.OldValue, (PlotModel?)e.NewValue)));

    /// <summary>
    /// The plot to show. The control attaches itself to the model (a model can be attached to
    /// only one view at a time), and the model's <c>InvalidatePlot</c> reaches this control
    /// from any thread.
    /// </summary>
    public PlotModel? Model
    {
        get => (PlotModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    /// <summary>
    /// The controller that maps input gestures onto plot commands. Null (the default) means a
    /// standard <see cref="PlotController"/> with the stock bindings; assign a customized
    /// controller to change them.
    /// </summary>
    public IPlotController? Controller
    {
        get => _host.Controller;
        set => _host.Controller = value;
    }

    /// <summary>
    /// The font family that plot text renders in when the model names no loadable application
    /// font: an application font URI such as
    /// <c>ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/Roboto.ttf</c>. Null (the default)
    /// means the application's default font.
    /// </summary>
    public string? PlotFontFamily
    {
        get => _host.PlotFontFamily;
        set
        {
            if (_host.PlotFontFamily == value)
            {
                return;
            }

            _host.PlotFontFamily = value;
            InvalidatePlot(false);
        }
    }

    /// <summary>The tracker box fill. Default: near-opaque dark gray.</summary>
    public PlotterColor TrackerBackground
    {
        get => _host.TrackerBackground;
        set { _host.TrackerBackground = value; InvalidateCanvas(); }
    }

    /// <summary>The tracker text color. Default: white.</summary>
    public PlotterColor TrackerForeground
    {
        get => _host.TrackerForeground;
        set { _host.TrackerForeground = value; InvalidateCanvas(); }
    }

    /// <summary>The tracker text size in DIPs. Default 12.</summary>
    public double TrackerFontSize
    {
        get => _host.TrackerFontSize;
        set { _host.TrackerFontSize = value; InvalidateCanvas(); }
    }

    /// <summary>The zoom rectangle fill. Default: translucent yellow.</summary>
    public PlotterColor ZoomRectangleFill
    {
        get => _host.ZoomRectangleFill;
        set { _host.ZoomRectangleFill = value; InvalidateCanvas(); }
    }

    /// <summary>The zoom rectangle border color. Default: black.</summary>
    public PlotterColor ZoomRectangleStroke
    {
        get => _host.ZoomRectangleStroke;
        set { _host.ZoomRectangleStroke = value; InvalidateCanvas(); }
    }

    /// <summary>The plot the control is showing (the <see cref="Model"/> property).</summary>
    public PlotModel? ActualModel => Model;

    /// <inheritdoc/>
    CodeBrix.Plotter.Model IView.ActualModel => Model!;

    /// <summary>
    /// The controller in effect: <see cref="Controller"/>, or the lazily created default
    /// <see cref="PlotController"/>.
    /// </summary>
    public IController ActualController => _host.ActualController;

    /// <inheritdoc/>
    public PlotterRect ClientArea => new PlotterRect(0, 0, _canvas.ActualWidth, _canvas.ActualHeight);

    /// <summary>
    /// Schedules a repaint, optionally re-reading the model's data first. Safe to call from
    /// any thread - this is the method <c>PlotModel.InvalidatePlot</c> reaches.
    /// </summary>
    /// <param name="updateData">Whether the data sources changed and must be re-read, not
    /// just the layout.</param>
    public void InvalidatePlot(bool updateData = true)
    {
        _host.MarkForUpdate(updateData);
        InvalidateCanvas();
    }

    /// <inheritdoc/>
    public void ShowTracker(TrackerHitResult trackerHitResult)
    {
        _host.ShowTracker(trackerHitResult);
        RenderCanvasSupply.Invalidate(_canvas);
    }

    /// <inheritdoc/>
    public void HideTracker()
    {
        if (!_host.HideTracker())
        {
            return;
        }

        RenderCanvasSupply.Invalidate(_canvas);
    }

    /// <inheritdoc/>
    public void ShowZoomRectangle(PlotterRect rectangle)
    {
        _host.ShowZoomRectangle(rectangle);
        RenderCanvasSupply.Invalidate(_canvas);
    }

    /// <inheritdoc/>
    public void HideZoomRectangle()
    {
        if (!_host.HideZoomRectangle())
        {
            return;
        }

        RenderCanvasSupply.Invalidate(_canvas);
    }

    /// <inheritdoc/>
    public void SetCursorType(CursorType cursorType)
    {
        if (!_host.SetCursorType(cursorType))
        {
            return;
        }

        if (CursorTypeMapper.ToCursorShape(cursorType) is { } shape)
        {
            if (!_cursors.TryGetValue(shape, out var cursor))
            {
                _cursors[shape] = cursor = InputSystemCursor.Create(shape);
            }

            ProtectedCursor = cursor;
        }
        else
        {
            ProtectedCursor = null;
        }
    }

    /// <inheritdoc/>
    public void SetClipboardText(string text)
    {
        var data = new DataPackage();
        data.SetText(text ?? string.Empty);
        try
        {
            //This framework's clipboard contract: SetContent, then Flush so the
            //content outlives the app (the TerminalView recipe).
            Clipboard.SetContent(data);
            Clipboard.Flush();
        }
        catch (Exception)
        {
            //The clipboard can be transiently unavailable; the copy simply doesn't take.
        }
    }

    private void OnModelChanged(PlotModel? oldModel, PlotModel? newModel)
    {
        //Detach/attach (to this view) and drop the tracker and zoom rectangle
        _host.SetModel(oldModel, newModel);
        InvalidatePlot(true);
    }

    private UIElement CreateTemplateRoot()
    {
        var root = new Grid
        {
            //No HitTestCore in this framework: a background brush is what makes empty
            //space hit-testable (the family recipe)
            Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
        };
        root.Children.Add(_canvas);
        return root;
    }

    //The canvas comes from the platform's canvas supply (Internal/RenderCanvasSupply.cs); the null check covers a
    //  property set before the constructor has created the canvas.
    private void InvalidateCanvas()
    {
        if (_canvas is not null)
        {
            RenderCanvasSupply.Invalidate(_canvas);
        }
    }

    private static bool IsTouch(PointerRoutedEventArgs e) =>
        e.Pointer.PointerDeviceType == PointerDeviceType.Touch;

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer); //key bindings (arrows, +/-, A) need the focus here

        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            var firstContact = _host.TouchTracker.Down(e.Pointer.PointerId, position);
            _canvas.CapturePointer(e.Pointer);
            if (firstContact)
            {
                _host.TouchStarted(position);
            }

            e.Handled = true;
            return;
        }

        var button = PointerButtonMapper.ToMouseButton(point.Properties.PointerUpdateKind);
        if (button == PlotterMouseButton.None)
        {
            return;
        }

        var tick = Environment.TickCount64;
        _canvas.CapturePointer(e.Pointer); //drag manipulators (pan, zoom rectangle) need it
        e.Handled = _host.MouseDown(button, position, tick);
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            if (_host.TouchMoved(e.Pointer.PointerId, position))
            {
                e.Handled = true;
            }

            return;
        }

        e.Handled = _host.MouseMove(position);
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            var lastContact = _host.TouchTracker.Up(e.Pointer.PointerId);
            _canvas.ReleasePointerCapture(e.Pointer);
            if (lastContact)
            {
                _host.TouchCompleted(position);
            }

            e.Handled = true;
            return;
        }

        _canvas.ReleasePointerCapture(e.Pointer);
        e.Handled = _host.MouseUp(position);
    }

    private void OnCanvasPointerLost(object sender, PointerRoutedEventArgs e)
    {
        //A canceled touch (or a capture torn away) must not leave a phantom contact behind:
        //drop it, and complete the gesture when it was the last one
        if (_host.TouchTracker.Count == 0 || !IsTouch(e))
        {
            return;
        }

        if (_host.TouchTracker.Up(e.Pointer.PointerId))
        {
            var point = e.GetCurrentPoint(_canvas);
            _host.TouchCompleted(new ScreenPoint(point.Position.X, point.Position.Y));
        }
    }

    private void OnCanvasPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        e.Handled = _host.MouseEnter(new ScreenPoint(point.Position.X, point.Position.Y));
    }

    private void OnCanvasPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        e.Handled = _host.MouseLeave(new ScreenPoint(point.Position.X, point.Position.Y));
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        e.Handled = _host.MouseWheel(new ScreenPoint(point.Position.X, point.Position.Y), point.Properties.MouseWheelDelta);
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);
        _host.UpdateModifier(ToModifierKey(e.Key), isDown: false);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (_host.UpdateModifier(ToModifierKey(e.Key), isDown: true))
        {
            return;
        }

        var key = VirtualKeyMapper.ToPlotterKey(e.Key);
        if (key == PlotterKey.Unknown)
        {
            return;
        }

        e.Handled = _host.KeyDown(key);
    }

    //The modifier a key is (either side of Shift/Control/Alt, either Windows key), or None.
    private static PlotterModifierKeys ToModifierKey(VirtualKey key)
    {
        switch (key)
        {
            case VirtualKey.Shift:
            case VirtualKey.LeftShift:
            case VirtualKey.RightShift:
                return PlotterModifierKeys.Shift;

            case VirtualKey.Control:
            case VirtualKey.LeftControl:
            case VirtualKey.RightControl:
                return PlotterModifierKeys.Control;

            case VirtualKey.Menu:
            case VirtualKey.LeftMenu:
            case VirtualKey.RightMenu:
                return PlotterModifierKeys.Alt;

            case VirtualKey.LeftWindows:
            case VirtualKey.RightWindows:
                return PlotterModifierKeys.Windows;

            default:
                return PlotterModifierKeys.None;
        }
    }
}
