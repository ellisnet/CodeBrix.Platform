#nullable enable

using System.Linq;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// Selection rectangles (T3) - the geometry a consumer paints behind selected text.
/// </summary>
public class TextLayoutSelectionTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 24f;

	[Fact]
	public void GetSelectionRects_returns_nothing_for_an_empty_range()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello world", TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(3, 0);

		//Assert
		rects.Should().BeEmpty();
	}

	[Fact]
	public void GetSelectionRects_returns_one_rect_for_a_range_within_a_single_line()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello world", TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(0, 5);

		//Assert
		rects.Should().HaveCount(1);
		rects[0].Width.Should().BeGreaterThan(0f);
		rects[0].Height.Should().BeApproximately(layout.LineHeight, 0.01f);
	}

	[Fact]
	public void GetSelectionRects_covers_the_same_span_as_the_character_rects()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello world", TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(0, 5);
		var expectedLeft = layout.GetRectForIndex(0).Left;
		var expectedRight = layout.GetRectForIndex(4).Right;

		//Assert
		rects[0].Left.Should().BeApproximately(expectedLeft, 0.01f);
		rects[0].Right.Should().BeApproximately(expectedRight, 0.01f);
	}

	[Fact]
	public void GetSelectionRects_returns_one_rect_per_line_for_a_multi_line_range()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("one\ntwo\nthree", TestFamily, TestSize);

		//Act - select everything
		var rects = layout.GetSelectionRects(0, layout.Text.Length);

		//Assert - a logical range spanning three lines is three visual rectangles, never one box
		rects.Should().HaveCount(3);
		rects.Select(r => r.Top).Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void GetSelectionRects_selecting_everything_spans_the_full_width()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello world", TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(0, layout.Text.Length);

		//Assert
		rects.Should().HaveCount(1);
		rects[0].Width.Should().BeApproximately(layout.Size.Width, 0.5f);
	}

	[Fact]
	public void GetSelectionRects_clamps_a_range_that_runs_past_the_end()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(0, 5000);

		//Assert
		rects.Should().HaveCount(1);
		rects[0].Width.Should().BeApproximately(layout.Size.Width, 0.5f);
	}

	[Fact]
	public void GetSelectionRects_of_empty_text_is_empty()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout(string.Empty, TestFamily, TestSize);

		//Act
		var rects = layout.GetSelectionRects(0, 10);

		//Assert
		rects.Should().BeEmpty();
	}

	[Fact]
	public void GetSelectionRects_of_a_bidi_range_can_be_discontiguous()
	{
		//Arrange - an RTL island inside LTR text
		var options = new TextLayoutOptions { BaseDirection = TextDirection.LeftToRight };
		using var layout = TextLayoutEngine.Layout("ab של cd", TestFamily, TestSize, options);

		//Act - one line, but the range crosses bidi run boundaries
		var rects = layout.GetSelectionRects(0, layout.Text.Length);

		//Assert - split per visual run rather than merged into a single bounding box
		rects.Should().NotBeEmpty();
		rects.Should().OnlyContain(r => r.Width > 0f);
	}
}
