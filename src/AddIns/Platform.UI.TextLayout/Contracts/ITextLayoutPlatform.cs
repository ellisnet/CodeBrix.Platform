#nullable enable

using System.Collections.Generic;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Contracts;

/// <summary>
/// The TextLayout add-in's text engine: shaping, bidi resolution, font resolution and line breaking. The code API
/// (<see cref="TextLayoutEngine"/>) validates its input and settles the options, and asks the engine for the layout
/// itself. The signatures name only this add-in's own types, SkiaSharp and the BCL (WPE1 C4): no XAML or WinRT type.
/// </summary>
/// <remarks>
/// Implementers: this assembly (<c>Engine.TextLayoutEnginePlatform</c>, over its own copy of the shared text engine).
/// <para>
/// Since the text-engine home change (WPE1 C5) the engine is compiled INTO this assembly - the same source the
/// framework's Skia assembly compiles for TextBlock - so no platform implements this contract any more: what differs
/// per platform is only the font source (<c>CodeBrix.Platform.Foundation.Contracts.IFontSourcePlatform</c>), which the
/// platform registers. The contract stays as the internal seam between the code API and the engine. Held once, in a
/// static field (<c>Internal.TextLayoutPlatform.Engine</c>).
/// </para>
/// </remarks>
internal interface ITextLayoutPlatform
{
	/// <summary>
	/// Makes sure the engine's native dependencies (ICU) are set up; after the first call this costs nothing.
	/// </summary>
	void EnsureEngineInitialized();

	/// <summary>
	/// Resolves the base direction of a text from its content (the first strong character).
	/// </summary>
	/// <param name="text">The text.</param>
	/// <returns>True when the text resolves right-to-left.</returns>
	bool DetectIsRightToLeft(string text);

	/// <summary>
	/// Lays out a sequence of styled runs.
	/// </summary>
	/// <param name="runs">The runs, validated (non-empty, no null run); concatenated in order to form the layout's text.</param>
	/// <param name="baseDirection">The resolved base direction (never <see cref="TextDirection.Auto"/>); a run whose
	/// direction is Auto inherits it.</param>
	/// <param name="availableWidth">The width to wrap and align within; zero when there is no box.</param>
	/// <param name="maxLines">The maximum number of lines, zero for no limit.</param>
	/// <param name="lineHeight">The line height, zero or NaN for the fonts' own.</param>
	/// <param name="alignment">The horizontal alignment of each line.</param>
	/// <param name="wrap">Whether lines wrap at <paramref name="availableWidth"/>.</param>
	/// <param name="desiredSize">The measured size of the laid-out text.</param>
	/// <returns>The completed layout.</returns>
	/// <exception cref="System.ArgumentOutOfRangeException">An alignment, or a run's style or stretch, is not a defined
	/// value.</exception>
	IEngineLayout CreateLayout(
		IReadOnlyList<TextRunDescriptor> runs,
		TextDirection baseDirection,
		float availableWidth,
		int maxLines,
		float lineHeight,
		TextAlign alignment,
		bool wrap,
		out SKSize desiredSize);
}
