#nullable enable

using System;
using System.IO;
using System.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Utils;

//was previously: ICSharpCode.AvalonEdit.Tests/Utils/RopeTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises <see cref="Rope{T}"/> and the <see cref="CharRope"/> extensions: construction from
/// strings, ranged reads, and concatenation by appending, prepending and middle insertion.
/// </summary>
public class RopeTests
{
	[Fact]
	public void empty_rope_has_zero_length_and_empty_text() // EmptyRope
	{
		//Arrange + Act
		Rope<char> empty = new Rope<char>();

		//Assert
		int length = empty.Length;
		Assert.Equal(0, length);
		Assert.Equal("", empty.ToString());
	}

	[Fact]
	public void rope_from_empty_string_has_zero_length_and_empty_text() // EmptyRopeFromString
	{
		//Arrange + Act
		Rope<char> empty = new Rope<char>(string.Empty);

		//Assert
		int length = empty.Length;
		Assert.Equal(0, length);
		Assert.Equal("", empty.ToString());
	}

	[Fact]
	public void rope_from_short_string_round_trips() // InitializeRopeFromShortString
	{
		//Arrange + Act
		Rope<char> rope = new Rope<char>("Hello, World");

		//Assert
		Assert.Equal(12, rope.Length);
		Assert.Equal("Hello, World", rope.ToString());
	}

	string BuildLongString(int lines)
	{
		StringWriter w = new StringWriter();
		w.NewLine = "\n";
		for (int i = 1; i <= lines; i++)
		{
			w.WriteLine(i.ToString());
		}
		return w.ToString();
	}

	[Fact]
	public void rope_from_long_string_round_trips() // InitializeRopeFromLongString
	{
		//Arrange
		string text = BuildLongString(1000);

		//Act
		Rope<char> rope = new Rope<char>(text);

		//Assert
		Assert.Equal(text.Length, rope.Length);
		Assert.Equal(text, rope.ToString());
		Assert.Equal(text.ToCharArray(), rope.ToArray());
	}

	[Fact]
	public void ranged_to_array_to_string_and_get_range_return_the_same_part() // TestToArrayAndToStringWithParts
	{
		//Arrange
		string text = BuildLongString(1000);
		Rope<char> rope = new Rope<char>(text);

		//Act
		string textPart = text.Substring(1200, 600);
		char[] arrayPart = textPart.ToCharArray();

		//Assert
		Assert.Equal(textPart, rope.ToString(1200, 600));
		Assert.Equal(arrayPart, rope.ToArray(1200, 600));

		Rope<char> partialRope = rope.GetRange(1200, 600);
		Assert.Equal(textPart, partialRope.ToString());
		Assert.Equal(arrayPart, partialRope.ToArray());
	}

	[Fact]
	public void appending_strings_and_chars_matches_string_builder() // ConcatenateStringToRope
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();

		//Act
		for (int i = 1; i <= 1000; i++)
		{
			b.Append(i.ToString());
			rope.AddText(i.ToString());
			b.Append(' ');
			rope.Add(' ');
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}

	[Fact]
	public void appending_small_ropes_matches_string_builder() // ConcatenateSmallRopesToRope
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();

		//Act
		for (int i = 1; i <= 1000; i++)
		{
			b.Append(i.ToString());
			b.Append(' ');
			rope.AddRange(CharRope.Create(i.ToString() + " "));
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}

	[Fact]
	public void appending_long_text_to_empty_rope_round_trips() // AppendLongTextToEmptyRope
	{
		//Arrange
		string text = BuildLongString(1000);
		Rope<char> rope = new Rope<char>();

		//Act
		rope.AddText(text);

		//Assert
		Assert.Equal(text, rope.ToString());
	}

	[Fact]
	public void prepending_strings_and_chars_matches_string_builder() // ConcatenateStringToRopeBackwards
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();
		for (int i = 1; i <= 1000; i++)
		{
			b.Append(i.ToString());
			b.Append(' ');
		}

		//Act
		for (int i = 1000; i >= 1; i--)
		{
			rope.Insert(0, ' ');
			rope.InsertText(0, i.ToString());
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}

	[Fact]
	public void prepending_small_ropes_matches_string_builder() // ConcatenateSmallRopesToRopeBackwards
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();
		for (int i = 1; i <= 1000; i++)
		{
			b.Append(i.ToString());
			b.Append(' ');
		}

		//Act
		for (int i = 1000; i >= 1; i--)
		{
			rope.InsertRange(0, CharRope.Create(i.ToString() + " "));
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}

	[Fact]
	public void inserting_text_in_the_middle_matches_string_builder() // ConcatenateStringToRopeByInsertionInMiddle
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();
		for (int i = 1; i <= 998; i++)
		{
			b.Append(i.ToString("d3"));
			b.Append(' ');
		}

		//Act
		int middle = 0;
		for (int i = 1; i <= 499; i++)
		{
			rope.InsertText(middle, i.ToString("d3"));
			middle += 3;
			rope.Insert(middle, ' ');
			middle++;
			rope.InsertText(middle, (999 - i).ToString("d3"));
			rope.Insert(middle + 3, ' ');
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}

	[Fact]
	public void inserting_small_ropes_in_the_middle_matches_string_builder() // ConcatenateSmallRopesByInsertionInMiddle
	{
		//Arrange
		StringBuilder b = new StringBuilder();
		Rope<char> rope = new Rope<char>();
		for (int i = 1; i <= 1000; i++)
		{
			b.Append(i.ToString("d3"));
			b.Append(' ');
		}

		//Act
		int middle = 0;
		for (int i = 1; i <= 500; i++)
		{
			rope.InsertRange(middle, CharRope.Create(i.ToString("d3") + " "));
			middle += 4;
			rope.InsertRange(middle, CharRope.Create((1001 - i).ToString("d3") + " "));
		}

		//Assert
		Assert.Equal(b.ToString(), rope.ToString());
	}
}
