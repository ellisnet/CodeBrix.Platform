#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Platform.UI.PlotterView.Internal;
using CodeBrix.Platform.UI.PlotterView.Rendering;
using CodeBrix.Plotter;
using CodeBrix.Plotter.Skia;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using PlotterHorizontalAlignment = CodeBrix.Plotter.HorizontalAlignment; //Required: 'HorizontalAlignment' alone is
using PlotterVerticalAlignment = CodeBrix.Plotter.VerticalAlignment;     //  ambiguous with the XAML layout enums

namespace CodeBrix.Platform.UI.PlotterView;

//New code written for this add-in against the CodeBrix.Plotter view contract (IPlotView +
//IPlotController); the interaction semantics - which handler feeds which controller method,
//what the tracker and zoom rectangle mean - follow the upstream OxyPlot view controls that
//CodeBrix.Plotter's controller was ported from, but no view code was ported. The hosting
//surface is the family's internal RenderCanvas (the AdvancedTextEdit/TerminalView pattern).

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
    private readonly RenderCanvas _canvas;
    private readonly AppFontTypefaceResolver _fontResolver;
    private readonly ClickCounter _clickCounter = new();
    private readonly TouchGestureTracker _touchTracker = new();
    private readonly Dictionary<InputSystemCursorShape, InputSystemCursor> _cursors = new();
    private readonly object _invalidateLock = new();

    private SkiaRenderContext? _renderContext;
    private IPlotController? _defaultController;
    private TrackerHitResult? _tracker;
    private PlotterRect? _zoomRectangle;
    private bool _updateRequired;
    private bool _updateDataRequired;
    private CursorType _cursorType = CursorType.Default;
    private bool _shiftDown;
    private bool _controlDown;
    private bool _altDown;
    private bool _windowsDown;

    /// <summary>Creates the control. Assign <see cref="Model"/> to show a plot.</summary>
    public PlotterControl()
    {
        IsTabStop = true;               //Required for key events

        _fontResolver = new AppFontTypefaceResolver();
        _fontResolver.FontLoaded += OnFontLoaded;

        _canvas = new RenderCanvas();
        _canvas.Paint += OnPaint;
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
    public IPlotController? Controller { get; set; }

    /// <summary>
    /// The font family that plot text renders in when the model names no loadable application
    /// font: an application font URI such as
    /// <c>ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/Roboto.ttf</c>. Null (the default)
    /// means the application's default font.
    /// </summary>
    public string? PlotFontFamily
    {
        get => _fontResolver.PlotFontFamily;
        set
        {
            if (_fontResolver.PlotFontFamily == value)
            {
                return;
            }

            _fontResolver.PlotFontFamily = value;
            ResetFontResolution();
            InvalidatePlot(false);
        }
    }

    private PlotterColor _trackerBackground = PlotterColor.FromArgb(0xE6, 0x2D, 0x2D, 0x30);

    /// <summary>The tracker box fill. Default: near-opaque dark gray.</summary>
    public PlotterColor TrackerBackground
    {
        get => _trackerBackground;
        set { _trackerBackground = value; _canvas?.Invalidate(); }
    }

    private PlotterColor _trackerForeground = PlotterColors.White;

    /// <summary>The tracker text color. Default: white.</summary>
    public PlotterColor TrackerForeground
    {
        get => _trackerForeground;
        set { _trackerForeground = value; _canvas?.Invalidate(); }
    }

    private double _trackerFontSize = 12;

    /// <summary>The tracker text size in DIPs. Default 12.</summary>
    public double TrackerFontSize
    {
        get => _trackerFontSize;
        set { _trackerFontSize = value > 4 ? value : 4; _canvas?.Invalidate(); }
    }

    private PlotterColor _zoomRectangleFill = PlotterColor.FromArgb(0x40, 0xFF, 0xFF, 0x00);

    /// <summary>The zoom rectangle fill. Default: translucent yellow.</summary>
    public PlotterColor ZoomRectangleFill
    {
        get => _zoomRectangleFill;
        set { _zoomRectangleFill = value; _canvas?.Invalidate(); }
    }

    private PlotterColor _zoomRectangleStroke = PlotterColors.Black;

    /// <summary>The zoom rectangle border color. Default: black.</summary>
    public PlotterColor ZoomRectangleStroke
    {
        get => _zoomRectangleStroke;
        set { _zoomRectangleStroke = value; _canvas?.Invalidate(); }
    }

    /// <summary>The plot the control is showing (the <see cref="Model"/> property).</summary>
    public PlotModel? ActualModel => Model;

    /// <inheritdoc/>
    CodeBrix.Plotter.Model IView.ActualModel => Model!;

    /// <summary>
    /// The controller in effect: <see cref="Controller"/>, or the lazily created default
    /// <see cref="PlotController"/>.
    /// </summary>
    public IController ActualController => Controller ?? (_defaultController ??= new PlotController());

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
        lock (_invalidateLock)
        {
            _updateRequired = true;
            _updateDataRequired |= updateData;
        }

        _canvas?.Invalidate();
    }

    /// <inheritdoc/>
    public void ShowTracker(TrackerHitResult trackerHitResult)
    {
        _tracker = trackerHitResult;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    public void HideTracker()
    {
        if (_tracker == null)
        {
            return;
        }

        _tracker = null;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    public void ShowZoomRectangle(PlotterRect rectangle)
    {
        _zoomRectangle = rectangle;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    public void HideZoomRectangle()
    {
        if (_zoomRectangle == null)
        {
            return;
        }

        _zoomRectangle = null;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    public void SetCursorType(CursorType cursorType)
    {
        if (cursorType == _cursorType)
        {
            return;
        }

        _cursorType = cursorType;
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
        if (oldModel != null)
        {
            ((IPlotModel)oldModel).AttachPlotView(null);
        }

        if (newModel != null)
        {
            ((IPlotModel)newModel).AttachPlotView(this);
        }

        _tracker = null;
        _zoomRectangle = null;
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

    private SkiaRenderContext EnsureRenderContext()
    {
        if (_renderContext == null)
        {
            _renderContext = new SkiaRenderContext
            {
                RenderTarget = RenderTarget.Screen,
                UseTextShaping = true,
                MiterLimit = 10,
                //The RenderCanvas pre-scales its canvas so one unit is one DIP; the display
                //scale must not be applied a second time here
                DpiScale = 1f,
            };
            _renderContext.TypefaceResolver = _fontResolver.Resolve;
        }

        return _renderContext;
    }

    private void ResetFontResolution()
    {
        _fontResolver.Reset();
        if (_renderContext != null)
        {
            //A method-group conversion creates a new delegate instance, so this assignment
            //always differs from the resolver in place and clears the context's typeface
            //cache - which is how interim typefaces from still-loading fonts get evicted
            _renderContext.TypefaceResolver = _fontResolver.Resolve;
        }
    }

    private void OnFontLoaded()
    {
        //Raised by the resolver on an arbitrary thread when an async font load completes
        var queue = DispatcherQueue;
        queue?.TryEnqueue(() =>
        {
            ResetFontResolution();
            InvalidatePlot(false);
        });
    }

    private void OnPaint(SKCanvas canvas, SKSize size)
    {
        bool update;
        bool updateData;
        lock (_invalidateLock)
        {
            update = _updateRequired;
            updateData = _updateDataRequired;
            _updateRequired = false;
            _updateDataRequired = false;
        }

        var model = Model;
        if (model == null)
        {
            return; //the RenderCanvas has already cleared the surface transparent
        }

        var context = EnsureRenderContext();
        context.SkCanvas = canvas;
        var clientRect = new PlotterRect(0, 0, size.Width, size.Height);

        //The model may be mutated from other threads between paints (the documented pattern
        //is mutate-then-InvalidatePlot); SyncRoot is the model's own lock for exactly this
        lock (model.SyncRoot)
        {
            var plotModel = (IPlotModel)model;
            if (update)
            {
                plotModel.Update(updateData);
            }

            if (model.Background.IsVisible())
            {
                canvas.Clear(ToSKColor(model.Background));
            }

            plotModel.Render(context, clientRect);
        }

        if (_zoomRectangle is { } zoomRectangle)
        {
            context.DrawRectangle(zoomRectangle, ZoomRectangleFill, ZoomRectangleStroke, 1,
                EdgeRenderingMode.Automatic);
        }

        DrawTracker(context, clientRect);
    }

    private void DrawTracker(SkiaRenderContext context, PlotterRect clientRect)
    {
        const double padding = 6;
        const double gap = 7;

        var tracker = _tracker;
        if (tracker == null || string.IsNullOrEmpty(tracker.Text))
        {
            return;
        }

        //The model's own default font keeps the tracker typographically consistent with the
        //axis and title text (the resolver maps it to an application font either way)
        var fontFamily = Model?.DefaultFont;
        var contentSize = context.MeasureText(tracker.Text, fontFamily, TrackerFontSize, FontWeights.Normal);
        var box = TrackerBoxLayout.Calculate(tracker.Position, contentSize, padding, gap, clientRect);

        context.DrawRectangle(box, TrackerBackground, TrackerForeground, 1, EdgeRenderingMode.Adaptive);
        context.DrawText(
            new ScreenPoint(box.Left + padding, box.Top + padding),
            tracker.Text,
            TrackerForeground,
            fontFamily,
            TrackerFontSize,
            FontWeights.Normal,
            0,
            PlotterHorizontalAlignment.Left,
            PlotterVerticalAlignment.Top);
    }

    private static SKColor ToSKColor(PlotterColor color) =>
        new SKColor(color.R, color.G, color.B, color.A);

    private static bool IsTouch(PointerRoutedEventArgs e) =>
        e.Pointer.PointerDeviceType == PointerDeviceType.Touch;

    private PlotterModifierKeys CurrentModifiers()
    {
        var modifiers = PlotterModifierKeys.None;
        if (_shiftDown) { modifiers |= PlotterModifierKeys.Shift; }
        if (_controlDown) { modifiers |= PlotterModifierKeys.Control; }
        if (_altDown) { modifiers |= PlotterModifierKeys.Alt; }
        if (_windowsDown) { modifiers |= PlotterModifierKeys.Windows; }
        return modifiers;
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer); //key bindings (arrows, +/-, A) need the focus here

        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            var firstContact = _touchTracker.Down(e.Pointer.PointerId, position);
            _canvas.CapturePointer(e.Pointer);
            if (firstContact)
            {
                var touchArgs = new PlotterTouchEventArgs
                {
                    Position = position,
                    DeltaTranslation = new ScreenVector(0, 0),
                    DeltaScale = new ScreenVector(1, 1),
                    ModifierKeys = CurrentModifiers(),
                };
                ActualController.HandleTouchStarted(this, touchArgs);
            }

            e.Handled = true;
            return;
        }

        var button = PointerButtonMapper.ToMouseButton(point.Properties.PointerUpdateKind);
        if (button == PlotterMouseButton.None)
        {
            return;
        }

        var args = new PlotterMouseDownEventArgs
        {
            ChangedButton = button,
            ClickCount = _clickCounter.Register(Environment.TickCount64, position.X, position.Y),
            Position = position,
            ModifierKeys = CurrentModifiers(),
        };

        _canvas.CapturePointer(e.Pointer); //drag manipulators (pan, zoom rectangle) need it
        ActualController.HandleMouseDown(this, args);
        e.Handled = args.Handled;
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            if (_touchTracker.Move(e.Pointer.PointerId, position, out var current, out var previous))
            {
                var touchArgs = new PlotterTouchEventArgs(current, previous)
                {
                    ModifierKeys = CurrentModifiers(),
                };
                ActualController.HandleTouchDelta(this, touchArgs);
                e.Handled = true;
            }

            return;
        }

        var args = new PlotterMouseEventArgs
        {
            Position = position,
            ModifierKeys = CurrentModifiers(),
        };
        ActualController.HandleMouseMove(this, args);
        e.Handled = args.Handled;
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var position = new ScreenPoint(point.Position.X, point.Position.Y);

        if (IsTouch(e))
        {
            var lastContact = _touchTracker.Up(e.Pointer.PointerId);
            _canvas.ReleasePointerCapture(e.Pointer);
            if (lastContact)
            {
                var touchArgs = new PlotterTouchEventArgs
                {
                    Position = position,
                    ModifierKeys = CurrentModifiers(),
                };
                ActualController.HandleTouchCompleted(this, touchArgs);
            }

            e.Handled = true;
            return;
        }

        var args = new PlotterMouseEventArgs
        {
            Position = position,
            ModifierKeys = CurrentModifiers(),
        };
        _canvas.ReleasePointerCapture(e.Pointer);
        ActualController.HandleMouseUp(this, args);
        e.Handled = args.Handled;
    }

    private void OnCanvasPointerLost(object sender, PointerRoutedEventArgs e)
    {
        //A canceled touch (or a capture torn away) must not leave a phantom contact behind:
        //drop it, and complete the gesture when it was the last one
        if (_touchTracker.Count == 0 || !IsTouch(e))
        {
            return;
        }

        if (_touchTracker.Up(e.Pointer.PointerId))
        {
            var point = e.GetCurrentPoint(_canvas);
            var touchArgs = new PlotterTouchEventArgs
            {
                Position = new ScreenPoint(point.Position.X, point.Position.Y),
                ModifierKeys = CurrentModifiers(),
            };
            ActualController.HandleTouchCompleted(this, touchArgs);
        }
    }

    private void OnCanvasPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var args = new PlotterMouseEventArgs
        {
            Position = new ScreenPoint(point.Position.X, point.Position.Y),
            ModifierKeys = CurrentModifiers(),
        };
        ActualController.HandleMouseEnter(this, args);
        e.Handled = args.Handled;
    }

    private void OnCanvasPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var args = new PlotterMouseEventArgs
        {
            Position = new ScreenPoint(point.Position.X, point.Position.Y),
            ModifierKeys = CurrentModifiers(),
        };
        ActualController.HandleMouseLeave(this, args);
        e.Handled = args.Handled;
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_canvas);
        var args = new PlotterMouseWheelEventArgs
        {
            Position = new ScreenPoint(point.Position.X, point.Position.Y),
            Delta = point.Properties.MouseWheelDelta,
            ModifierKeys = CurrentModifiers(),
        };
        ActualController.HandleMouseWheel(this, args);
        e.Handled = args.Handled;
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);
        UpdateModifier(e.Key, isDown: false);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (UpdateModifier(e.Key, isDown: true))
        {
            return;
        }

        var key = VirtualKeyMapper.ToPlotterKey(e.Key);
        if (key == PlotterKey.Unknown)
        {
            return;
        }

        var args = new PlotterKeyEventArgs
        {
            Key = key,
            ModifierKeys = CurrentModifiers(),
        };
        ActualController.HandleKeyDown(this, args);
        e.Handled = args.Handled;
    }

    private bool UpdateModifier(VirtualKey key, bool isDown)
    {
        switch (key)
        {
            case VirtualKey.Shift:
            case VirtualKey.LeftShift:
            case VirtualKey.RightShift:
                _shiftDown = isDown;
                return true;

            case VirtualKey.Control:
            case VirtualKey.LeftControl:
            case VirtualKey.RightControl:
                _controlDown = isDown;
                return true;

            case VirtualKey.Menu:
            case VirtualKey.LeftMenu:
            case VirtualKey.RightMenu:
                _altDown = isDown;
                return true;

            case VirtualKey.LeftWindows:
            case VirtualKey.RightWindows:
                _windowsDown = isDown;
                return true;

            default:
                return false;
        }
    }
}
