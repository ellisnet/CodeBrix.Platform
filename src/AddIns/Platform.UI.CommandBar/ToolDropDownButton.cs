using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A tool bar button carrying a flyout - a menu of recent files behind an Open button, a list of
/// modes behind a Run button, or a plain chooser.
/// </summary>
/// <remarks>
/// <para>
/// How the button divides its behaviour between running its command and opening its flyout is
/// <see cref="PopupMode"/>. The flyout itself is any flyout, and it may be shared BY REFERENCE
/// with a menu bar, so one menu definition feeds both places.
/// </para>
/// <para>
/// Bindings inside the flyout are re-hooked after it closes, so a flyout that is opened, closed
/// and opened again keeps working - a flyout's items are unloaded on close, and anything that
/// unsubscribed then has to be subscribed again.
/// </para>
/// </remarks>
public partial class ToolDropDownButton : ToolButton
{
	/// <summary>The name of the arrow half of the control template.</summary>
	private const string ArrowPartName = "PART_Arrow";

	/// <summary>The press-and-hold delay a Delayed button uses when nothing else is asked for.</summary>
	public const double DefaultPressAndHoldMilliseconds = 600d;

	/// <summary>
	/// The timer that turns a held press into an opened flyout, created the first time a press is
	/// actually held: most buttons are never a Delayed one and should not pay for a timer.
	/// </summary>
	private DispatcherTimer? _holdTimer;

	/// <summary>The arrow half of the template, once the template has been applied.</summary>
	private FrameworkElement? _arrowPart;

	/// <summary>The flyout this button is currently subscribed to.</summary>
	private FlyoutBase? _subscribedFlyout;

	/// <summary>Whether a press is in progress at all.</summary>
	private bool _pressInProgress;

	/// <summary>Whether the flyout has already opened during this press, so the release does nothing.</summary>
	private bool _pressConsumedByFlyout;

	/// <summary>Initializes a new drop-down button.</summary>
	public ToolDropDownButton()
	{
		DefaultStyleKey = typeof(ToolDropDownButton);

		RegisterPropertyChangedCallback(FlyoutProperty, OnFlyoutChanged);
		RegisterPropertyChangedCallback(PopupModeProperty, OnPopupModeChanged);

		UpdateArrowVisibility();
	}

	/// <summary>
	/// Occurs after the flyout has closed and the bindings inside it have been re-hooked.
	/// </summary>
	/// <remarks>
	/// The moment to refresh whatever the menu shows - a recent-files list, for instance - is here
	/// rather than in the flyout's own Closed event, because by now the items are usable again.
	/// </remarks>
	public event TypedEventHandler<ToolDropDownButton, RoutedEventArgs>? FlyoutClosed;

	#region Flyout

	/// <summary>
	/// Identifies the <see cref="Flyout"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty FlyoutProperty =
		DependencyProperty.Register(
			nameof(Flyout),
			typeof(FlyoutBase),
			typeof(ToolDropDownButton),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the flyout this button opens.
	/// </summary>
	/// <remarks>
	/// Any flyout will do; a menu flyout is what a tool bar usually wants. The same flyout object
	/// may be referenced by a menu bar as well, so one definition of a menu serves both.
	/// </remarks>
	public FlyoutBase? Flyout
	{
		get => (FlyoutBase?)GetValue(FlyoutProperty);
		set => SetValue(FlyoutProperty, value);
	}

	#endregion

	#region PopupMode

	/// <summary>
	/// Identifies the <see cref="PopupMode"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty PopupModeProperty =
		DependencyProperty.Register(
			nameof(PopupMode),
			typeof(PopupMode),
			typeof(ToolDropDownButton),
			new FrameworkPropertyMetadata(PopupMode.MenuButton));

	/// <summary>
	/// Gets or sets how the button divides its behaviour between its command and its flyout.
	/// </summary>
	public PopupMode PopupMode
	{
		get => (PopupMode)GetValue(PopupModeProperty);
		set => SetValue(PopupModeProperty, value);
	}

	#endregion

	#region PressAndHoldDelay

	/// <summary>
	/// Identifies the <see cref="PressAndHoldDelay"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty PressAndHoldDelayProperty =
		DependencyProperty.Register(
			nameof(PressAndHoldDelay),
			typeof(TimeSpan),
			typeof(ToolDropDownButton),
			new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(DefaultPressAndHoldMilliseconds)));

	/// <summary>
	/// Gets or sets how long a <see cref="CommandBar.PopupMode.Delayed"/> button must be held
	/// before the flyout opens instead of the command running.
	/// </summary>
	/// <remarks>
	/// Six hundred milliseconds by default: long enough that an ordinary click is never mistaken
	/// for a hold, short enough that holding does not feel broken.
	/// </remarks>
	public TimeSpan PressAndHoldDelay
	{
		get => (TimeSpan)GetValue(PressAndHoldDelayProperty);
		set => SetValue(PressAndHoldDelayProperty, value);
	}

