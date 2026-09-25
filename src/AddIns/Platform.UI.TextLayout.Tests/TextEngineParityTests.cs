#nullable enable

using System.Linq;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// The text-engine home change (WPE1 C5): the engine is ONE source compiled into the framework's Skia assembly (which
/// lays out every TextBlock) and into CodeBrix.Platform.UI.TextLayout.Core (which lays out TextLayout, and through it the
/// editor and the terminal). Both copies must measure identically - this lays the same runs out through the add-in and
/// through the framework's copy (reached through the framework's InternalsVisibleTo grant to this suite) and compares
/// every measurement exactly.
/// </summary>
public class TextEngineParityTests
{
	private const string TestFamily = "sans-serif";
	private const float TestSize = 17f;
	private const float MaxWidth = 120f;

	[Fact]
	public void When_The_Same_Runs_Are_Laid_Out_By_TextLayout_And_By_The_Framework_Engine_Then_Every_Measurement_Is_Identical()
	{
		//Arrange
		var runs = new[]
		{
			new TextRunDescriptor("Hello, wörld! שלום 123 ", TestFamily, TestSize),
			new TextRunDescriptor("a bold tail that wraps", TestFamily, TestSize, TextFontWeight.Bold),
		};

		//Act
		using var addIn = TextLayoutEngine.Layout(runs, new TextLayoutOptions { MaxWidth = MaxWidth });

		UnicodeText.EnsureEngineInitialized();
		var specs = runs
			.Select(r =>
			{
				var weight = (ushort)r.Weight;
				var (details, _) = FontDetailsCache.GetFont(r.FontFamily, r.FontSize, weight, EngineFontStretch.Normal, EngineFontStyle.Normal);
				return new TextRunSpec(r.Text, details, EngineFlowDirection.LeftToRight, r.FontSize, weight, EngineFontStretch.Normal, EngineFontStyle.Normal, null);
			})
			.ToArray();
		var framework = new UnicodeText(
			new EngineSize(MaxWidth, double.PositiveInfinity),
			specs,
			specs[0].FontDetails,
			0,
			0f,
			EngineLineStackingStrategy.MaxHeight,
			EngineFlowDirection.LeftToRight,
			EngineTextAlignment.Left,
			EngineTextWrapping.Wrap,
			out var frameworkSize);

		//Assert
		addIn.Text.Should().Be(framework.LayoutText);
		addIn.Size.Width.Should().Be((float)frameworkSize.Width);
		addIn.Size.Height.Should().Be((float)frameworkSize.Height);
		addIn.LineCount.Should().Be(framework.LineCount);
		addIn.LineCount.Should().BeGreaterThan(1);
		for (var line = 0; line < addIn.LineCount; line++)
		{
			var metrics = addIn.GetLineMetrics(line);
			metrics.Start.Should().Be(framework.GetLineStartInText(line));
			(metrics.Start + metrics.Length).Should().Be(framework.GetLineEndInText(line));
			metrics.Top.Should().Be(framework.GetLineTop(line));
			metrics.Height.Should().Be(framework.GetLineHeight(line));
			metrics.BaselineOffset.Should().Be(framework.GetLineBaselineOffset(line));
		}

		for (var index = 0; index <= addIn.Text.Length; index++)
		{
			var caret = addIn.GetCaretRect(index);
			var expected = framework.GetCaretRectForIndex(index, 1f);
			caret.Left.Should().Be((float)expected.Left);
			caret.Top.Should().Be((float)expected.Top);
			caret.Right.Should().Be((float)expected.Right);
			caret.Bottom.Should().Be((float)expected.Bottom);
		}
	}
}
