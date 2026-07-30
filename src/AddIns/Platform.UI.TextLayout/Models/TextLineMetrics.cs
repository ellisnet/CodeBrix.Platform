#nullable enable

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// The geometry of one laid-out line: where it sits vertically, how tall it is, where its
/// baseline falls, and which slice of the layout's text it covers.
/// </summary>
/// <param name="Start">The index of the line's first character within the layout's text.</param>
/// <param name="Length">The number of characters on the line, including any trailing line break.</param>
/// <param name="Top">The vertical offset of the line's top edge, in layout coordinates.</param>
/// <param name="Height">The line's height.</param>
/// <param name="BaselineOffset">The distance from the line's top edge down to its baseline.</param>
/// <remarks>
/// This is what a document renderer needs to place per-line decorations - underlines sit
/// relative to <paramref name="BaselineOffset"/>, and selection or highlight backgrounds fill
/// <paramref name="Top"/> to <paramref name="Top"/> + <paramref name="Height"/>.
/// </remarks>
public readonly record struct TextLineMetrics(
	int Start,
	int Length,
	float Top,
	float Height,
	float BaselineOffset);