	#endregion

	#region Template contract

	/// <summary>
	/// Identifies the <see cref="ArrowVisibility"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ArrowVisibilityProperty =
		DependencyProperty.Register(
			nameof(ArrowVisibility),
			typeof(Visibility),
			typeof(ToolDropDownButton),
			new FrameworkPropertyMetadata(Visibility.Visible));

	/// <summary>
	/// Gets whether the arrow half of the template is shown.
	/// </summary>
	/// <remarks>
	/// Shown for the two modes where a visible target opens the flyout, and hidden for
	/// <see cref="CommandBar.PopupMode.Delayed"/>, where the flyout has no target of its own: the
	/// whole button is the target, and only when it is held.
	/// </remarks>
	public Visibility ArrowVisibility => (Visibility)GetValue(ArrowVisibilityProperty);

	#endregion

	/// <summary>
	/// Gets whether the flyout is open right now.
	/// </summary>
	public bool IsFlyoutOpen { get; private set; }

	/// <summary>What a press or a release should make the button do.</summary>
	internal enum DropDownAction
	{
		/// <summary>Do nothing yet.</summary>
		None,

		/// <summary>Run the button's command, as an ordinary click does.</summary>
		Invoke,

		/// <summary>Open the flyout.</summary>
		OpenFlyout,
	}

	/// <summary>Gets whether a press is being tracked, for the state machine's tests.</summary>
	internal bool IsPressInProgress => _pressInProgress;

	/// <summary>
	/// Decides what a press on one half of the button means.
	/// </summary>
	/// <param name="onArrow">Whether the press landed on the arrow half.</param>
	/// <returns>What to do now.</returns>
	/// <remarks>
	/// A menu opens on the press, not on the release - that is how every desktop menu behaves, and
	/// it is why the arrow half answers here rather than waiting for
	/// <see cref="CompletePress"/>.
	/// </remarks>
	internal DropDownAction BeginPress(bool onArrow)
	{
		_pressInProgress = true;
		_pressConsumedByFlyout = false;

		switch (PopupMode)
		{
			case PopupMode.Instant:
				_pressConsumedByFlyout = true;
				return DropDownAction.OpenFlyout;

			case PopupMode.MenuButton when onArrow:
				_pressConsumedByFlyout = true;
				return DropDownAction.OpenFlyout;

			case PopupMode.Delayed:
				StartHoldTimer();
				return DropDownAction.None;

			default:
				return DropDownAction.None;
		}
	}

	/// <summary>
	/// Decides what the release that ends a press means.
	/// </summary>
	/// <returns>What to do now.</returns>
	internal DropDownAction CompletePress()
	{
		StopHoldTimer();

		var wasPressing = _pressInProgress;
		var consumed = _pressConsumedByFlyout;

		_pressInProgress = false;
		_pressConsumedByFlyout = false;

		if (!wasPressing || consumed)
		{
			return DropDownAction.None;
		}

		return DropDownAction.Invoke;
	}

	/// <summary>
	/// Decides what the press-and-hold delay elapsing means.
	/// </summary>
	/// <returns>What to do now.</returns>
	internal DropDownAction HoldElapsed()
	{
		StopHoldTimer();

		if (!_pressInProgress || PopupMode != PopupMode.Delayed)
		{
			return DropDownAction.None;
		}

		//The command must not run as well: holding is how the user asked for the menu INSTEAD.
		_pressConsumedByFlyout = true;
		return DropDownAction.OpenFlyout;
	}

	/// <summary>Abandons the press being tracked, because the pointer left or capture was lost.</summary>
	internal void CancelPress()
	{
		StopHoldTimer();
		_pressInProgress = false;
		_pressConsumedByFlyout = false;
	}

	/// <summary>Opens the flyout at this button, if there is one.</summary>
	/// <remarks>
	/// Public because an application sometimes has to open the menu itself - from a keyboard
	/// shortcut, or after a step in a wizard.
	/// </remarks>
	public void OpenFlyout() => Flyout?.ShowAt(this);

	/// <summary>Closes the flyout, if it is open.</summary>
	public void CloseFlyout() => Flyout?.Hide();

	/// <summary>
	/// Re-hooks the command bindings of the items inside a menu flyout.
	/// </summary>
	/// <returns>The number of items that were re-hooked.</returns>
	/// <remarks>
	/// A flyout's items are unloaded when it closes, and an item that unsubscribed from its
	/// command's CanExecuteChanged at that moment never subscribes again: the second time the menu
	/// opens, its items no longer follow the command. Re-assigning each item's command puts the
	/// subscription back, and costs nothing when there was nothing wrong.
	/// </remarks>
	internal int RehookFlyoutBindings()
	{
		if (Flyout is not MenuFlyout menuFlyout)
		{
			return 0;
		}

		var rehooked = 0;
		RehookItems(menuFlyout.Items, ref rehooked);
		return rehooked;

		static void RehookItems(IList<MenuFlyoutItemBase> items, ref int rehooked)
		{
			foreach (var item in items)
			{
				switch (item)
				{
					case MenuFlyoutSubItem subItem:
						RehookItems(subItem.Items, ref rehooked);
						break;

					case MenuFlyoutItem menuItem when menuItem.Command is { } command:
						menuItem.Command = null;
						menuItem.Command = command;
						rehooked++;
						break;
				}
			}
		}
	}

