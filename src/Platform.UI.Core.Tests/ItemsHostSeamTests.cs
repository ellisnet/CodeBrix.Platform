#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The WPE1-5 seam additions requested by the Android lists lane (AP3b): the items host
/// (<see cref="ElementHandlerCapabilities.OwnsItemsHost"/> + <see cref="IItemsHostHandler"/>, hook H14), ScrollIntoView
/// routed to the handler (H15), a handler retained across Leave/Enter (H16), and the TabView / CalendarDatePicker raise
/// entry points. Each is driven through a stand-in handler, and each is inert with no factory registered (the Skia
/// configuration).
/// </summary>
public class ItemsHostSeamTests
{
	/// <summary>A root made live the way a visual tree's root is entered; elements added to it enter the live tree.</summary>
	private static Grid LiveHost()
	{
		var host = new Grid { Name = "host" };
		host.Enter(new EnterParams(isLive: true), 0);
		return host;
	}

	private const ElementHandlerCapabilities ListCapabilities = ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.OwnsItemsHost;

	// ---------------------------------------------------------------- H14: the items host

	[Fact]
	public void When_A_Handler_Owns_The_Items_Host_Then_Core_Reads_Its_Containers_And_Tells_It_About_Item_Changes()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is ListView ? new FakeItemsHostHandler(ListCapabilities) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var items = new ObservableCollection<string> { "a", "b", "c", "d" };
		var list = new ListView { ItemsSource = items };
		host.Children.Add(list);
		var handler = (FakeItemsHostHandler)list.Handler!;

		//Act
		handler.Realize(list, 0, 3); // the platform list shows rows 0..2
		var second = list.ContainerFromIndex(1);
		var fromItem = list.ContainerFromItem("c");
		var notShown = list.ContainerFromIndex(3);
		list.SelectedIndex = 1;
		items.Insert(0, "z");

