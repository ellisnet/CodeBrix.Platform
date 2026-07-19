#nullable enable

using System.Linq;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// Glyph outlines (T4) and canvas rendering (T5).
/// </summary>
public class TextLayoutOutlineTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 32f;

	[Fact]
	public void GetGlyphOutlines_returns_one_entry_per_glyph()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);

		//Act
		var outlines = layout.GetGlyphOutlines();

		//Assert
		outlines.Should().HaveCount(3);
		foreach (var outline in outlines)
		{
			outline.Dispose();
		}
	}

	[Fact]
	public void GetGlyphOutlines_gives_each_glyph_a_non_empty_path_and_an_advance()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);

		//Act
		var outlines = layout.GetGlyphOutlines();

		//Assert
		try
		{
			outlines.Should().OnlyContain(o => o.Advance > 0f);
			outlines.Should().OnlyContain(o => o.Path != null && o.Path.PointCount > 0);
		}
		finally
		{
			foreach (var outline in outlines)
			{
				outline.Dispose();
			}
		}
	}

	[Fact]
	public void GetGlyphOutlines_positions_glyphs_left_to_right()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);

		//Act
		var outlines = layout.GetGlyphOutlines();

		//Assert
		try
		{
			var xs = outlines.Select(o => o.Origin.X).ToArray();
			xs.Should().BeInAscendingOrder();
			xs[0].Should().Be(0f);
		}
		finally
		{
			foreach (var outline in outlines)
			{
				outline.Dispose();
			}
		}
	}

	[Fact]
	public void A_space_has_an_advance_but_nothing_to_draw()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("A B", TestFamily, TestSize);

		//Act
		var outlines = layout.GetGlyphOutlines();

		//Assert
		try
		{
			var space = outlines[1];
			space.Advance.Should().BeGreaterThan(0f);
			space.Path!.PointCount.Should().Be(0);
		}
		finally
		{
			foreach (var outline in outlines)
			{
				outline.Dispose();
			}
		}
	}

	[Fact]
	public void GetOutlinePath_combines_every_glyph()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);

		//Act
		using var path = layout.GetOutlinePath();

		//Assert
		path.PointCount.Should().BeGreaterThan(0);
		path.Bounds.Width.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void GetOutlinePath_bounds_sit_within_the_measured_layout()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);

		//Act
		using var path = layout.GetOutlinePath();

		//Assert - glyph ink is never wider than the advance width it was laid out with
		path.Bounds.Left.Should().BeGreaterThanOrEqualTo(-0.5f);
		path.Bounds.Right.Should().BeLessThanOrEqualTo(layout.Size.Width + 0.5f);
	}

	[Fact]
	public void GetOutlinePath_of_empty_text_is_empty()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout(string.Empty, TestFamily, TestSize);

		//Act
		using var path = layout.GetOutlinePath();

		//Assert
		path.PointCount.Should().Be(0);
	}

	[Fact]
	public void Draw_puts_ink_on_a_canvas()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("ABC", TestFamily, TestSize);
		var info = new SKImageInfo(256, 128, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.White);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

		//Act
		layout.Draw(surface.Canvas, new SKPoint(4f, 4f), paint);

		//Assert
		CountNonWhitePixels(surface, info).Should().BeGreaterThan(0);
	}

	[Fact]
	public void Draw_at_different_origins_puts_ink_in_different_places()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout("A", TestFamily, TestSize);
		var info = new SKImageInfo(256, 128, SKColorType.Rgba8888, SKAlphaType.Premul);

		//Act
		var leftInk = InkColumnSum(layout, info, new SKPoint(4f, 4f));
		var rightInk = InkColumnSum(layout, info, new SKPoint(120f, 4f));

		//Assert - drawing further right moves the ink's centre of mass right
		rightInk.Should().BeGreaterThan(leftInk);
	}

	[Fact]
	public void Draw_of_empty_text_leaves_the_canvas_alone()
	{
		//Arrange
		using var layout = TextLayoutEngine.Layout(string.Empty, TestFamily, TestSize);
		var info = new SKImageInfo(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.White);
		using var paint = new SKPaint { Color = SKColors.Black };

		//Act
		layout.Draw(surface.Canvas, SKPoint.Empty, paint);

		//Assert
		CountNonWhitePixels(surface, info).Should().Be(0);
	}

	private static int CountNonWhitePixels(SKSurface surface, SKImageInfo info)
	{
		using var image = surface.Snapshot();
		using var bitmap = SKBitmap.FromImage(image);
		var count = 0;
		for (var y = 0; y < info.Height; y++)
		{
			for (var x = 0; x < info.Width; x++)
			{
				if (bitmap.GetPixel(x, y) != SKColors.White)
				{
					count++;
				}
			}
		}

		return count;
	}

	private static double InkColumnSum(TextLayoutResult layout, SKImageInfo info, SKPoint origin)
	{
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.White);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		layout.Draw(surface.Canvas, origin, paint);

		using var image = surface.Snapshot();
		using var bitmap = SKBitmap.FromImage(image);
		double weighted = 0;
		var inked = 0;
		for (var y = 0; y < info.Height; y++)
		{
			for (var x = 0; x < info.Width; x++)
			{
				if (bitmap.GetPixel(x, y) != SKColors.White)
				{
					weighted += x;
					inked++;
				}
			}
		}

		return inked == 0 ? 0 : weighted / inked;
	}
}
