#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.TerminalView.Rendering;
using CodeBrix.Platform.UI.TextLayout;
using CodeBrix.Terminal.Engine;
using SkiaSharp;
using TerminalBuffer = CodeBrix.Terminal.Engine.Buffer;   //Required: 'Buffer' alone is ambiguous with System.Buffer
using TerminalEngine = CodeBrix.Terminal.Engine.Terminal; //Required: the simple name 'Terminal' binds to the
                                                          //  CodeBrix.Terminal NAMESPACE, not the engine type

namespace CodeBrix.Platform.UI.TerminalView.Engine;

/// <summary>
/// The terminal ENGINE (WPE1 C6): owns a CodeBrix.Terminal terminal and its selection, the font and the colour
/// palette, the cursor blink phase and the pointer selection gestures, and paints the cell grid onto an
/// <see cref="SKCanvas"/>. It names no XAML type: a host (TerminalControl on CodeBrix.Platform, a CodeBrix.Mobile
/// view) supplies the surface, the timers, the thread marshalling and the input events, and repaints when
/// <see cref="InvalidateRequested"/> is raised.
/// </summary>
/// <remarks>
/// Not thread-safe: a host calls every member on its UI thread (TerminalControl marshals Feed there).
/// </remarks>
internal sealed class TerminalRenderer
{
	/// <summary>The default terminal font: Roboto Mono from the RobotoMono fonts package the add-in depends on.</summary>
	internal const string DefaultFontFamily =
		"ms-appx:///CodeBrix.Platform.Fonts.RobotoMono/Fonts/RobotoMono.ttf";

	/// <summary>Two presses on the same cell closer together than this (ms) are a double-click.</summary>
	private const long DoubleClickMilliseconds = 400;

	private readonly TerminalEngine _terminal;
	private readonly SelectionService _selection;

	private CellMetrics? _metrics;
	private string _fontFamily = DefaultFontFamily;
	private float _fontSize = 14f;
	private bool _selecting;
	private (int Column, int Row) _lastDragCell = (-1, -1);
	private double _lastPointerX;
	private double _lastPointerY;
	private long _lastClickTick;
	private (int Column, int Row) _lastClickCell = (-1, -1);
	private SKRect _lastCaretRect;
	private bool _isLastCaretRectKnown;

	/// <summary>Creates the engine with an 80x25 terminal (a host resizes it with <see cref="FitToSize"/>).</summary>
	internal TerminalRenderer()
	{
		_terminal = new TerminalEngine(new EngineDelegate(this), new TerminalOptions
		{
			Cols = 80,
			Rows = 25,
			//Most terminal hosts feed explicit CR+LF; double conversion would add blank rows
			ConvertEol = false
		});

		_selection = new SelectionService(_terminal);
		_selection.SelectionChanged += () => RequestInvalidate();

		_terminal.Scrolled += (_, _) =>
		{
			Scrolled?.Invoke();
			RequestInvalidate();
		};
	}

	/// <summary>Raised when the grid must be repainted (the host invalidates its surface).</summary>
	internal event Action? InvalidateRequested;

	/// <summary>Raised when the viewport scrolled (the host updates its scroll bar), before the repaint request.</summary>
	internal event Action? Scrolled;

	/// <summary>Raised when the terminal title changes (OSC 0/2).</summary>
	internal event Action<string>? TitleChanged;

	/// <summary>Raised with the input the terminal itself sends back to the host application (status reports).</summary>
	internal event Action<string>? InputSent;

	/// <summary>
	/// Raised after an operation of this engine moved the caret rectangle (<see cref="GetCaretRect"/>): the cursor moved
	/// or was shown/hidden by fed data, the viewport scrolled, the grid was refitted, the font changed, or a reset.
	/// Only computed while something is subscribed (a platform that places its keyboard focus view on the caret), so a
	/// host without a subscriber does exactly the work it did before.
	/// </summary>
	internal event Action? CaretRectChanged;

	/// <summary>The terminal (buffer, modes, scrolling).</summary>
	internal TerminalEngine Terminal => _terminal;

	/// <summary>The selection over the terminal buffer.</summary>
	internal SelectionService Selection => _selection;

	/// <summary>The current column count.</summary>
	internal int Columns => _terminal.Cols;

	/// <summary>The current row count.</summary>
	internal int Rows => _terminal.Rows;

	/// <summary>Whether the cursor keys are in application mode (DECCKM), for the key encoder.</summary>
	internal bool ApplicationCursor => _terminal.ApplicationCursor;

