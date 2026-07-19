#nullable enable

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// Where a line sits within the layout's text.
/// </summary>
/// <param name="Start">The index of the line's first character within the layout's text.</param>
/// <param name="Length">The number of characters on the line, including any trailing line break.</param>
/// <param name="LineIndex">The zero-based line number.</param>
/// <param name="IsFirstLine">True when this is the first line.</param>
/// <param name="IsLastLine">True when this is the last line.</param>
public readonly record struct TextLineInfo(
	int Start,
	int Length,
	int LineIndex,
	bool IsFirstLine,
	bool IsLastLine);
