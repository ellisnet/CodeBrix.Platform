using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The tool bar toggle: a button with a state, and a state that belongs to the view model.
/// </summary>
public class ToolToggleButtonTests
{
	[Fact]
	public void IsChecked_starts_false()
	{
		//Arrange
		//Act
		var toggle = new ToolToggleButton();

		//Assert
		toggle.IsChecked.Should().BeFalse();
	}

	[Fact]
	public void A_click_flips_the_checked_state()
	{
		//Arrange
		var toggle = new ToolToggleButton();

		//Act
		toggle.PerformClick();
		var afterFirst = toggle.IsChecked;
		toggle.PerformClick();
		var afterSecond = toggle.IsChecked;

		//Assert
		afterFirst.Should().BeTrue();
		afterSecond.Should().BeFalse();
	}

	[Fact]
	public void IsCheckedChanged_is_raised_whoever_changed_it()
	{
		//Arrange
		var toggle = new ToolToggleButton();
		var raised = 0;
		toggle.IsCheckedChanged += (_, _) => raised++;

		//Act
		toggle.IsChecked = true;
		toggle.PerformClick();

		//Assert
		raised.Should().Be(2);
	}

	[Fact]
	public void IsChecked_writes_back_to_a_two_way_binding()
	{
		//Arrange
		var model = new ToggleModel();
		var toggle = new ToolToggleButton();
		toggle.SetBinding(
			ToolToggleButton.IsCheckedProperty,
			new Binding { Path = new PropertyPath(nameof(ToggleModel.IsOn)), Source = model, Mode = BindingMode.TwoWay });

		//Act
		toggle.PerformClick();

		//Assert
		//A click is the view changing its mind, and a two-way binding is how the view model hears it.
		model.IsOn.Should().BeTrue();
		toggle.IsChecked.Should().BeTrue();
	}

	[Fact]
	public void IsChecked_follows_the_view_model_through_a_two_way_binding()
	{
		//Arrange
		var model = new ToggleModel();
		var toggle = new ToolToggleButton();
		toggle.SetBinding(
			ToolToggleButton.IsCheckedProperty,
			new Binding { Path = new PropertyPath(nameof(ToggleModel.IsOn)), Source = model, Mode = BindingMode.TwoWay });

		//Act
		model.IsOn = true;

		//Assert
		//The other direction: something else in the application turned the magnifier on, and the
		//button has to say so.
		toggle.IsChecked.Should().BeTrue();
	}

	[Fact]
	public void The_automation_peer_reports_and_changes_the_toggle_state()
	{
		//Arrange
		var toggle = new ToolToggleButton { Text = "Magnifier" };
		var peer = (ToolToggleButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(toggle);

		//Act
		var before = peer.ToggleState;
		peer.Toggle();
		var after = peer.ToggleState;

		//Assert
		before.Should().Be(ToggleState.Off);
		after.Should().Be(ToggleState.On);
		toggle.IsChecked.Should().BeTrue();
		peer.GetPattern(PatternInterface.Toggle).Should().BeSameAs(peer);
	}

	[Fact]
	public void The_automation_peer_still_offers_the_invoke_pattern()
	{
		//Arrange
		var toggle = new ToolToggleButton();
		var peer = (ToolToggleButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(toggle);

		//Act
		var invoke = peer.GetPattern(PatternInterface.Invoke);

		//Assert
		//A toggle is still a button: an automation client that only knows how to invoke should be
		//able to press it.
		invoke.Should().BeSameAs(peer);
	}

	[Fact]
	public void The_toggle_is_a_tool_button_in_every_other_respect()
	{
		//Arrange
		var command = new SwitchableCommand(canExecute: false);

		//Act
		var toggle = new ToolToggleButton { Text = "Magnifier", Command = command };

		//Assert
		toggle.IsEnabled.Should().BeFalse();
		ToolTipService.GetToolTip(toggle).Should().Be("Magnifier");
	}

	/// <summary>A view model with one switchable property, for the two-way binding tests.</summary>
	private sealed class ToggleModel : System.ComponentModel.INotifyPropertyChanged
	{
		private bool _isOn;

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

		public bool IsOn
		{
			get => _isOn;
			set
			{
				if (_isOn != value)
				{
					_isOn = value;
					PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsOn)));
				}
			}
		}
	}
}
