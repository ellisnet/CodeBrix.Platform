#nullable enable

using System;
using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Platform.UI.PlotterView.Rendering;
using CodeBrix.Plotter;
using CodeBrix.Plotter.Skia;
using SkiaSharp;
using PlotterHorizontalAlignment = CodeBrix.Plotter.HorizontalAlignment; //Explicit: the enclosing namespaces'
using PlotterVerticalAlignment = CodeBrix.Plotter.VerticalAlignment;     //  simple-name lookup never reaches XAML enums

namespace CodeBrix.Platform.UI.PlotterView.Engine;

/// <summary>
/// The chart ENGINE (WPE1 C7): hosts a CodeBrix.Plotter <see cref="PlotModel"/> for a view - attaches the model, renders
/// it (and the tracker box and zoom rectangle) onto an <see cref="SKCanvas"/> through the Plotter's
/// <see cref="SkiaRenderContext"/>, resolves the chart typefaces from the application's own fonts (the platform's font
/// source; <see cref="PlotFontFamily"/> is the font family parameter), keeps the tracker/zoom/cursor state, and turns
/// input (mouse, touch through <see cref="TouchGestureTracker"/>, click counting through <see cref="ClickCounter"/>,
/// keys) into controller calls. It names no XAML type: the host view (PlotterControl on CodeBrix.Platform, a
/// CodeBrix.Mobile view) implements <see cref="IPlotView"/> by forwarding to this engine, supplies the surface, maps its
/// platform's input onto the Plotter's neutral types, repaints when asked (<c>invalidate</c>) and runs work on its UI
/// thread (<c>marshal</c>).
/// </summary>
/// <remarks>
/// Not thread-safe except <see cref="MarkForUpdate"/>, which the model's InvalidatePlot reaches from any thread.
/// </remarks>
internal sealed class PlotHost
{
	private readonly IPlotView _view;
	private readonly Action _invalidate;
	private readonly Func<Action, bool> _marshal;
	private readonly AppFontTypefaceResolver _fontResolver;
	private readonly object _invalidateLock = new();

	private PlotModel? _model;
	private SkiaRenderContext? _renderContext;
	private IPlotController? _defaultController;
	private TrackerHitResult? _tracker;
	private PlotterRect? _zoomRectangle;
	private bool _updateRequired;
	private bool _updateDataRequired;
	private CursorType _cursorType = CursorType.Default;
	private PlotterModifierKeys _modifiers;
	private double _trackerFontSize = 12;

	/// <summary>Creates the engine for a view.</summary>
	/// <param name="view">The view the model attaches to and the controller receives (the host's IPlotView).</param>
	/// <param name="invalidate">Requests a repaint of the host's surface (any thread).</param>
	/// <param name="marshal">Runs an action on the host's UI thread; false when that is not possible (then the action
	/// is dropped, as a view without a dispatcher drops it).</param>
	internal PlotHost(IPlotView view, Action invalidate, Func<Action, bool> marshal)
	{
		_view = view ?? throw new ArgumentNullException(nameof(view));
		_invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
		_marshal = marshal ?? throw new ArgumentNullException(nameof(marshal));

		_fontResolver = new AppFontTypefaceResolver();
		_fontResolver.FontLoaded += OnFontLoaded;
	}

	/// <summary>The attached model (see <see cref="SetModel"/>).</summary>
	internal PlotModel? Model => _model;

	/// <summary>The controller the host set (null = the default <see cref="PlotController"/>).</summary>
	internal IPlotController? Controller { get; set; }

	/// <summary>The controller in effect: <see cref="Controller"/>, or the lazily created default.</summary>
	internal IController ActualController => Controller ?? (_defaultController ??= new PlotController());

	/// <summary>The click counter for mouse presses (double-click detection).</summary>
	internal ClickCounter ClickCounter { get; } = new();

	/// <summary>The touch contacts (single-finger pan, two-finger pinch).</summary>
	internal TouchGestureTracker TouchTracker { get; } = new();

	/// <summary>The modifier keys currently held (see <see cref="UpdateModifier"/>).</summary>
	internal PlotterModifierKeys Modifiers => _modifiers;

	/// <summary>The tracker currently shown, or null.</summary>
	internal TrackerHitResult? Tracker => _tracker;

	/// <summary>The zoom rectangle currently shown, or null.</summary>
	internal PlotterRect? ZoomRectangle => _zoomRectangle;

	/// <summary>The cursor the controller last asked for.</summary>
	internal CursorType CursorType => _cursorType;

	/// <summary>
	/// The font family (an application font URI) plot text renders in when the model names no loadable application
	/// font; null = the application's default font. Setting it re-resolves every typeface.
	/// </summary>
	internal string? PlotFontFamily
	{
		get => _fontResolver.PlotFontFamily;
		set
		{
			_fontResolver.PlotFontFamily = value;
			ResetFontResolution();
		}
	}

