using System.Reflection;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml_Media;

/// <summary>
/// Fences the shape of RadialGradientBrush that a consuming application sees: the defaults every
/// head starts from, and GradientOrigin as a real, working dependency property.
/// <para>
/// GradientOrigin used to carry a "not implemented" marker even though every head this framework
/// ships renders through Skia and the Skia painter honours the property - it copies the origin
/// into CompositionRadialGradientBrush.GradientOriginOffset and paints a two-point conical
/// gradient whose focal point is the origin. The marker was therefore a stale claim that made the
/// analyzer warn on every use of the property in every application, on every head. The reflection
/// test below is what keeps that marker from ever coming back.
/// </para>
/// </summary>
[TestClass]
public class RadialGradientBrushTests
{
	[TestMethod]
	public void Center_defaults_to_the_middle_of_the_element() =>
		new RadialGradientBrush().Center.Should().Be(new Point(0.5d, 0.5d));

	[TestMethod]
	public void GradientOrigin_defaults_to_the_middle_of_the_element() =>
		new RadialGradientBrush().GradientOrigin.Should().Be(new Point(0.5d, 0.5d));

	[TestMethod]
	public void RadiusX_and_RadiusY_default_to_half_the_element()
	{
		//Arrange
		var brush = new RadialGradientBrush();

		//Act
		var radiusX = brush.RadiusX;
		var radiusY = brush.RadiusY;

		//Assert
		radiusX.Should().Be(0.5d);
		radiusY.Should().Be(0.5d);
	}

	[TestMethod]
	public void SpreadMethod_defaults_to_Pad() =>
		new RadialGradientBrush().SpreadMethod.Should().Be(GradientSpreadMethod.Pad);

	[TestMethod]
	public void MappingMode_defaults_to_RelativeToBoundingBox() =>
		new RadialGradientBrush().MappingMode.Should().Be(BrushMappingMode.RelativeToBoundingBox);

	[TestMethod]
	public void GradientStops_starts_empty() =>
		new RadialGradientBrush().GradientStops.Count.Should().Be(0);

	[TestMethod]
	public void GradientOrigin_set_through_the_property_is_readable_from_the_dependency_property()
	{
		//Arrange
		var brush = new RadialGradientBrush();

		//Act
		brush.GradientOrigin = new Point(0.2d, 0.75d);

		//Assert
		brush.GetValue(RadialGradientBrush.GradientOriginProperty).Should().Be(new Point(0.2d, 0.75d));
	}

	[TestMethod]
	public void GradientOrigin_set_through_the_dependency_property_is_readable_from_the_property()
	{
		//Arrange
		var brush = new RadialGradientBrush();

		//Act
		brush.SetValue(RadialGradientBrush.GradientOriginProperty, new Point(0.8d, 0.1d));

		//Assert
		brush.GradientOrigin.Should().Be(new Point(0.8d, 0.1d));
	}

	[TestMethod]
	public void GradientOrigin_carries_no_not_implemented_marker_so_the_analyzer_never_warns_on_it_again()
	{
		//Arrange
		var property = typeof(RadialGradientBrush).GetProperty(nameof(RadialGradientBrush.GradientOrigin));

		//Act
		var marker = property!.GetCustomAttribute<CodeBrix.Platform.NotImplementedAttribute>();

		//Assert
		marker.Should().BeNull();
	}
}
