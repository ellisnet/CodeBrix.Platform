using System;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// What the three label modes cost in space, measured through the add-in's real default template.
/// </summary>
/// <remarks>
/// <para>
/// These are the tests that prove the template in Themes/Generic.xaml is wired to the button's
/// properties rather than merely present: a mode that is not read would measure the same in every
/// case.
/// </para>
/// <para>
/// A default style is applied by the framework during the measure pass, and it does NOT show up in
/// the control's Style or Template property - both stay null and ApplyTemplate() answers false.
/// Measured, and the reason these tests look at the visual tree and the desired size rather than at
/// those two properties.
/// </para>
/// </remarks>
public class ToolButtonMeasureTests
{
	/// <summary>An unbounded box, so a measure reports what the button wants rather than what fits.</summary>
	private static readonly Size Unbounded = new(double.PositiveInfinity, double.PositiveInfinity);

	/// <summary>Prepares the process the way an application head would have.</summary>
	public ToolButtonMeasureTests() => TestHost.EnsureReady();

	[Fact]
	public void The_default_template_reaches_a_host_free_button()
	{
		//Arrange
		var button = new ToolButton { Text = "Engrave", Icon = new FakeToolIconSource() };

		//Act
		button.Measure(Unbounded);

		//Assert
		//Everything below depends on this: if the add-in's Themes/Generic.xaml were not found, the
		//button would have no visual tree and every measurement would be a measurement of nothing.
		VisualTreeHelper.GetChildrenCount(button).Should().BeGreaterThan(0);
		button.DesiredSize.Width.Should().BeGreaterThan(0);
		button.DesiredSize.Height.Should().BeGreaterThan(0);
	}

	[Fact]
	public void The_text_engine_really_measures_the_label()
	{
		//Arrange
		var narrow = Measure(LabelMode.TextOnly, LabelPosition.Right, text: "A");
		var wide = Measure(LabelMode.TextOnly, LabelPosition.Right, text: "A much longer caption");

		//Act
		//Assert
		//Without the native text library loaded a label silently measures as nothing, and every
		//label test would then pass for the wrong reason. This is the one that would notice.
		wide.Width.Should().BeGreaterThan(narrow.Width);
	}

	[Fact]
	public void IconAndText_measures_wider_than_IconOnly()
	{
		//Arrange
		var iconOnly = Measure(LabelMode.IconOnly, LabelPosition.Right);
		var iconAndText = Measure(LabelMode.IconAndText, LabelPosition.Right);

		//Act
		//Assert
		iconAndText.Width.Should().BeGreaterThan(iconOnly.Width);
		iconAndText.Height.Should().Be(iconOnly.Height);
	}

	[Fact]
	public void A_label_under_the_icon_measures_taller_and_narrower_than_one_beside_it()
	{
		//Arrange
		var beside = Measure(LabelMode.IconAndText, LabelPosition.Right);
		var below = Measure(LabelMode.IconAndText, LabelPosition.Bottom);

		//Act
		//Assert
		below.Height.Should().BeGreaterThan(beside.Height);
		below.Width.Should().BeLessThan(beside.Width);
	}

	[Fact]
	public void TextOnly_measures_narrower_than_IconAndText()
	{
		//Arrange
		var textOnly = Measure(LabelMode.TextOnly, LabelPosition.Right);
		var iconAndText = Measure(LabelMode.IconAndText, LabelPosition.Right);

		//Act
		//Assert
		textOnly.Width.Should().BeLessThan(iconAndText.Width);
	}

	[Fact]
	public void IconOnly_measures_at_least_the_themes_minimum_button_size()
	{
		//Arrange
		var iconOnly = Measure(LabelMode.IconOnly, LabelPosition.Right);

		//Act
		//Assert
		//The theme's ToolBarMinButtonHeight and ToolBarMinButtonWidth are both 32: a tool bar button
		//is a target for a mouse, not only a box around an icon.
		iconOnly.Height.Should().BeGreaterThanOrEqualTo(32d);
		iconOnly.Width.Should().BeGreaterThanOrEqualTo(32d);
	}

	[Fact]
	public void A_larger_icon_size_makes_the_button_larger()
	{
		//Arrange
		var small = Measure(LabelMode.IconOnly, LabelPosition.Right, iconSize: 24d);
		var large = Measure(LabelMode.IconOnly, LabelPosition.Right, iconSize: 48d);

		//Act
		//Assert
		large.Width.Should().BeGreaterThan(small.Width);
		large.Height.Should().BeGreaterThan(small.Height);
	}

	[Fact]
	public void A_drop_down_button_measures_wider_than_the_same_button_without_an_arrow()
	{
		//Arrange
		var plain = new ToolButton { Text = "Engrave", Icon = new FakeToolIconSource() };
		var dropDown = new ToolDropDownButton { Text = "Engrave", Icon = new FakeToolIconSource() };

		//Act
		plain.Measure(Unbounded);
		dropDown.Measure(Unbounded);

		//Assert
		//The arrow half is real space, which is what makes it a target a pointer can aim at.
		dropDown.DesiredSize.Width.Should().BeGreaterThan(plain.DesiredSize.Width);
	}

	[Fact]
	public void A_Delayed_drop_down_button_measures_as_narrow_as_a_plain_one()
	{
		//Arrange
		var plain = new ToolButton { Text = "Engrave", Icon = new FakeToolIconSource() };
		var delayed = new ToolDropDownButton
		{
			Text = "Engrave",
			Icon = new FakeToolIconSource(),
			PopupMode = PopupMode.Delayed,
		};

		//Act
		plain.Measure(Unbounded);
		delayed.Measure(Unbounded);

		//Assert
		//A held press needs no arrow, so the button does not pay for one.
		delayed.DesiredSize.Width.Should().Be(plain.DesiredSize.Width);
	}

	/// <summary>Measures a fully configured button through the real default template.</summary>
	/// <param name="labelMode">Which parts to show.</param>
	/// <param name="labelPosition">Where the text sits.</param>
	/// <param name="iconSize">The icon's edge length.</param>
	/// <param name="text">The label to measure.</param>
	/// <returns>The size the button asked for.</returns>
	private static Size Measure(
		LabelMode labelMode,
		LabelPosition labelPosition,
		double iconSize = 24d,
		string text = "Engrave")
	{
		var button = new ToolButton { Text = text, Icon = new FakeToolIconSource() };

		ToolBarProperties.SetLabelMode(button, labelMode);
		ToolBarProperties.SetLabelPosition(button, labelPosition);
		ToolBarProperties.SetIconSize(button, iconSize);

		button.Measure(Unbounded);

		return button.DesiredSize;
	}
}
