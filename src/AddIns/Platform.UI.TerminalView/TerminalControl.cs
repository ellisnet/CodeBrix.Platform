#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.TerminalView.Input;
using CodeBrix.Platform.UI.TerminalView.Internal;
using CodeBrix.Platform.UI.TerminalView.Rendering;
using CodeBrix.Platform.UI.TextLayout;
using CodeBrix.Terminal.Engine;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using TerminalBuffer = CodeBrix.Terminal.Engine.Buffer;   //Required: 'Buffer' alone is ambiguous with System.Buffer
using TerminalEngine = CodeBrix.Terminal.Engine.Terminal; //Required: inside the CodeBrix.Platform.UI.TerminalView
                                                          //  namespace the simple name 'Terminal' binds to the
                                                          //  CodeBrix.Terminal NAMESPACE, not the engine type

namespace CodeBrix.Platform.UI.TerminalView;

//was previously: Lily.Shell.TerminalView.TerminalControl (the author's original code,
//relicensed from that GPL-3 tool repo to Apache-2.0 for this add-in), reworked for the
//add-in template: SKXamlCanvas base replaced by Control + the internal RenderCanvas
//(the AdvancedTextEdit pattern, dropping the SkiaSharp.Views dependency), a vertical
//scrollbar added (AdvancedTextEdit's minimal ScrollBar template - a bare theme ScrollBar
//paints nothing standalone on these heads), viewport math moved onto the engine's
//ScrollLines/ScrollToBottom/IsAtBottom (CodeBrix.Terminal 1.0.223+), keyboard encoding
//moved onto the engine's TerminalKeyEncoder, the UnicodeKeyReader reflection hack replaced
//by direct internal KeyRoutedEventArgs.UnicodeKey access (InternalsVisibleTo), and
//clipboard copy AND paste owned by the control (context menu + Ctrl+Shift+C/V).

/// <summary>
/// A terminal view: renders a CodeBrix.Terminal buffer as a fixed monospace
/// cell grid on a Skia surface and turns keyboard input into VT byte
/// sequences. Wire <see cref="InputEmitted"/> to the transport's input, wire
/// <see cref="GridResized"/> to its window-size channel (for SSH:
/// ShellStream.ChangeWindowSize), and call <see cref="Feed(string)"/> /
/// <see cref="Feed(byte[], int)"/> with the transport's output — the control
/// is the screen and keyboard half of a terminal, the way a pty master would
/// see it.
/// </summary>
/// <remarks>
/// <para>
/// Selection follows the engine: drag to select, double-click for
/// word/expression selection. Copy and paste are built in: right-click opens
/// a context menu, Ctrl+Shift+C copies, Ctrl+Shift+V pastes (line endings
/// normalized to CR). Scrollback is reachable via the scrollbar, the mouse
/// wheel, and Shift+PageUp/PageDown; typing snaps back to live output.
/// </para>
/// <para>
/// Give the control a bounded size (a Grid star cell is ideal); the grid
/// dimensions follow the control size. Mouse-reporting escape protocols
/// (X10/SGR) are not forwarded to the hosted application, and there is no
/// IME path.
/// </para>
/// </remarks>
public sealed partial class TerminalControl : Control
{
    private const string DefaultFontFamily =
        "ms-appx:///CodeBrix.Platform.Fonts.RobotoMono/Fonts/RobotoMono.ttf";

    private const double ScrollBarThickness = 12.0;

    private readonly TerminalEngine _terminal;
    private readonly SelectionService _selection;
    private readonly RenderCanvas _canvas;
    private readonly ScrollBar _verticalScrollBar;
    private readonly MenuFlyout _contextMenu;
    private readonly MenuFlyoutItem _copyMenuItem;
    private readonly DispatcherTimer _blinkTimer;
    private readonly DispatcherTimer _dragScrollTimer;

