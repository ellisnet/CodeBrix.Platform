using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.CommandBar.Automation;
using CodeBrix.Platform.UI.CommandBar.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A row (or column) of tool bar items: buttons, groups, separators, spacers, and any other
/// control an application wants to put inline.
/// </summary>
/// <remarks>
/// <para>
/// The bar is an items control over plain UI elements, so its items are whatever the application
/// writes: this add-in's <see cref="ToolButton"/>, <see cref="ToolToggleButton"/> and
/// <see cref="ToolDropDownButton"/>, its <see cref="ToolBarGroup"/>,
/// <see cref="ToolBarSeparator"/> and <see cref="ToolBarSpacer"/>, or an ordinary
/// <c>ComboBox</c>, <c>TextBox</c> or anything else, hosted inline and centred across the bar.
/// </para>
/// <para>
/// Presentation settings that belong to the whole bar - icon size, label mode, label position,
/// whether tooltips are shown - are the inherited attached properties on
/// <see cref="ToolBarProperties"/>, so an item can override any of them for itself. The bar's own
/// <see cref="IconSize"/>, <see cref="LabelMode"/>, <see cref="LabelPosition"/> and
/// <see cref="ShowToolTips"/> properties ARE those attached properties rather than copies of them,
/// so a bar that says nothing lets a value set higher up - on a <see cref="ToolBarTray"/> or on the
/// page - through untouched, and a bar that does say something is what its items inherit. To bind
/// one of them, bind the attached form (<c>cb:ToolBarProperties.LabelMode="{Binding ...}"</c>).
/// </para>
/// <para>
/// When <see cref="OverflowMode"/> is <see cref="OverflowMode.Chevron"/> the trailing items that do
/// not fit are moved, in order, into a flyout behind a chevron button, and moved back when the
/// space returns. They are the SAME element instances throughout - never copies - so bindings,
/// event handlers, toggle states and focus all survive the trip. Items whose
/// <see cref="UIElement.Visibility"/> is <see cref="Visibility.Collapsed"/> take no space and are
/// never counted as overflowing.
/// </para>
/// <para>
/// The bar lays its items out itself, in <see cref="ToolBarPanel"/>, rather than in an items
/// presenter: overflow means moving elements between two panels, which the items presenter's own
/// child management would undo. <see cref="ItemsControl.ItemsPanel"/> is therefore not used;
/// re-template the bar to change the layout.
/// </para>
/// </remarks>
public partial class ToolBar : ItemsControl
{
	/// <summary>The gap a bar leaves between two items when nothing sets one: 4 pixels.</summary>
	public const double DefaultItemSpacing = 4d;

	/// <summary>The name of the panel in the bar's template that holds the items.</summary>
	public const string ItemsHostPartName = "PART_ItemsHost";

	private const double FallbackChevronExtent = 28d;

	private readonly List<UIElement> _layoutItems = new();
	private readonly List<ContentPresenter> _wrapperPool = new();

	private ToolBarPanel? _itemsHost;
	private ToolBarPanel? _overflowHost;
	private ToolBarOverflowButton? _overflowButton;
	private Flyout? _overflowFlyout;
	private int _splitIndex = -1;

