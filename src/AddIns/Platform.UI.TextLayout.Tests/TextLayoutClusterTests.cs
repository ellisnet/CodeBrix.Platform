#nullable enable

using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// Cluster mapping and bidi - the behaviour a shaper exists to provide.
/// </summary>
/// <remarks>
/// ASCII-only text would pass these even with a broken cluster model, so the text here is chosen to
/// shape non-trivially. Note that the engine deliberately disables the 'liga' OpenType feature, so
/// "fi" stays two glyphs; combining marks are therefore the reliable way to produce a multi-character
/// cluster.
/// </remarks>
public class TextLayoutClusterTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 32f;

	// "e" followed by COMBINING ACUTE ACCENT: two chars, one grapheme, one cluster.
	private const string CombiningText = "é";

	[Fact]
	public void Combining_mark_shares_a_cluster_with_its_base_character()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout(CombiningText, TestFamily, TestSize);

		//Act
		var baseRect = layout.GetRectForIndex(0);
		var markRect = layout.GetRectForIndex(1);

		//Assert - both indices address the same cluster, so they report the same rectangle
		markRect.Should().Be(baseRect);
	}

	[Fact]
	public void Combining_mark_does_not_advance_the_caret_past_its_base()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout(CombiningText, TestFamily, TestSize);

		//Act
		var caretAtBase = layout.GetCaretRect(0, 1f);
		var caretAtMark = layout.GetCaretRect(1, 1f);

		//Assert - the mark carries no advance of its own
		caretAtMark.Left.Should().Be(caretAtBase.Left);
	}

	[Fact]
	public void Combining_mark_text_measures_as_one_advance_width()
	{
		//Arrange
		using var combining = TextLayoutEngine.Layout(CombiningText, TestFamily, TestSize);
		using var bare = TextLayoutEngine.Layout("e", TestFamily, TestSize);

		//Assert - an accent adds no horizontal advance
		combining.Size.Width.Should().BeApproximately(bare.Size.Width, 0.01f);
	}

	[Fact]
	public void Rtl_text_is_detected_as_right_to_left()
	{
		//Arrange - Hebrew "shalom"
		using var layout = TextLayoutEngine.Layout("שלום", TestFamily, TestSize);

		//Assert
		layout.IsBaseDirectionRightToLeft.Should().BeTrue();
	}

	[Fact]
	public void Ltr_text_is_detected_as_left_to_right()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("hello", TestFamily, TestSize);

		//Assert
		layout.IsBaseDirectionRightToLeft.Should().BeFalse();
	}

	[Fact]
	public void Explicit_base_direction_overrides_detection()
	{
		//Arrange
		var options = new TextLayoutOptions { BaseDirection = TextDirection.RightToLeft };

		//Act
		using var layout = TextLayoutEngine.Layout("hello", TestFamily, TestSize, options);

		//Assert
		layout.IsBaseDirectionRightToLeft.Should().BeTrue();
	}

	[Fact]
	public void Rtl_caret_advances_leftwards()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("שלום", TestFamily, TestSize);

		//Act
		var first = layout.GetCaretRect(0, 1f).Left;
		var last = layout.GetCaretRect(layout.Text.Length, 1f).Left;

		//Assert - in RTL the start of the text is to the RIGHT of its end
		first.Should().BeGreaterThan(last);
	}

	[Fact]
	public void Bidi_text_places_the_rtl_run_in_visual_order()
	{
		//Arrange - Latin, then Hebrew, then Latin; base direction is LTR
		var options = new TextLayoutOptions { BaseDirection = TextDirection.LeftToRight };
		using var layout = TextLayoutEngine.Layout("ab של cd", TestFamily, TestSize, options);

		//Act
		var firstLatin = layout.GetRectForIndex(0);
		var lastLatin = layout.GetRectForIndex(layout.Text.Length - 1);

		//Assert - the surrounding LTR text keeps its left-to-right order around the RTL island
		firstLatin.Left.Should().BeLessThan(lastLatin.Left);
		layout.IsBaseDirectionRightToLeft.Should().BeFalse();
	}

	[Fact]
	public void Hit_testing_inside_an_rtl_run_stays_within_the_text()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("שלום", TestFamily, TestSize);

		//Act
		var rect = layout.GetRectForIndex(1);
		var hit = layout.GetIndexAt(new SKPoint(rect.Left + (rect.Width / 4f), rect.Top + (rect.Height / 2f)));

		//Assert
		hit.Should().BeInRange(0, layout.Text.Length);
	}
}
