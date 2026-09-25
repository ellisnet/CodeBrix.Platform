#nullable enable

using System;
using System.Linq;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The TextLayout engine (WPE1 C4 + C5): since the text-engine home change CodeBrix.Platform.UI.TextLayout.Core carries
/// its own copy of the shared text engine (the same source the framework compiles for TextBlock), so text is shaped,
/// bidi-resolved, broken into lines, measured, hit-tested and drawn with only the engine Core, SkiaSharp/HarfBuzz and a
/// platform font source in the process - and no WinUI assembly loads.
/// </summary>
public class TextLayoutEngineTests
{
	[Fact]
	public void When_Text_Is_Laid_Out_Measured_And_Hit_Tested_Then_The_Geometry_Is_Consistent_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var fontSource = TestFontSourcePlatform.EnsureRegistered();
		const string text = "The quick brown fox jumps over the lazy dog";

		//Act
		using var single = TextLayoutEngine.Layout(text, fontSize: 20f);
		using var wrapped = TextLayoutEngine.Layout(text, fontSize: 20f, options: new TextLayoutOptions { MaxWidth = single.Size.Width / 2 });
		using var rightToLeft = TextLayoutEngine.Layout("שלום עולם", fontSize: 20f);
		var caretStart = single.GetCaretRect(0);
		var caretEnd = single.GetCaretRect(text.Length);
		var middleIndex = single.GetNearestIndexAt(new SKPoint(single.Size.Width / 2, single.LineHeight / 2));
		var selection = wrapped.GetSelectionRects(0, text.Length);
		var lastLine = wrapped.GetLineMetrics(wrapped.LineCount - 1);

		//Assert
		single.Text.Should().Be(text);
		single.LineCount.Should().Be(1);
		single.Size.Width.Should().BeGreaterThan(200f);
		single.Size.Height.Should().BeGreaterThan(15f);
		single.IsBaseDirectionRightToLeft.Should().BeFalse();
		caretEnd.Left.Should().BeGreaterThan(caretStart.Left);
		middleIndex.Should().BeInRange(1, text.Length - 1);
		wrapped.LineCount.Should().BeGreaterThan(1);
		wrapped.Size.Width.Should().BeLessThanOrEqualTo(single.Size.Width / 2);
		wrapped.Size.Height.Should().BeGreaterThan(single.Size.Height);
		selection.Count.Should().BeGreaterThanOrEqualTo(wrapped.LineCount);
		(lastLine.Start + lastLine.Length).Should().Be(text.Length);
		rightToLeft.IsBaseDirectionRightToLeft.Should().BeTrue();
		fontSource.TypefaceRequests.Should().BeGreaterThan(0); // the engine's fonts came from the platform's font source

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.TextLayout.Core");
		EngineIsolation.AssertNoWinUILoaded("TextLayout");
	}

	[Fact]
	public void When_A_Layout_Is_Drawn_And_Outlined_Then_Glyphs_Reach_The_Canvas_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		TestFontSourcePlatform.EnsureRegistered();
		using var layout = TextLayoutEngine.Layout(
			new[]
			{
				new TextRunDescriptor("Bold ", null, 24f, TextFontWeight.Bold),
				new TextRunDescriptor("red", null, 24f) { Color = SKColors.Red },
			});
		using var bitmap = new SKBitmap((int)Math.Ceiling(layout.Size.Width) + 4, (int)Math.Ceiling(layout.Size.Height) + 4);
		using var canvas = new SKCanvas(bitmap);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		canvas.Clear(SKColors.Transparent);

		//Act
		layout.Draw(canvas, new SKPoint(2, 2), paint);
		canvas.Flush();
		using var outline = layout.GetOutlinePath();
		var glyphs = layout.GetGlyphOutlines();
		var pixels = bitmap.Pixels;

		//Assert
		layout.Text.Should().Be("Bold red");
		pixels.Count(p => p.Alpha > 0).Should().BeGreaterThan(50);
		pixels.Any(p => p.Alpha == 255 && p.Red > 200 && p.Green < 60 && p.Blue < 60).Should().BeTrue(); // the run's own colour
		outline.IsEmpty.Should().BeFalse();
		glyphs.Count.Should().BeGreaterThanOrEqualTo(7); // one per letter (the space may or may not be a glyph)
		foreach (var glyph in glyphs)
		{
			glyph.Dispose();
		}

		EngineIsolation.AssertNoWinUILoaded("TextLayout");
	}
}
