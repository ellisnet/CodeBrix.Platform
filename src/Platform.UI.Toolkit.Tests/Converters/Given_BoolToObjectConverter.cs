using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_BoolToObjectConverter
{
	private readonly BoolToObjectConverter _converter = new() { TrueValue = "YES", FalseValue = "NO" };

	[TestMethod]
	public void When_True_Returns_TrueValue() =>
		_converter.Convert(true, typeof(string), null, null).Should().Be("YES");

	[TestMethod]
	public void When_False_Returns_FalseValue() =>
		_converter.Convert(false, typeof(string), null, null).Should().Be("NO");

	[TestMethod]
	public void When_Null_Returns_FalseValue() =>
		_converter.Convert(null, typeof(string), null, null).Should().Be("NO");

	[TestMethod]
	public void When_NonBool_Returns_FalseValue() =>
		_converter.Convert(42, typeof(string), null, null).Should().Be("NO");

	[TestMethod]
	public void When_Values_Unset_Returns_Null()
	{
		//Arrange
		var converter = new BoolToObjectConverter();

		//Act + Assert
		Assert.IsNull(converter.Convert(true, typeof(object), null, null));
		Assert.IsNull(converter.Convert(false, typeof(object), null, null));
	}

	[TestMethod]
	public void When_ConvertBack_Matches_TrueValue() =>
		_converter.ConvertBack("YES", typeof(bool), null, null).Should().Be(true);

	[TestMethod]
	public void When_ConvertBack_Does_Not_Match_TrueValue() =>
		_converter.ConvertBack("NO", typeof(bool), null, null).Should().Be(false);
}
