using System;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SilverAssertions;
using Windows.Foundation;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The ordinary tool bar button: what it shows, what it says, and what it does when clicked.
/// </summary>
/// <remarks>
/// Everything here runs without an application head. A button is parented to a
/// <see cref="ToolBarGroup"/> where a parent is needed, because that is a Panel and parents its
/// children the moment they are added - the scaffold's own note about why an item inside a
/// <see cref="ToolBar"/> cannot yet be observed the same way.
/// </remarks>
public class ToolButtonTests
{
	[Fact]
	public void Text_is_shown_when_the_label_mode_asks_for_it()
	{
		//Arrange
		var button = new ToolButton { Text = "Save", Icon = new FakeToolIconSource() };

		//Act
		ToolBarProperties.SetLabelMode(button, LabelMode.IconAndText);

		//Assert
		button.ResolvedText.Should().Be("Save");
		button.TextVisibility.Should().Be(Visibility.Visible);
		button.IconVisibility.Should().Be(Visibility.Visible);
	}

	[Fact]
	public void Text_is_hidden_in_the_default_icon_only_mode()
	{
		//Arrange
		//Act
		var button = new ToolButton { Text = "Save", Icon = new FakeToolIconSource() };

		//Assert
		//IconOnly is the default, and the text still exists - it is what the tooltip says.
		ToolBarProperties.GetLabelMode(button).Should().Be(LabelMode.IconOnly);
		button.IconVisibility.Should().Be(Visibility.Visible);
		button.TextVisibility.Should().Be(Visibility.Collapsed);
		button.ResolvedText.Should().Be("Save");
	}

	[Fact]
	public void Text_is_shown_in_icon_only_mode_when_there_is_no_icon()
	{
		//Arrange
		//Act
		var button = new ToolButton { Text = "Save" };

		//Assert
		//An icon-only button with no icon would be a blank square; showing the label is the only
		//useful answer, and it is what a bar of mixed items needs.
		button.IconVisibility.Should().Be(Visibility.Collapsed);
		button.TextVisibility.Should().Be(Visibility.Visible);
	}

	[Fact]
	public void Icon_is_shown_in_text_only_mode_when_there_is_no_text()
	{
		//Arrange
		//Act
		var button = new ToolButton { Icon = new FakeToolIconSource() };
		ToolBarProperties.SetLabelMode(button, LabelMode.TextOnly);

		//Assert
		button.TextVisibility.Should().Be(Visibility.Collapsed);
		button.IconVisibility.Should().Be(Visibility.Visible);
	}

	[Fact]
	public void LabelPosition_decides_how_the_icon_and_the_text_are_stacked()
	{
		//Arrange
		var button = new ToolButton { Text = "Save", Icon = new FakeToolIconSource() };
		ToolBarProperties.SetLabelMode(button, LabelMode.IconAndText);

		//Act
		var beside = button.LabelOrientation;
		ToolBarProperties.SetLabelPosition(button, LabelPosition.Bottom);
		var below = button.LabelOrientation;

		//Assert
		beside.Should().Be(Orientation.Horizontal);
		below.Should().Be(Orientation.Vertical);
	}

	[Fact]
	public void IconVisual_is_built_from_the_icon_source()
	{
		//Arrange
		var icon = new FakeToolIconSource();

		//Act
		var button = new ToolButton { Icon = icon };

		//Assert
		button.IconVisual.Should().NotBeNull();
		icon.CreatedElementCount.Should().Be(1);
	}

	[Fact]
	public void IconVisual_is_rebuilt_only_when_the_source_changes()
	{
		//Arrange
		var icon = new FakeToolIconSource();
		var button = new ToolButton { Icon = icon };

		//Act
		//An element has one parent, so a rebuild for every unrelated change would tear the icon out
		//of the template each time a bar changed its label mode.
		ToolBarProperties.SetLabelMode(button, LabelMode.IconAndText);
		ToolBarProperties.SetIconSize(button, 32d);

		//Assert
		icon.CreatedElementCount.Should().Be(1);
	}

