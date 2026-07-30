#nullable enable

using System;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Utils;

//was previously: ICSharpCode.AvalonEdit.Tests/Utils/CompressingTreeListTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises <see cref="CompressingTreeList{T}"/>: run compression on insert, ranged removal at
/// the start/middle/end, overflow detection, and the Transform operations.
/// </summary>
public class CompressingTreeListTests
{
	[Fact]
	public void empty_list_has_no_elements_and_copies_nothing() // EmptyTreeList
	{
		//Arrange + Act
		CompressingTreeList<string> list = new CompressingTreeList<string>(string.Equals);

		//Assert
		Assert.Empty(list);
		foreach (string v in list)
		{
			Assert.Fail("The empty list should not yield any element, but yielded " + v);
		}
		string[] arr = new string[0];
		list.CopyTo(arr, 0);
	}

	[Fact]
	public void adding_ten_billion_elements_works_and_overflow_is_detected() // CheckAdd10BillionElements
	{
		//Arrange
		const int billion = 1000000000;
		CompressingTreeList<string> list = new CompressingTreeList<string>(string.Equals);

		//Act
		list.InsertRange(0, billion, "A");
		list.InsertRange(1, billion, "B");

		//Assert
		Assert.Equal(2 * billion, list.Count);
		Assert.Throws<OverflowException>(delegate { list.InsertRange(2, billion, "C"); });
	}

	[Fact]
	public void repeated_values_are_stored_as_one_run() // AddRepeated
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);

		//Act
		list.Add(42);
		list.Add(42);
		list.Add(42);
		list.Insert(0, 42);
		list.Insert(1, 42);

		//Assert
		Assert.Equal(new[] { 42, 42, 42, 42, 42 }, list.ToArray());
	}

	[Fact]
	public void remove_range_removes_across_runs_and_merges_neighbours() // RemoveRange
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		for (int i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());

		//Act + Assert
		list.RemoveRange(1, 4);
		Assert.Equal(new[] { 1, 3 }, list.ToArray());
		list.Insert(1, 1);
		list.InsertRange(2, 2, 2);
		list.Insert(4, 1);
		Assert.Equal(new[] { 1, 1, 2, 2, 1, 3 }, list.ToArray());
		list.RemoveRange(2, 2);
		Assert.Equal(new[] { 1, 1, 1, 3 }, list.ToArray());
	}

	[Fact]
	public void remove_range_at_end_keeps_leading_elements() // RemoveAtEnd
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		for (int i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());

		//Act
		list.RemoveRange(3, 3);

		//Assert
		Assert.Equal(new[] { 1, 1, 2 }, list.ToArray());
	}

	[Fact]
	public void remove_single_element_at_start_keeps_the_rest() // RemoveAtStart
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		for (int i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());

		//Act
		list.RemoveRange(0, 1);

		//Assert
		Assert.Equal(new[] { 1, 2, 2, 3, 3 }, list.ToArray());
	}

	[Fact]
	public void remove_range_at_start_spanning_runs_keeps_the_rest() // RemoveAtStart2
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		for (int i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());

		//Act
		list.RemoveRange(0, 3);

		//Assert
		Assert.Equal(new[] { 2, 3, 3 }, list.ToArray());
	}

	[Fact]
	public void transform_visits_each_run_once() // Transform
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange(new[] { 0, 1, 1, 0 });
		int calls = 0;

		//Act
		list.Transform(i => { calls++; return i + 1; });

		//Assert
		Assert.Equal(3, calls);
		Assert.Equal(new[] { 1, 2, 2, 1 }, list.ToArray());
	}

	[Fact]
	public void transform_to_constant_merges_all_runs() // TransformToZero
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange(new[] { 0, 1, 1, 0 });

		//Act
		list.Transform(i => 0);

		//Assert
		Assert.Equal(new[] { 0, 0, 0, 0 }, list.ToArray());
	}

	[Fact]
	public void transform_range_only_affects_the_requested_range() // TransformRange
	{
		//Arrange
		CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange(new[] { 0, 1, 1, 1, 0, 0 });

		//Act
		list.TransformRange(2, 3, i => 0);

		//Assert
		Assert.Equal(new[] { 0, 1, 0, 0, 0, 0 }, list.ToArray());
	}
}
