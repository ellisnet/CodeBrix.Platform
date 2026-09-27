#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-8 (decision D4 + GroupItem from D6): group headers of a grouped ListView / GridView / ItemsControl. Host-free: the
/// gate (a grouped source AND a GroupStyle), the items-host seam's group queries and header containers, GroupStyle's
/// change notification, and the GroupItem path of a non-virtualizing panel. The virtualized header lines and sticky
/// headers are fenced by the UIReqs features GroupedListView / GroupedGridView.
/// </summary>
public class GroupedListTests
{
	/// <summary>A group of the test sources: a list of strings with a name.</summary>
	public sealed class NamedGroup : List<string>
	{
		public NamedGroup(string name, params string[] items) : base(items) => Name = name;

		public string Name { get; }

		public override string ToString() => Name;
	}

	private static List<NamedGroup> Groups() => new()
	{
		new NamedGroup("Fruit", "Apple", "Pear"),
		new NamedGroup("Empty"),
		new NamedGroup("Nuts", "Almond", "Walnut", "Hazel"),
	};

	private static object GroupedView(List<NamedGroup> groups)
		=> new CollectionViewSource { IsSourceGrouped = true, Source = groups }.View;

	private static DataTemplate HeaderTemplate()
		=> (DataTemplate)XamlReader.Load(
			"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Name}' /></DataTemplate>");

	// ---------------------------------------------------------------- the gate

	[Fact]
	public void When_A_List_Has_A_Grouped_Source_And_A_GroupStyle_Then_It_Shows_Group_Headers()
	{
		//Arrange
		var grouped = new ListView { ItemsSource = GroupedView(Groups()) };
		var groupedWithStyle = new ListView { ItemsSource = GroupedView(Groups()) };
		groupedWithStyle.GroupStyle.Add(new GroupStyle());
		var flatWithStyle = new ListView { ItemsSource = new[] { "a", "b" } };
		flatWithStyle.GroupStyle.Add(new GroupStyle());

		//Assert
		grouped.IsGrouping.Should().BeTrue();
		grouped.ShowsGroupHeaders.Should().BeFalse(); // grouped source, no GroupStyle: flattened, no headers (WinUI)
		groupedWithStyle.ShowsGroupHeaders.Should().BeTrue();
		flatWithStyle.ShowsGroupHeaders.Should().BeFalse();
		grouped.GetItemsHostGroupCount().Should().Be(0);
	}

	// ---------------------------------------------------------------- the items-host seam

	[Fact]
	public void When_An_Items_Host_Asks_For_The_Groups_Then_Core_Reports_Each_Displayed_Group()
	{
		//Arrange
		var groups = Groups();
		var list = new ListView { ItemsSource = GroupedView(groups) };
		list.GroupStyle.Add(new GroupStyle());

		//Act
		var shown = Enumerable.Range(0, list.GetItemsHostGroupCount()).Select(list.GetItemsHostGroup).ToArray();
		list.GroupStyle[0].HidesIfEmpty = true;
		var hidden = Enumerable.Range(0, list.GetItemsHostGroupCount()).Select(list.GetItemsHostGroup).ToArray();

		//Assert
		list.ItemsHostShowsGroupHeaders.Should().BeTrue();
		shown.Select(g => (g.FirstIndex, g.Count)).Should().Equal((0, 2), (2, 0), (2, 3));
		shown.Select(g => g.Group).Should().Equal(groups[0], groups[1], groups[2]);
		hidden.Select(g => (g.FirstIndex, g.Count)).Should().Equal((0, 2), (2, 3));
		hidden[1].Group.Should().BeSameAs(groups[2]);
	}

	[Fact]
	public void When_An_Items_Host_Creates_A_Group_Header_Then_It_Is_The_Lists_Header_Container_Prepared_From_The_GroupStyle()
	{
		//Arrange
		var groups = Groups();
		var template = HeaderTemplate();
		var containerStyle = new Style(typeof(ListViewHeaderItem));
		var list = new ListView { ItemsSource = GroupedView(groups) };
		list.GroupStyle.Add(new GroupStyle { HeaderTemplate = template, HeaderContainerStyle = containerStyle });
		var grid = new GridView { ItemsSource = GroupedView(groups) };
		grid.GroupStyle.Add(new GroupStyle { HeaderTemplate = template });

		//Act
		var header = (ContentControl)list.CreateGroupHeaderContainerForItemsHost(2);
		var gridHeader = grid.CreateGroupHeaderContainerForItemsHost(0);
		list.PrepareGroupHeaderContainerForItemsHost(header, 0);
		var rebound = header.Content;
		list.ReleaseGroupHeaderContainerFromItemsHost(header);

		//Assert
		header.Should().BeOfType<ListViewHeaderItem>();
		gridHeader.Should().BeOfType<GridViewHeaderItem>();
		((GridViewHeaderItem)gridHeader).Content.Should().BeSameAs(groups[0]);
		((GridViewHeaderItem)gridHeader).ContentTemplate.Should().BeSameAs(template);
		header.Style.Should().BeSameAs(containerStyle);
		rebound.Should().BeSameAs(groups[0]);
		header.Content.Should().BeNull();
		ItemsControl.IsGroupHeaderContainer(header).Should().BeFalse();
	}

	// ---------------------------------------------------------------- GroupStyle and GroupItem

