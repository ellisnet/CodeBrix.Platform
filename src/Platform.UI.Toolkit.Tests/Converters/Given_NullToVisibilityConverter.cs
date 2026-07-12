using System;
using CodeBrix.Platform.UI.Converters;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_NullToVisibilityConverter
{
	private readonly NullToVisibilityConverter _converter = new();
	private readonly NullToVisibilityConverter _inverted = new() { Invert = true };

	[TestMethod]
	public void When_NonNull() =>
		_converter.Convert(new object(), typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Null() =>
		_converter.Convert(null, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_EmptyString_Counts_As_Value() =>
		_converter.Convert(string.Empty, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_NonNull_Inverted() =>
		_inverted.Convert(new object(), typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Null_Inverted() =>
		_inverted.Convert(null, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_ConvertBack_Throws() =>
		Assert.ThrowsExactly<NotSupportedException>(() =>
			_converter.ConvertBack(Visibility.Visible, typeof(object), null, null));
}
