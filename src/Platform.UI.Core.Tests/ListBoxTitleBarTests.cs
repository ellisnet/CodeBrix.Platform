#nullable enable

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-10 (decision D6): ListBox / ListBoxItem and TitleBar are implemented in Core (they were NotImplemented markers).
/// Host-free: the ListBox's selection modes (pointer clicks with modifiers, keyboard navigation, SelectAll) through its
/// internal entry points, with no template realized; TitleBar's WinUI defaults, template settings and automation peer.
/// The rendered behaviour (templates, taps, injected keys, layout) is fenced by the UIReqs features ListBox and TitleBar.
/// </summary>
public class ListBoxTitleBarTests
{
	private static readonly string[] Letters = { "A", "B", "C", "D", "E" };

	private static ListBox NewListBox(SelectionMode mode) => new() { ItemsSource = Letters, SelectionMode = mode };

	private static string Selected(ListBox box) => string.Join(",", box.SelectedItems.Cast<string>().OrderBy(s => s));

	// ---------------------------------------------------------------- ListBox

	[Fact]
	public void When_A_ListBox_Is_Created_Then_It_Has_The_WinUI_Defaults_And_Is_Implemented()
	{
		//Arrange
		var box = new ListBox();

		//Assert
		box.SelectionMode.Should().Be(SelectionMode.Single);
		box.SingleSelectionFollowsFocus.Should().BeTrue();
		box.SelectedItems.Should().BeEmpty();
		box.SelectedIndex.Should().Be(-1);
		typeof(ListBox).GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
		typeof(ListBoxItem).GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
		typeof(TitleBar).GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
	}

