#nullable enable

using System;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// One styled run of text to be laid out.
/// </summary>
/// <remarks>
/// A layout is a sequence of these. Runs are concatenated in order to form the layout's text, so
/// text indices returned by <see cref="TextLayoutResult"/> address that concatenation rather than
/// any individual run. Line breaks come from the text itself; a run is not a line.
/// </remarks>
public sealed class TextRunDescriptor
{
	/// <summary>
	/// Creates a run.
	/// </summary>
	/// <param name="text">The run's text. May be empty, but not null.</param>
	/// <param name="fontFamily">
	/// The font family to resolve, for example "sans-serif" or "Open Sans". Null uses the platform's
	/// default family. Resolution is machine-dependent - the same name can land on different faces on
	/// different hosts.
	/// </param>
	/// <param name="fontSize">The em size, in layout units. Must be greater than zero.</param>
	/// <param name="weight">The face weight.</param>
	/// <param name="style">The face slant.</param>
	/// <param name="stretch">The face width.</param>
	/// <param name="direction">
	/// The run's own base direction. <see cref="TextDirection.Auto"/> defers to the layout's base
	/// direction rather than resolving per run.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="fontSize"/> is not greater than zero.</exception>
	public TextRunDescriptor(
		string text,
		string? fontFamily = null,
		float fontSize = 12f,
		TextFontWeight weight = TextFontWeight.Normal,
		TextFontStyle style = TextFontStyle.Normal,
		TextFontStretch stretch = TextFontStretch.Normal,
		TextDirection direction = TextDirection.Auto)
	{
		if (text is null)
		{
			throw new ArgumentNullException(nameof(text));
		}

		if (!(fontSize > 0f))
		{
			throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
		}

		Text = text;
		FontFamily = fontFamily;
		FontSize = fontSize;
		Weight = weight;
		Style = style;
		Stretch = stretch;
		Direction = direction;
	}

	/// <summary>The run's text.</summary>
	public string Text { get; }

	/// <summary>The font family to resolve, or null for the platform default.</summary>
	public string? FontFamily { get; }

	/// <summary>The em size, in layout units.</summary>
	public float FontSize { get; }

	/// <summary>The face weight.</summary>
	public TextFontWeight Weight { get; }

	/// <summary>The face slant.</summary>
	public TextFontStyle Style { get; }

	/// <summary>The face width.</summary>
	public TextFontStretch Stretch { get; }

	/// <summary>The run's base direction.</summary>
	public TextDirection Direction { get; }

	/// <summary>
	/// The colour to paint this run's glyphs with when the layout is drawn, or null to use the
	/// colour of the paint passed to <see cref="TextLayoutResult.Draw(SkiaSharp.SKCanvas, SkiaSharp.SKPoint, SkiaSharp.SKPaint)"/>.
	/// </summary>
	/// <remarks>
	/// Set with an object initializer: <c>new TextRunDescriptor("if", fontSize: 13f) { Color = new
	/// SKColor(0x56, 0x9C, 0xD6) }</c>. Colour affects drawing only - never measurement, shaping,
	/// or hit-testing - so mixing coloured and uncoloured runs is free.
	/// </remarks>
	public SKColor? Color { get; init; }

	/// <summary>
	/// Creates a run using the common bold/italic shorthand instead of the full weight and slant.
	/// </summary>
	/// <param name="text">The run's text.</param>
	/// <param name="fontFamily">The font family to resolve, or null for the platform default.</param>
	/// <param name="fontSize">The em size, in layout units.</param>
	/// <param name="bold">True for <see cref="TextFontWeight.Bold"/>, false for <see cref="TextFontWeight.Normal"/>.</param>
	/// <param name="italic">True for <see cref="TextFontStyle.Italic"/>, false for <see cref="TextFontStyle.Normal"/>.</param>
	/// <returns>The new run.</returns>
	public static TextRunDescriptor Create(
		string text,
		string? fontFamily = null,
		float fontSize = 12f,
		bool bold = false,
		bool italic = false) =>
		new(
			text,
			fontFamily,
			fontSize,
			bold ? TextFontWeight.Bold : TextFontWeight.Normal,
			italic ? TextFontStyle.Italic : TextFontStyle.Normal);
}
