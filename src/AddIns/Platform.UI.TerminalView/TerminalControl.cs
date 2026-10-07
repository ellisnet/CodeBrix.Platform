#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.TerminalView.Engine;
using CodeBrix.Platform.UI.TerminalView.Input;
using CodeBrix.Platform.UI.TerminalView.Internal;
using CodeBrix.Platform.UI.Xaml.Controls.Extensions;
using CodeBrix.Terminal.Engine;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

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
//WPE1 C6: the grid drawing, the terminal, the selection gestures and the key encoding moved
//into the WinUI-free engine (Engine/TerminalRenderer, Engine/TerminalInputEncoder); this
//control wraps them and keeps only the XAML half (surface, scroll bar, menu, timers, focus,
//clipboard, VirtualKey mapping).

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
/// With a finger: a vertical drag scrolls the history, a horizontal drag
/// selects, a long press then a drag selects in any direction, and a long
/// press without a drag opens the Copy/Paste menu where the finger lifts.
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
    private const double ScrollBarThickness = 12.0;

    private readonly TerminalRenderer _renderer;
    private readonly TerminalInputEncoder _keyInput = new();
    private readonly TerminalTouchGesture _touch;
    private readonly FrameworkElement _canvas;
    private readonly ScrollBar _verticalScrollBar;
    private readonly MenuFlyout _contextMenu;
    private readonly MenuFlyoutItem _copyMenuItem;
    private readonly SoftwareKeyboardFlyoutGuard _keyboardGuard;
    private readonly DispatcherTimer _blinkTimer;
    private readonly DispatcherTimer _dragScrollTimer;

    private bool _updatingScrollBar;

    /// <summary>Creates the control with an 80x25 terminal that resizes to fit.</summary>
    public TerminalControl()
    {
        _renderer = new TerminalRenderer();
        _renderer.InvalidateRequested += InvalidateCanvas;
        _renderer.Scrolled += UpdateScrollBar;
        _renderer.TitleChanged += title => TitleChanged?.Invoke(title);
        _renderer.InputSent += data => InputEmitted?.Invoke(data);
        _touch = new TerminalTouchGesture(_renderer);

        IsTabStop = true;               //Required for key events

        _canvas = RenderCanvasSupply.Create();
        RenderCanvasSupply.AddPaintHandler(_canvas, _renderer.Paint);
        _canvas.SizeChanged += (_, _) => RecalculateGrid();
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        _canvas.PointerCaptureLost += (_, _) => _touch.Cancel();
        _canvas.PointerCanceled += (_, _) => _touch.Cancel();

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
        //The Copy/Paste menu borrowing focus is not the user leaving the
        //terminal: the software keyboard must not hide on open / re-show on
        //Paste. The guard suppresses the keyboard notifications across that
        //round-trip and delivers the withheld unfocus itself if the menu
        //closes with focus somewhere else.
        _contextMenu.DoesNotAffectSoftwareKeyboard = true;
        _keyboardGuard = new SoftwareKeyboardFlyoutGuard(this, _contextMenu,
            () => SoftwareKeyboardFocus.NotifyUnfocused(this));

        Template = new ControlTemplate(CreateTemplateRoot);

        _dragScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _dragScrollTimer.Tick += (_, _) =>
        {
            if (!_renderer.AutoScrollDrag()) { _dragScrollTimer.Stop(); }
        };

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (_, _) => { _renderer.IsBlinkOn = !_renderer.IsBlinkOn; RenderCanvasSupply.Invalidate(_canvas); };
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

    /// <summary>The translucent overlay painted over selected cells.</summary>
    public SKColor SelectionColor
    {
        get => _renderer.SelectionColor;
        set { _renderer.SelectionColor = value; InvalidateCanvas(); }
    }

    /// <summary>The terminal's current column count.</summary>
    public int Columns => _renderer.Columns;

    /// <summary>The terminal's current row count.</summary>
    public int Rows => _renderer.Rows;

    /// <summary>The default text color. Default: the engine's white.</summary>
    public SKColor ForegroundColor
    {
        get => _renderer.ForegroundColor;
        set { _renderer.ForegroundColor = value; InvalidateCanvas(); }
    }

    /// <summary>The terminal background. Default: the engine's black.</summary>
    public SKColor BackgroundColor
    {
        get => _renderer.BackgroundColor;
        set { _renderer.BackgroundColor = value; InvalidateCanvas(); }
    }

    /// <summary>
    /// Whether a bare LF in fed data is treated as CRLF. Default false, which
    /// suits transports that emit explicit CR+LF (remote shells over SSH, most
    /// PTYs); set true for hosts that emit bare LF line endings.
    /// </summary>
    public bool ConvertEol
    {
        get => _renderer.ConvertEol;
        set => _renderer.ConvertEol = value;
    }

    /// <summary>
    /// The number of scrollback lines kept beyond the visible rows. Default
    /// 1000 (the engine default). Set it before the control is loaded; a
    /// change afterwards takes effect on the next grid resize.
    /// </summary>
    public int Scrollback
    {
        get => _renderer.Scrollback;
        set => _renderer.Scrollback = value;
    }

    /// <summary>
    /// The terminal font family (a font URI or family name understood by
    /// TextLayout). Default: Roboto Mono from the RobotoMono fonts package,
    /// which this add-in ships as a dependency.
    /// </summary>
    public string TerminalFontFamily
    {
        get => _renderer.FontFamily;
        set
        {
            _renderer.FontFamily = value;
            RecalculateGrid();
            InvalidateCanvas();
        }
    }

    /// <summary>The terminal font size in DIPs. Default 14.</summary>
    public float TerminalFontSize
    {
        get => _renderer.FontSize;
        set
        {
            _renderer.FontSize = value;
            RecalculateGrid();
            InvalidateCanvas();
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
            _renderer.Feed(data);
            UpdateScrollBar();
            RenderCanvasSupply.Invalidate(_canvas);
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
            _renderer.Feed(copy, copy.Length);
            UpdateScrollBar();
            RenderCanvasSupply.Invalidate(_canvas);
        });
    }

    /// <summary>
    /// Performs a full terminal reset (RIS) — a host clearing the screen
    /// between sessions calls this.
    /// </summary>
    public void Reset()
    {
        //RIS plus emptying the screen and the scrollback (see TerminalRenderer.Reset)
        _renderer.Reset();
        UpdateScrollBar();
        RenderCanvasSupply.Invalidate(_canvas);
    }

    /// <summary>Gives the control keyboard focus.</summary>
    public void GrabFocus() => Focus(FocusState.Programmatic);

    /// <summary>
    /// Gets the text of the rows the terminal shows now: one string per row, top to bottom, following the scroll
    /// position (a scrolled-back view returns the history rows it shows). Trailing blanks are removed; an empty row
    /// is an empty string. Read-only - a snapshot taken when called.
    /// </summary>
    /// <remarks>Call it on the UI thread: <see cref="Feed(string)"/> applies data there, so text fed from another
    /// thread appears once the UI thread has processed it.</remarks>
    /// <returns>The visible rows' text.</returns>
    public IReadOnlyList<string> GetVisibleLines() => _renderer.GetVisibleLines();

    /// <summary>
    /// Gets the text of the whole buffer - the scrollback (up to <see cref="Scrollback"/> rows) and the screen - as
    /// one string. Rows are separated by '\n'; a row the terminal wrapped is joined to the row it continues, so a
    /// long line reads back as one line. Trailing blanks and trailing empty lines are removed. Read-only - a
    /// snapshot taken when called.
    /// </summary>
    /// <remarks>Call it on the UI thread, as for <see cref="GetVisibleLines"/>.</remarks>
    /// <returns>The buffer's text; empty when nothing has been written (or after <see cref="Reset"/>).</returns>
    public string GetText() => _renderer.GetText();

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
        _renderer.PrepareForInput();
        InputEmitted?.Invoke(data);
        RenderCanvasSupply.Invalidate(_canvas);
    }

    private void UpdateScrollBar()
    {
        if (_verticalScrollBar == null) { return; }

        var terminal = _renderer.Terminal;
        var buffer = terminal.Buffer;

        _updatingScrollBar = true;
        try
        {
            _verticalScrollBar.Maximum = buffer.YBase;
            _verticalScrollBar.ViewportSize = terminal.Rows;
            _verticalScrollBar.LargeChange = Math.Max(1, terminal.Rows - 1);
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

        var delta = (int)Math.Round(e.NewValue) - _renderer.Terminal.Buffer.YDisp;
        if (delta != 0) { _renderer.ScrollLines(delta); }
    }

    private void RecalculateGrid()
    {
        if (_canvas.ActualWidth < 1 || _canvas.ActualHeight < 1) { return; }

        if (_renderer.FitToSize(_canvas.ActualWidth, _canvas.ActualHeight, out var cols, out var rows))
        {
            GridResized?.Invoke(cols, rows);
            UpdateScrollBar();
        }

        RenderCanvasSupply.Invalidate(_canvas);
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        var point = e.GetCurrentPoint(_canvas);

        //A finger: scroll, select or open the menu, decided as the contact goes on (Engine/TerminalTouchGesture)
        if (IsTouch(e))
        {
            _touch.Press(point.Position.X, point.Position.Y, Environment.TickCount64);
            _canvas.CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsRightButtonPressed)
        {
            ShowContextMenu(point.Position);
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            return;
        }

        //A double-click selects the word/expression; any other press starts a drag selection
        if (_renderer.PressAt(point.Position.X, point.Position.Y, Environment.TickCount64))
        {
            _canvas.CapturePointer(e.Pointer);
        }

        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (IsTouch(e) && _touch.State != TerminalTouchState.None)
        {
            var touchPosition = e.GetCurrentPoint(_canvas).Position;
            if (_touch.Move(touchPosition.X, touchPosition.Y, Environment.TickCount64) == TerminalTouchState.Selecting)
            {
                UpdateDragAutoScroll(touchPosition.Y);
            }

            e.Handled = true;
            return;
        }

        if (!_renderer.IsSelecting) { return; }

        var position = e.GetCurrentPoint(_canvas).Position;

        //Dragging beyond the top/bottom edge scrolls the view while held there
        if (position.Y < 0 || position.Y > _canvas.ActualHeight)
        {
            if (!_dragScrollTimer.IsEnabled) { _dragScrollTimer.Start(); }
        }
        else if (_dragScrollTimer.IsEnabled)
        {
            _dragScrollTimer.Stop();
        }

        _renderer.DragTo(position.X, position.Y);
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (IsTouch(e) && _touch.State != TerminalTouchState.None)
        {
            var lift = e.GetCurrentPoint(_canvas).Position;
            var outcome = _touch.Release(lift.X, lift.Y, Environment.TickCount64);
            _dragScrollTimer.Stop();
            _canvas.ReleasePointerCapture(e.Pointer);
            if (outcome == TerminalTouchOutcome.ContextMenu)
            {
                ShowContextMenu(lift);
            }

            e.Handled = true;
            return;
        }

        //A touch can lift beyond its last move (a mouse's last move is its release point), so the
        //selection is extended to where the pointer came up before the drag ends
        if (_renderer.IsSelecting)
        {
            var position = e.GetCurrentPoint(_canvas).Position;
            _renderer.DragTo(position.X, position.Y);
        }

        if (!_renderer.EndDrag()) { return; }

        _dragScrollTimer.Stop();
        _canvas.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private static bool IsTouch(PointerRoutedEventArgs e) =>
        e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch;

    /// <summary>The Copy/Paste menu at a point of the surface (right button, or a finger's long press).</summary>
    private void ShowContextMenu(global::Windows.Foundation.Point position)
    {
        _copyMenuItem.IsEnabled = _renderer.Selection.Active;
        _contextMenu.ShowAt(_canvas, new FlyoutShowOptions { Position = position });
    }

    /// <summary>A selection drag held beyond the top/bottom edge scrolls the view while it is held there.</summary>
    private void UpdateDragAutoScroll(double y)
    {
        if (y < 0 || y > _canvas.ActualHeight)
        {
            if (!_dragScrollTimer.IsEnabled) { _dragScrollTimer.Start(); }
        }
        else if (_dragScrollTimer.IsEnabled)
        {
            _dragScrollTimer.Stop();
        }
    }

    private void CopySelection()
    {
        var text = _renderer.GetSelectedText();
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
        _renderer.ScrollWheel(e.GetCurrentPoint(_canvas).Properties.MouseWheelDelta);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _renderer.IsFocused = true;
        _renderer.IsBlinkOn = true;
        RenderCanvasSupply.Invalidate(_canvas);
        //A focused terminal is typed into, so it summons the software keyboard
        //on heads that have one. Consume the guard's hold even when disabled,
        //so a suppressed round-trip can never leave it armed.
        var returningFromContextMenu = _keyboardGuard.ShouldSuppressFocus();
        if (!returningFromContextMenu && IsEnabled)
        {
            SoftwareKeyboardFocus.NotifyFocused(this);
        }
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _renderer.IsFocused = false;
        RenderCanvasSupply.Invalidate(_canvas);
        if (!_keyboardGuard.ShouldSuppressUnfocus())
        {
            SoftwareKeyboardFocus.NotifyUnfocused(this);
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e) =>
        _keyInput.UpdateModifier(VirtualKeyMapper.ToModifierKey(e.Key), isDown: false);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (_keyInput.UpdateModifier(VirtualKeyMapper.ToModifierKey(e.Key), isDown: true)) { return; }

        var key = VirtualKeyMapper.ToTerminalKey(e.Key);
        switch (_keyInput.GetCommand(key))
        {
            case TerminalKeyCommand.Copy:
                CopySelection();
                e.Handled = true;
                return;

            case TerminalKeyCommand.Paste:
                PasteFromClipboard();
                e.Handled = true;
                return;

            case TerminalKeyCommand.ScrollPageUp:
            case TerminalKeyCommand.ScrollPageDown:
                _renderer.ScrollPage(up: key == TerminalKey.PageUp);
                e.Handled = true;
                return;
        }

        //Printables prefer the platform's layout-composed character. UnicodeKey is
        //  internal framework API, reached via InternalsVisibleTo (the
        //  AdvancedTextEdit TextArea precedent).
        var encoded = _keyInput.Encode(key, e.UnicodeKey, _renderer.ApplicationCursor);
        if (encoded != null)
        {
            RaiseInput(encoded);
            e.Handled = true;
        }
        else if (e.UnicodeKey is { } half && char.IsSurrogate(half))
        {
            //The first half of a non-BMP character: the encoder holds it until the second half arrives, and the pair
            //  is emitted as one string then
            e.Handled = true;
        }
    }

    //The canvas comes from the platform's canvas supply (Internal/RenderCanvasSupply.cs); the null check covers the
    //  callbacks the constructor wires up before the canvas exists.
    private void InvalidateCanvas()
    {
        if (_canvas is not null)
        {
            RenderCanvasSupply.Invalidate(_canvas);
        }
    }
}