    private CellMetrics? _metrics;
    private bool _selecting;
    private bool _updatingScrollBar;
    private (int Column, int Row) _lastDragCell = (-1, -1);
    private double _lastPointerX;
    private double _lastPointerY;
    private long _lastClickTick;
    private (int Column, int Row) _lastClickCell = (-1, -1);
    private string _fontFamily = DefaultFontFamily;
    private float _fontSize = 14f;
    private bool _blinkOn = true;
    private bool _focused;
    private bool _shiftDown;
    private bool _controlDown;
    private bool _altDown;
    private bool _capsLock;

    /// <summary>Creates the control with an 80x25 terminal that resizes to fit.</summary>
    public TerminalControl()
    {
        _terminal = new TerminalEngine(new ViewDelegate(this), new TerminalOptions
        {
            Cols = 80,
            Rows = 25,
            //Most terminal hosts feed explicit CR+LF; double conversion would add blank rows
            ConvertEol = false
        });

        _selection = new SelectionService(_terminal);
        _selection.SelectionChanged += () => _canvas?.Invalidate();

        _terminal.Scrolled += (_, _) =>
        {
            UpdateScrollBar();
            _canvas?.Invalidate();
        };

        IsTabStop = true;               //Required for key events

        _canvas = new RenderCanvas();
        _canvas.Paint += OnPaint;
        _canvas.SizeChanged += (_, _) => RecalculateGrid();
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;

        // The theme's ScrollBar template renders through indicator visual states that a
        // hosting ScrollViewer normally drives; standing alone on these heads the bar
        // occupies space but paints nothing - so it gets the minimal code-built template
        // (the AdvancedTextEdit recipe) providing exactly the named parts the control's
        // track layout looks up.
        _verticalScrollBar = new ScrollBar
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 0,
            SmallChange = 1,
            IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
            Visibility = Visibility.Collapsed,
            IsTabStop = false,
            Width = ScrollBarThickness,
        };
        _verticalScrollBar.Template = new ControlTemplate(BuildScrollBarTemplateRoot);
        _verticalScrollBar.ValueChanged += OnScrollBarValueChanged;

        _copyMenuItem = new MenuFlyoutItem { Text = "Copy" };
        _copyMenuItem.Click += (_, _) => CopySelection();
        var pasteMenuItem = new MenuFlyoutItem { Text = "Paste" };
        pasteMenuItem.Click += (_, _) => PasteFromClipboard();
        _contextMenu = new MenuFlyout();
        _contextMenu.Items.Add(_copyMenuItem);
        _contextMenu.Items.Add(pasteMenuItem);

        Template = new ControlTemplate(CreateTemplateRoot);