	[Fact]
	public void EffectiveIconSize_follows_the_inherited_property_and_the_buttons_own_override()
	{
		//Arrange
		var group = new ToolBarGroup();
		var inheriting = new ToolButton();
		var overriding = new ToolButton();
		group.Children.Add(inheriting);
		group.Children.Add(overriding);

		//Act
		ToolBarProperties.SetIconSize(group, 16d);
		ToolBarProperties.SetIconSize(overriding, 40d);

		//Assert
		inheriting.EffectiveIconSize.Should().Be(16d);
		overriding.EffectiveIconSize.Should().Be(40d);
	}

	[Fact]
	public void IsEnabled_follows_CanExecute()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: false);

		//Act
		var button = new ToolButton { Command = command };

		//Assert
		button.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void IsEnabled_follows_a_later_CanExecuteChanged()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: false);
		var button = new ToolButton { Command = command };

		//Act
		command.SetCanExecute(true);

		//Assert
		button.IsEnabled.Should().BeTrue();

		//Act
		command.SetCanExecute(false);

		//Assert
		button.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void IsEnabled_follows_CanExecute_of_the_command_parameter()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: true);
		var button = new ToolButton { Command = command, CommandParameter = "first" };

		//Act
		command.SetCanExecute(false);
		button.CommandParameter = "second";

		//Assert
		//Changing the parameter re-asks the command, because the answer can depend on it.
		button.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void IsEnabled_stays_true_for_a_command_built_from_an_action_alone()
	{
		//Arrange
		var command = new ActionOnlyCommand(() => { });

		//Act
		var button = new ToolButton { Command = command };

		//Assert
		//The shape of SimpleCommand's action-only constructor, which once answered CanExecute false
		//and disabled every button bound to it.
		button.IsEnabled.Should().BeTrue();
	}

	[Fact]
	public void An_explicit_IsEnabled_false_wins_over_CanExecute()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: true);
		var button = new ToolButton { Command = command };

		//Act
		button.IsEnabled = false;
		command.SetCanExecute(true);

		//Assert
		//The application said no; a command saying yes does not overrule it.
		button.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void Click_runs_the_command_with_the_command_parameter()
	{
		//Arrange
		var command = new SwitchableCommand();
		var button = new ToolButton { Command = command, CommandParameter = 42 };

		//Act
		button.PerformClick();

		//Assert
		command.ExecutionCount.Should().Be(1);
		command.LastParameter.Should().Be(42);
	}

	[Fact]
	public void ClickWithModifiers_carries_the_keys_that_were_down_at_the_click()
	{
		//Arrange
		var button = new ToolButton();
		var captured = VirtualKeyModifiers.None;
		var raised = 0;
		button.ClickWithModifiers += (_, args) =>
		{
			captured = args.Modifiers;
			raised++;
		};

		var previousProbe = ToolCommandSupport.ModifierProbe;

		//Act
		try
		{
			ToolCommandSupport.ModifierProbe =
				() => VirtualKeyModifiers.Shift | VirtualKeyModifiers.Control;
			button.PerformClick();
		}
		finally
		{
			ToolCommandSupport.ModifierProbe = previousProbe;
		}

		//Assert
		raised.Should().Be(1);
		captured.Should().Be(VirtualKeyModifiers.Shift | VirtualKeyModifiers.Control);
	}

	[Fact]
	public void ClickWithModifiers_reports_no_modifiers_for_a_plain_click()
	{
		//Arrange
		var button = new ToolButton();
		ClickWithModifiersEventArgs? args = null;
		button.ClickWithModifiers += (_, e) => args = e;

		var previousProbe = ToolCommandSupport.ModifierProbe;

		//Act
		try
		{
			ToolCommandSupport.ModifierProbe = () => VirtualKeyModifiers.None;
			button.PerformClick();
		}
		finally
		{
			ToolCommandSupport.ModifierProbe = previousProbe;
		}

		//Assert
		args.Should().NotBeNull();
		args!.Modifiers.Should().Be(VirtualKeyModifiers.None);
		args.IsShiftPressed.Should().BeFalse();
		args.IsControlPressed.Should().BeFalse();
		args.IsAltPressed.Should().BeFalse();
	}

	[Fact]
	public void A_XamlUICommand_supplies_the_label_the_icon_and_the_description()
	{
		//Arrange
		var icon = new FakeToolIconSource();
		var command = new XamlUICommand
		{
			Label = "Engrave",
			IconSource = icon,
			Description = "Typeset the score",
		};

		//Act
		var button = new ToolButton { Command = command };

		//Assert
		button.ResolvedText.Should().Be("Engrave");
		button.IconVisual.Should().NotBeNull();
		button.ResolvedDescription.Should().Be("Typeset the score");
	}

	[Fact]
	public void The_buttons_own_values_beat_the_commands()
	{
		//Arrange
		var commandIcon = new FakeToolIconSource();
		var buttonIcon = new FakeToolIconSource();
		var command = new XamlUICommand { Label = "Engrave", IconSource = commandIcon };

		//Act
		var button = new ToolButton
		{
			Text = "Typeset",
			Icon = buttonIcon,
			Command = command,
		};

		//Assert
		//The whole point of "only where the button did not set its own": a shared command may drive
		//twenty places, and one of them is allowed to differ.
		button.ResolvedText.Should().Be("Typeset");
		buttonIcon.CreatedElementCount.Should().Be(1);
		commandIcon.CreatedElementCount.Should().Be(0);
	}

	[Fact]
	public void A_XamlUICommands_accelerators_are_registered_on_the_button()
	{
		//Arrange
		var command = new XamlUICommand { Label = "Save" };
		command.KeyboardAccelerators.Add(new KeyboardAccelerator
		{
			Key = VirtualKey.S,
			Modifiers = VirtualKeyModifiers.Control,
		});

		//Act
		var button = new ToolButton { Command = command };

		//Assert
		//Registered as COPIES: an accelerator is a dependency object and cannot belong to both the
		//command and the button.
		button.KeyboardAccelerators.Count.Should().Be(1);
		button.KeyboardAccelerators[0].Key.Should().Be(VirtualKey.S);
		button.KeyboardAccelerators[0].Modifiers.Should().Be(VirtualKeyModifiers.Control);
		button.KeyboardAccelerators[0].Should().NotBeSameAs(command.KeyboardAccelerators[0]);
	}

	[Fact]
	public void An_accelerator_the_application_registered_is_left_alone()
	{
		//Arrange
		var button = new ToolButton();
		button.KeyboardAccelerators.Add(new KeyboardAccelerator
		{
			Key = VirtualKey.F5,
			Modifiers = VirtualKeyModifiers.None,
		});

		var command = new XamlUICommand();
		command.KeyboardAccelerators.Add(new KeyboardAccelerator
		{
			Key = VirtualKey.S,
			Modifiers = VirtualKeyModifiers.Control,
		});

		//Act
		button.Command = command;

		//Assert
		button.KeyboardAccelerators.Count.Should().Be(1);
		button.KeyboardAccelerators[0].Key.Should().Be(VirtualKey.F5);
	}

	[Fact]
	public void A_XamlUICommands_access_key_is_taken_only_when_the_button_has_none()
	{
		//Arrange
		var command = new XamlUICommand { AccessKey = "S" };
		var withoutOwn = new ToolButton();
		var withOwn = new ToolButton { AccessKey = "T" };

		//Act
		withoutOwn.Command = command;
		withOwn.Command = command;

		//Assert
		withoutOwn.AccessKey.Should().Be("S");
		withOwn.AccessKey.Should().Be("T");
	}

	[Fact]
	public void The_tooltip_is_composed_from_the_text_and_the_shortcut()
	{
		//Arrange
		var button = new ToolButton { Text = "Save", Shortcut = "Ctrl+S" };

		//Act
		var tip = ToolTipService.GetToolTip(button);

		//Assert
		tip.Should().Be("Save (Ctrl+S)");
	}

	[Fact]
	public void The_tooltips_shortcut_comes_from_the_commands_accelerator_when_none_was_given()
	{
		//Arrange
		var command = new XamlUICommand { Label = "Save" };
		command.KeyboardAccelerators.Add(new KeyboardAccelerator
		{
			Key = VirtualKey.S,
			Modifiers = VirtualKeyModifiers.Control,
		});

		//Act
		var button = new ToolButton { Command = command };

		//Assert
		ToolTipService.GetToolTip(button).Should().Be("Save (Ctrl+S)");
	}

	[Fact]
	public void The_tooltip_is_suppressed_when_the_bar_says_no_tooltips()
	{
		//Arrange
		var group = new ToolBarGroup();
		var button = new ToolButton { Text = "Save" };
		group.Children.Add(button);

		//Act
		ToolBarProperties.SetShowToolTips(group, false);

		//Assert
		button.IsToolTipShown.Should().BeFalse();
		ToolTipService.GetToolTip(button).Should().BeNull();

		//The wording still exists - a bar that hides tooltips can still show the text elsewhere.
		button.ComposedToolTipText.Should().Be("Save");
	}

	[Fact]
	public void A_button_can_show_its_tooltip_in_a_bar_that_hides_them()
	{
		//Arrange
		var group = new ToolBarGroup();
		var button = new ToolButton { Text = "Save" };
		group.Children.Add(button);
		ToolBarProperties.SetShowToolTips(group, false);

		//Act
		button.ShowToolTip = true;

		//Assert
		button.IsToolTipShown.Should().BeTrue();
		ToolTipService.GetToolTip(button).Should().Be("Save");
	}

	[Fact]
	public void A_button_can_hide_its_tooltip_in_a_bar_that_shows_them()
	{
		//Arrange
		var group = new ToolBarGroup();
		var button = new ToolButton { Text = "Save" };
		group.Children.Add(button);

		//Act
		button.ShowToolTip = false;

		//Assert
		ToolBarProperties.GetShowToolTips(group).Should().BeTrue();
		button.IsToolTipShown.Should().BeFalse();
		ToolTipService.GetToolTip(button).Should().BeNull();
	}

	[Fact]
	public void The_accessible_name_names_the_bar_the_button_is_in()
	{
		//Arrange
		var group = new ToolBarGroup();
		var button = new ToolButton { Text = "Save", Shortcut = "Ctrl+S" };
		group.Children.Add(button);

		//Act
		AutomationProperties.SetName(group, "Main");

		//Assert
		button.AccessibleName.Should().Be("Save (Ctrl+S), Main");
	}

	[Fact]
	public void The_automation_peer_reports_the_composed_name_and_invokes_the_button()
	{
		//Arrange
		var command = new SwitchableCommand();
		var button = new ToolButton { Text = "Save", Shortcut = "Ctrl+S", Command = command };
		var peer = (ToolButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button);

		//Act
		var name = peer.GetName();
		var pattern = peer.GetPattern(PatternInterface.Invoke);
		peer.Invoke();

		//Assert
		name.Should().Be("Save (Ctrl+S)");
		pattern.Should().BeSameAs(peer);
		peer.GetAutomationControlType().Should().Be(AutomationControlType.Button);
		command.ExecutionCount.Should().Be(1);
	}

	[Fact]
	public void The_automation_peer_does_not_invoke_a_disabled_button()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: false);
		var button = new ToolButton { Text = "Save", Command = command };
		var peer = (ToolButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button);

		//Act
		peer.Invoke();

		//Assert
		command.ExecutionCount.Should().Be(0);
	}
}