	/// <summary>The translucent overlay painted over selected cells.</summary>
	internal SKColor SelectionColor { get; set; } = new(0x4d, 0x8b, 0xd8, 0x66);

	/// <summary>The default text colour.</summary>
	internal SKColor ForegroundColor { get; set; } = new(0xff, 0xff, 0xff);

	/// <summary>The terminal background.</summary>
	internal SKColor BackgroundColor { get; set; } = new(0x00, 0x00, 0x00);

	/// <summary>Whether the host has keyboard focus: a focused terminal shows a blinking block cursor, else a hollow one.</summary>
	internal bool IsFocused { get; set; }

	/// <summary>The cursor blink phase (true = the block is shown).</summary>
	internal bool IsBlinkOn { get; set; } = true;

	/// <summary>Whether a bare LF in fed data is treated as CRLF.</summary>
	internal bool ConvertEol
	{
		get => _terminal.Options.ConvertEol;
		set => _terminal.Options.ConvertEol = value;
	}

	/// <summary>The number of scrollback lines kept beyond the visible rows.</summary>
	internal int Scrollback
	{
		get => _terminal.Options.Scrollback ?? 0;
		set => _terminal.Options.Scrollback = Math.Max(0, value);
	}

	/// <summary>The font family (a font URI or family name the text engine understands); blank = the default.</summary>
	internal string FontFamily
	{
		get => _fontFamily;
		set
		{
			_fontFamily = string.IsNullOrWhiteSpace(value) ? DefaultFontFamily : value;
			_metrics = null;
			NotifyCaretRectIfChanged();
		}
	}

