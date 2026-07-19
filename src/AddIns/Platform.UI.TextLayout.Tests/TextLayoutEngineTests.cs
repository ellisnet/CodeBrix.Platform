#nullable enable

using System;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// The T1 exit gate: a layout built from plain text, with no XAML object and no application host,
/// measures, places a caret, and hit-tests correctly.
/// </summary>
/// <remarks>
/// These assert GEOMETRY, never rendered pixels or resolved font family names. Family-name
/// resolution is machine-dependent - "sans-serif" lands on Noto Sans here and may land elsewhere on
/// another box - so anything that would encode this machine's font choices is deliberately avoided.
/// </remarks>
public class TextLayoutEngineTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 24f;

	[Fact]
	public void Layout_produces_a_measurable_layout_with_no_host()
	{
		//Act
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Assert
		layout.Text.Should().Be("Hello");
		layout.LineCount.Should().Be(1);
		layout.Size.Width.Should().BeGreaterThan(0f);
		layout.Size.Height.Should().BeGreaterThan(0f);
		layout.LineHeight.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void Layout_caret_at_start_is_at_the_origin()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var caret = layout.GetCaretRect(0, 1f);

		//Assert
		caret.Left.Should().Be(0f);
		caret.Top.Should().Be(0f);
		caret.Width.Should().Be(1f);
		caret.Height.Should().BeApproximately(layout.LineHeight, 0.01f);
	}

	[Fact]
	public void Layout_caret_advances_monotonically_across_ltr_text()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var previous = float.NegativeInfinity;

		//Assert
		for (var i = 0; i <= layout.Text.Length; i++)
		{
			var x = layout.GetCaretRect(i, 1f).Left;
			x.Should().BeGreaterThan(previous);
			previous = x;
		}
	}

	[Fact]
	public void Layout_hit_test_round_trips_for_every_index()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello world", TestFamily, TestSize);

		//Act & Assert
		for (var i = 0; i < layout.Text.Length; i++)
		{
			var rect = layout.GetRectForIndex(i);

			// Probe the left quarter, not the centre. The centre of a cluster is exactly equidistant
			// from the caret positions on either side of it, and the engine resolves that tie towards
			// the cluster's end - so a centre probe legitimately returns i+1.
			var probe = new SKPoint(rect.Left + (rect.Width / 4f), rect.Top + (rect.Height / 2f));
			var hit = layout.GetIndexAt(probe);

			// Assert the hit lands on the SAME CLUSTER, not the same integer. For text where shaping
			// is not one-to-one the two differ, and the cluster is the correct unit.
			layout.GetRectForIndex(hit).Should().Be(rect, $"a probe inside index {i}'s cluster should resolve to that cluster");
		}
	}

	[Fact]
	public void Layout_hit_test_outside_the_text_returns_negative_one()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var index = layout.GetIndexAt(new SKPoint(-50f, -50f));

		//Assert
		index.Should().Be(-1);
	}

	[Fact]
	public void Layout_nearest_index_clamps_instead_of_failing()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var index = layout.GetNearestIndexAt(new SKPoint(-50f, -50f));

		//Assert
		index.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public void Layout_empty_text_still_has_a_caret_and_a_line_height()
	{
		//Act
		using var layout = TextLayoutEngine.Layout(string.Empty, TestFamily, TestSize);

		//Assert
		layout.Text.Should().BeEmpty();
		layout.Size.Width.Should().Be(0f);
		layout.LineHeight.Should().BeGreaterThan(0f);
		layout.GetCaretRect(0, 1f).Height.Should().BeApproximately(layout.LineHeight, 0.01f);
	}

	[Fact]
	public void Layout_explicit_newlines_produce_multiple_lines()
	{
		//Act
		using var layout = TextLayoutEngine.Layout("one\ntwo\nthree", TestFamily, TestSize);

		//Assert
		layout.LineCount.Should().Be(3);
		layout.GetLineAt(0).LineIndex.Should().Be(0);
		layout.GetLineAt(0).IsFirstLine.Should().BeTrue();
		layout.GetLineAt(layout.Text.Length).IsLastLine.Should().BeTrue();
	}

	[Fact]
	public void Layout_without_a_max_width_does_not_wrap()
	{
		//Arrange - a long single-word-free string that would certainly wrap in any sane width
		var text = string.Join(" ", new string('x', 20), new string('y', 20), new string('z', 20));

		//Act
		using var layout = TextLayoutEngine.Layout(text, TestFamily, TestSize);

		//Assert
		layout.LineCount.Should().Be(1);
	}

	[Fact]
	public void Layout_rejects_an_empty_run_list()
	{
		//Act
		var act = () => TextLayoutEngine.Layout(Array.Empty<TextRunDescriptor>());

		//Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void GetCaretRect_rejects_an_out_of_range_index()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("Hello", TestFamily, TestSize);

		//Act
		var act = () => layout.GetCaretRect(layout.Text.Length + 1, 1f);

		//Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
