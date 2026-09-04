using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SilverAssertions;
using Windows.Foundation;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The bar itself: how it lays its items out, what it does with the ones that do not fit, and how
/// the keyboard walks along it.
/// </summary>
/// <remarks>
/// Every test here is host-free. The bar's own panel, rather than an items presenter, is what makes
/// that possible: an items presenter only builds its panel once it is loaded into a window, and a
/// test process has no window. See DefaultStyleInitializer for the one thing a test process does
/// have to do that an application head does for itself.
/// </remarks>
public class ToolBarTests
{
	private const double BarPadding = 8d;

	[Fact]
	public void OverflowMode_none_keeps_every_item_in_the_bar()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.None, 5);

		//Act
		LayOut(bar, 120, 100);

		//Assert
		bar.HasOverflowItems.Should().BeFalse();
		bar.ItemsHost!.Children.Should().HaveCount(5);
		bar.OverflowHost.Children.Should().BeEmpty();
	}

	[Theory]
	//The bar's own padding is 4 a side, so the items are laid in width - 8. The partition arithmetic
	//itself is pinned in ToolBarLayoutTests; these prove the bar really applies it.
	[InlineData(500, 5, 0)]
	[InlineData(260, 4, 1)]
	[InlineData(200, 3, 2)]
	[InlineData(150, 2, 3)]
	[InlineData(100, 1, 4)]
	public void OverflowMode_chevron_moves_the_trailing_items_into_the_flyout(
		double width, int expectedInBar, int expectedInFlyout)
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);

		//Act
		LayOut(bar, width, 100);

		//Assert
		var itemsInBar = bar.ItemsHost!.Children.Count(c => c is Border);
		itemsInBar.Should().Be(expectedInBar);
		bar.OverflowHost.Children.Should().HaveCount(expectedInFlyout);
		bar.HasOverflowItems.Should().Be(expectedInFlyout > 0);
	}

	[Fact]
	public void OverflowMode_chevron_shows_the_chevron_only_while_something_has_overflowed()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);

		//Act
		LayOut(bar, 500, 100);
		var wide = bar.ItemsHost!.Children.Any(c => c is ToolBarOverflowButton);
		LayOut(bar, 150, 100);
		var narrow = bar.ItemsHost.Children.Any(c => c is ToolBarOverflowButton);

		//Assert
		wide.Should().BeFalse();
		narrow.Should().BeTrue();
	}

	[Fact]
	public void OverflowMode_chevron_returns_the_same_element_instances_when_the_space_returns()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);
		var declared = bar.Items.Cast<UIElement>().ToArray();

		//Act
		LayOut(bar, 150, 100);
		var moved = bar.OverflowHost.Children.ToArray();
		LayOut(bar, 500, 100);

		//Assert
		//The SAME instances, in their original order - not copies. Bindings, handlers and toggle
		//state only survive overflow because the elements themselves make the trip.
		moved.Should().NotBeEmpty();
		bar.OverflowHost.Children.Should().BeEmpty();
		bar.ItemsHost!.Children.Cast<UIElement>().ToArray().Should().Equal(declared);
	}

	[Fact]
	public void OverflowMode_chevron_keeps_the_items_in_order_across_the_two_panels()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);
		var declared = bar.Items.Cast<UIElement>().ToArray();

		//Act
		LayOut(bar, 200, 100);
		var inBar = bar.ItemsHost!.Children.Where(c => c is Border).ToArray();
		var inFlyout = bar.OverflowHost.Children.ToArray();

		//Assert
		inBar.Concat(inFlyout).Should().Equal(declared);
	}

	[Fact]
	public void OverflowMode_wrap_puts_the_items_that_do_not_fit_on_a_further_line()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Wrap, 5);

		//Act
		LayOut(bar, 150, 200);

		//Assert
		//Nothing leaves the bar in wrap mode; the bar simply grows taller. Five 50-pixel items in
		//142 make three lines of 20 with a 4-pixel gap - 68 - plus the bar's 8 of padding and its
		//one-pixel bottom border.
		bar.HasOverflowItems.Should().BeFalse();
		bar.ItemsHost!.Children.Should().HaveCount(5);
		bar.DesiredSize.Height.Should().BeApproximately(77, 0.5);
	}

	[Fact]
	public void Visibility_collapsed_items_take_no_space_and_never_overflow()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);
		for (var i = 1; i < 5; i++)
		{
			((UIElement)bar.Items[i]).Visibility = Visibility.Collapsed;
		}

		//Act
		LayOut(bar, 150, 100);

		//Assert
		//One visible 50-pixel item fits in 142 with room to spare, so nothing overflows even though
		//the bar holds five items.
		bar.HasOverflowItems.Should().BeFalse();
		bar.OverflowHost.Children.Should().BeEmpty();
		bar.ItemsHost!.ActualWidth.Should().BeApproximately(142, 0.5);
	}

	[Fact]
	public void ItemSpacing_puts_the_stated_gap_between_two_items()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.None, 2);
		bar.ItemSpacing = 12;

		//Act
		LayOut(bar, 500, 100);

		//Assert
		//Two 50-pixel items with a 12-pixel gap.
		bar.DesiredSize.Width.Should().BeApproximately(50 + 12 + 50 + BarPadding, 0.5);
	}

	[Fact]
	public void IsCompact_halves_the_gap_between_two_items()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.None, 2);
		bar.ItemSpacing = 12;
		bar.IsCompact = true;

		//Act
		LayOut(bar, 500, 100);

		//Assert
		bar.ItemsHost!.ItemSpacing.Should().Be(6);
	}

	[Fact]
	public void Orientation_vertical_stacks_the_items_down_the_bar()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.None, 3);
		bar.Orientation = Orientation.Vertical;

		//Act
		LayOut(bar, 200, 500);

		//Assert
		//Three 20-pixel-tall items with the default 4-pixel gap, plus the bar's padding and its
		//one-pixel bottom border.
		bar.DesiredSize.Height.Should().BeApproximately((3 * 20) + (2 * 4) + BarPadding + 1, 0.5);
		bar.DesiredSize.Width.Should().BeApproximately(50 + BarPadding, 0.5);
	}

	[Fact]
	public void Orientation_vertical_turns_the_separators_and_groups_it_hosts()
	{
		//Arrange
		var bar = new ToolBar { Orientation = Orientation.Vertical, OverflowMode = OverflowMode.None };
		var separator = new ToolBarSeparator();
		var group = new ToolBarGroup();
		bar.Items.Add(group);
		bar.Items.Add(separator);

		//Act
		LayOut(bar, 200, 500);

		//Assert
		//The line runs across the bar, and the group runs along it.
		separator.Orientation.Should().Be(Orientation.Horizontal);
		group.Orientation.Should().Be(Orientation.Vertical);
	}

	[Fact]
	public void SeparatorBetweenGroups_inserts_a_separator_between_two_adjacent_groups()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		bar.Items.Add(CreateGroup(2));
		bar.Items.Add(CreateGroup(2));

		//Act
		LayOut(bar, 500, 100);

		//Assert
		bar.LayoutItems.Should().HaveCount(3);
		bar.LayoutItems[1].Should().BeOfType<ToolBarSeparator>();
	}

	[Fact]
	public void SeparatorBetweenGroups_does_not_double_a_separator_the_application_wrote()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		bar.Items.Add(CreateGroup(2));
		bar.Items.Add(new ToolBarSeparator());
		bar.Items.Add(CreateGroup(2));

		//Act
		LayOut(bar, 500, 100);

		//Assert
		bar.LayoutItems.Should().HaveCount(3);
		bar.LayoutItems.Count(i => i is ToolBarSeparator).Should().Be(1);
	}

	[Fact]
	public void SeparatorBetweenGroups_false_inserts_nothing()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None, SeparatorBetweenGroups = false };
		bar.Items.Add(CreateGroup(2));
		bar.Items.Add(CreateGroup(2));

		//Act
		LayOut(bar, 500, 100);

		//Assert
		bar.LayoutItems.Should().HaveCount(2);
		bar.LayoutItems.Should().NotContain(i => i is ToolBarSeparator);
	}

	[Fact]
	public void SeparatorBetweenGroups_leaves_a_lone_group_alone()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		bar.Items.Add(CreateGroup(2));

		//Act
		LayOut(bar, 500, 100);

		//Assert
		bar.LayoutItems.Should().HaveCount(1);
	}

	[Fact]
	public void IconSize_set_on_the_bar_reaches_the_items_through_the_attached_property()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		var item = new Border();
		bar.Items.Add(item);

		//Act
		bar.IconSize = 32;
		bar.LabelMode = LabelMode.IconAndText;
		bar.ShowToolTips = false;
		LayOut(bar, 500, 100);

		//Assert
		ToolBarProperties.GetIconSize(bar).Should().Be(32);
		ToolBarProperties.GetLabelMode(bar).Should().Be(LabelMode.IconAndText);
		ToolBarProperties.GetShowToolTips(bar).Should().BeFalse();
	}

	[Fact]
	public void IconSize_left_unset_on_the_bar_does_not_overwrite_a_value_set_above_it()
	{
		//Arrange
		//A tray states the icon size for every bar under it; a bar that says nothing must let it
		//through rather than pushing its own default over the top. This is the fence on the
		//name-shadowing trap recorded on ToolBar's dependency-property block: a bar that registered
		//a dependency property of its own called IconSize would read 24 here.
		var tray = new ToolBarTray();
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		tray.Children.Add(bar);
		ToolBarProperties.SetIconSize(tray, 40);

		//Act
		LayOut(tray, 500, 100);

		//Assert
		ToolBarProperties.GetIconSize(bar).Should().Be(40);
		bar.IconSize.Should().Be(40);
	}

	[Fact]
	public void IconSize_set_on_the_bar_reaches_an_item_inside_it()
	{
		//Arrange
		//The whole point of the four bar settings: one statement on the bar, read by every item,
		//with no plumbing in between and nothing for the item to subscribe to.
		var tray = new ToolBarTray();
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		var item = new Border();
		bar.Items.Add(item);
		tray.Children.Add(bar);
		ToolBarProperties.SetIconSize(tray, 40);

		//Act
		bar.IconSize = 32;
		LayOut(tray, 500, 100);

		//Assert
		//The bar's own value wins over the tray's for the items below it.
		ToolBarProperties.GetIconSize(item).Should().Be(32);
	}

	[Fact]
	public void Title_becomes_the_bar_accessibility_name()
	{
		//Arrange
		var bar = new ToolBar();

		//Act
		bar.Title = "Music";

		//Assert
		AutomationProperties.GetName(bar).Should().Be("Music");
	}

	[Fact]
	public void ShowOverflow_answers_false_when_the_bar_is_not_in_a_window()
	{
		//Arrange
		var bar = CreateBar(OverflowMode.Chevron, 5);
		LayOut(bar, 150, 100);

		//Act
		var shown = bar.ShowOverflow();

		//Assert
		//A flyout needs a window to appear in. The bar reports that it did not open one rather
		//than throwing.
		bar.HasOverflowItems.Should().BeTrue();
		shown.Should().BeFalse();
	}

	[Fact]
	public void GetKeyboardNavigationItems_flattens_a_group_and_skips_a_separator_and_a_spacer()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		var first = new Button();
		var group = new ToolBarGroup();
		var inGroup = new Button();
		group.Children.Add(inGroup);
		var last = new Button();
		bar.Items.Add(first);
		bar.Items.Add(new ToolBarSeparator());
		bar.Items.Add(group);
		bar.Items.Add(new ToolBarSpacer { Fill = true });
		bar.Items.Add(last);
		LayOut(bar, 500, 100);

		//Act
		var items = bar.GetKeyboardNavigationItems();

		//Assert
		//A group is a container, not a stop; a separator and a spacer are not focusable at all.
		items.Should().Equal(first, inGroup, last);
	}

	[Fact]
	public void GetKeyboardNavigationItems_ends_with_the_chevron_once_the_bar_has_overflowed()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.Chevron };
		for (var i = 0; i < 5; i++)
		{
			bar.Items.Add(new Button { Width = 50, Height = 20 });
		}

		//Act
		LayOut(bar, 150, 100);
		var items = bar.GetKeyboardNavigationItems();

		//Assert
		bar.HasOverflowItems.Should().BeTrue();
		items[items.Count - 1].Should().BeSameAs(bar.OverflowButton);
	}

	[Fact]
	public void GetNavigationTarget_moves_right_and_left_along_a_horizontal_bar()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var next = bar.GetNavigationTarget(items[0], VirtualKey.Right);
		var back = bar.GetNavigationTarget(items[1], VirtualKey.Left);

		//Assert
		next.Should().BeSameAs(items[1]);
		back.Should().BeSameAs(items[0]);
	}

	[Fact]
	public void GetNavigationTarget_moves_down_and_up_along_a_vertical_bar()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Vertical, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var next = bar.GetNavigationTarget(items[0], VirtualKey.Down);
		var back = bar.GetNavigationTarget(items[1], VirtualKey.Up);
		var sideways = bar.GetNavigationTarget(items[0], VirtualKey.Right);

		//Assert
		next.Should().BeSameAs(items[1]);
		back.Should().BeSameAs(items[0]);
		//Across the bar is not along it: a vertical bar leaves Left and Right to its items.
		sideways.Should().BeNull();
	}

	[Fact]
	public void GetNavigationTarget_does_not_wrap_around_the_ends()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var pastStart = bar.GetNavigationTarget(items[0], VirtualKey.Left);
		var pastEnd = bar.GetNavigationTarget(items[2], VirtualKey.Right);

		//Assert
		//A held arrow key stops at the end of the bar instead of cycling it.
		pastStart.Should().BeSameAs(items[0]);
		pastEnd.Should().BeSameAs(items[2]);
	}

	[Fact]
	public void GetNavigationTarget_home_and_end_jump_to_the_ends()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var home = bar.GetNavigationTarget(items[2], VirtualKey.Home);
		var end = bar.GetNavigationTarget(items[0], VirtualKey.End);

		//Assert
		home.Should().BeSameAs(items[0]);
		end.Should().BeSameAs(items[2]);
	}

	[Fact]
	public void GetNavigationTarget_from_outside_the_bar_enters_at_the_near_end()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var forward = bar.GetNavigationTarget(null, VirtualKey.Right);
		var backward = bar.GetNavigationTarget(null, VirtualKey.Left);

		//Assert
		forward.Should().BeSameAs(items[0]);
		backward.Should().BeSameAs(items[2]);
	}

	[Fact]
	public void GetNavigationTarget_finds_the_item_from_a_focused_element_inside_it()
	{
		//Arrange
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		var group = new ToolBarGroup();
		var first = new Button();
		var second = new Button();
		group.Children.Add(first);
		group.Children.Add(second);
		bar.Items.Add(group);
		LayOut(bar, 500, 100);

		//Act
		var target = bar.GetNavigationTarget(first, VirtualKey.Right);

		//Assert
		target.Should().BeSameAs(second);
	}

	[Fact]
	public void TryHandleNavigationKey_consumes_the_drop_down_key_for_an_item_with_a_flyout()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();
		FlyoutBase.SetAttachedFlyout(items[0], new MenuFlyout());

		//Act
		var handled = bar.TryHandleNavigationKey(VirtualKey.Down, items[0]);

		//Assert
		//Down belongs to the item's drop-down in a horizontal bar, so the bar does not also move
		//focus with it. Showing the flyout itself needs a window; the key is the drop-down's either
		//way.
		handled.Should().BeTrue();
	}

	[Fact]
	public void TryHandleNavigationKey_leaves_the_drop_down_key_alone_for_an_item_without_a_flyout()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var handled = bar.TryHandleNavigationKey(VirtualKey.Down, items[0]);

		//Assert
		handled.Should().BeFalse();
	}

	[Fact]
	public void TryHandleNavigationKey_reports_a_key_that_moves_nothing_as_unhandled()
	{
		//Arrange
		var bar = CreateButtonBar(Orientation.Horizontal, 3);
		var items = bar.GetKeyboardNavigationItems();

		//Act
		var atEnd = bar.TryHandleNavigationKey(VirtualKey.Right, items[2]);
		var unrelated = bar.TryHandleNavigationKey(VirtualKey.A, items[0]);

		//Assert
		atEnd.Should().BeFalse();
		unrelated.Should().BeFalse();
	}

	private static ToolBar CreateBar(OverflowMode mode, int items)
	{
		var bar = new ToolBar { OverflowMode = mode };
		for (var i = 0; i < items; i++)
		{
			bar.Items.Add(new Border { Width = 50, Height = 20 });
		}

		return bar;
	}

	private static ToolBar CreateButtonBar(Orientation orientation, int items)
	{
		var bar = new ToolBar { OverflowMode = OverflowMode.None, Orientation = orientation };
		for (var i = 0; i < items; i++)
		{
			bar.Items.Add(new Button { Width = 50, Height = 20 });
		}

		LayOut(bar, 500, 500);

		return bar;
	}

	private static ToolBarGroup CreateGroup(int items)
	{
		var group = new ToolBarGroup();
		for (var i = 0; i < items; i++)
		{
			group.Children.Add(new Border { Width = 30, Height = 20 });
		}

		return group;
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
