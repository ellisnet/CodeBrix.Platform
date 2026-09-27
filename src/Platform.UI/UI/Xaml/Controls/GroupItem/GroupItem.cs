using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the root element of a group of items in a grouped <see cref="ItemsControl"/> whose items panel is not a
/// virtualizing panel: the group's header, then the group's items in a panel of their own.
/// </summary>
/// <remarks>
/// An ItemsControl generates one GroupItem per displayed group when its items source is grouped and it has a
/// <see cref="GroupStyle"/> (see <see cref="ItemsControl.ShowsGroupHeaders"/>). The item containers inside a GroupItem
/// belong to the ItemsControl (it creates, prepares, indexes and selects them exactly as it does in its own panel); the
/// GroupItem only hosts them. A virtualizing panel (ItemsStackPanel) realizes ListViewHeaderItem / GridViewHeaderItem
/// headers instead and uses no GroupItem, as in WinUI.
/// </remarks>
public partial class GroupItem : ContentControl
{
	/// <summary>The name of the template part that receives the panel of the group's items.</summary>
	internal const string ItemsHostPartName = "ItemsHost";

	private Panel _itemsPanel;
	private Border _itemsHost;

	/// <summary>Initializes a new instance of the <see cref="GroupItem"/> class.</summary>
	public GroupItem()
	{
		DefaultStyleKey = typeof(GroupItem);
	}

	/// <summary>Gets the panel that holds the item containers of this group (null before the ItemsControl gives it one).</summary>
	internal Panel ItemsPanel => _itemsPanel;

	/// <summary>Gets or sets the display index of the group this GroupItem shows (-1 while it shows none).</summary>
	internal int GroupIndex { get; set; } = -1;

	/// <summary>Gets or sets the header container the GroupItem shows as its content, when its ItemsControl uses one.</summary>
	internal ContentControl HeaderContainer { get; set; }

	/// <summary>Gives the GroupItem the panel of its items; the panel is shown in the template's ItemsHost part.</summary>
	/// <param name="panel">The panel, or null to take the current one away.</param>
	internal void SetItemsPanel(Panel panel)
	{
		if (ReferenceEquals(_itemsPanel, panel))
		{
			return;
		}

		if (_itemsHost is not null && ReferenceEquals(_itemsHost.Child, _itemsPanel))
		{
			_itemsHost.Child = null;
		}

		_itemsPanel = panel;

		if (_itemsHost is not null)
		{
			_itemsHost.Child = panel;
		}
	}

	/// <summary>Gets the item containers this GroupItem hosts, in order.</summary>
	/// <returns>The containers (empty when it has no panel).</returns>
	internal IEnumerable<DependencyObject> GetItemContainers()
		=> _itemsPanel?.Children.OfType<DependencyObject>() ?? Enumerable.Empty<DependencyObject>();

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (_itemsHost is not null && ReferenceEquals(_itemsHost.Child, _itemsPanel))
		{
			_itemsHost.Child = null;
		}

		_itemsHost = GetTemplateChild(ItemsHostPartName) as Border;

		if (_itemsHost is not null)
		{
			_itemsHost.Child = _itemsPanel;
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		var desired = base.MeasureOverride(availableSize);

		// In a wrapping panel a group takes a whole line, so that the next group starts on a new one (the way WinUI's
		// ItemsWrapGrid, for which the wrap panel stands in, lays out its groups).
		if (VisualTreeHelper.GetParent(this) is WrapPanel wrapPanel)
		{
			if (wrapPanel.Orientation == Orientation.Horizontal && !double.IsInfinity(availableSize.Width))
			{
				desired.Width = System.Math.Max(desired.Width, availableSize.Width);
			}
			else if (wrapPanel.Orientation == Orientation.Vertical && !double.IsInfinity(availableSize.Height))
			{
				desired.Height = System.Math.Max(desired.Height, availableSize.Height);
			}
		}

		return desired;
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new GroupItemAutomationPeer(this);
}