		//Assert
		list.ItemsHostHandler.Should().BeSameAs(handler);
		second.Should().BeSameAs(handler.Containers[1]);
		second.Should().BeOfType<ListViewItem>();
		((ListViewItem)second).Content.Should().Be("b");
		fromItem.Should().BeSameAs(handler.Containers[2]);
		list.IndexFromContainer(handler.Containers[2]).Should().Be(3); // repaired after the insert at 0
		notShown.Should().BeNull();
		((ListViewItem)handler.Containers[1]).IsSelected.Should().BeTrue();
		handler.ItemChanges.Last().Should().NotBeNull();
		handler.ItemChanges.Last()!.Action.Should().Be(NotifyCollectionChangedAction.Add);
		handler.ItemChanges.Last()!.NewStartingIndex.Should().Be(0);
		list.ItemsPanelRoot.Should().BeNull(); // Core made no panel of its own
	}

	[Fact]
	public void When_An_Items_Host_Releases_A_Container_Then_Core_Clears_It_And_Forgets_Its_Index()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is ListView ? new FakeItemsHostHandler(ListCapabilities) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var list = new ListView { ItemsSource = new[] { "a", "b" } };
		host.Children.Add(list);
		var handler = (FakeItemsHostHandler)list.Handler!;
		handler.Realize(list, 0, 2);
		var recycled = handler.Containers[0];

		//Act
		handler.Containers.RemoveAt(0);
		list.ReleaseContainerFromItemsHost(recycled);
		list.PrepareContainerForItemsHost(recycled, 1); // re-bound to another row, as a recycler does

		//Assert
		((ListViewItem)recycled).Content.Should().Be("b");
		list.IndexFromContainer(recycled).Should().Be(1);
	}

	[Fact]
	public void When_A_Handler_Does_Not_Own_The_Items_Host_Then_The_ItemsControl_Keeps_Its_Own_Container_Path()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is ListView ? new FakeItemsHostHandler(ElementHandlerCapabilities.OwnsVisuals) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var items = new ObservableCollection<string> { "a" };
		var list = new ListView { ItemsSource = items };
		host.Children.Add(list);
		var handler = (FakeItemsHostHandler)list.Handler!;

		//Act
		items.Add("b");
		list.ScrollIntoView("b");

		//Assert
		list.ItemsHostHandler.Should().BeNull();
		handler.ItemChanges.Should().BeEmpty();
		handler.Invokes.Should().BeEmpty();
	}

	// ---------------------------------------------------------------- H15: ScrollIntoView

	[Fact]
	public void When_A_List_Whose_Handler_Owns_The_Items_Host_Scrolls_An_Item_Into_View_Then_The_Handler_Is_Invoked_With_The_Request()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is ListView ? new FakeItemsHostHandler(ListCapabilities) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var list = new ListView { ItemsSource = Enumerable.Range(0, 50).Select(i => $"row {i}").ToList() };
		host.Children.Add(list);
		var handler = (FakeItemsHostHandler)list.Handler!;

		//Act
		list.ScrollIntoView("row 42");
		list.ScrollIntoView("row 7", ScrollIntoViewAlignment.Leading);

		//Assert
		handler.Invokes.Select(i => i.Command).Should().Equal(ElementHandlerCommands.ScrollIntoView, ElementHandlerCommands.ScrollIntoView);
		var first = (ScrollIntoViewRequest)handler.Invokes[0].Args!;
		first.Item.Should().Be("row 42");
		first.Index.Should().Be(42);
		first.Alignment.Should().Be(ScrollIntoViewAlignment.Default);
		var second = (ScrollIntoViewRequest)handler.Invokes[1].Args!;
		second.Index.Should().Be(7);
		second.Alignment.Should().Be(ScrollIntoViewAlignment.Leading);
	}

	[Fact]
	public void When_No_Handler_Factory_Is_Registered_Then_A_List_Has_No_Items_Host_And_ScrollIntoView_Asks_No_Handler()
	{
		//Arrange
		using var _ = ElementHandlerTestPlatform.Activate(null);
		var host = LiveHost();
		var list = new ListView { ItemsSource = new[] { "a", "b" } };
		host.Children.Add(list);

		//Act
		list.ScrollIntoView("b");

		//Assert
		UIElement.AreHandlersActive.Should().BeFalse();
		list.Handler.Should().BeNull();
		list.ItemsHostHandler.Should().BeNull();
	}

	// ---------------------------------------------------------------- H16: handler retained across Leave/Enter

	[Fact]
	public void When_A_Handler_Is_Retained_Across_Leave_Then_The_Same_Handler_Is_Connected_Again_Without_Asking_The_Factory()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is Border { Name: "row" }
			? new FakeElementHandler(ElementHandlerCapabilities.RetainedAcrossLeave) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var row = new Border { Name = "row" };
		host.Children.Add(row);
		var handler = (FakeElementHandler)row.Handler!;

		//Act
		host.Children.Remove(row);
		var afterLeave = (Handler: row.Handler, Retained: row.HasRetainedHandler, handler.DisconnectCount);
		row.Width = 12; // a detached row's native view stays current
		host.Children.Add(row);
		var afterReEnter = (Handler: row.Handler, Retained: row.HasRetainedHandler, handler.ConnectCount, Created: factory.Created.Count);

		//Assert
		afterLeave.Handler.Should().BeSameAs(handler);
		afterLeave.Retained.Should().BeTrue();
		afterLeave.DisconnectCount.Should().Be(1);
		handler.Updates.Should().Contain(FrameworkElement.WidthProperty);
		afterReEnter.Handler.Should().BeSameAs(handler);
		afterReEnter.Retained.Should().BeFalse();
		afterReEnter.ConnectCount.Should().Be(2);
		afterReEnter.Created.Should().Be(1); // the factory made no second handler for the row
	}

	[Fact]
	public void When_A_Retained_Handler_Is_Released_Then_The_Next_Enter_Creates_A_New_Handler()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is Border { Name: "row" }
			? new FakeElementHandler(ElementHandlerCapabilities.RetainedAcrossLeave) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var row = new Border { Name = "row" };
		host.Children.Add(row);
		var first = row.Handler;
		host.Children.Remove(row);

		//Act
		var releasedWhileLive = false;
		var released = row.ReleaseRetainedHandler();
		host.Children.Add(row);
		releasedWhileLive = row.ReleaseRetainedHandler();

		//Assert
		released.Should().BeTrue();
		releasedWhileLive.Should().BeFalse();
		row.Handler.Should().NotBeNull().And.NotBeSameAs(first);
		factory.Created.Count.Should().Be(2);
	}

	[Fact]
	public void When_A_Handler_Is_Not_Retained_Then_Leave_Still_Forgets_It()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is Border { Name: "row" } ? new FakeElementHandler() : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var row = new Border { Name = "row" };
		host.Children.Add(row);

		//Act
		host.Children.Remove(row);

		//Assert
		row.Handler.Should().BeNull();
		row.HasRetainedHandler.Should().BeFalse();
	}

	// ---------------------------------------------------------------- raise entry points

	[Fact]
	public void When_A_Platform_Selects_A_Tab_Of_A_Template_Less_TabView_Then_SelectionChanged_Is_Raised_Once_With_The_Items()
	{
		//Arrange
		var tabView = new TabView { TabItemsSource = new[] { "one", "two", "three" } };
		var events = new List<SelectionChangedEventArgs>();
		tabView.SelectionChanged += (_, e) => events.Add(e);

		//Act
		tabView.RaiseSelectionChangedFromPlatform(2);
		tabView.RaiseSelectionChangedFromPlatform(2); // the same tab again: nothing

		//Assert
		events.Count.Should().Be(1);
		events[0].AddedItems.Should().Equal("three");
		tabView.SelectedIndex.Should().Be(2);
		tabView.SelectedItem.Should().Be("three");
	}

	[Fact]
	public void When_A_Platform_Moves_The_Selection_Of_A_TabView_Then_The_Previous_Tab_Is_Reported_As_Removed()
	{
		//Arrange
		var tabView = new TabView { TabItemsSource = new[] { "one", "two" } };
		tabView.RaiseSelectionChangedFromPlatform(0);
		var events = new List<SelectionChangedEventArgs>();
		tabView.SelectionChanged += (_, e) => events.Add(e);

		//Act
		tabView.RaiseSelectionChangedFromPlatform(1);

		//Assert
		events.Count.Should().Be(1);
		events[0].RemovedItems.Should().Equal("one");
		events[0].AddedItems.Should().Equal("two");
	}

	[Fact]
	public void When_A_Platform_Opens_And_Closes_The_Calendar_Of_A_CalendarDatePicker_Then_Opened_And_Closed_Are_Raised()
	{
		//Arrange
		var picker = new CalendarDatePicker();
		var events = new List<string>();
		picker.Opened += (_, _) => events.Add($"Opened {picker.IsCalendarOpen}");
		picker.Closed += (_, _) => events.Add($"Closed {picker.IsCalendarOpen}");

		//Act
		picker.RaiseOpenedFromPlatform();
		picker.RaiseClosedFromPlatform();

		//Assert
		events.Should().Equal("Opened True", "Closed False");
	}
}

