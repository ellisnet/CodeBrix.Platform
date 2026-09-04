using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The seams: what happens where the bar, the buttons and the icons meet, assembled the way an
/// application assembles them.
/// </summary>
/// <remarks>
/// <para>
/// Every other suite here tests one part on its own - a bar full of Borders, a button parented to a
/// group, an icon source with no button around it. These tests do the opposite: they build the real
/// arrangement (a tray holding bars, holding groups, holding buttons, holding icons) and measure
/// what an application would see. Every defect this file fences was found by running it, not by
/// reading the three parts.
/// </para>
/// <para>
/// Host-free, like the rest of the suite. The on-screen half of the same seams is driven by
/// CommandBarDemo's self-test on the X11 head.
/// </para>
/// </remarks>
public class ToolBarIntegrationTests
{
	/// <summary>Prepares the process the way an application head would have.</summary>
	public ToolBarIntegrationTests() => TestHost.EnsureReady();

	#region A bar holding buttons with icons

	[Fact]
	public void An_svg_icon_source_on_a_button_in_a_bar_becomes_an_svg_icon_element()
	{
		//Arrange
		var button = new ToolButton
		{
			Text = "Open",
			Icon = new SvgIconSource { Source = IconFixtures.FileUri(IconFixtures.CurrentColorSvg) },
		};
		var bar = CreateBar(button);

		//Act
		LayOut(bar, 400, 100);

		//Assert
		//The core's IconSourceElement understands only its own four icon-source types, so the button
		//goes through IconSource.CreateIconElement() instead. This is the test that would notice if
		//it stopped doing so.
		button.IconVisual.Should().BeOfType<SvgIcon>();
	}

	[Fact]
	public void A_raster_icon_source_on_a_button_in_a_bar_resolves_its_artwork()
	{
		//Arrange
		var button = new ToolButton
		{
			Text = "Magnifier",
			Icon = new RasterIconSource { Source = IconFixtures.FileUri(IconFixtures.AlphaPng) },
		};
		var bar = CreateBar(button);

		//Act
		LayOut(bar, 400, 100);

		//Assert
		var icon = button.IconVisual.Should().BeOfType<RasterIcon>().Subject;
		icon.ResolvedUriSource.Should().Be(IconFixtures.FileUri(IconFixtures.AlphaPng));
	}

	[Fact]
	public void The_bars_icon_size_reaches_an_svg_icon_inside_a_button()
	{
		//Arrange
		var button = new ToolButton
		{
			Icon = new SvgIconSource { Source = IconFixtures.FileUri(IconFixtures.CurrentColorSvg) },
		};
		var bar = CreateBar(button);
		bar.IconSize = 40d;

		//Act
		LayOut(bar, 400, 100);

		//Assert
		//An icon is rasterised at its size, not stretched to it, so the size the bar chose has to
		//reach the icon BEFORE it renders - not just the slot the icon sits in.
		var icon = (SvgIcon)button.IconVisual!;
		icon.EffectiveIconSize.Should().Be(40d);
		icon.Width.Should().Be(40d);
		icon.LastKey.Size.Should().Be(40d);
	}

	[Fact]
	public void The_bars_icon_size_reaches_a_raster_icon_inside_a_button()
	{
		//Arrange
		var button = new ToolButton
		{
			Icon = new RasterIconSource { Source = IconFixtures.FileUri(IconFixtures.AlphaPng) },
		};
		var bar = CreateBar(button);
		bar.IconSize = 40d;

		//Act
		LayOut(bar, 400, 100);

		//Assert
		var icon = (RasterIcon)button.IconVisual!;
		icon.EffectiveIconSize.Should().Be(40d);
		icon.Width.Should().Be(40d);
	}

	[Fact]
	public void An_icon_re_rasterises_when_the_bar_changes_its_icon_size_at_run_time()
	{
		//Arrange
		var button = new ToolButton
		{
			Icon = new SvgIconSource { Source = IconFixtures.FileUri(IconFixtures.CurrentColorSvg) },
		};
		var bar = CreateBar(button);
		LayOut(bar, 400, 100);
		var icon = (SvgIcon)button.IconVisual!;
		var before = icon.LastKey.Size;

		//Act
		bar.IconSize = 48d;
		LayOut(bar, 400, 120);

		//Assert
		//Frescobaldi lets the user pick the tool bar icon size while the window is open. Rendering
		//the icon once at start-up and stretching it afterwards is exactly what this add-in exists
		//to avoid.
		before.Should().Be(ToolBarProperties.DefaultIconSize);
		icon.LastKey.Size.Should().Be(48d);
		icon.Width.Should().Be(48d);
	}

