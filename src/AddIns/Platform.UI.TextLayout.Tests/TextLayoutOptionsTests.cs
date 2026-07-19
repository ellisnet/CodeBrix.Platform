#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// Multi-run layouts and the options that shape them.
/// </summary>
public class TextLayoutOptionsTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 24f;

	[Fact]
	public void Runs_are_concatenated_into_the_layout_text()
	{
		//Arrange
		var runs = new[]
		{
			new TextRunDescriptor("Hello ", TestFamily, TestSize),
			new TextRunDescriptor("world", TestFamily, TestSize, TextFontWeight.Bold),
		};

		//Act
		using var layout = TextLayoutEngine.Layout(runs);

		//Assert
		layout.Text.Should().Be("Hello world");
	}

	[Fact]
	public void A_bold_run_measures_wider_than_the_same_text_unbolded()
	{
		//Arrange
		using var normal = TextLayoutEngine.Layout([new TextRunDescriptor("Handgloves", TestFamily, TestSize)]);
		using var bold = TextLayoutEngine.Layout([new TextRunDescriptor("Handgloves", TestFamily, TestSize, TextFontWeight.Bold)]);

		//Assert
		bold.Size.Width.Should().BeGreaterThan(normal.Size.Width);
	}

	[Fact]
	public void A_larger_font_size_measures_taller()
	{
		//Arrange
		using var small = TextLayoutEngine.Layout("Hg", TestFamily, 12f);
		using var large = TextLayoutEngine.Layout("Hg", TestFamily, 48f);

		//Assert
		large.Size.Height.Should().BeGreaterThan(small.Size.Height);
		large.Size.Width.Should().BeGreaterThan(small.Size.Width);
	}

	[Fact]
	public void Mixed_size_runs_take_the_taller_line_height()
	{
		//Arrange
		var runs = new[]
		{
			new TextRunDescriptor("small ", TestFamily, 12f),
			new TextRunDescriptor("BIG", TestFamily, 48f),
		};

		//Act
		using var mixed = TextLayoutEngine.Layout(runs);
		using var smallOnly = TextLayoutEngine.Layout("small ", TestFamily, 12f);

		//Assert
		mixed.LineHeight.Should().BeGreaterThan(smallOnly.LineHeight);
	}

	[Fact]
	public void A_max_width_wraps_text_onto_more_lines()
	{
		//Arrange
		var text = "the quick brown fox jumps over the lazy dog";
		var options = new TextLayoutOptions { MaxWidth = 120f };

		//Act
		using var wrapped = TextLayoutEngine.Layout(text, TestFamily, TestSize, options);
		using var unwrapped = TextLayoutEngine.Layout(text, TestFamily, TestSize);

		//Assert
		unwrapped.LineCount.Should().Be(1);
		wrapped.LineCount.Should().BeGreaterThan(1);
		wrapped.Size.Width.Should().BeLessThanOrEqualTo(120f);
	}

	[Fact]
	public void MaxLines_clamps_the_line_count()
	{
		//Arrange
		var options = new TextLayoutOptions { MaxWidth = 120f, MaxLines = 2 };

		//Act
		using var layout = TextLayoutEngine.Layout(
			"the quick brown fox jumps over the lazy dog",
			TestFamily,
			TestSize,
			options);

		//Assert
		layout.LineCount.Should().Be(2);
	}

	[Fact]
	public void Centre_alignment_indents_a_line_narrower_than_the_layout_width()
	{
		//Arrange
		var centred = new TextLayoutOptions { MaxWidth = 400f, Alignment = TextAlign.Center };
		var left = new TextLayoutOptions { MaxWidth = 400f, Alignment = TextAlign.Left };

		//Act
		using var centredLayout = TextLayoutEngine.Layout("hi", TestFamily, TestSize, centred);
		using var leftLayout = TextLayoutEngine.Layout("hi", TestFamily, TestSize, left);

		//Assert
		centredLayout.GetCaretRect(0, 1f).Left.Should().BeGreaterThan(leftLayout.GetCaretRect(0, 1f).Left);
	}

	[Fact]
	public void Alignment_is_ignored_when_there_is_no_max_width()
	{
		//Arrange - with no width there is no box to align within
		var options = new TextLayoutOptions { Alignment = TextAlign.Center };

		//Act
		using var layout = TextLayoutEngine.Layout("hi", TestFamily, TestSize, options);

		//Assert
		layout.GetCaretRect(0, 1f).Left.Should().Be(0f);
	}

	[Fact]
	public void An_explicit_line_height_raises_a_short_line()
	{
		//Arrange
		var options = new TextLayoutOptions { LineHeight = 200f };

		//Act
		using var layout = TextLayoutEngine.Layout("hi", TestFamily, TestSize, options);

		//Assert
		layout.LineHeight.Should().BeApproximately(200f, 0.01f);
	}

	[Fact]
	public void Line_metrics_report_each_line_correctly()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("one\ntwo\nthree", TestFamily, TestSize);

		//Act
		var firstLine = layout.GetLineAt(0);
		var secondLine = layout.GetLineAt(4);

		//Assert
		firstLine.LineIndex.Should().Be(0);
		firstLine.IsFirstLine.Should().BeTrue();
		firstLine.IsLastLine.Should().BeFalse();
		secondLine.LineIndex.Should().Be(1);
		secondLine.Start.Should().Be(firstLine.Start + firstLine.Length);
	}

	[Fact]
	public void Layout_rejects_a_null_run_list()
	{
		//Act - the cast is required: an untyped null is ambiguous between the two Layout overloads
		var act = () => TextLayoutEngine.Layout((IReadOnlyList<TextRunDescriptor>)null!);

		//Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void A_run_rejects_null_text()
	{
		//Act
		var act = () => new TextRunDescriptor(null!);

		//Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void A_run_rejects_a_non_positive_font_size()
	{
		//Act
		var act = () => new TextRunDescriptor("x", TestFamily, 0f);

		//Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Create_maps_the_bold_and_italic_shorthand()
	{
		//Act
		var run = TextRunDescriptor.Create("x", TestFamily, TestSize, bold: true, italic: true);

		//Assert
		run.Weight.Should().Be(TextFontWeight.Bold);
		run.Style.Should().Be(TextFontStyle.Italic);
	}
}