	/// <inheritdoc/>
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolDropDownButtonAutomationPeer(this);

	/// <inheritdoc/>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_arrowPart = GetTemplateChild(ArrowPartName) as FrameworkElement;
	}

	/// <inheritdoc/>
	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		var action = BeginPress(IsOverArrowPart(args));

		if (action == DropDownAction.OpenFlyout)
		{
			//Not calling the base is what stops the press from also becoming a click: in these
			//modes the press belongs to the menu.
			args.Handled = true;
			OpenFlyout();
			return;
		}

		base.OnPointerPressed(args);
	}

	/// <inheritdoc/>
	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		var action = CompletePress();

		if (action == DropDownAction.None && PopupMode != PopupMode.MenuButton)
		{
			args.Handled = true;
			return;
		}

		base.OnPointerReleased(args);
	}

	/// <inheritdoc/>
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		CancelPress();
		base.OnPointerCaptureLost(args);
	}

	/// <inheritdoc/>
	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		CancelPress();
		base.OnPointerExited(args);
	}

	/// <summary>Gets whether a pointer event landed on the arrow half of the template.</summary>
	/// <param name="args">The pointer event.</param>
	/// <returns>True when the point is inside the arrow part.</returns>
	private bool IsOverArrowPart(PointerRoutedEventArgs args)
	{
		if (_arrowPart is null || _arrowPart.Visibility != Visibility.Visible)
		{
			return false;
		}

		var point = args.GetCurrentPoint(this).Position;
		var origin = _arrowPart.TransformToVisual(this).TransformPoint(new Point(0, 0));

		return point.X >= origin.X
			&& point.X <= origin.X + _arrowPart.ActualWidth
			&& point.Y >= origin.Y
			&& point.Y <= origin.Y + _arrowPart.ActualHeight;
	}

	/// <summary>Starts the press-and-hold timer.</summary>
	private void StartHoldTimer()
	{
		if (_holdTimer is null)
		{
			_holdTimer = new DispatcherTimer();
			_holdTimer.Tick += OnHoldTimerTick;
		}

		_holdTimer.Interval = PressAndHoldDelay;
		_holdTimer.Start();
	}

	/// <summary>Stops the press-and-hold timer.</summary>
	private void StopHoldTimer() => _holdTimer?.Stop();

	/// <summary>Handles the press-and-hold delay elapsing.</summary>
	/// <param name="sender">The timer.</param>
	/// <param name="args">Unused.</param>
	private void OnHoldTimerTick(object? sender, object args)
	{
		if (HoldElapsed() == DropDownAction.OpenFlyout)
		{
			OpenFlyout();
		}
	}

	/// <summary>Keeps the button subscribed to whichever flyout it currently owns.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="property">The Flyout property.</param>
	private void OnFlyoutChanged(DependencyObject sender, DependencyProperty property)
	{
		if (_subscribedFlyout is not null)
		{
			_subscribedFlyout.Opened -= OnFlyoutOpened;
			_subscribedFlyout.Closed -= OnFlyoutWasClosed;
		}

		_subscribedFlyout = Flyout;

		if (_subscribedFlyout is not null)
		{
			_subscribedFlyout.Opened += OnFlyoutOpened;
			_subscribedFlyout.Closed += OnFlyoutWasClosed;
		}
	}

	/// <summary>Notes that the flyout is open.</summary>
	/// <param name="sender">The flyout.</param>
	/// <param name="args">Unused.</param>
	private void OnFlyoutOpened(object? sender, object args) => IsFlyoutOpen = true;

	/// <summary>Re-hooks the flyout's bindings once it has closed.</summary>
	/// <param name="sender">The flyout.</param>
	/// <param name="args">Unused.</param>
	private void OnFlyoutWasClosed(object? sender, object args)
	{
		IsFlyoutOpen = false;
		CancelPress();
		RehookFlyoutBindings();
		FlyoutClosed?.Invoke(this, new RoutedEventArgs());
	}

	/// <summary>Shows or hides the arrow when the popup mode changes.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="property">The PopupMode property.</param>
	private void OnPopupModeChanged(DependencyObject sender, DependencyProperty property)
		=> UpdateArrowVisibility();

	/// <summary>Applies the arrow's visibility for the current popup mode.</summary>
	private void UpdateArrowVisibility()
		=> SetValue(
			ArrowVisibilityProperty,
			PopupMode == PopupMode.Delayed ? Visibility.Collapsed : Visibility.Visible);
}