	[Fact]
	public void When_An_Item_Of_A_Single_Selection_ListBox_Is_Clicked_Then_It_Is_The_Only_Selected_Item()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Single);
		var changes = 0;
		box.SelectionChanged += (_, _) => changes++;

		//Act
		box.OnItemClicked(1, VirtualKeyModifiers.None);
		box.OnItemClicked(3, VirtualKeyModifiers.None);

		//Assert
		box.SelectedIndex.Should().Be(3);
		box.SelectedItem.Should().Be("D");
		Selected(box).Should().Be("D");
		changes.Should().Be(2);

		//Act: Control and a click on the selected item clears the selection
		box.OnItemClicked(3, VirtualKeyModifiers.Control);

		//Assert
		box.SelectedIndex.Should().Be(-1);
		Selected(box).Should().Be("");
	}

	[Fact]
	public void When_Items_Of_A_Multiple_Selection_ListBox_Are_Clicked_Then_Each_Click_Toggles_Its_Item()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Multiple);

		//Act
		box.OnItemClicked(0, VirtualKeyModifiers.None);
		box.OnItemClicked(2, VirtualKeyModifiers.None);
		box.OnItemClicked(4, VirtualKeyModifiers.None);
		box.OnItemClicked(2, VirtualKeyModifiers.None);

		//Assert
		Selected(box).Should().Be("A,E");
		box.SelectedIndex.Should().Be(0);
	}

	[Fact]
	public void When_An_Extended_Selection_ListBox_Is_Clicked_With_Shift_And_Control_Then_It_Selects_Ranges_And_Toggles()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Extended);

		//Act: a plain click, then Shift+click extends from the anchor
		box.OnItemClicked(1, VirtualKeyModifiers.None);
		box.OnItemClicked(3, VirtualKeyModifiers.Shift);

		//Assert
		Selected(box).Should().Be("B,C,D");

		//Act: Control+click toggles one item and moves the anchor; a plain click selects that item alone
		box.OnItemClicked(0, VirtualKeyModifiers.Control);
		var afterControl = Selected(box);
		box.OnItemClicked(4, VirtualKeyModifiers.None);

		//Assert
		afterControl.Should().Be("A,B,C,D");
		Selected(box).Should().Be("E");
	}

	[Fact]
	public void When_Arrow_Keys_Reach_A_Single_Selection_ListBox_Then_The_Selection_Follows_Unless_Control_Is_Held()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Single);
		box.SelectedIndex = 0;

		//Act
		var down = box.TryHandleKeyDown(VirtualKey.Down, VirtualKeyModifiers.None);
		var end = box.TryHandleKeyDown(VirtualKey.End, VirtualKeyModifiers.None);
		var pastEnd = box.TryHandleKeyDown(VirtualKey.Down, VirtualKeyModifiers.None);

		//Assert
		down.Should().BeTrue();
		end.Should().BeTrue();
		pastEnd.Should().BeFalse(); // at the last item: nothing moves, the key is not handled
		box.SelectedIndex.Should().Be(4);

		//Act: with SingleSelectionFollowsFocus false the selection stays
		box.SingleSelectionFollowsFocus = false;
		box.TryHandleKeyDown(VirtualKey.Home, VirtualKeyModifiers.None);

		//Assert
		box.SelectedIndex.Should().Be(4);
	}

	[Fact]
	public void When_Shift_Arrows_And_Control_A_Reach_An_Extended_Selection_ListBox_Then_It_Extends_And_Selects_All()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Extended);
		box.OnItemClicked(1, VirtualKeyModifiers.None);

		//Act
		box.TryHandleKeyDown(VirtualKey.Down, VirtualKeyModifiers.Shift);
		var extended = Selected(box);
		var selectAll = box.TryHandleKeyDown(VirtualKey.A, VirtualKeyModifiers.Control);

		//Assert
		extended.Should().Be("B,C");
		selectAll.Should().BeTrue();
		Selected(box).Should().Be("A,B,C,D,E");
	}

	[Fact]
	public void When_SelectAll_Is_Called_Then_Only_A_Multiple_Or_Extended_ListBox_Selects_Everything()
	{
		//Arrange
		var single = NewListBox(SelectionMode.Single);
		var multiple = NewListBox(SelectionMode.Multiple);

		//Act
		single.SelectAll();
		multiple.SelectAll();

		//Assert
		Selected(single).Should().Be("");
		Selected(multiple).Should().Be("A,B,C,D,E");
	}

	[Fact]
	public void When_The_SelectionMode_Of_A_ListBox_Changes_Then_Its_Selection_Is_Cleared()
	{
		//Arrange
		var box = NewListBox(SelectionMode.Multiple);
		box.OnItemClicked(0, VirtualKeyModifiers.None);
		box.OnItemClicked(1, VirtualKeyModifiers.None);

		//Act
		box.SelectionMode = SelectionMode.Single;

		//Assert
		box.SelectedItems.Should().BeEmpty();
		box.SelectedIndex.Should().Be(-1);
	}

	[Fact]
	public void When_A_ListBox_Makes_Containers_Then_They_Are_ListBoxItems_With_Their_Automation_Peers()
	{
		//Arrange
		var box = new ProbeListBox();
		var item = new ListBoxItem();

		//Assert
		box.MakeContainer().Should().BeOfType<ListBoxItem>();
		box.IsItemOwnContainer(item).Should().BeTrue();
		box.IsItemOwnContainer("A").Should().BeFalse();
		box.Peer().Should().BeOfType<ListBoxAutomationPeer>();
		new ProbeListBoxItem().Peer().Should().BeOfType<ListBoxItemAutomationPeer>();
	}

	// ---------------------------------------------------------------- TitleBar

	[Fact]
	public void When_A_TitleBar_Is_Created_Then_It_Has_The_WinUI_Defaults()
	{
		//Arrange
		var bar = new TitleBar();

		//Assert
		bar.Title.Should().Be(string.Empty);
		bar.Subtitle.Should().Be(string.Empty);
		bar.IsBackButtonVisible.Should().BeFalse();
		bar.IsBackButtonEnabled.Should().BeTrue();
		bar.IsPaneToggleButtonVisible.Should().BeFalse();
		bar.TemplateSettings.Should().NotBeNull();
		bar.TemplateSettings.IconElement.Should().BeNull();
		bar.LeftHeader.Should().BeNull();
		bar.Content.Should().BeNull();
		bar.RightHeader.Should().BeNull();
	}

	[Fact]
	public void When_A_TitleBar_Has_A_Title_Then_Its_Automation_Peer_Is_Named_After_It()
	{
		//Arrange
		var bar = new ProbeTitleBar { Title = "Mail" };

		//Act
		var peer = bar.Peer();

		//Assert
		peer.Should().BeOfType<TitleBarAutomationPeer>();
		peer.GetName().Should().Be("Mail");
		peer.GetClassName().Should().Be(nameof(TitleBar));
		peer.GetAutomationControlType().Should().Be(AutomationControlType.TitleBar);
	}

	[Fact]
	public void When_A_TitleBar_Gets_An_IconSource_Before_Its_Template_Then_Setting_It_Does_Not_Throw()
	{
		//Arrange
		var bar = new TitleBar();

		//Act
		bar.IconSource = new SymbolIconSource { Symbol = Symbol.Mail };
		bar.IsBackButtonVisible = true;
		bar.IsPaneToggleButtonVisible = true;
		bar.LeftHeader = new Button();
		bar.Content = new Button();
		bar.RightHeader = new Button();
		bar.Title = "Mail";
		bar.Title = string.Empty;

		//Assert
		bar.Title.Should().Be(string.Empty);
	}

	private sealed class ProbeListBox : ListBox
	{
		public DependencyObject MakeContainer() => GetContainerForItemOverride();

		public bool IsItemOwnContainer(object item) => IsItemItsOwnContainerOverride(item);

		public AutomationPeer Peer() => OnCreateAutomationPeer();
	}

	private sealed class ProbeListBoxItem : ListBoxItem
	{
		public AutomationPeer Peer() => OnCreateAutomationPeer();
	}

	private sealed class ProbeTitleBar : TitleBar
	{
		public AutomationPeer Peer() => OnCreateAutomationPeer();
	}
}