	/// <summary>The font size in device-independent pixels (at least 4).</summary>
	internal float FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value > 4f ? value : 4f;
			_metrics = null;
			NotifyCaretRectIfChanged();
		}
	}

	/// <summary>The cell geometry of the current font (measured once per font change).</summary>
	internal CellMetrics Metrics => _metrics ??= CellMetrics.Measure(_fontFamily, _fontSize);

	/// <summary>Whether a drag selection is in progress.</summary>
	internal bool IsSelecting => _selecting;

	/// <summary>The last pointer Y a drag reported (above 0 / below the height means "auto-scroll that way").</summary>
	internal double LastPointerY => _lastPointerY;

	/// <summary>Feeds VT output text into the terminal.</summary>
	/// <param name="data">The text.</param>
	internal void Feed(string data)
	{
		_terminal.Feed(data);
		NotifyCaretRectIfChanged();
	}

	/// <summary>Feeds VT output bytes into the terminal.</summary>
	/// <param name="data">The bytes.</param>
	/// <param name="length">How many of them.</param>
	internal void Feed(byte[] data, int length)
	{
		_terminal.Feed(data, length);
		NotifyCaretRectIfChanged();
	}

	/// <summary>
	/// A full reset (RIS) that also empties the screen and the scrollback and clears the selection - the state a
	/// freshly constructed terminal is in.
	/// </summary>
	internal void Reset()
	{
		_terminal.Reset();

		//The engine's RIS restores the modes but leaves the screen and the
		//scrollback exactly as they were, so a reset on its own would hand the
		//next session the previous one's output. Emptying the active buffer is
		//what "reset to initial state" means to the person looking at the
		//control - and it is the state a freshly constructed buffer is in, since
		//the engine's own Buffer constructor starts by calling Clear().
		_terminal.Buffer.Clear();

		_selection.SelectNone();
		NotifyCaretRectIfChanged();
	}

	/// <summary>Input is about to be sent: the view snaps back to the live tail and the cursor shows.</summary>
	internal void PrepareForInput()
	{
		//Typing snaps the view back to the live tail, like every terminal
		_terminal.ScrollToBottom();
		IsBlinkOn = true;
		NotifyCaretRectIfChanged();
	}

	/// <summary>Scrolls the viewport by whole lines (negative = back into history).</summary>
	/// <param name="delta">The line count.</param>
	internal void ScrollLines(int delta)
	{
		_terminal.ScrollLines(delta);
		NotifyCaretRectIfChanged();
	}

	/// <summary>Scrolls one page (the rows less one) back into history or forward.</summary>
	/// <param name="up">True for back into history.</param>
	internal void ScrollPage(bool up)
	{
		var page = Math.Max(1, _terminal.Rows - 1);
		_terminal.ScrollLines(up ? -page : page);
		NotifyCaretRectIfChanged();
	}

	/// <summary>Scrolls for a mouse wheel delta (120 per notch; positive = back into history, three lines a notch).</summary>
	/// <param name="wheelDelta">The wheel delta.</param>
	internal void ScrollWheel(int wheelDelta)
	{
		//Wheel up (positive delta) scrolls back into history
		_terminal.ScrollLines(-(wheelDelta / 120 * 3));
		NotifyCaretRectIfChanged();
	}

	/// <summary>The grid (columns, rows) that fits a surface of the given size in the current font.</summary>
	/// <param name="width">The surface width in DIPs.</param>
	/// <param name="height">The surface height in DIPs.</param>
	/// <returns>The column and row counts (at least 4 x 2).</returns>
	internal (int Columns, int Rows) MeasureGrid(double width, double height)
	{
		var cell = Metrics;
		return (Math.Max(4, (int)(width / cell.Width)), Math.Max(2, (int)(height / cell.Height)));
	}

	/// <summary>
	/// Resizes the terminal to the grid that fits a surface of the given size, keeping a view that was at the live
	/// tail there. Returns true when the grid changed (the host then reports the new size to its transport).
	/// </summary>
	/// <param name="width">The surface width in DIPs.</param>
	/// <param name="height">The surface height in DIPs.</param>
	/// <param name="columns">The grid's column count afterwards.</param>
	/// <param name="rows">The grid's row count afterwards.</param>
	/// <returns>Whether the grid changed.</returns>
	internal bool FitToSize(double width, double height, out int columns, out int rows)
	{
		(columns, rows) = MeasureGrid(width, height);

		if (columns == _terminal.Cols && rows == _terminal.Rows) { return false; }

		var wasAtBottom = _terminal.IsAtBottom;
		_terminal.Resize(columns, rows);
		if (wasAtBottom) { _terminal.ScrollToBottom(); }
		NotifyCaretRectIfChanged();
		return true;
	}

	/// <summary>Maps a point in DIPs to a (column, viewport row) cell, clamped to the grid.</summary>
	/// <param name="x">The X coordinate.</param>
	/// <param name="y">The Y coordinate.</param>
	/// <returns>The cell.</returns>
	internal (int Column, int Row) HitTest(double x, double y) =>
		SelectionGeometry.ToCell(x, y, Metrics, _terminal.Cols, _terminal.Rows);

	/// <summary>
	/// A primary-button press at a point: a second press on the same cell within the double-click time selects the
	/// word or expression there; any other press starts a drag selection. Returns true when a drag started (the host
	/// captures the pointer).
	/// </summary>
	/// <param name="x">The X coordinate in DIPs.</param>
	/// <param name="y">The Y coordinate in DIPs.</param>
	/// <param name="tickMilliseconds">The press time (a monotonic millisecond clock, e.g. Environment.TickCount64).</param>
	/// <returns>Whether a drag selection started.</returns>
	internal bool PressAt(double x, double y, long tickMilliseconds)
	{
		var cell = HitTest(x, y);

		if (tickMilliseconds - _lastClickTick < DoubleClickMilliseconds && cell == _lastClickCell)
		{
			//Double-click: word/expression selection. NOTE the engine's
			//  (col, row) parameter order - unlike its (row, col) siblings.
			_selection.SelectWordOrExpression(cell.Column, cell.Row);
			_lastClickTick = 0;
			return false;
		}

		StartSelectionAt(x, y);
		_lastClickTick = tickMilliseconds;
		_lastClickCell = cell;
		return true;
	}

	/// <summary>
	/// Starts a drag selection at a point, clearing any selection there was, with no double-click check (a touch
	/// gesture that has already decided it is a selection uses this; a press uses <see cref="PressAt"/>).
	/// </summary>
	/// <param name="x">The X coordinate in DIPs.</param>
	/// <param name="y">The Y coordinate in DIPs.</param>
	internal void StartSelectionAt(double x, double y)
	{
		var cell = HitTest(x, y);
		if (_selection.Active) { _selection.SelectNone(); }
		_selection.SetSoftStart(cell.Row, cell.Column);
		_selecting = true;
		_lastDragCell = cell;
	}

	/// <summary>A drag selection moved to a point (DIPs; may lie outside the surface).</summary>
	/// <param name="x">The X coordinate.</param>
	/// <param name="y">The Y coordinate.</param>
	internal void DragTo(double x, double y)
	{
		if (!_selecting) { return; }

		_lastPointerX = x;
		_lastPointerY = y;
		ExtendSelectionTo(x, y);
	}

	/// <summary>Ends a drag selection. Returns false when none was in progress.</summary>
	/// <returns>Whether a drag selection ended.</returns>
	internal bool EndDrag()
	{
		if (!_selecting) { return false; }

		_selecting = false;
		return true;
	}

	/// <summary>
	/// One auto-scroll step of a drag held beyond the top/bottom edge: scrolls one line that way and extends the
	/// selection. Returns false when no drag is in progress (the host stops its auto-scroll timer).
	/// </summary>
	/// <returns>Whether a step was taken.</returns>
	internal bool AutoScrollDrag()
	{
		if (!_selecting) { return false; }

		//Above the top edge scrolls back into history; below scrolls forward
		_terminal.ScrollLines(_lastPointerY < 0 ? -1 : 1);
		ExtendSelectionTo(_lastPointerX, _lastPointerY);
		NotifyCaretRectIfChanged();
		return true;
	}

	/// <summary>The selected text, or null when nothing is selected.</summary>
	/// <returns>The text.</returns>
	internal string? GetSelectedText() => _selection.Active ? _selection.GetSelectedText() : null;

	/// <summary>Paints the grid, the selection and the cursor.</summary>
	/// <param name="canvas">The canvas, scaled so one unit is one DIP.</param>
	/// <param name="size">The paintable size in DIPs.</param>
	internal void Paint(SKCanvas canvas, SKSize size)
	{
		canvas.Clear(BackgroundColor);

		var cell = Metrics;
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

	/// <summary>
	/// The cursor cell's rectangle in the surface's DIPs (the coordinates <see cref="Paint"/> draws in): column x cell
	/// width, viewport row (the cursor's buffer line less the scroll offset) x cell height, one cell in size - the
	/// block the cursor is painted as. <see cref="SKRect.Empty"/> when the cursor is hidden (DECTCEM) or its line is
	/// scrolled out of the viewport.
	/// </summary>
	/// <returns>The caret rectangle, or an empty rectangle when there is no visible caret.</returns>
	internal SKRect GetCaretRect()
	{
		if (_terminal.CursorHidden) { return SKRect.Empty; }

		var buffer = _terminal.Buffer;
		var screenRow = buffer.YBase + buffer.Y - buffer.YDisp;
		if (screenRow < 0 || screenRow >= _terminal.Rows) { return SKRect.Empty; }

		//The same cell DrawCursor paints
		var cell = Metrics;
		return SKRect.Create(buffer.X * cell.Width, screenRow * cell.Height, cell.Width, cell.Height);
	}

	private void RequestInvalidate() => InvalidateRequested?.Invoke();

	//Raises CaretRectChanged when the caret rectangle differs from the last one reported. Nothing is computed (and the
	//  font is not measured) while no one listens; the first computation after a subscriber appears always reports.
	private void NotifyCaretRectIfChanged()
	{
		var handler = CaretRectChanged;
		if (handler is null)
		{
			_isLastCaretRectKnown = false;
			return;
		}

		var rect = GetCaretRect();
		if (_isLastCaretRectKnown && rect == _lastCaretRect) { return; }

		_lastCaretRect = rect;
		_isLastCaretRectKnown = true;
		handler();
	}

	private void ExtendSelectionTo(double x, double y)
	{
		var cell = HitTest(x, y);
		if (cell == _lastDragCell && _selection.Active) { return; }

		if (!_selection.Active) { _selection.StartSelection(); }
		_selection.DragExtend(cell.Row, cell.Column);
		_lastDragCell = cell;
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

		if (!IsFocused)
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

		if (!IsBlinkOn) { return; }

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

	private sealed class EngineDelegate : ITerminalDelegate
	{
		private readonly TerminalRenderer _owner;

		public EngineDelegate(TerminalRenderer owner) => _owner = owner;

		public void ShowCursor(TerminalEngine source) => _owner.RequestInvalidate();

		public void SetTerminalTitle(TerminalEngine source, string title) =>
			_owner.TitleChanged?.Invoke(title);

		public void SetTerminalIconTitle(TerminalEngine source, string title)
		{
		}

		public void SizeChanged(TerminalEngine source)
		{
			//Escape-sequence-driven resize is not supported; the grid follows the host's size
		}

		public void Send(byte[] data) =>
			_owner.InputSent?.Invoke(Encoding.UTF8.GetString(data));

		public string? WindowCommand(TerminalEngine source, WindowManipulationCommand command,
			params int[] args) => null;

		public bool IsProcessTrusted() => true;
	}
}