/// <summary>
/// A stand-in for a platform list (a recycling native list): it owns the items host, realizes the containers it is told
/// to through Core's entry points, and records the item changes Core reports.
/// </summary>
internal sealed class FakeItemsHostHandler : FakeElementHandler, IItemsHostHandler
{
	internal FakeItemsHostHandler(ElementHandlerCapabilities capabilities)
		: base(capabilities)
	{
	}

	internal List<DependencyObject> Containers { get; } = new();

	internal List<NotifyCollectionChangedEventArgs?> ItemChanges { get; } = new();

	public int FirstVisibleIndex { get; set; } = -1;

	public int LastVisibleIndex { get; set; } = -1;

	/// <summary>Realizes <paramref name="count"/> containers from <paramref name="first"/>, as a list scrolled there would.</summary>
	internal void Realize(ItemsControl owner, int first, int count)
	{
		for (var i = first; i < first + count; i++)
		{
			Containers.Add(owner.CreateContainerForItemsHost(i));
		}

		FirstVisibleIndex = first;
		LastVisibleIndex = first + count - 1;
	}

	public IEnumerable<DependencyObject> GetMaterializedContainers() => Containers;

	public void OnItemsChanged(NotifyCollectionChangedEventArgs? args) => ItemChanges.Add(args);
}
