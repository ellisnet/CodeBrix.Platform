#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.TextLayout.Contracts;
using CodeBrix.Platform.UI.TextLayout.Internal;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Engine;

/// <summary>
/// The TextLayout add-in's text engine (<see cref="ITextLayoutPlatform"/>): this assembly's own copy of the shared text
/// engine (<see cref="UnicodeText"/>, <see cref="FontDetailsCache"/> and <see cref="TextRunSpec"/>, link-compiled from
/// the framework's source, WPE1 C5). It names no XAML or WinRT type; the fonts come from the platform's font source.
/// </summary>
/// <remarks>
/// Formerly the Skia twin's TextLayoutSkiaPlatform (an adapter over the framework assembly's engine), moved here at the
/// text-engine home change with only the types at this boundary changed to the engine's own.
/// </remarks>
internal sealed class TextLayoutEnginePlatform : ITextLayoutPlatform
{
	/// <summary>The one engine of the process.</summary>
	internal static TextLayoutEnginePlatform Instance { get; } = new();

	private TextLayoutEnginePlatform()
	{
	}

	/// <inheritdoc />
	public void EnsureEngineInitialized() => UnicodeText.EnsureEngineInitialized();

	/// <inheritdoc />
	public bool DetectIsRightToLeft(string text) => UnicodeText.DetectIsRightToLeft(text);

	/// <inheritdoc />
	public IEngineLayout CreateLayout(
		IReadOnlyList<TextRunDescriptor> runs,
		TextDirection baseDirection,
		float availableWidth,
		int maxLines,
		float lineHeight,
		TextAlign alignment,
		bool wrap,
		out SKSize desiredSize)
	{
		var flowDirection = baseDirection == TextDirection.RightToLeft ? EngineFlowDirection.RightToLeft : EngineFlowDirection.LeftToRight;

		// Converted before the runs are built, as the alignment was converted before the platform call it replaced.
		var textAlignment = alignment.ToEngineTextAlignment();

		var specs = new TextRunSpec[runs.Count];
		for (var i = 0; i < runs.Count; i++)
		{
			specs[i] = BuildSpec(runs[i], flowDirection);
		}

		var layout = new UnicodeText(
			new EngineSize(availableWidth, double.PositiveInfinity),
			specs,
			specs[0].FontDetails,
			maxLines,
			lineHeight,
			EngineLineStackingStrategy.MaxHeight,
			flowDirection,
			textAlignment,
			wrap ? EngineTextWrapping.Wrap : EngineTextWrapping.NoWrap,
			out var engineDesiredSize);

		desiredSize = new SKSize((float)engineDesiredSize.Width, (float)engineDesiredSize.Height);
		return new EngineLayout(layout);
	}

	//Moved verbatim from the Skia twin's TextLayoutSkiaPlatform.BuildSpec (formerly TextLayoutEngine.BuildSpec) at the
	//text-engine home change: building an engine run spec resolves the run's font through the engine's font cache.
	private static TextRunSpec BuildSpec(TextRunDescriptor run, EngineFlowDirection layoutFlowDirection)
	{
		var weight = run.Weight.ToEngineWeight();
		var stretch = run.Stretch.ToEngineStretch();
		var style = run.Style.ToEngineStyle();

		// GetFont also hands back a task that completes if the family resolves to a font that has to
		// be downloaded or loaded asynchronously. The details returned immediately are always usable -
		// a fallback face until then - so layout never blocks on it.
		var (details, _) = FontDetailsCache.GetFont(run.FontFamily, run.FontSize, weight, stretch, style);

		var runFlowDirection = run.Direction switch
		{
			TextDirection.LeftToRight => EngineFlowDirection.LeftToRight,
			TextDirection.RightToLeft => EngineFlowDirection.RightToLeft,
			_ => layoutFlowDirection,
		};

		return new TextRunSpec(run.Text, details, runFlowDirection, run.FontSize, weight, stretch, style, run.Color);
	}
}
