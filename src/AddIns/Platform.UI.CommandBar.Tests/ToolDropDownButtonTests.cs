using System;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The drop-down button: which half of a press belongs to the command and which to the menu.
/// </summary>
/// <remarks>
/// The press, the release and the hold are each one call, so the state machine can be driven here
/// without a pointer and without waiting six hundred milliseconds for a timer.
/// </remarks>
public class ToolDropDownButtonTests
{
	[Fact]
	public void PopupMode_is_MenuButton_by_default()
	{
		//Arrange
		//Act
		var button = new ToolDropDownButton();

		//Assert
		button.PopupMode.Should().Be(PopupMode.MenuButton);
		button.PressAndHoldDelay.Should().Be(TimeSpan.FromMilliseconds(600));
	}

	[Fact]
	public void MenuButton_runs_the_command_when_the_main_half_is_pressed_and_released()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.MenuButton };

		//Act
		var onPress = button.BeginPress(onArrow: false);
		var onRelease = button.CompletePress();

		//Assert
		onPress.Should().Be(ToolDropDownButton.DropDownAction.None);
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.Invoke);
	}

	[Fact]
	public void MenuButton_opens_the_flyout_on_a_press_of_the_arrow_half()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.MenuButton };

		//Act
		var onPress = button.BeginPress(onArrow: true);
		var onRelease = button.CompletePress();

		//Assert
		//A menu opens on the press, as every desktop menu does, and the release that follows must
		//not then also run the command.
		onPress.Should().Be(ToolDropDownButton.DropDownAction.OpenFlyout);
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.None);
	}

	[Fact]
	public void Instant_opens_the_flyout_wherever_the_press_lands()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.Instant };

		//Act
		var onMainPart = button.BeginPress(onArrow: false);
		button.CompletePress();
		var onArrowPart = button.BeginPress(onArrow: true);
		var afterArrow = button.CompletePress();

		//Assert
		onMainPart.Should().Be(ToolDropDownButton.DropDownAction.OpenFlyout);
		onArrowPart.Should().Be(ToolDropDownButton.DropDownAction.OpenFlyout);
		afterArrow.Should().Be(ToolDropDownButton.DropDownAction.None);
	}

	[Fact]
	public void Delayed_runs_the_command_when_the_press_is_released_in_time()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.Delayed };

		//Act
		var onPress = button.BeginPress(onArrow: false);
		var onRelease = button.CompletePress();

		//Assert
		onPress.Should().Be(ToolDropDownButton.DropDownAction.None);
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.Invoke);
	}

	[Fact]
	public void Delayed_opens_the_flyout_when_the_press_is_held()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.Delayed };

		//Act
		button.BeginPress(onArrow: false);
		var onHold = button.HoldElapsed();
		var onRelease = button.CompletePress();

		//Assert
		//Holding is how the user asked for the menu INSTEAD of the command, so the release that
		//follows a hold does nothing at all.
		onHold.Should().Be(ToolDropDownButton.DropDownAction.OpenFlyout);
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.None);
	}

	[Fact]
	public void A_hold_that_arrives_without_a_press_does_nothing()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.Delayed };

		//Act
		var onHold = button.HoldElapsed();

		//Assert
		onHold.Should().Be(ToolDropDownButton.DropDownAction.None);
	}

	[Fact]
	public void A_hold_in_a_mode_that_does_not_use_it_does_nothing()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.MenuButton };

		//Act
		button.BeginPress(onArrow: false);
		var onHold = button.HoldElapsed();
		var onRelease = button.CompletePress();

		//Assert
		onHold.Should().Be(ToolDropDownButton.DropDownAction.None);
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.Invoke);
	}

	[Fact]
	public void A_cancelled_press_neither_invokes_nor_opens()
	{
		//Arrange
		var button = new ToolDropDownButton { PopupMode = PopupMode.MenuButton };

		//Act
		button.BeginPress(onArrow: false);
		button.CancelPress();
		var onRelease = button.CompletePress();

		//Assert
		//The pointer left the button, or capture was lost: nothing happened.
		button.IsPressInProgress.Should().BeFalse();
		onRelease.Should().Be(ToolDropDownButton.DropDownAction.None);
	}

	[Fact]
	public void The_arrow_is_hidden_only_in_the_Delayed_mode()
	{
		//Arrange
		var button = new ToolDropDownButton();

		//Act
		var asMenuButton = button.ArrowVisibility;
		button.PopupMode = PopupMode.Instant;
		var asInstant = button.ArrowVisibility;
		button.PopupMode = PopupMode.Delayed;
		var asDelayed = button.ArrowVisibility;

		//Assert
		//A Delayed button has no arrow to aim at: the whole face is the target, and only when held.
		asMenuButton.Should().Be(Visibility.Visible);
		asInstant.Should().Be(Visibility.Visible);
		asDelayed.Should().Be(Visibility.Collapsed);
	}

	[Fact]
	public void RehookFlyoutBindings_re_subscribes_every_menu_items_command()
	{
		//Arrange
		var command = new SwitchableCommand();
		var flyout = new MenuFlyout();
		var item = new MenuFlyoutItem { Text = "Recent score", Command = command };
		flyout.Items.Add(item);
		flyout.Items.Add(new MenuFlyoutSeparator());

		var button = new ToolDropDownButton { Flyout = flyout };

		//Act
		var rehooked = button.RehookFlyoutBindings();

		//Assert
		rehooked.Should().Be(1);
		item.Command.Should().BeSameAs(command);
	}

	[Fact]
	public void RehookFlyoutBindings_reaches_into_sub_menus()
	{
		//Arrange
		var command = new SwitchableCommand();
		var subItem = new MenuFlyoutSubItem { Text = "Export" };
		subItem.Items.Add(new MenuFlyoutItem { Text = "PDF", Command = command });
		subItem.Items.Add(new MenuFlyoutItem { Text = "MIDI", Command = command });

		var flyout = new MenuFlyout();
		flyout.Items.Add(subItem);
		flyout.Items.Add(new MenuFlyoutItem { Text = "Print", Command = command });

		var button = new ToolDropDownButton { Flyout = flyout };

		//Act
		var rehooked = button.RehookFlyoutBindings();

		//Assert
		//A sub-menu's items are unloaded with the rest, so they need the same treatment.
		rehooked.Should().Be(3);
	}

	[Fact]
	public void RehookFlyoutBindings_leaves_a_command_free_menu_alone()
	{
		//Arrange
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Nothing bound here" });
		var button = new ToolDropDownButton { Flyout = flyout };

		//Act
		var rehooked = button.RehookFlyoutBindings();

		//Assert
		rehooked.Should().Be(0);
	}

	[Fact]
	public void RehookFlyoutBindings_does_nothing_without_a_menu_flyout()
	{
		//Arrange
		var button = new ToolDropDownButton { Flyout = new Flyout() };

		//Act
		var rehooked = button.RehookFlyoutBindings();

		//Assert
		rehooked.Should().Be(0);
	}

	[Fact]
	public void The_automation_peer_reports_the_expand_collapse_state()
	{
		//Arrange
		var button = new ToolDropDownButton { Text = "Engrave", Flyout = new MenuFlyout() };
		var peer = (ToolDropDownButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button);

		//Act
		var state = peer.ExpandCollapseState;
		var pattern = peer.GetPattern(PatternInterface.ExpandCollapse);

		//Assert
		state.Should().Be(ExpandCollapseState.Collapsed);
		pattern.Should().BeSameAs(peer);
		peer.GetAutomationControlType().Should().Be(AutomationControlType.SplitButton);
	}

	[Fact]
	public void The_automation_peer_offers_no_invoke_pattern_for_an_Instant_button()
	{
		//Arrange
		var menuButton = new ToolDropDownButton { PopupMode = PopupMode.MenuButton };
		var instantButton = new ToolDropDownButton { PopupMode = PopupMode.Instant };

		//Act
		var menuPeer = (ToolDropDownButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(menuButton);
		var instantPeer = (ToolDropDownButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(instantButton);

		//Assert
		//An Instant button has no command to invoke; saying otherwise would have an automation
		//client pressing a button that does nothing.
		menuPeer.GetPattern(PatternInterface.Invoke).Should().BeSameAs(menuPeer);
		instantPeer.GetPattern(PatternInterface.Invoke).Should().BeNull();
	}

	[Fact]
	public void The_automation_peer_refuses_to_expand_a_button_with_no_flyout()
	{
		//Arrange
		var button = new ToolDropDownButton();
		var peer = (ToolDropDownButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button);

		//Act
		var expand = () => peer.Expand();

		//Assert
		expand.Should().Throw<InvalidOperationException>();
	}
}