	/// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ToolBar),
			new PropertyMetadata(Orientation.Horizontal, OnStructuralPropertyChanged));

	/// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(
			nameof(Title),
			typeof(string),
			typeof(ToolBar),
			new PropertyMetadata(string.Empty, OnTitleChanged));

	/// <summary>Identifies the <see cref="ItemSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty ItemSpacingProperty =
		DependencyProperty.Register(
			nameof(ItemSpacing),
			typeof(double),
			typeof(ToolBar),
			new PropertyMetadata(DefaultItemSpacing, OnStructuralPropertyChanged));

	/// <summary>Identifies the <see cref="IsCompact"/> dependency property.</summary>
	public static readonly DependencyProperty IsCompactProperty =
		DependencyProperty.Register(
			nameof(IsCompact),
			typeof(bool),
			typeof(ToolBar),
			new PropertyMetadata(false, OnIsCompactChanged));

	/// <summary>Identifies the <see cref="OverflowMode"/> dependency property.</summary>
	public static readonly DependencyProperty OverflowModeProperty =
		DependencyProperty.Register(
			nameof(OverflowMode),
			typeof(OverflowMode),
			typeof(ToolBar),
			new PropertyMetadata(OverflowMode.Chevron, OnStructuralPropertyChanged));

	/// <summary>Identifies the <see cref="SeparatorBetweenGroups"/> dependency property.</summary>
	public static readonly DependencyProperty SeparatorBetweenGroupsProperty =
		DependencyProperty.Register(
			nameof(SeparatorBetweenGroups),
			typeof(bool),
			typeof(ToolBar),
			new PropertyMetadata(true, OnItemsStructureChanged));

	/// <summary>Identifies the <see cref="HasOverflowItems"/> dependency property.</summary>
	public static readonly DependencyProperty HasOverflowItemsProperty =
		DependencyProperty.Register(
			nameof(HasOverflowItems),
			typeof(bool),
			typeof(ToolBar),
			new PropertyMetadata(false));

	//THE FOUR BAR SETTINGS ARE THE ATTACHED PROPERTIES THEMSELVES, not copies of them, and they
	//must stay that way. The framework propagates an inherited attached property to a child by
	//looking for a property WITH THE SAME NAME on the child's type
	//(DependencyObjectStore.GetLocalPropertyDetails: "Look for a property with the same name, even
	//if it is not of the same type"). MEASURED (wave 2, LAYOUT stream): a ToolBar that registered
	//its own IconSize/LabelMode/LabelPosition/ShowToolTips dependency properties stopped
	//ToolBarProperties' inherited values dead - a tray set IconSize 40 and the bar under it still
	//read 24, while a ToolBarGroup, a ToolBarPanel, a Border and a plain ItemsControl in the same
	//place all read 40. Re-exposing the attached property here instead of registering a second one
	//keeps the name unregistered for this type, so inheritance flows through the bar to its items,
	//and setting the bar's property IS setting the value the items inherit.

	/// <summary>
	/// Identifies the <see cref="IconSize"/> property: the <c>ToolBarProperties.IconSize</c>
	/// inherited attached property itself.
	/// </summary>
	public static readonly DependencyProperty IconSizeProperty = ToolBarProperties.IconSizeProperty;

	/// <summary>
	/// Identifies the <see cref="LabelMode"/> property: the <c>ToolBarProperties.LabelMode</c>
	/// inherited attached property itself.
	/// </summary>
	public static readonly DependencyProperty LabelModeProperty = ToolBarProperties.LabelModeProperty;

	/// <summary>
	/// Identifies the <see cref="LabelPosition"/> property: the
	/// <c>ToolBarProperties.LabelPosition</c> inherited attached property itself.
	/// </summary>
	public static readonly DependencyProperty LabelPositionProperty = ToolBarProperties.LabelPositionProperty;

	/// <summary>
	/// Identifies the <see cref="ShowToolTips"/> property: the
	/// <c>ToolBarProperties.ShowToolTips</c> inherited attached property itself.
	/// </summary>
	public static readonly DependencyProperty ShowToolTipsProperty = ToolBarProperties.ShowToolTipsProperty;

	/// <summary>Initializes a new, empty bar.</summary>
	public ToolBar()
	{
		DefaultStyleKey = typeof(ToolBar);

		//The overflow flyout's panel is NOT a child of the bar, so the four bar settings cannot
		//reach the items inside it by inheritance; the bar copies them across instead, and has to
		//notice when they change. Watching an inherited attached property is safe here - it is
		//registering a property OF THAT NAME on this type that would sever the inheritance, which
		//is the trap recorded above.
		RegisterPropertyChangedCallback(ToolBarProperties.IconSizeProperty, OnBarSettingChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.LabelModeProperty, OnBarSettingChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.LabelPositionProperty, OnBarSettingChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.ShowToolTipsProperty, OnBarSettingChanged);
	}

	/// <summary>Gets or sets the axis the bar's items run along.</summary>
	/// <value><see cref="Orientation.Horizontal"/> by default.</value>
	/// <remarks>
	/// The bar sets the matching orientation on every <see cref="ToolBarGroup"/> and
	/// <see cref="ToolBarSeparator"/> it hosts, so those never have to state it themselves.
	/// </remarks>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>Gets or sets the bar's name.</summary>
	/// <value>The empty string by default.</value>
	/// <remarks>
	/// The title is the bar's accessibility name - what a screen reader announces when focus
	/// enters it - and names the overflow flyout as well, so "Main" and "Music" are distinguishable
	/// when both bars have overflowed.
	/// </remarks>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>Gets or sets the gap between two adjacent items, in logical pixels.</summary>
	/// <value>Four logical pixels by default.</value>
	public double ItemSpacing
	{
		get => (double)GetValue(ItemSpacingProperty);
		set => SetValue(ItemSpacingProperty, value);
	}

	/// <summary>Gets or sets whether the bar uses the denser metrics.</summary>
	/// <value>False by default.</value>
	/// <remarks>
	/// A compact bar halves its padding and the gap between its items. It is the right density for
	/// a bar attached to a panel rather than to the window.
	/// </remarks>
	public bool IsCompact
	{
		get => (bool)GetValue(IsCompactProperty);
		set => SetValue(IsCompactProperty, value);
	}

	/// <summary>Gets or sets what the bar does with items that do not fit.</summary>
	/// <value><see cref="CommandBar.OverflowMode.Chevron"/> by default.</value>
	public OverflowMode OverflowMode
	{
		get => (OverflowMode)GetValue(OverflowModeProperty);
		set => SetValue(OverflowModeProperty, value);
	}

	/// <summary>
	/// Gets or sets whether the bar inserts a <see cref="ToolBarSeparator"/> between two adjacent
	/// <see cref="ToolBarGroup"/> items.
	/// </summary>
	/// <value>True by default.</value>
	/// <remarks>
	/// A separator the application wrote itself between two groups is left alone: the bar only
	/// inserts one where two groups are genuinely adjacent, so turning this on never doubles up.
	/// </remarks>
	public bool SeparatorBetweenGroups
	{
		get => (bool)GetValue(SeparatorBetweenGroupsProperty);
		set => SetValue(SeparatorBetweenGroupsProperty, value);
	}

	/// <summary>Gets whether some of the bar's items are currently in the overflow flyout.</summary>
	/// <value>True while the chevron is shown.</value>
	public bool HasOverflowItems
	{
		get => (bool)GetValue(HasOverflowItemsProperty);
		private set => SetValue(HasOverflowItemsProperty, value);
	}

	/// <summary>Gets the items that are currently behind the chevron, in order.</summary>
	/// <value>Empty while everything fits.</value>
	/// <remarks>
	/// These are the SAME element instances the bar holds, re-parented into the overflow flyout's
	/// panel, so an application can read their state - or re-read the list after a resize - without
	/// the bar having to hand out copies. An auto-inserted separator can be among them.
	/// </remarks>
	public IReadOnlyList<UIElement> OverflowItems
	{
		get
		{
			if (_overflowHost is null)
			{
				return Array.Empty<UIElement>();
			}

			var items = new UIElement[_overflowHost.Children.Count];
			for (var i = 0; i < items.Length; i++)
			{
				items[i] = _overflowHost.Children[i];
			}

			return items;
		}
	}

	/// <summary>Gets or sets the icon size the bar's items use, in logical pixels.</summary>
	/// <value>24 logical pixels by default, or whatever a tray or page above the bar states.</value>
	/// <remarks>
	/// This IS <c>ToolBarProperties.IconSize</c> on the bar: setting it is what every item below
	/// inherits, and leaving it alone lets a value set higher up through.
	/// </remarks>
	public double IconSize
	{
		get => ToolBarProperties.GetIconSize(this);
		set => ToolBarProperties.SetIconSize(this, value);
	}

	/// <summary>Gets or sets whether the bar's items show their icon, their text, or both.</summary>
	/// <value>
	/// <see cref="CommandBar.LabelMode.IconOnly"/> by default, or whatever a tray or page above the
	/// bar states.
	/// </value>
	/// <remarks>This IS <c>ToolBarProperties.LabelMode</c> on the bar.</remarks>
	public LabelMode LabelMode
	{
		get => ToolBarProperties.GetLabelMode(this);
		set => ToolBarProperties.SetLabelMode(this, value);
	}

	/// <summary>Gets or sets where an item's text sits relative to its icon.</summary>
	/// <value>
	/// <see cref="CommandBar.LabelPosition.Right"/> by default, or whatever a tray or page above
	/// the bar states.
	/// </value>
	/// <remarks>This IS <c>ToolBarProperties.LabelPosition</c> on the bar.</remarks>
	public LabelPosition LabelPosition
	{
		get => ToolBarProperties.GetLabelPosition(this);
		set => ToolBarProperties.SetLabelPosition(this, value);
	}

	/// <summary>Gets or sets whether the bar's items show their composed tooltip.</summary>
	/// <value>True by default, or whatever a tray or page above the bar states.</value>
	/// <remarks>This IS <c>ToolBarProperties.ShowToolTips</c> on the bar.</remarks>
	public bool ShowToolTips
	{
		get => ToolBarProperties.GetShowToolTips(this);
		set => ToolBarProperties.SetShowToolTips(this, value);
	}

	/// <summary>The panel in the bar's template that holds the items which fit.</summary>
	internal ToolBarPanel? ItemsHost => _itemsHost;

	/// <summary>The panel inside the overflow flyout that holds the items which did not fit.</summary>
	internal ToolBarPanel OverflowHost => EnsureOverflowHost();

	/// <summary>The chevron button, once the bar has needed one.</summary>
	internal ToolBarOverflowButton? OverflowButton => _overflowButton;

	/// <summary>The bar's items as elements, in order, including any auto-inserted separator.</summary>
	internal IReadOnlyList<UIElement> LayoutItems => _layoutItems;

	/// <summary>Opens the overflow flyout, if the bar has one to open.</summary>
	/// <returns>True when a flyout was shown.</returns>
	/// <remarks>
	/// A flyout needs a window to appear in, so this does nothing - and answers false - for a bar
	/// that is not in one yet.
	/// </remarks>
	public bool ShowOverflow()
	{
		if (!HasOverflowItems || _overflowButton is null || XamlRoot is null)
		{
			return false;
		}

		EnsureOverflowFlyout().ShowAt(_overflowButton);

		return true;
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_itemsHost = GetTemplateChild(ItemsHostPartName) as ToolBarPanel;
		_splitIndex = -1;
		SyncPanelSettings();
		RebuildLayoutItems();
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		//The partition is decided BEFORE the template is measured, so no panel ever gains or loses
		//a child while it is measuring. That is what keeps a bar which is one pixel too narrow from
		//oscillating between "the chevron fits" and "then it does not".
		var padding = Padding;
		var border = BorderThickness;
		var inner = new Size(
			Math.Max(0, availableSize.Width - padding.Left - padding.Right - border.Left - border.Right),
			Math.Max(0, availableSize.Height - padding.Top - padding.Bottom - border.Top - border.Bottom));

		UpdateOverflow(inner);

		return base.MeasureOverride(availableSize);
	}

	/// <inheritdoc />
	protected override void OnItemsChanged(object e)
	{
		base.OnItemsChanged(e);
		RebuildLayoutItems();
	}

	/// <inheritdoc />
	protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnItemsSourceChanged(e);
		RebuildLayoutItems();
	}

	/// <inheritdoc />
	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.Handled)
		{
			return;
		}

		//The item the key started at has already had its chance: KeyDown bubbles up to the bar, so
		//a drop-down button that opens on Down has handled it before this runs.
		var focused = XamlRoot is { } root
			? FocusManager.GetFocusedElement(root) as FrameworkElement
			: null;

		if (TryHandleNavigationKey(e.Key, focused))
		{
			e.Handled = true;
		}
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarAutomationPeer(this);

	/// <summary>
	/// The items along the bar a keyboard can reach, in order, with the contents of a group
	/// flattened into place and the chevron last when it is shown.
	/// </summary>
	/// <returns>The focusable items, in navigation order.</returns>
	internal IReadOnlyList<Control> GetKeyboardNavigationItems()
	{
		var result = new List<Control>();

		//Only what is actually in the bar: an item that has moved into the overflow flyout is
		//reached by opening the flyout, not by walking past the chevron.
		var end = _splitIndex < 0 ? _layoutItems.Count : Math.Min(_splitIndex, _layoutItems.Count);

		for (var i = 0; i < end; i++)
		{
			var item = _layoutItems[i];
			if (item.Visibility == Visibility.Collapsed)
			{
				continue;
			}

			if (item is ToolBarGroup group)
			{
				//A group is a container, never a stop of its own: the keyboard walks through it.
				for (var c = 0; c < group.Children.Count; c++)
				{
					AddIfFocusable(result, group.Children[c]);
				}

				continue;
			}

			AddIfFocusable(result, item);
		}

		if (HasOverflowItems && _overflowButton is not null)
		{
			result.Add(_overflowButton);
		}

		return result;
	}

	/// <summary>
	/// Works out which item a navigation key moves to from <paramref name="current"/>.
	/// </summary>
	/// <param name="current">The item focus is on, or null when focus is not on the bar yet.</param>
	/// <param name="key">The key that was pressed.</param>
	/// <returns>The item to move to, or null when the key does not move focus.</returns>
	/// <remarks>
	/// Focus does not wrap: the first item stays put on a backwards key and the last on a forwards
	/// one, so a held arrow key never cycles the bar.
	/// </remarks>
	internal Control? GetNavigationTarget(FrameworkElement? current, VirtualKey key)
	{
		var items = GetKeyboardNavigationItems();
		if (items.Count == 0)
		{
			return null;
		}

		var horizontal = Orientation == Orientation.Horizontal;
		var forward = horizontal ? VirtualKey.Right : VirtualKey.Down;
		var backward = horizontal ? VirtualKey.Left : VirtualKey.Up;

		if (key == VirtualKey.Home)
		{
			return items[0];
		}

		if (key == VirtualKey.End)
		{
			return items[items.Count - 1];
		}

		if (key != forward && key != backward)
		{
			return null;
		}

		var index = IndexOfOwningItem(items, current);
		if (index < 0)
		{
			//Focus is not on the bar; a navigation key brings it to the near end.
			return key == forward ? items[0] : items[items.Count - 1];
		}

		var target = key == forward ? index + 1 : index - 1;

		return target >= 0 && target < items.Count ? items[target] : items[index];
	}

	/// <summary>
	/// Applies a key press to the bar: moves focus along it, or opens the focused item's attached
	/// flyout.
	/// </summary>
	/// <param name="key">The key that was pressed.</param>
	/// <param name="focused">The element focus is on, or null.</param>
	/// <returns>True when the bar consumed the key.</returns>
	internal bool TryHandleNavigationKey(VirtualKey key, FrameworkElement? focused)
	{
		if (IsDropDownKey(key) && focused is not null && TryOpenDropDown(focused))
		{
			return true;
		}

		var target = GetNavigationTarget(focused, key);
		if (target is null || ReferenceEquals(target, focused))
		{
			return false;
		}

		target.Focus(FocusState.Keyboard);

		return true;
	}

	/// <summary>Opens the menu of the focused item, if that item has one.</summary>
	/// <param name="focused">The item the drop-down key was pressed on.</param>
	/// <returns>True when the key belonged to that item's menu and must go no further.</returns>
	/// <remarks>
	/// Two kinds of item carry a menu, and both have to answer the key. A
	/// <see cref="ToolDropDownButton"/> keeps its menu in its own <c>Flyout</c> property, which is
	/// what makes the button a drop-down at all; anything else - a plain button an application put
	/// in the bar, say - carries it as an attached flyout. A bar that looked only for the attached
	/// kind would move focus along the bar instead of opening a drop-down button's menu, which is
	/// the one thing the key exists for.
	/// A flyout needs a window; host-free the key is still the drop-down's, so it is reported as
	/// consumed either way and the bar does not also move focus with it.
	/// </remarks>
	private bool TryOpenDropDown(FrameworkElement focused)
	{
		if (focused is ToolDropDownButton dropDown)
		{
			if (dropDown.Flyout is null)
			{
				return false;
			}

			if (XamlRoot is not null)
			{
				dropDown.OpenFlyout();
			}

			return true;
		}

		if (FlyoutBase.GetAttachedFlyout(focused) is null)
		{
			return false;
		}

		if (XamlRoot is not null)
		{
			FlyoutBase.ShowAttachedFlyout(focused);
		}

		return true;
	}

	/// <summary>Recomputes which items fit and moves the rest into (or out of) the flyout.</summary>
	/// <param name="available">The space inside the bar's border and padding.</param>
	internal void UpdateOverflow(Size available)
	{
		if (_itemsHost is null)
		{
			return;
		}

		if (OverflowMode != OverflowMode.Chevron || _layoutItems.Count == 0)
		{
			ApplySplit(_layoutItems.Count, hasOverflow: false);
			return;
		}

		var horizontal = Orientation == Orientation.Horizontal;
		var limit = horizontal ? available.Width : available.Height;
		var childAvailable = horizontal
			? new Size(double.PositiveInfinity, available.Height)
			: new Size(available.Width, double.PositiveInfinity);

		var extents = new List<double>(_layoutItems.Count);
		var positions = new List<int>(_layoutItems.Count);

		for (var i = 0; i < _layoutItems.Count; i++)
		{
			var item = _layoutItems[i];
			if (item.Visibility == Visibility.Collapsed)
			{
				continue;
			}

			item.Measure(childAvailable);
			extents.Add(horizontal ? item.DesiredSize.Width : item.DesiredSize.Height);
			positions.Add(i);
		}

		var chevronExtent = MeasureChevron(childAvailable, horizontal);
		var visibleCount = ToolBarLayout.ComputeVisibleCount(
			extents, ItemSpacing, limit, chevronExtent, out var hasOverflow);

		var split = visibleCount >= positions.Count
			? _layoutItems.Count
			: positions[visibleCount];

		ApplySplit(split, hasOverflow);
	}

	private static void OnStructuralPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var bar = (ToolBar)d;
		bar.SyncPanelSettings();
		bar.SyncItemOrientation();
		bar._splitIndex = -1;
		bar.InvalidateMeasure();
	}

	private static void OnItemsStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ToolBar)d).RebuildLayoutItems();

	private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var bar = (ToolBar)d;
		bar.UpdateDensityState();
		bar.SyncPanelSettings();
		bar.InvalidateMeasure();
	}

	private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var bar = (ToolBar)d;
		var title = bar.Title;

		AutomationProperties.SetName(bar, title ?? string.Empty);

		if (bar._overflowHost is not null)
		{
			AutomationProperties.SetName(bar._overflowHost, title ?? string.Empty);
		}
	}

	private static void AddIfFocusable(List<Control> into, UIElement element)
	{
		if (element is Control { IsEnabled: true, IsTabStop: true, Visibility: Visibility.Visible } control)
		{
			into.Add(control);
		}
	}

	private static int IndexOfOwningItem(IReadOnlyList<Control> items, FrameworkElement? current)
	{
		for (var element = current; element is not null; element = element.Parent as FrameworkElement)
		{
			for (var i = 0; i < items.Count; i++)
			{
				if (ReferenceEquals(items[i], element))
				{
					return i;
				}
			}
		}

		return -1;
	}

	private bool IsDropDownKey(VirtualKey key)
		=> Orientation == Orientation.Horizontal ? key == VirtualKey.Down : key == VirtualKey.Right;

	private void UpdateDensityState()
		=> VisualStateManager.GoToState(this, IsCompact ? "Compact" : "Comfortable", false);

	private double EffectiveItemSpacing => IsCompact ? ItemSpacing / 2 : ItemSpacing;

	private void SyncPanelSettings()
	{
		if (_itemsHost is not null)
		{
			_itemsHost.Orientation = Orientation;
			_itemsHost.ItemSpacing = EffectiveItemSpacing;
			_itemsHost.Wrap = OverflowMode == OverflowMode.Wrap;
		}

		if (_overflowHost is not null)
		{
			//The flyout runs across the bar: a horizontal bar's overflow is a vertical menu.
			_overflowHost.Orientation = Orientation == Orientation.Horizontal
				? Orientation.Vertical
				: Orientation.Horizontal;
			_overflowHost.ItemSpacing = EffectiveItemSpacing;
		}
	}

	private void SyncItemOrientation()
	{
		var separatorOrientation = Orientation == Orientation.Horizontal
			? Orientation.Vertical
			: Orientation.Horizontal;

		for (var i = 0; i < _layoutItems.Count; i++)
		{
			switch (_layoutItems[i])
			{
				case ToolBarGroup group:
					group.Orientation = Orientation;
					break;
				case ToolBarSeparator separator:
					separator.Orientation = separatorOrientation;
					break;
			}
		}
	}

	private void RebuildLayoutItems()
	{
		if (_itemsHost is null)
		{
			//Nothing to lay out into yet; OnApplyTemplate rebuilds once the panel exists.
			return;
		}

		_layoutItems.Clear();

		var wrapped = 0;
		var elements = new List<UIElement>(Items.Count);
		for (var i = 0; i < Items.Count; i++)
		{
			elements.Add(AsElement(Items[i], ref wrapped));
		}

		for (var i = 0; i < elements.Count; i++)
		{
			if (SeparatorBetweenGroups
				&& _layoutItems.Count > 0
				&& elements[i] is ToolBarGroup
				&& LastVisibleLayoutItem() is ToolBarGroup)
			{
				//Two groups next to each other get a separator between them. A separator the
				//application wrote itself sits between the two groups, so this never doubles up.
				_layoutItems.Add(CreateAutoSeparator());
			}

			_layoutItems.Add(elements[i]);
		}

		DropUnusedWrappers(wrapped);
		SyncItemOrientation();

		//A rebuild changes what the split index means, so the panels are repopulated from scratch;
		//the next measure works the partition out again.
		_splitIndex = -1;
		ApplySplit(_layoutItems.Count, hasOverflow: false);
		InvalidateMeasure();
	}

	private UIElement? LastVisibleLayoutItem()
	{
		for (var i = _layoutItems.Count - 1; i >= 0; i--)
		{
			if (_layoutItems[i].Visibility != Visibility.Collapsed)
			{
				return _layoutItems[i];
			}
		}

		return null;
	}

	private ToolBarSeparator CreateAutoSeparator()
		=> new()
		{
			Orientation = Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal,
		};

	private UIElement AsElement(object item, ref int wrapped)
	{
		if (item is UIElement element)
		{
			return element;
		}

		//A bar over data rather than over elements still works: each such item gets a presenter,
		//taken from a pool by position so that two equal data items never end up sharing one
		//element, and so that a rebuild does not throw away live content.
		if (wrapped >= _wrapperPool.Count)
		{
			_wrapperPool.Add(new ContentPresenter());
		}

		var presenter = _wrapperPool[wrapped++];
		presenter.Content = item;
		presenter.ContentTemplate = ItemTemplate;
		presenter.ContentTemplateSelector = ItemTemplateSelector;

		return presenter;
	}

	private void DropUnusedWrappers(int used)
	{
		while (_wrapperPool.Count > used)
		{
			_wrapperPool.RemoveAt(_wrapperPool.Count - 1);
		}
	}

	private double MeasureChevron(Size childAvailable, bool horizontal)
	{
		//The chevron is measured whether or not it is in the panel: what has to be reserved is the
		//space it WILL need. It is never left in the panel collapsed, because toggling a child's
		//visibility from inside a measure pass invalidates the pass that is running.
		var chevron = EnsureOverflowButton();
		chevron.Measure(childAvailable);
		var extent = horizontal ? chevron.DesiredSize.Width : chevron.DesiredSize.Height;

		return extent > 0 ? extent : FallbackChevronExtent;
	}

	private void ApplySplit(int split, bool hasOverflow)
	{
		if (_itemsHost is null)
		{
			return;
		}

		split = Math.Clamp(split, 0, _layoutItems.Count);

		if (_splitIndex == split && HasOverflowItems == hasOverflow)
		{
			return;
		}

		_splitIndex = split;

		var mainWanted = new List<UIElement>(split + 1);
		for (var i = 0; i < split; i++)
		{
			mainWanted.Add(_layoutItems[i]);
		}

		var overflowWanted = new List<UIElement>(Math.Max(0, _layoutItems.Count - split));
		for (var i = split; i < _layoutItems.Count; i++)
		{
			overflowWanted.Add(_layoutItems[i]);
		}

		if (hasOverflow)
		{
			mainWanted.Add(EnsureOverflowButton());
		}

		//An element belongs to one panel at a time, so everything that is moving leaves its old
		//panel before it joins its new one; what is staying put is never touched, which is what
		//keeps focus and pointer capture on an item the bar did not move.
		var overflowHost = overflowWanted.Count > 0 || _overflowHost is not null ? EnsureOverflowHost() : null;

		if (overflowHost is not null)
		{
			RemoveAll(overflowHost.Children, mainWanted);
		}

		RemoveAll(_itemsHost.Children, overflowWanted);

		SyncChildren(_itemsHost.Children, mainWanted);

		if (overflowHost is not null)
		{
			var arriving = new List<UIElement>(overflowWanted.Count);
			for (var i = 0; i < overflowWanted.Count; i++)
			{
				if (!overflowHost.Children.Contains(overflowWanted[i]))
				{
					arriving.Add(overflowWanted[i]);
				}
			}

			SyncChildren(overflowHost.Children, overflowWanted);
			RehookCommands(arriving);
		}

		HasOverflowItems = hasOverflow;
	}

	/// <summary>
	/// Puts back the command subscription of every button that has just moved into the overflow.
	/// </summary>
	/// <param name="arriving">The elements that changed panel on this pass.</param>
	/// <remarks>
	/// The framework subscribes a button to its command's <c>CanExecuteChanged</c> when the button
	/// ENTERS the tree and unsubscribes when it LEAVES it
	/// (<c>ButtonBase.EnterImpl</c> / <c>LeaveImpl</c>). The overflow flyout's panel is not in the
	/// bar's tree, so a button moved into it leaves and never arrives: MEASURED as a button in the
	/// overflow staying enabled after its command answered CanExecute false. Re-assigning the
	/// command subscribes again - the same remedy
	/// <see cref="ToolDropDownButton.RehookFlyoutBindings"/> applies to a menu's items, for the
	/// same reason - and the framework disposes that subscription on the next Leave, so nothing is
	/// subscribed twice and nothing is held open.
	/// </remarks>
	private static void RehookCommands(List<UIElement> arriving)
	{
		for (var i = 0; i < arriving.Count; i++)
		{
			RehookCommands(arriving[i]);
		}

		static void RehookCommands(UIElement element)
		{
			if (element is ButtonBase { Command: { } command } button)
			{
				button.Command = null;
				button.Command = command;
			}

			//A group travels whole, so the buttons inside it moved too.
			if (element is Panel panel)
			{
				for (var i = 0; i < panel.Children.Count; i++)
				{
					RehookCommands(panel.Children[i]);
				}
			}
		}
	}

	private static void RemoveAll(UIElementCollection from, List<UIElement> elements)
	{
		for (var i = 0; i < elements.Count; i++)
		{
			var index = from.IndexOf(elements[i]);
			if (index >= 0)
			{
				from.RemoveAt(index);
			}
		}
	}

	private static void SyncChildren(UIElementCollection target, List<UIElement> wanted)
	{
		for (var i = 0; i < wanted.Count; i++)
		{
			if (i < target.Count && ReferenceEquals(target[i], wanted[i]))
			{
				continue;
			}

			var existing = target.IndexOf(wanted[i]);
			if (existing >= 0)
			{
				target.RemoveAt(existing);
			}

			target.Insert(i, wanted[i]);
		}

		while (target.Count > wanted.Count)
		{
			target.RemoveAt(target.Count - 1);
		}
	}

	private ToolBarPanel EnsureOverflowHost()
	{
		if (_overflowHost is null)
		{
			_overflowHost = new ToolBarPanel
			{
				Orientation = Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal,
				ItemSpacing = EffectiveItemSpacing,
			};

			AutomationProperties.SetName(_overflowHost, Title ?? string.Empty);
		}

		SyncOverflowHostSettings();

		return _overflowHost;
	}

	/// <summary>
	/// Copies the bar's four presentation settings onto the overflow flyout's panel.
	/// </summary>
	/// <remarks>
	/// The panel is the flyout's content, not a child of the bar, so nothing about the bar reaches
	/// the items inside it on its own. Copying the EFFECTIVE values - what the bar itself reads,
	/// whether the bar set them or inherited them from a tray or a page - keeps an overflowed
	/// button looking like the one still on the bar. An item that set a value for itself still
	/// wins: a local value outranks an inherited one.
	/// </remarks>
	private void SyncOverflowHostSettings()
	{
		if (_overflowHost is null)
		{
			return;
		}

		ToolBarProperties.SetIconSize(_overflowHost, ToolBarProperties.GetIconSize(this));
		ToolBarProperties.SetLabelMode(_overflowHost, ToolBarProperties.GetLabelMode(this));
		ToolBarProperties.SetLabelPosition(_overflowHost, ToolBarProperties.GetLabelPosition(this));
		ToolBarProperties.SetShowToolTips(_overflowHost, ToolBarProperties.GetShowToolTips(this));
	}

	private void OnBarSettingChanged(DependencyObject sender, DependencyProperty property)
		=> SyncOverflowHostSettings();

	private ToolBarOverflowButton EnsureOverflowButton()
	{
		if (_overflowButton is null)
		{
			_overflowButton = new ToolBarOverflowButton();
			AutomationProperties.SetName(_overflowButton, "More");
			_overflowButton.Click += (_, _) => ShowOverflow();
		}

		return _overflowButton;
	}

	private Flyout EnsureOverflowFlyout()
	{
		if (_overflowFlyout is null)
		{
			_overflowFlyout = new Flyout
			{
				Content = EnsureOverflowHost(),
				Placement = FlyoutPlacementMode.Bottom,
			};
		}

		return _overflowFlyout;
	}
}
