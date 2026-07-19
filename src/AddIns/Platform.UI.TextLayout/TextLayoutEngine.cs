#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.TextLayout.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// Lays text out, shaped and bidi-resolved, with no XAML and no application host.
/// </summary>
/// <remarks>
/// <para>
/// This is a façade over the text engine that already drives every TextBlock in a CodeBrix.Platform
/// application - the same shaping, the same itemisation and font fallback, the same caret and
/// cluster maths. There is deliberately only one implementation in the family: a bug fixed here is
/// fixed for TextBlock too, and vice versa.
/// </para>
/// <para>
/// Nothing in this API accepts or returns a XAML type, so it can be used from a document model, a
/// game, an image pipeline, or a test - anywhere with a canvas and no visual tree.
/// </para>
/// </remarks>
public static class TextLayoutEngine
{
	/// <summary>
	/// Lays out a sequence of styled runs.
	/// </summary>
	/// <param name="runs">The runs, concatenated in order to form the layout's text.</param>
	/// <param name="options">Layout options, or null for the defaults.</param>
	/// <returns>The completed layout.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="runs"/> is null, or contains a null run.</exception>
	/// <exception cref="ArgumentException"><paramref name="runs"/> is empty.</exception>
	public static TextLayoutResult Layout(IReadOnlyList<TextRunDescriptor> runs, TextLayoutOptions? options = null)
	{
		if (runs is null)
		{
			throw new ArgumentNullException(nameof(runs));
		}

		if (runs.Count == 0)
		{
			throw new ArgumentException(
				"At least one run is required. To lay out empty text, pass a single run whose text is empty.",
				nameof(runs));
		}

		options ??= new TextLayoutOptions();

		// Shaping, bidi and line breaking all call into native ICU, which an application head sets up
		// from a generated module initializer. There is no head here by design, so ask the engine to
		// initialise itself; inside an application this is already done and costs nothing.
		UnicodeText.EnsureEngineInitialized();

		// The base direction has to be settled before the runs are built, because a run that asks for
		// TextDirection.Auto inherits it.
		var combinedText = BuildCombinedText(runs);
		var isRightToLeft = options.BaseDirection switch
		{
			TextDirection.LeftToRight => false,
			TextDirection.RightToLeft => true,
			_ => UnicodeText.DetectIsRightToLeft(combinedText),
		};
		var flowDirection = isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

		var specs = new TextRunSpec[runs.Count];
		for (var i = 0; i < runs.Count; i++)
		{
			var run = runs[i];
			if (run is null)
			{
				throw new ArgumentNullException(nameof(runs), $"Run at index {i} is null.");
			}

			specs[i] = BuildSpec(run, flowDirection);
		}

		// With no width there is no box to align within, so pass zero: the engine's alignment maths
		// only shifts a line when it fits inside the available width, and nothing fits inside zero.
		// Passing infinity here would produce an infinite alignment offset.
		var availableWidth = options.MaxWidth ?? 0f;
		var wrapping = options.MaxWidth.HasValue ? TextWrapping.Wrap : TextWrapping.NoWrap;

		var layout = new UnicodeText(
			new Size(availableWidth, double.PositiveInfinity),
			specs,
			specs[0].FontDetails,
			Math.Max(0, options.MaxLines),
			options.LineHeight,
			LineStackingStrategy.MaxHeight,
			flowDirection,
			options.Alignment.ToTextAlignment(),
			wrapping,
			out var desiredSize);

		return new TextLayoutResult(layout, desiredSize);
	}

	/// <summary>
	/// Lays out a single run of uniformly styled text.
	/// </summary>
	/// <param name="text">The text to lay out.</param>
	/// <param name="fontFamily">The font family to resolve, or null for the platform default.</param>
	/// <param name="fontSize">The em size, in layout units.</param>
	/// <param name="options">Layout options, or null for the defaults.</param>
	/// <returns>The completed layout.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
	public static TextLayoutResult Layout(
		string text,
		string? fontFamily = null,
		float fontSize = 12f,
		TextLayoutOptions? options = null) =>
		Layout([new TextRunDescriptor(text, fontFamily, fontSize)], options);

	private static string BuildCombinedText(IReadOnlyList<TextRunDescriptor> runs)
	{
		if (runs.Count == 1)
		{
			return runs[0]?.Text ?? throw new ArgumentNullException(nameof(runs), "Run at index 0 is null.");
		}

		var builder = new StringBuilder();
		for (var i = 0; i < runs.Count; i++)
		{
			var run = runs[i];
			if (run is null)
			{
				throw new ArgumentNullException(nameof(runs), $"Run at index {i} is null.");
			}

			builder.Append(run.Text);
		}

		return builder.ToString();
	}

	private static TextRunSpec BuildSpec(TextRunDescriptor run, FlowDirection layoutFlowDirection)
	{
		var weight = run.Weight.ToFontWeight();
		var stretch = run.Stretch.ToFontStretch();
		var style = run.Style.ToFontStyle();

		// GetFont also hands back a task that completes if the family resolves to a font that has to
		// be downloaded or loaded asynchronously. The details returned immediately are always usable -
		// a fallback face until then - so layout never blocks on it.
		var (details, _) = FontDetailsCache.GetFont(run.FontFamily, run.FontSize, weight, stretch, style);

		var runFlowDirection = run.Direction switch
		{
			TextDirection.LeftToRight => FlowDirection.LeftToRight,
			TextDirection.RightToLeft => FlowDirection.RightToLeft,
			_ => layoutFlowDirection,
		};

		return new TextRunSpec(run.Text, details, runFlowDirection, run.FontSize, weight, stretch, style);
	}
}
