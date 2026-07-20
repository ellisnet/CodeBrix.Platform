#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_basis.c (MIT, Microsoft Corporation).
/// </summary>
public class BasisTests
{
	[Fact]
	public void basis_sets_the_main_axis_size_when_no_main_axis_size_is_given() // test_basis1
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item { Width = 100, Basis = new Basis(60) };
		root.Add(child1);

		var child2 = new Item(100, 40);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 60);
		AssertFrame(child2, 0, 60, 100, 40);
	}

	[Fact]
	public void basis_overrides_the_main_axis_size() // test_basis2
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 40) { Basis = new Basis(60) };
		root.Add(child1);

		var child2 = new Item(100, 40);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 60);
		AssertFrame(child2, 0, 60, 100, 40);
	}

	[Fact]
	public void basis_of_zero_is_honored() // test_basis3
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 40) { Basis = new Basis(0) };
		root.Add(child1);

		var child2 = new Item(100, 40);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 0);
		AssertFrame(child2, 0, 0, 100, 40);
	}
}
