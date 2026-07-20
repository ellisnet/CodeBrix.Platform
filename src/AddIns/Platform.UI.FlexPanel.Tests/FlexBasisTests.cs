#nullable enable

using System;
using Xunit;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Tests for the public <see cref="FlexBasis"/> struct - the XAML-facing basis type this add-in
/// introduces (not part of the ported C suite).
/// </summary>
public class FlexBasisTests
{
	[Fact]
	public void default_value_is_auto()
	{
		//Arrange + Act
		var basis = default(FlexBasis);

		//Assert
		Assert.True(basis.IsAuto);
		Assert.False(basis.IsRelative);
		Assert.Equal(0f, basis.Length);
		Assert.Equal(FlexBasis.Auto, basis);
	}

	[Fact]
	public void an_absolute_basis_is_not_auto()
	{
		//Arrange + Act
		var basis = new FlexBasis(150);

		//Assert
		Assert.False(basis.IsAuto);
		Assert.False(basis.IsRelative);
		Assert.Equal(150f, basis.Length);
	}

	[Fact]
	public void a_zero_basis_is_distinct_from_auto()
	{
		//Arrange + Act
		var basis = new FlexBasis(0);

		//Assert
		Assert.False(basis.IsAuto);
		Assert.NotEqual(FlexBasis.Auto, basis);
	}

	[Fact]
	public void a_relative_basis_keeps_its_fraction()
	{
		//Arrange + Act
		var basis = new FlexBasis(0.3f, isRelative: true);

		//Assert
		Assert.False(basis.IsAuto);
		Assert.True(basis.IsRelative);
		Assert.Equal(0.3f, basis.Length);
	}

	[Fact]
	public void negative_lengths_are_rejected()
		=> Assert.Throws<ArgumentException>(() => new FlexBasis(-1));

	[Fact]
	public void relative_lengths_above_one_are_rejected()
		=> Assert.Throws<ArgumentException>(() => new FlexBasis(1.5f, isRelative: true));

	[Fact]
	public void create_from_string_parses_auto_case_insensitively()
	{
		Assert.True(FlexBasis.CreateFromString("auto").IsAuto);
		Assert.True(FlexBasis.CreateFromString("Auto").IsAuto);
		Assert.True(FlexBasis.CreateFromString("AUTO").IsAuto);
		Assert.True(FlexBasis.CreateFromString("  auto  ").IsAuto);
	}

	[Fact]
	public void create_from_string_parses_absolute_lengths()
	{
		//Arrange + Act
		var basis = FlexBasis.CreateFromString("150");

		//Assert
		Assert.False(basis.IsAuto);
		Assert.False(basis.IsRelative);
		Assert.Equal(150f, basis.Length);

		Assert.Equal(12.5f, FlexBasis.CreateFromString("12.5").Length);
	}

	[Fact]
	public void create_from_string_parses_percentages_as_relative_fractions()
	{
		//Arrange + Act
		var basis = FlexBasis.CreateFromString("30%");

		//Assert
		Assert.True(basis.IsRelative);
		Assert.Equal(0.3f, basis.Length);

		Assert.Equal(1f, FlexBasis.CreateFromString("100%").Length);
		Assert.Equal(0.125f, FlexBasis.CreateFromString("12.5%").Length);
	}

	[Fact]
	public void create_from_string_rejects_garbage()
	{
		Assert.Throws<FormatException>(() => FlexBasis.CreateFromString("banana"));
		Assert.Throws<FormatException>(() => FlexBasis.CreateFromString(""));
		Assert.Throws<ArgumentNullException>(() => FlexBasis.CreateFromString(null!));
	}

	[Fact]
	public void implicit_conversion_from_float_is_absolute()
	{
		//Arrange + Act
		FlexBasis basis = 42f;

		//Assert
		Assert.False(basis.IsAuto);
		Assert.False(basis.IsRelative);
		Assert.Equal(42f, basis.Length);
	}

	[Fact]
	public void equality_compares_kind_and_length()
	{
		Assert.Equal(new FlexBasis(50), new FlexBasis(50));
		Assert.NotEqual(new FlexBasis(50), new FlexBasis(51));
		Assert.NotEqual(new FlexBasis(0.5f, isRelative: true), new FlexBasis(0.5f));
		Assert.True(new FlexBasis(50) == new FlexBasis(50));
		Assert.True(new FlexBasis(50) != new FlexBasis(0.5f, isRelative: true));
	}

	[Fact]
	public void to_string_round_trips_the_three_kinds()
	{
		Assert.Equal("Auto", FlexBasis.Auto.ToString());
		Assert.Equal("150", new FlexBasis(150).ToString());
		Assert.Equal("30%", new FlexBasis(0.3f, isRelative: true).ToString());
	}
}