	/// <summary>The tracker box fill.</summary>
	internal PlotterColor TrackerBackground { get; set; } = PlotterColor.FromArgb(0xE6, 0x2D, 0x2D, 0x30);

	/// <summary>The tracker text colour.</summary>
	internal PlotterColor TrackerForeground { get; set; } = PlotterColors.White;

	/// <summary>The tracker text size in DIPs (at least 4).</summary>
	internal double TrackerFontSize
	{
		get => _trackerFontSize;
		set => _trackerFontSize = value > 4 ? value : 4;
	}

	/// <summary>The zoom rectangle fill.</summary>
	internal PlotterColor ZoomRectangleFill { get; set; } = PlotterColor.FromArgb(0x40, 0xFF, 0xFF, 0x00);

	/// <summary>The zoom rectangle border colour.</summary>
	internal PlotterColor ZoomRectangleStroke { get; set; } = PlotterColors.Black;

	/// <summary>
	/// Replaces the model: detaches the old one from the view, attaches the new one (a model can be attached to only
	/// one view at a time) and drops the tracker and zoom rectangle. The host then invalidates with a data update.
	/// </summary>
	/// <param name="oldModel">The model shown until now.</param>
	/// <param name="newModel">The model to show.</param>
	internal void SetModel(PlotModel? oldModel, PlotModel? newModel)
	{
		if (oldModel != null)
		{
			((IPlotModel)oldModel).AttachPlotView(null);
		}

		_model = newModel;

		if (newModel != null)
		{
			((IPlotModel)newModel).AttachPlotView(_view);
		}

		_tracker = null;
		_zoomRectangle = null;
	}

	/// <summary>Records that the next paint must update the model (and re-read its data when asked). Any thread.</summary>
	/// <param name="updateData">Whether the data sources changed.</param>
	internal void MarkForUpdate(bool updateData)
	{
		lock (_invalidateLock)
		{
			_updateRequired = true;
			_updateDataRequired |= updateData;
		}
	}

	/// <summary>Shows a tracker (the host repaints).</summary>
	/// <param name="trackerHitResult">The tracker.</param>
	internal void ShowTracker(TrackerHitResult trackerHitResult) => _tracker = trackerHitResult;

	/// <summary>Hides the tracker. Returns false when none was shown (no repaint needed).</summary>
	/// <returns>Whether a tracker was hidden.</returns>
	internal bool HideTracker()
	{
		if (_tracker == null)
		{
			return false;
		}

		_tracker = null;
		return true;
	}

	/// <summary>Shows the zoom rectangle (the host repaints).</summary>
	/// <param name="rectangle">The rectangle.</param>
	internal void ShowZoomRectangle(PlotterRect rectangle) => _zoomRectangle = rectangle;

	/// <summary>Hides the zoom rectangle. Returns false when none was shown (no repaint needed).</summary>
	/// <returns>Whether a zoom rectangle was hidden.</returns>
	internal bool HideZoomRectangle()
	{
		if (_zoomRectangle == null)
		{
			return false;
		}

		_zoomRectangle = null;
		return true;
	}

	/// <summary>Records the cursor the controller asks for. Returns false when it is the current one already.</summary>
	/// <param name="cursorType">The cursor.</param>
	/// <returns>Whether the cursor changed (the host then shows it).</returns>
	internal bool SetCursorType(CursorType cursorType)
	{
		if (cursorType == _cursorType)
		{
			return false;
		}

		_cursorType = cursorType;
		return true;
	}

	/// <summary>Records a modifier key going down or up. Returns false for <see cref="PlotterModifierKeys.None"/>.</summary>
	/// <param name="modifier">The modifier (one flag).</param>
	/// <param name="isDown">True for key down.</param>
	/// <returns>Whether the key was a modifier (the host does nothing else with it).</returns>
	internal bool UpdateModifier(PlotterModifierKeys modifier, bool isDown)
	{
		if (modifier == PlotterModifierKeys.None)
		{
			return false;
		}

		_modifiers = isDown ? _modifiers | modifier : _modifiers & ~modifier;
		return true;
	}

	/// <summary>Paints the model, the zoom rectangle and the tracker.</summary>
	/// <param name="canvas">The canvas, scaled so one unit is one DIP (and already cleared by the host).</param>
	/// <param name="size">The paintable size in DIPs.</param>
	internal void Paint(SKCanvas canvas, SKSize size)
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

