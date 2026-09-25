#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.TextLayout.Internal;

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
		TextLayoutPlatform.Engine.EnsureEngineInitialized();

		// The base direction has to be settled before the runs are built, because a run that asks for
		// TextDirection.Auto inherits it.
		var combinedText = BuildCombinedText(runs);
		var isRightToLeft = options.BaseDirection switch
		{
			TextDirection.LeftToRight => false,
			TextDirection.RightToLeft => true,
			_ => TextLayoutPlatform.Engine.DetectIsRightToLeft(combinedText),
		};
		var baseDirection = isRightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;

		for (var i = 0; i < runs.Count; i++)
		{
			if (runs[i] is null)
			{
				throw new ArgumentNullException(nameof(runs), $"Run at index {i} is null.");
			}
		}

		// With no width there is no box to align within, so pass zero: the engine's alignment maths
		// only shifts a line when it fits inside the available width, and nothing fits inside zero.
		// Passing infinity here would produce an infinite alignment offset.
		var availableWidth = options.MaxWidth ?? 0f;
		var wrap = options.MaxWidth.HasValue;

		// Each run is resolved to a font and built into the engine's run spec by the engine
		// (Engine/TextLayoutEnginePlatform.cs), in order, before the engine lays the runs out.
		var layout = TextLayoutPlatform.Engine.CreateLayout(
			runs,
			baseDirection,
			availableWidth,
			Math.Max(0, options.MaxLines),
			options.LineHeight,
			options.Alignment,
			wrap,
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
}
