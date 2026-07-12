using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_BoolToVisibilityConverter
{
	private readonly BoolToVisibilityConverter _converter = new();
	private readonly BoolToVisibilityConverter _inverted = new() { Invert = true };

	[TestMethod]
	public void When_True() =>
		_converter.Convert(true, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_False() =>
		_converter.Convert(false, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Null() =>
		_converter.Convert(null, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_NullableBool_True() =>
		_converter.Convert((bool?)true, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_NonBool_Value() =>
		_converter.Convert("true", typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_True_Inverted() =>
		_inverted.Convert(true, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_False_Inverted() =>
		_inverted.Convert(false, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_ConvertBack_Visible() =>
		_converter.ConvertBack(Visibility.Visible, typeof(bool), null, null).Should().Be(true);

	[TestMethod]
	public void When_ConvertBack_Collapsed() =>
		_converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null).Should().Be(false);

	[TestMethod]
	public void When_ConvertBack_Visible_Inverted() =>
		_inverted.ConvertBack(Visibility.Visible, typeof(bool), null, null).Should().Be(false);

	[TestMethod]
	public void When_ConvertBack_RoundTrips()
	{
		//Arrange
		var value = true;

		//Act
		var visibility = _converter.Convert(value, typeof(Visibility), null, null);
		var roundTripped = _converter.ConvertBack(visibility, typeof(bool), null, null);

		//Assert
		roundTripped.Should().Be(value);
	}
}