	[Fact]
	public void A_button_that_sets_its_own_icon_size_keeps_it_when_the_bar_sets_another()
	{
		//Arrange
		var barSized = new ToolButton
		{
			Icon = new SvgIconSource { Source = IconFixtures.FileUri(IconFixtures.CurrentColorSvg) },
		};
		var selfSized = new ToolButton
		{
			Icon = new SvgIconSource { Source = IconFixtures.FileUri(IconFixtures.CurrentColorSvg) },
		};
		ToolBarProperties.SetIconSize(selfSized, 16d);

		var bar = CreateBar(barSized, selfSized);
		bar.IconSize = 40d;

		//Act
		LayOut(bar, 400, 100);

		//Assert
		((SvgIcon)barSized.IconVisual!).EffectiveIconSize.Should().Be(40d);
		((SvgIcon)selfSized.IconVisual!).EffectiveIconSize.Should().Be(16d);
	}

	#endregion

	#region A tray of two bars overflowing

	[Fact]
	public void A_tray_too_narrow_for_its_bars_wraps_the_second_and_overflows_the_first()
	{
		//Arrange
		var first = CreateBar(Button("New"), Button("Open"), Button("Save"), Button("Print"));
		var second = CreateBar(Button("Note"), Button("Rest"));
		var tray = new ToolBarTray();
		tray.Children.Add(first);
		tray.Children.Add(second);

		//Act
		//Narrow enough that the first bar cannot hold its four buttons, so both halves of the
		//tray's job are under test at once: the wrap, and the bar being told what it has to fit in.
		LayOut(tray, 120, 400);

		//Assert
		//A wrapping tray has to offer each bar the LINE's width rather than infinity, or the bar
		//decides nothing overflows and is then simply clipped.
		first.HasOverflowItems.Should().BeTrue();
		second.ActualHeight.Should().BeGreaterThan(0);
		tray.DesiredSize.Height.Should().BeGreaterThan(first.DesiredSize.Height);
	}

	[Fact]
	public void A_button_that_moved_into_the_overflow_is_the_same_instance_and_comes_back()
	{
		//Arrange
		var tail = Button("Print");
		var bar = CreateBar(Button("New"), Button("Open"), Button("Save"), tail);
		var command = new SwitchableCommand();
		tail.Command = command;

		//Act
		LayOut(bar, 150, 100);
		var inFlyout = bar.OverflowHost.Children.Contains(tail);
		LayOut(bar, 600, 100);

		//Assert
		inFlyout.Should().BeTrue();
		bar.ItemsHost!.Children.Should().Contain(tail);
		//The trip through the flyout must not cost the button its command binding: it is the same
		//element, re-parented, not a copy.
		tail.Command.Should().BeSameAs(command);
		tail.IsEnabled.Should().BeTrue();
	}