		var model = _model;
		if (model == null)
		{
			return; //the host's surface has already been cleared transparent
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

	/// <summary>A touch contact went down; the first contact starts a touch gesture.</summary>
	/// <param name="position">Where (DIPs).</param>
	internal void TouchStarted(ScreenPoint position)
	{
		var touchArgs = new PlotterTouchEventArgs
		{
			Position = position,
			DeltaTranslation = new ScreenVector(0, 0),
			DeltaScale = new ScreenVector(1, 1),
			ModifierKeys = _modifiers,
		};
		ActualController.HandleTouchStarted(_view, touchArgs);
	}

	/// <summary>A touch contact moved. Returns false when the contact is not a tracked one.</summary>
	/// <param name="pointerId">The contact.</param>
	/// <param name="position">Where (DIPs).</param>
	/// <returns>Whether the move was a gesture delta (handled).</returns>
	internal bool TouchMoved(uint pointerId, ScreenPoint position)
	{
		if (!TouchTracker.Move(pointerId, position, out var current, out var previous))
		{
			return false;
		}

		var touchArgs = new PlotterTouchEventArgs(current, previous)
		{
			ModifierKeys = _modifiers,
		};
		ActualController.HandleTouchDelta(_view, touchArgs);
		return true;
	}

	/// <summary>The last touch contact went up (or was canceled): completes the touch gesture.</summary>
	/// <param name="position">Where (DIPs).</param>
	internal void TouchCompleted(ScreenPoint position)
	{
		var touchArgs = new PlotterTouchEventArgs
		{
			Position = position,
			ModifierKeys = _modifiers,
		};
		ActualController.HandleTouchCompleted(_view, touchArgs);
	}

	/// <summary>A mouse button went down (the click count comes from <see cref="ClickCounter"/>).</summary>
	/// <param name="button">The button.</param>
	/// <param name="position">Where (DIPs).</param>
	/// <param name="tickMilliseconds">When (a monotonic millisecond clock).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseDown(PlotterMouseButton button, ScreenPoint position, long tickMilliseconds)
	{
		var args = new PlotterMouseDownEventArgs
		{
			ChangedButton = button,
			ClickCount = ClickCounter.Register(tickMilliseconds, position.X, position.Y),
			Position = position,
			ModifierKeys = _modifiers,
		};
		ActualController.HandleMouseDown(_view, args);
		return args.Handled;
	}

	/// <summary>The mouse moved.</summary>
	/// <param name="position">Where (DIPs).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseMove(ScreenPoint position)
	{
		var args = NewMouseArgs(position);
		ActualController.HandleMouseMove(_view, args);
		return args.Handled;
	}

	/// <summary>A mouse button went up.</summary>
	/// <param name="position">Where (DIPs).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseUp(ScreenPoint position)
	{
		var args = NewMouseArgs(position);
		ActualController.HandleMouseUp(_view, args);
		return args.Handled;
	}

	/// <summary>The mouse entered the surface.</summary>
	/// <param name="position">Where (DIPs).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseEnter(ScreenPoint position)
	{
		var args = NewMouseArgs(position);
		ActualController.HandleMouseEnter(_view, args);
		return args.Handled;
	}

	/// <summary>The mouse left the surface.</summary>
	/// <param name="position">Where (DIPs).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseLeave(ScreenPoint position)
	{
		var args = NewMouseArgs(position);
		ActualController.HandleMouseLeave(_view, args);
		return args.Handled;
	}

	/// <summary>The mouse wheel turned.</summary>
	/// <param name="position">Where (DIPs).</param>
	/// <param name="delta">The wheel delta (120 per notch).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool MouseWheel(ScreenPoint position, int delta)
	{
		var args = new PlotterMouseWheelEventArgs
		{
			Position = position,
			Delta = delta,
			ModifierKeys = _modifiers,
		};
		ActualController.HandleMouseWheel(_view, args);
		return args.Handled;
	}

	/// <summary>A (non-modifier) key went down.</summary>
	/// <param name="key">The key (<see cref="PlotterKey.Unknown"/> is ignored).</param>
	/// <returns>Whether the controller handled it.</returns>
	internal bool KeyDown(PlotterKey key)
	{
		if (key == PlotterKey.Unknown)
		{
			return false;
		}

		var args = new PlotterKeyEventArgs
		{
			Key = key,
			ModifierKeys = _modifiers,
		};
		ActualController.HandleKeyDown(_view, args);
		return args.Handled;
	}

	private PlotterMouseEventArgs NewMouseArgs(ScreenPoint position) => new()
	{
		Position = position,
		ModifierKeys = _modifiers,
	};

	private SkiaRenderContext EnsureRenderContext()
	{
		if (_renderContext == null)
		{
			_renderContext = new SkiaRenderContext
			{
				RenderTarget = RenderTarget.Screen,
				UseTextShaping = true,
				MiterLimit = 10,
				//The host pre-scales its canvas so one unit is one DIP; the display
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
		_marshal(() =>
		{
			ResetFontResolution();
			MarkForUpdate(false);
			_invalidate();
		});
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
		var fontFamily = _model?.DefaultFont;
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
}
