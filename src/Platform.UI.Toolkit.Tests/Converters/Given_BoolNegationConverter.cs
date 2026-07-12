using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_BoolNegationConverter
{
	private readonly BoolNegationConverter _converter = new();

	[TestMethod]
	public void When_True() =>
		_converter.Convert(true, typeof(bool), null, null).Should().Be(false);

	[TestMethod]
	public void When_False() =>
		_converter.Convert(false, typeof(bool), null, null).Should().Be(true);

	[TestMethod]
	public void When_Null_Treated_As_False() =>
		_converter.Convert(null, typeof(bool), null, null).Should().Be(true);

	[TestMethod]
	public void When_NonBool_Treated_As_False() =>
		_converter.Convert("yes", typeof(bool), null, null).Should().Be(true);

	[TestMethod]
	public void When_ConvertBack_Negates() =>
		_converter.ConvertBack(true, typeof(bool), null, null).Should().Be(false);

	[TestMethod]
	public void When_ConvertBack_RoundTrips()
	{
		//Arrange
		var value = true;

		//Act
		var negated = _converter.Convert(value, typeof(bool), null, null);
		var roundTripped = _converter.ConvertBack(negated, typeof(bool), null, null);

		//Assert
		roundTripped.Should().Be(value);
	}
}