        _dragScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _dragScrollTimer.Tick += (_, _) => AutoScrollDrag();

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (_, _) => { _blinkOn = !_blinkOn; _canvas.Invalidate(); };
        Loaded += (_, _) => _blinkTimer.Start();
        Unloaded += (_, _) => { _blinkTimer.Stop(); _dragScrollTimer.Stop(); };
    }

    /// <summary>Raised with VT-encoded keyboard (or pasted) input, on the UI thread.</summary>
    public event Action<string>? InputEmitted;

    /// <summary>Raised when the terminal title changes (OSC 0/2).</summary>
    public event Action<string>? TitleChanged;

    /// <summary>
    /// Raised with (columns, rows) whenever the grid dimensions change with the
    /// control size. Forward this to the transport's window-size channel — for
    /// SSH, ShellStream.ChangeWindowSize(cols, rows, 0, 0).
    /// </summary>
    public event Action<int, int>? GridResized;

    /// <summary>
    /// Raised with the selected text whenever it is copied (context menu or
    /// Ctrl+Shift+C). Observational: the control has already placed the text
    /// on the clipboard.
    /// </summary>
    public event Action<string>? CopyRequested;

    private SKColor _selectionColor = new(0x4d, 0x8b, 0xd8, 0x66);

    /// <summary>The translucent overlay painted over selected cells.</summary>
    public SKColor SelectionColor
    {
        get => _selectionColor;
        set { _selectionColor = value; _canvas?.Invalidate(); }
    }

    /// <summary>The terminal's current column count.</summary>
    public int Columns => _terminal.Cols;

    /// <summary>The terminal's current row count.</summary>
    public int Rows => _terminal.Rows;

    private SKColor _foregroundColor = new(0xff, 0xff, 0xff);

    /// <summary>The default text color. Default: the engine's white.</summary>
    public SKColor ForegroundColor
    {
        get => _foregroundColor;
        set { _foregroundColor = value; _canvas?.Invalidate(); }
    }

    private SKColor _backgroundColor = new(0x00, 0x00, 0x00);

    /// <summary>The terminal background. Default: the engine's black.</summary>
    public SKColor BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; _canvas?.Invalidate(); }
    }

    /// <summary>
    /// Whether a bare LF in fed data is treated as CRLF. Default false, which
    /// suits transports that emit explicit CR+LF (remote shells over SSH, most
    /// PTYs); set true for hosts that emit bare LF line endings.
    /// </summary>
    public bool ConvertEol
    {
        get => _terminal.Options.ConvertEol;
        set => _terminal.Options.ConvertEol = value;
    }

    /// <summary>
    /// The number of scrollback lines kept beyond the visible rows. Default
    /// 1000 (the engine default). Set it before the control is loaded; a
    /// change afterwards takes effect on the next grid resize.
    /// </summary>
    public int Scrollback
    {
        get => _terminal.Options.Scrollback ?? 0;
        set => _terminal.Options.Scrollback = Math.Max(0, value);
    }

    /// <summary>
    /// The terminal font family (a font URI or family name understood by
    /// TextLayout). Default: Roboto Mono from the RobotoMono fonts package,
    /// which this add-in ships as a dependency.
    /// </summary>
    public string TerminalFontFamily
    {
        get => _fontFamily;
        set
        {
            _fontFamily = string.IsNullOrWhiteSpace(value) ? DefaultFontFamily : value;
            _metrics = null;
            RecalculateGrid();
            _canvas?.Invalidate();
        }
    }

    /// <summary>The terminal font size in DIPs. Default 14.</summary>
    public float TerminalFontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value > 4f ? value : 4f;
            _metrics = null;
            RecalculateGrid();
            _canvas?.Invalidate();
        }
    }

    /// <summary>
    /// Feeds VT output data into the terminal. Safe to call from any thread —
    /// the work is marshalled to the UI thread.
    /// </summary>
    public void Feed(string data)
    {
        if (string.IsNullOrEmpty(data)) { return; }

        var queue = DispatcherQueue;
        if (queue == null) { return; }

        queue.TryEnqueue(() =>
        {
            _terminal.Feed(data);
            UpdateScrollBar();
            _canvas.Invalidate();
        });
    }

    /// <summary>
    /// Feeds raw VT output bytes into the terminal (the natural shape for an
    /// SSH ShellStream read loop). The buffer is copied before marshalling to
    /// the UI thread, so the caller may reuse it immediately.
    /// </summary>
    public void Feed(byte[] data, int length)
    {
        if (data == null || length <= 0) { return; }

        var queue = DispatcherQueue;
        if (queue == null) { return; }

        var copy = new byte[Math.Min(length, data.Length)];
        Array.Copy(data, copy, copy.Length);

        queue.TryEnqueue(() =>
        {
            _terminal.Feed(copy, copy.Length);
            UpdateScrollBar();
            _canvas.Invalidate();
        });
    }

    /// <summary>
    /// Performs a full terminal reset (RIS) — a host clearing the screen
    /// between sessions calls this.
    /// </summary>
    public void Reset()
    {
        _terminal.Reset();
        _selection.SelectNone();
        UpdateScrollBar();
        _canvas.Invalidate();
    }

    /// <summary>Gives the control keyboard focus.</summary>
    public void GrabFocus() => Focus(FocusState.Programmatic);

    private UIElement CreateTemplateRoot()
    {
        var root = new Grid
        {
            //No HitTestCore in this framework: a background brush is what makes empty
            //space hit-testable (the family recipe)
            Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });

        Grid.SetColumn(_canvas, 0);
        Grid.SetColumn(_verticalScrollBar, 1);
        root.Children.Add(_canvas);
        root.Children.Add(_verticalScrollBar);
        return root;
    }

    /// <summary>
    /// The minimal scroll bar template (the AdvancedTextEdit recipe): a
    /// track-colored root grid holding two transparent large-change repeat
    /// buttons and the thumb, with the part names the control's track layout
    /// looks up.
    /// </summary>
    private static UIElement BuildScrollBarTemplateRoot()
    {
        static RepeatButton CreateTrackButton(string name)
        {
            return new RepeatButton
            {
                Name = name,
                IsTabStop = false,
                // Transparent but hit-testable: clicking the track pages toward the click.
                Template = new ControlTemplate(() => new Border
                {
                    Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                }),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinWidth = 0,
                MinHeight = 0,
            };
        }

        var thumb = new Thumb
        {
            Name = "VerticalThumb",
            IsTabStop = false,
            MinWidth = 0,
            MinHeight = 0,
            Template = new ControlTemplate(() => new Border
            {
                Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xA0, 0x80, 0x80, 0x80)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(2),
            }),
        };

        var root = new Grid
        {
            Name = "VerticalRoot",
            Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0x14, 0x00, 0x00, 0x00)),
        };

        var decrease = CreateTrackButton("VerticalLargeDecrease");
        var increase = CreateTrackButton("VerticalLargeIncrease");
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(decrease, 0);
        Grid.SetRow(thumb, 1);
        Grid.SetRow(increase, 2);

        root.Children.Add(decrease);
        root.Children.Add(thumb);
        root.Children.Add(increase);
        return root;
    }

    private void RaiseInput(string data)
    {
        //Typing snaps the view back to the live tail, like every terminal
        _terminal.ScrollToBottom();
        _blinkOn = true;
        InputEmitted?.Invoke(data);
        _canvas.Invalidate();
    }

    private void UpdateScrollBar()
    {
        if (_verticalScrollBar == null) { return; }

        var buffer = _terminal.Buffer;

        _updatingScrollBar = true;
        try
        {
            _verticalScrollBar.Maximum = buffer.YBase;
            _verticalScrollBar.ViewportSize = _terminal.Rows;
            _verticalScrollBar.LargeChange = Math.Max(1, _terminal.Rows - 1);
            _verticalScrollBar.Value = buffer.YDisp;
            _verticalScrollBar.Visibility = buffer.YBase > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _updatingScrollBar = false;
        }
    }

    private void OnScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingScrollBar) { return; }

        var delta = (int)Math.Round(e.NewValue) - _terminal.Buffer.YDisp;
        if (delta != 0) { _terminal.ScrollLines(delta); }
    }

    private void RecalculateGrid()
    {
        if (_canvas.ActualWidth < 1 || _canvas.ActualHeight < 1) { return; }

        var cell = EnsureMetrics();
        var cols = Math.Max(4, (int)(_canvas.ActualWidth / cell.Width));
        var rows = Math.Max(2, (int)(_canvas.ActualHeight / cell.Height));

        if (cols != _terminal.Cols || rows != _terminal.Rows)
        {
            var wasAtBottom = _terminal.IsAtBottom;
            _terminal.Resize(cols, rows);
            if (wasAtBottom) { _terminal.ScrollToBottom(); }

            GridResized?.Invoke(cols, rows);
            UpdateScrollBar();
        }

        _canvas.Invalidate();
    }

    private CellMetrics EnsureMetrics() =>
        _metrics ??= CellMetrics.Measure(_fontFamily, _fontSize);

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        var point = e.GetCurrentPoint(_canvas);

        if (point.Properties.IsRightButtonPressed)
        {
            _copyMenuItem.IsEnabled = _selection.Active;
            _contextMenu.ShowAt(_canvas, new FlyoutShowOptions { Position = point.Position });
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            return;
        }

        var cell = SelectionGeometry.ToCell(point.Position.X, point.Position.Y,
            EnsureMetrics(), _terminal.Cols, _terminal.Rows);
        var now = Environment.TickCount64;

        if (now - _lastClickTick < 400 && cell == _lastClickCell)
        {
            //Double-click: word/expression selection. NOTE the engine's
            //  (col, row) parameter order - unlike its (row, col) siblings.
            _selection.SelectWordOrExpression(cell.Column, cell.Row);
            _lastClickTick = 0;
        }
        else
        {
            if (_selection.Active) { _selection.SelectNone(); }
            _selection.SetSoftStart(cell.Row, cell.Column);
            _selecting = true;
            _lastDragCell = cell;
            _canvas.CapturePointer(e.Pointer);
            _lastClickTick = now;
            _lastClickCell = cell;
        }

        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_selecting) { return; }

        var position = e.GetCurrentPoint(_canvas).Position;
        _lastPointerX = position.X;
        _lastPointerY = position.Y;

        //Dragging beyond the top/bottom edge scrolls the view while held there
        if (position.Y < 0 || position.Y > _canvas.ActualHeight)
        {
            if (!_dragScrollTimer.IsEnabled) { _dragScrollTimer.Start(); }
        }
        else if (_dragScrollTimer.IsEnabled)
        {
            _dragScrollTimer.Stop();
        }

        ExtendSelectionTo(position.X, position.Y);
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_selecting) { return; }

        _selecting = false;
        _dragScrollTimer.Stop();
        _canvas.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void ExtendSelectionTo(double x, double y)
    {
        var cell = SelectionGeometry.ToCell(x, y, EnsureMetrics(),
            _terminal.Cols, _terminal.Rows);
        if (cell == _lastDragCell && _selection.Active) { return; }

        if (!_selection.Active) { _selection.StartSelection(); }
        _selection.DragExtend(cell.Row, cell.Column);
        _lastDragCell = cell;
    }

    private void AutoScrollDrag()
    {
        if (!_selecting)
        {
            _dragScrollTimer.Stop();
            return;
        }

        //Above the top edge scrolls back into history; below scrolls forward
        _terminal.ScrollLines(_lastPointerY < 0 ? -1 : 1);
        ExtendSelectionTo(_lastPointerX, _lastPointerY);
    }

    private void CopySelection()
    {
        if (!_selection.Active) { return; }

        var text = _selection.GetSelectedText();
        if (string.IsNullOrEmpty(text)) { return; }

        var data = new DataPackage();
        data.SetText(text);
        try
        {
            //This framework's clipboard contract: SetContent, then Flush so the
            //content outlives the app (the AdvancedTextEdit recipe).
            Clipboard.SetContent(data);
            Clipboard.Flush();
        }
        catch (Exception)
        {
            //The clipboard can be transiently unavailable; the copy simply doesn't take.
        }

        CopyRequested?.Invoke(text);
    }

    private async void PasteFromClipboard()
    {
        string? text = null;
        try
        {
            var view = Clipboard.GetContent();
            if (view != null && view.Contains(StandardDataFormats.Text))
            {
                //Reading clipboard text is asynchronous in this framework
                text = await view.GetTextAsync();
            }
        }
        catch (Exception)
        {
            //The clipboard can be transiently unavailable; the paste simply doesn't happen.
        }

        if (string.IsNullOrEmpty(text)) { return; }

        //Terminals receive CR for line breaks; unnormalized LF doubles lines
        text = text.Replace("\r\n", "\r").Replace('\n', '\r');
        RaiseInput(text);
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(_canvas).Properties.MouseWheelDelta;
        //Wheel up (positive delta) scrolls back into history
        _terminal.ScrollLines(-(delta / 120 * 3));
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _focused = true;
        _blinkOn = true;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _focused = false;
        _canvas.Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e) =>
        UpdateModifier(e.Key, isDown: false);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (UpdateModifier(e.Key, isDown: true)) { return; }

        //Ctrl+Shift+C / Ctrl+Shift+V are the terminal-conventional clipboard
        //chords (never reach the shell as input)
        if (_controlDown && _shiftDown && e.Key == VirtualKey.C)
        {
            CopySelection();
            e.Handled = true;
            return;
        }

        if (_controlDown && _shiftDown && e.Key == VirtualKey.V)
        {
            PasteFromClipboard();
            e.Handled = true;
            return;
        }

        //Shift+PageUp/PageDown page through the scrollback
        if (_shiftDown && e.Key is VirtualKey.PageUp or VirtualKey.PageDown)
        {
            var page = Math.Max(1, _terminal.Rows - 1);
            _terminal.ScrollLines(e.Key == VirtualKey.PageUp ? -page : page);
            e.Handled = true;
            return;
        }

        var encoded = EncodeKey(e);
        if (encoded != null)
        {
            RaiseInput(encoded);
            e.Handled = true;
        }
    }

    private string? EncodeKey(KeyRoutedEventArgs e)
    {
        var key = VirtualKeyMapper.ToTerminalKey(e.Key);
        var modifiers = CurrentModifiers();

        //Chords go through the full key mapping (Ctrl -> C0 codes, Alt -> ESC prefix)
        if (_controlDown || _altDown)
        {
            return TerminalKeyEncoder.Encode(key, modifiers, _terminal.ApplicationCursor);
        }

        //Shift+Tab must reach Encode to become back-tab (EncodeSpecial is modifier-free)
        if (key == TerminalKey.Tab && _shiftDown)
        {
            return TerminalKeyEncoder.Encode(key, modifiers, _terminal.ApplicationCursor);
        }

        var special = TerminalKeyEncoder.EncodeSpecial(key, _terminal.ApplicationCursor);
        if (special != null) { return special; }

        //Printables: prefer the platform's layout-composed character - the
        //  raw-key path cannot see shifted digit-row symbols like '(' on
        //  non-US layouts. UnicodeKey is internal framework API, reached via
        //  InternalsVisibleTo (the AdvancedTextEdit TextArea precedent).
        if (e.UnicodeKey is { } composed)
        {
            var encoded = TerminalKeyEncoder.EncodeComposed(composed, modifiers);
            if (encoded != null) { return encoded; }
        }

        return TerminalKeyEncoder.Encode(key, modifiers, _terminal.ApplicationCursor);
    }

    private TerminalModifiers CurrentModifiers()
    {
        var modifiers = TerminalModifiers.None;
        if (_shiftDown) { modifiers |= TerminalModifiers.Shift; }
        if (_controlDown) { modifiers |= TerminalModifiers.Control; }
        if (_altDown) { modifiers |= TerminalModifiers.Alt; }
        if (_capsLock) { modifiers |= TerminalModifiers.CapsLock; }
        return modifiers;
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

            case VirtualKey.CapitalLock:
                if (isDown) { _capsLock = !_capsLock; }
                return true;

            default:
                return false;
        }
    }

    private void OnPaint(SKCanvas canvas, SKSize size)
    {
        canvas.Clear(BackgroundColor);

        var cell = EnsureMetrics();
        var buffer = _terminal.Buffer;

        for (var row = 0; row < _terminal.Rows; row++)
        {
            var lineIndex = buffer.YDisp + row;
            if (lineIndex >= buffer.Lines.Length) { break; }

            DrawLine(canvas, RunBuilder.BuildRuns(buffer.Lines[lineIndex]), row * cell.Height, cell);
        }

        if (_selection.Active) { DrawSelection(canvas, buffer, cell); }

        DrawCursor(canvas, buffer, cell);
    }

    private void DrawSelection(SKCanvas canvas, TerminalBuffer buffer, CellMetrics cell)
    {
        var start = _selection.Start;
        var end = _selection.End;
        using var paint = new SKPaint { Color = SelectionColor };

        for (var row = 0; row < _terminal.Rows; row++)
        {
            if (SelectionGeometry.TryGetRowSpan(start.X, start.Y, end.X, end.Y,
                buffer.YDisp + row, _terminal.Cols, out var first, out var last))
            {
                canvas.DrawRect(first * cell.Width, row * cell.Height,
                    (last - first + 1) * cell.Width, cell.Height, paint);
            }
        }
    }

    private void DrawLine(SKCanvas canvas, List<TextRunSegment> segments,
        float top, CellMetrics cell)
    {
        foreach (var segment in segments)
        {
            var style = AttributeDecoder.Decode(segment.Attribute, ForegroundColor, BackgroundColor);
            var left = segment.StartColumn * cell.Width;
            var width = segment.CellCount * cell.Width;

            if (style.HasVisibleBackground(BackgroundColor))
            {
                using var backPaint = new SKPaint { Color = style.Background };
                canvas.DrawRect(left, top, width, cell.Height, backPaint);
            }

            var isBlank = string.IsNullOrWhiteSpace(segment.Text);
            if (!isBlank)
            {
                var descriptor = new TextRunDescriptor(segment.Text, _fontFamily, _fontSize,
                    style.Bold ? TextFontWeight.Bold : TextFontWeight.Normal,
                    style.Italic ? TextFontStyle.Italic : TextFontStyle.Normal)
                {
                    Color = style.Foreground
                };

                using var layout = TextLayoutEngine.Layout([descriptor]);
                using var textPaint = new SKPaint { Color = style.Foreground, IsAntialias = true };
                layout.Draw(canvas, new SKPoint(left, top), textPaint);
            }

            if (style.Underline || style.CrossedOut)
            {
                using var linePaint = new SKPaint
                {
                    Color = style.Foreground,
                    StrokeWidth = Math.Max(1f, _fontSize / 14f)
                };

                if (style.Underline)
                {
                    var y = top + cell.Baseline + 2f;
                    canvas.DrawLine(left, y, left + width, y, linePaint);
                }

                if (style.CrossedOut)
                {
                    var y = top + cell.Height * 0.5f;
                    canvas.DrawLine(left, y, left + width, y, linePaint);
                }
            }
        }
    }

    private void DrawCursor(SKCanvas canvas, TerminalBuffer buffer, CellMetrics cell)
    {
        if (_terminal.CursorHidden) { return; }

        var screenRow = buffer.YBase + buffer.Y - buffer.YDisp;
        if (screenRow < 0 || screenRow >= _terminal.Rows) { return; }

        var left = buffer.X * cell.Width;
        var top = screenRow * cell.Height;

        if (!_focused)
        {
            //Steady hollow cursor while unfocused
            using var stroke = new SKPaint
            {
                Color = ForegroundColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f
            };
            canvas.DrawRect(left + 0.5f, top + 0.5f, cell.Width - 1f, cell.Height - 1f, stroke);
            return;
        }

        if (!_blinkOn) { return; }

        using var fill = new SKPaint { Color = ForegroundColor };
        canvas.DrawRect(left, top, cell.Width, cell.Height, fill);

        //Repaint the character under the block in the background color
        var lineIndex = buffer.YBase + buffer.Y;
        if (lineIndex < buffer.Lines.Length && buffer.X < buffer.Lines[lineIndex].Length)
        {
            var text = RunBuilder.CellText(buffer.Lines[lineIndex][buffer.X]);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var descriptor = new TextRunDescriptor(text, _fontFamily, _fontSize)
                {
                    Color = BackgroundColor
                };
                using var layout = TextLayoutEngine.Layout([descriptor]);
                using var paint = new SKPaint { Color = BackgroundColor, IsAntialias = true };
                layout.Draw(canvas, new SKPoint(left, top), paint);
            }
        }
    }

    private sealed class ViewDelegate : ITerminalDelegate
    {
        private readonly TerminalControl _owner;

        public ViewDelegate(TerminalControl owner) => _owner = owner;

        public void ShowCursor(TerminalEngine source) => _owner._canvas?.Invalidate();

        public void SetTerminalTitle(TerminalEngine source, string title) =>
            _owner.TitleChanged?.Invoke(title);

        public void SetTerminalIconTitle(TerminalEngine source, string title)
        {
        }

        public void SizeChanged(TerminalEngine source)
        {
            //Escape-sequence-driven resize is not supported; the grid follows the control size
        }

        public void Send(byte[] data) =>
            _owner.InputEmitted?.Invoke(Encoding.UTF8.GetString(data));

        public string? WindowCommand(TerminalEngine source, WindowManipulationCommand command,
            params int[] args) => null;

        public bool IsProcessTrusted() => true;
    }
}
