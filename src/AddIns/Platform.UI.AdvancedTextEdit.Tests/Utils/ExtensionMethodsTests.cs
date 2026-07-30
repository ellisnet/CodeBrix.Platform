#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Utils;

//was previously: ICSharpCode.AvalonEdit.Tests/Utils/ExtensionMethodsTests.cs in the AvalonEdit repo (MIT).
//The IsClose extension is internal in the port; this suite reaches it through InternalsVisibleTo.

/// <summary>
/// Exercises the numeric <c>IsClose</c> double comparison helper.
/// </summary>
public class ExtensionMethodsTests
{
	[Fact]
	public void zero_is_not_close_to_one() // ZeroIsNotCloseToOne
	{
		//Arrange + Act + Assert
		Assert.False(0.0.IsClose(1));
	}

	[Fact]
	public void zero_is_close_to_zero() // ZeroIsCloseToZero
	{
		//Arrange + Act + Assert
		Assert.True(0.0.IsClose(0));
	}

	[Fact]
	public void infinity_is_close_to_infinity() // InfinityIsCloseToInfinity
	{
		//Arrange + Act + Assert
		Assert.True(double.PositiveInfinity.IsClose(double.PositiveInfinity));
	}

	[Fact]
	public void nan_is_not_close_to_nan() // NaNIsNotCloseToNaN
	{
		//Arrange + Act + Assert
		Assert.False(double.NaN.IsClose(double.NaN));
	}
}
