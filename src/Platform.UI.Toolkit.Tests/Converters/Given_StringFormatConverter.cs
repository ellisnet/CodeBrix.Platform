using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_StringFormatConverter
{
	private readonly StringFormatConverter _converter = new();

	[TestMethod]
	public void When_Format_Parameter_Supplied() =>
		_converter.Convert(5, typeof(string), "{0} chars", null).Should().Be("5 chars");

	[TestMethod]
	public void When_Format_Includes_Format_Specifier() =>
		_converter.Convert(3.14159, typeof(string), "{0:0.00}", null).Should().Be(3.14159.ToString("0.00"));

	[TestMethod]
	public void When_No_Parameter_Returns_ToString() =>
		_converter.Convert(42, typeof(string), null, null).Should().Be("42");

	[TestMethod]
	public void When_Empty_Format_Returns_ToString() =>
		_converter.Convert(42, typeof(string), string.Empty, null).Should().Be("42");

	[TestMethod]
	public void When_NonString_Parameter_Returns_ToString() =>
		_converter.Convert(42, typeof(string), 99, null).Should().Be("42");

	[TestMethod]
	public void When_Null_Value_Returns_Null() =>
		Assert.IsNull(_converter.Convert(null, typeof(string), "{0}", null));

	[TestMethod]
	public void When_Invalid_Format_Throws() =>
		Assert.ThrowsExactly<FormatException>(() =>
			_converter.Convert(42, typeof(string), "{1} nope", null));

	[TestMethod]
	public void When_ConvertBack_Throws() =>
		Assert.ThrowsExactly<NotSupportedException>(() =>
			_converter.ConvertBack("42", typeof(int), null, null));
}