	[Fact]
	public void A_button_in_the_bar_follows_its_commands_can_execute()
	{
		//Arrange
		var button = Button("Print");
		var command = new SwitchableCommand();
		button.Command = command;
		var bar = CreateBar(Button("New"), button);
		LayOut(bar, 600, 100);

		//Act
		command.SetCanExecute(false);

		//Assert
		//The control for the overflow test below: being parented into the bar's own panel costs the
		//button nothing, so a failure there is about the overflow and not about parenting.
		bar.ItemsHost!.Children.Should().Contain(button);
		button.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void A_button_in_the_overflow_still_follows_its_commands_can_execute()
	{
		//Arrange
		var tail = Button("Print");
		var command = new SwitchableCommand();
		tail.Command = command;
		var bar = CreateBar(Button("New"), Button("Open"), Button("Save"), tail);
		LayOut(bar, 150, 100);

		//Act
		command.SetCanExecute(false);

		//Assert
		//Re-parenting unloads and reloads the element; a command subscription dropped on unload and
		//never restored is the shape of Fresco's trap 41, and this is where it would show up.
		bar.OverflowHost.Children.Should().Contain(tail);
		tail.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void A_drop_down_button_keeps_its_flyout_across_a_trip_into_the_overflow()
	{
		//Arrange
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Recent" });
		var dropDown = new ToolDropDownButton { Text = "Open", Flyout = flyout };
		var bar = CreateBar(Button("New"), Button("Save"), Button("Print"), dropDown);

		//Act
		LayOut(bar, 150, 100);
		var inFlyout = bar.OverflowHost.Children.Contains(dropDown);
		LayOut(bar, 600, 100);

		//Assert
		inFlyout.Should().BeTrue();
		dropDown.Flyout.Should().BeSameAs(flyout);
		bar.ItemsHost!.Children.Should().Contain(dropDown);
	}

	#endregion

	#region A drop-down inside a bar, opened by the keyboard

	[Fact]
	public void The_drop_down_key_opens_a_drop_down_button_in_the_bar()
	{
		//Arrange
		var dropDown = new ToolDropDownButton { Text = "Open", Flyout = new MenuFlyout() };
		var bar = CreateBar(Button("New"), dropDown);
		LayOut(bar, 400, 100);

		//Act
		var handled = bar.TryHandleNavigationKey(VirtualKey.Down, dropDown);

		//Assert
		//A ToolDropDownButton carries its menu in its own Flyout property, not as an attached
		//flyout, so a bar that only looked for an attached one would move focus with Down instead
		//of opening the menu. Showing the flyout itself needs a window; the key is the
		//drop-down's either way.
		handled.Should().BeTrue();
	}

	[Fact]
	public void The_drop_down_key_moves_on_past_a_drop_down_button_that_has_no_flyout()
	{
		//Arrange
		var dropDown = new ToolDropDownButton { Text = "Open" };
		var bar = CreateBar(Button("New"), dropDown);
		LayOut(bar, 400, 100);

		//Act
		var handled = bar.TryHandleNavigationKey(VirtualKey.Down, dropDown);

		//Assert
		handled.Should().BeFalse();
	}

	[Fact]
	public void The_drop_down_key_of_a_vertical_bar_opens_a_drop_down_button_in_it()
	{
		//Arrange
		var dropDown = new ToolDropDownButton { Text = "Open", Flyout = new MenuFlyout() };
		var bar = CreateBar(Button("New"), dropDown);
		bar.Orientation = Orientation.Vertical;
		LayOut(bar, 100, 400);

		//Act
		var alongTheBar = bar.TryHandleNavigationKey(VirtualKey.Down, dropDown);
		var acrossTheBar = bar.TryHandleNavigationKey(VirtualKey.Right, dropDown);

		//Assert
		//In a vertical bar Down walks along the bar and Right is the drop-down key, the other way
		//round from a horizontal one.
		alongTheBar.Should().BeFalse();
		acrossTheBar.Should().BeTrue();
	}

	[Fact]
	public void One_menu_flyout_can_be_shared_by_two_drop_down_buttons()
	{
		//Arrange
		var command = new SwitchableCommand();
		var shared = new MenuFlyout();
		shared.Items.Add(new MenuFlyoutItem { Text = "Engrave", Command = command });

		var first = new ToolDropDownButton { Text = "Engrave", Flyout = shared };
		var second = new ToolDropDownButton { Text = "Engrave again", Flyout = shared };
		var bar = CreateBar(first, second);

		//Act
		LayOut(bar, 600, 100);
		var rehookedFromFirst = first.RehookFlyoutBindings();
		var rehookedFromSecond = second.RehookFlyoutBindings();

		//Assert
		//One command source feeding a tool bar button and a menu at once is what a shared flyout is
		//for: the flyout is a reference, not a copy, so neither button owns it.
		first.Flyout.Should().BeSameAs(shared);
		second.Flyout.Should().BeSameAs(shared);
		rehookedFromFirst.Should().Be(1);
		rehookedFromSecond.Should().Be(1);
		((MenuFlyoutItem)shared.Items[0]).Command.Should().BeSameAs(command);
	}

	#endregion

	#region LabelMode switched at run time

	[Fact]
	public void LabelMode_set_on_the_bar_at_run_time_reaches_every_button_in_it()
	{
		//Arrange
		var loose = Button("New");
		var grouped = Button("Open");
		var group = new ToolBarGroup();
		group.Children.Add(grouped);
		var bar = CreateBar(loose, group);
		LayOut(bar, 600, 100);

		//Act
		bar.LabelMode = LabelMode.IconAndText;
		LayOut(bar, 600, 100);

		//Assert
		//Frescobaldi's "show button text" preference switches this while the window is open, and a
		//button inside a group is two levels down from the bar.
		loose.TextVisibility.Should().Be(Visibility.Visible);
		grouped.TextVisibility.Should().Be(Visibility.Visible);
	}

	[Fact]
	public void LabelMode_set_on_the_bar_reaches_a_button_that_has_moved_into_the_overflow()
	{
		//Arrange
		var tail = Button("Print");
		var bar = CreateBar(Button("New"), Button("Open"), Button("Save"), tail);
		bar.LabelMode = LabelMode.IconAndText;

		//Act
		LayOut(bar, 150, 100);

		//Assert
		//The overflow's panel is the flyout's content, not a child of the bar, so the bar's settings
		//do not reach it by inheritance on their own. A menu of unlabelled icons is precisely the
		//wrong thing to put behind a chevron.
		bar.OverflowHost.Children.Should().Contain(tail);
		tail.TextVisibility.Should().Be(Visibility.Visible);
		ToolBarProperties.GetLabelMode(tail).Should().Be(LabelMode.IconAndText);
	}

	[Fact]
	public void The_bars_icon_size_reaches_a_button_that_has_moved_into_the_overflow()
	{
		//Arrange
		var tail = Button("Print");
		var bar = CreateBar(Button("New"), Button("Open"), Button("Save"), tail);
		bar.IconSize = 40d;

		//Act
		LayOut(bar, 150, 100);

		//Assert
		bar.OverflowHost.Children.Should().Contain(tail);
		tail.EffectiveIconSize.Should().Be(40d);
	}

	[Fact]
	public void A_button_that_states_its_own_label_mode_ignores_the_bars()
	{
		//Arrange
		var follower = Button("New");
		var stubborn = Button("Open");
		ToolBarProperties.SetLabelMode(stubborn, LabelMode.IconOnly);
		var bar = CreateBar(follower, stubborn);

		//Act
		bar.LabelMode = LabelMode.IconAndText;
		LayOut(bar, 600, 100);

		//Assert
		follower.TextVisibility.Should().Be(Visibility.Visible);
		stubborn.TextVisibility.Should().Be(Visibility.Collapsed);
		//Both still show their icon: the modes differ in the label, not in the artwork.
		stubborn.IconVisibility.Should().Be(Visibility.Visible);
		follower.IconVisibility.Should().Be(Visibility.Visible);
	}

	#endregion

	#region ShowToolTips on the bar, overridden on one button

	[Fact]
	public void ShowToolTips_false_on_the_bar_silences_its_buttons_and_one_can_speak_up()
	{
		//Arrange
		var quiet = new ToolButton { Text = "Print", Shortcut = "Ctrl+P" };
		var loud = new ToolButton { Text = "Save", Shortcut = "Ctrl+S", ShowToolTip = true };
		var bar = CreateBar(quiet, loud);

		//Act
		bar.ShowToolTips = false;
		LayOut(bar, 600, 100);

		//Assert
		ToolTipService.GetToolTip(quiet).Should().BeNull();
		ToolTipService.GetToolTip(loud).Should().Be("Save (Ctrl+S)");
	}

	[Fact]
	public void ShowToolTips_false_on_the_bar_reaches_a_button_inside_a_group()
	{
		//Arrange
		var grouped = new ToolButton { Text = "Open", Shortcut = "Ctrl+O" };
		var group = new ToolBarGroup();
		group.Children.Add(grouped);
		var bar = CreateBar(group);

		//Act
		bar.ShowToolTips = false;
		LayOut(bar, 600, 100);

		//Assert
		ToolTipService.GetToolTip(grouped).Should().BeNull();
	}

	[Fact]
	public void ShowToolTips_false_on_the_bar_reaches_a_button_in_the_overflow()
	{
		//Arrange
		var tail = new ToolButton { Text = "Print", Shortcut = "Ctrl+P" };
		var bar = CreateBar(Button("New"), Button("Open"), Button("Save"), tail);
		bar.ShowToolTips = false;

		//Act
		LayOut(bar, 150, 100);

		//Assert
		bar.OverflowHost.Children.Should().Contain(tail);
		ToolTipService.GetToolTip(tail).Should().BeNull();
	}

	#endregion

	private static ToolButton Button(string text) => new() { Text = text, Icon = new FakeToolIconSource() };

	private static ToolBar CreateBar(params UIElement[] items)
	{
		var bar = new ToolBar();
		foreach (var item in items)
		{
			bar.Items.Add(item);
		}

		return bar;
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
