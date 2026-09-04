using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The scaffold's own smoke test: every control the add-in declares is constructed host-free, and
/// the bar-level inherited attached properties report their documented defaults.
/// </summary>
/// <remarks>
/// <para>
/// This file is deliberately NOT one of the per-control suites (ToolBarTests, ToolButtonTests and
/// the rest, which the feature work adds beside it). Its job is narrower and permanent: to prove
/// that the test project's own machinery - the dispatcher overrides, the fake display extension,
/// the Skia runtime-assembly swap - is enough to bring a XAML control of this add-in into
/// existence with no application head. When it fails, the harness is broken, not the control.
/// </para>
/// </remarks>
public class ScaffoldSmokeTests
{
	[Fact]
	public void every_control_constructs_without_a_head()
	{
		//Arrange
		//Act
		var tray = new ToolBarTray();
		var bar = new ToolBar();
		var group = new ToolBarGroup();
		var separator = new ToolBarSeparator();
		var spacer = new ToolBarSpacer();
		var button = new ToolButton();
		var toggle = new ToolToggleButton();
		var dropDown = new ToolDropDownButton();

		//Assert
		tray.Should().NotBeNull();
		bar.Should().NotBeNull();
		group.Should().NotBeNull();
		separator.Should().NotBeNull();
		spacer.Should().NotBeNull();
		button.Should().NotBeNull();
		toggle.Should().NotBeNull();
		dropDown.Should().NotBeNull();
	}

	[Fact]
	public void toggle_and_drop_down_buttons_are_tool_buttons()
	{
		//Arrange
		//Act
		var toggle = new ToolToggleButton();
		var dropDown = new ToolDropDownButton();

		//Assert
		//The whole button family shares ToolButton's command binding, icon and label behaviour;
		//if this ever stops holding, every rule documented once now has to be documented three times.
		toggle.Should().BeAssignableTo<ToolButton>();
		dropDown.Should().BeAssignableTo<ToolButton>();
	}

	[Fact]
	public void attached_properties_report_their_documented_defaults()
	{
		//Arrange
		var button = new ToolButton();

		//Act
		var iconSize = ToolBarProperties.GetIconSize(button);
		var labelMode = ToolBarProperties.GetLabelMode(button);
		var labelPosition = ToolBarProperties.GetLabelPosition(button);
		var showToolTips = ToolBarProperties.GetShowToolTips(button);

		//Assert
		iconSize.Should().Be(ToolBarProperties.DefaultIconSize);
		iconSize.Should().Be(24d);
		labelMode.Should().Be(LabelMode.IconOnly);
		labelPosition.Should().Be(LabelPosition.Right);
		showToolTips.Should().BeTrue();
	}

	[Fact]
	public void attached_properties_inherit_down_the_tree()
	{
		//Arrange
		//A ToolBarGroup is a Panel, so adding to Children parents the button immediately, with no
		//host and no layout pass. An item inside a realized ToolBar inherits by the same mechanism
		//once its container is in the tree; the container realization is what a host-free test
		//cannot do, and it is not what this test is about.
		var group = new ToolBarGroup();
		var button = new ToolButton();
		group.Children.Add(button);

		//Act
		ToolBarProperties.SetIconSize(group, 16d);
		ToolBarProperties.SetLabelMode(group, LabelMode.IconAndText);
		ToolBarProperties.SetLabelPosition(group, LabelPosition.Bottom);
		ToolBarProperties.SetShowToolTips(group, false);

		//Assert
		//Inheritance is the whole point of these four: a bar states them once and every item below
		//reads them, with no plumbing in between.
		ToolBarProperties.GetIconSize(button).Should().Be(16d);
		ToolBarProperties.GetLabelMode(button).Should().Be(LabelMode.IconAndText);
		ToolBarProperties.GetLabelPosition(button).Should().Be(LabelPosition.Bottom);
		ToolBarProperties.GetShowToolTips(button).Should().BeFalse();
	}

	[Fact]
	public void an_item_overrides_an_inherited_attached_property_for_itself()
	{
		//Arrange
		var group = new ToolBarGroup();
		var inheriting = new ToolButton();
		var overriding = new ToolButton();
		group.Children.Add(inheriting);
		group.Children.Add(overriding);
		ToolBarProperties.SetIconSize(group, 16d);
		ToolBarProperties.SetShowToolTips(group, false);

		//Act
		ToolBarProperties.SetIconSize(overriding, 32d);
		ToolBarProperties.SetShowToolTips(overriding, true);

		//Assert
		ToolBarProperties.GetIconSize(inheriting).Should().Be(16d);
		ToolBarProperties.GetIconSize(overriding).Should().Be(32d);
		ToolBarProperties.GetShowToolTips(inheriting).Should().BeFalse();
		ToolBarProperties.GetShowToolTips(overriding).Should().BeTrue();
	}
}