	[Fact]
	public void When_A_GroupStyle_Property_Changes_Then_It_Raises_PropertyChanged()
	{
		//Arrange
		var style = new GroupStyle();
		var changed = new List<string?>();
		style.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		//Act
		style.HeaderTemplate = HeaderTemplate();
		style.HeaderContainerStyle = new Style(typeof(ListViewHeaderItem));
		style.HidesIfEmpty = true;
		style.HidesIfEmpty = true; // no change, no event
		style.Panel = new ItemsPanelTemplate();
		style.ContainerStyle = new Style(typeof(GroupItem));
		style.ContainerStyleSelector = new StyleSelector();

		//Assert
		changed.Should().Equal(
			nameof(GroupStyle.HeaderTemplate), nameof(GroupStyle.HeaderContainerStyle), nameof(GroupStyle.HidesIfEmpty),
			nameof(GroupStyle.Panel), nameof(GroupStyle.ContainerStyle), nameof(GroupStyle.ContainerStyleSelector));
	}

	[Fact]
	public void When_GroupItem_And_The_GroupStyle_Members_Are_Inspected_Then_None_Is_A_NotImplemented_Marker()
	{
		//Assert
		typeof(GroupItem).GetCustomAttributes(typeof(NotImplementedAttribute), inherit: false).Should().BeEmpty();
		typeof(GroupItem).GetConstructor(System.Type.EmptyTypes)!.GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
		foreach (var name in new[] { nameof(GroupStyle.Panel), nameof(GroupStyle.ContainerStyle), nameof(GroupStyle.ContainerStyleSelector) })
		{
			typeof(GroupStyle).GetProperty(name)!.GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
		}

		typeof(GroupStyle).GetEvent(nameof(GroupStyle.PropertyChanged))!.GetCustomAttributes(typeof(NotImplementedAttribute), false).Should().BeEmpty();
		new GroupItem().IsTabStop.Should().BeTrue(); // the default style (IsTabStop False) comes with the Application; the type itself is a plain ContentControl
	}

	[Fact]
	public void When_A_Grouped_ItemsControl_Has_A_Plain_Panel_Then_Each_Group_Is_A_GroupItem_Holding_Its_Item_Containers()
	{
		//Arrange
		var groups = Groups();
		var panel = new StackPanel();
		var items = new ItemsControl { ItemsPanelRoot = panel, InternalItemsPanelRoot = panel };
		items.GroupStyle.Add(new GroupStyle { HeaderTemplate = HeaderTemplate() });

		//Act
		items.ItemsSource = GroupedView(groups);
		var groupItems = panel.Children.OfType<GroupItem>().ToArray();
		var contents = groupItems.Select(g => g.Content).ToArray();
		var counts = groupItems.Select(g => g.ItemsPanel!.Children.Count).ToArray();
		var hasTemplate = groupItems[0].ContentTemplate is not null;
		var almond = items.ContainerFromIndex(2);
		var almondInGroup = ReferenceEquals(almond, groupItems[2].ItemsPanel!.Children[0]);
		var almondIndex = items.IndexFromContainer(almond);
		items.GroupStyle.Clear(); // no GroupStyle: back to the flat items
		var flatChildren = panel.Children.ToArray();

		//Assert
		groupItems.Should().HaveCount(3);
		contents.Should().Equal(groups[0], groups[1], groups[2]);
		counts.Should().Equal(2, 0, 3);
		hasTemplate.Should().BeTrue();
		almondInGroup.Should().BeTrue();
		almondIndex.Should().Be(2);
		groupItems[0].Content.Should().BeNull(); // the GroupItems were dismantled
		flatChildren.Should().HaveCount(5);
		flatChildren.OfType<GroupItem>().Should().BeEmpty();
	}

	[Fact]
	public void When_A_Grouped_GridView_Has_A_Plain_Panel_Then_Its_GroupItems_Carry_GridViewHeaderItems_And_Selection_Works_Across_Groups()
	{
		//Arrange
		var groups = Groups();
		var panel = new WrapPanel();
		var grid = new GridView { ItemsPanelRoot = panel, InternalItemsPanelRoot = panel };
		grid.GroupStyle.Add(new GroupStyle { HeaderTemplate = HeaderTemplate(), HidesIfEmpty = true });

		//Act
		grid.ItemsSource = GroupedView(groups);
		var groupItems = panel.Children.OfType<GroupItem>().ToArray();
		grid.SelectedIndex = 3;

		//Assert
		groupItems.Should().HaveCount(2); // the empty group is hidden
		groupItems.Select(g => g.Content).Should().AllBeOfType<GridViewHeaderItem>();
		((GridViewHeaderItem)groupItems[1].Content).Content.Should().BeSameAs(groups[2]);
		grid.ContainerFromIndex(3).Should().BeOfType<GridViewItem>();
		((GridViewItem)grid.ContainerFromIndex(3)).IsSelected.Should().BeTrue();
		((GridViewItem)grid.ContainerFromIndex(3)).Content.Should().Be("Walnut");
		ItemsControl.ItemsControlFromItemContainer(grid.ContainerFromIndex(3)).Should().BeSameAs(grid);
		grid.SelectedItem.Should().Be("Walnut");
	}

	[Fact]
	public void When_A_Group_Object_Changes_Then_The_Realized_Header_Of_A_Grouped_List_Is_Updated_Instead_Of_Throwing()
	{
		//Arrange
		var groups = Groups();
		var panel = new StackPanel();
		var list = new ListView { ItemsPanelRoot = panel, InternalItemsPanelRoot = panel };
		list.GroupStyle.Add(new GroupStyle());
		list.ItemsSource = GroupedView(groups);
		var view = (ICollectionView)list.ItemsSource;
		var group = (ICollectionViewGroup)view.CollectionGroups[0];

		//Act (the managed ListViewBase's ContainerFromGroupIndex threw NotImplementedException here)
		list.OnGroupPropertyChanged(group, 0);
		var header = panel.Children.OfType<GroupItem>().First().HeaderContainer;

		//Assert
		header.Should().BeOfType<ListViewHeaderItem>();
		header!.DataContext.Should().BeSameAs(groups[0]);
	}
}
