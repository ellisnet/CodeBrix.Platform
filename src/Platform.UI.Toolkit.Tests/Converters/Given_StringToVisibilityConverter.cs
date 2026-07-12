using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_StringToVisibilityConverter
{
	private readonly StringToVisibilityConverter _converter = new();
	private readonly StringToVisibilityConverter _inverted = new() { Invert = true };

	[TestMethod]
	public void When_NonEmpty_String() =>
		_converter.Convert("hello", typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Empty_String() =>
		_converter.Convert(string.Empty, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Null() =>
		_converter.Convert(null, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Whitespace_Counts_As_Content() =>
		_converter.Convert(" ", typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_NonString_Value_Counts_As_Content() =>
		_converter.Convert(42, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_NonEmpty_String_Inverted() =>
		_inverted.Convert("hello", typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Empty_String_Inverted() =>
		_inverted.Convert(string.Empty, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_ConvertBack_Throws() =>
		Assert.ThrowsExactly<NotSupportedException>(() =>
			_converter.ConvertBack(Visibility.Visible, typeof(string), null, null));
}
