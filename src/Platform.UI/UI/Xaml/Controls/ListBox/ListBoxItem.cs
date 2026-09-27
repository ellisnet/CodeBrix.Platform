using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>Represents the container for an item in a <see cref="ListBox"/> control.</summary>
/// <remarks>
/// Its default template uses WinUI's ListBoxItem visual states: Normal, PointerOver, Pressed, Selected,
/// SelectedUnfocused, SelectedPointerOver, SelectedPressed and Disabled.
/// </remarks>
public partial class ListBoxItem : SelectorItem
{
	/// <summary>Initializes a new instance of the <see cref="ListBoxItem"/> class.</summary>
	public ListBoxItem()
	{
		DefaultStyleKey = typeof(ListBoxItem);
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ListBoxItemAutomationPeer(this);

	/// <summary>
	/// The ListBoxItem names of the selection states (WinUI ListBoxItem_Partial.cpp ChangeVisualState): "SelectedPointerOver"
	/// and "SelectedPressed" rather than the ListViewItem names, and "Disabled" for a disabled item whose content is not a
	/// control (a control shows its own disabled look). A selected item goes to "Selected" whether or not the list has the
	/// focus (the default template draws "SelectedUnfocused" the same way).
	/// </summary>
	private protected override string MapCommonVisualState(string state, bool isEnabled, bool isSelected)
	{
		if (!isEnabled)
		{
			return Content is Control ? "Normal" : "Disabled";
		}

		return state switch
		{
			"PointerOverSelected" => "SelectedPointerOver",
			"PressedSelected" => "SelectedPressed",
			_ => state,
		};
	}
}
