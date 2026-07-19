namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// The base writing direction of a piece of text.
/// </summary>
public enum TextDirection
{
	/// <summary>Resolve the direction from the text's own content, per UAX #9.</summary>
	Auto = 0,

	/// <summary>Left-to-right.</summary>
	LeftToRight = 1,

	/// <summary>Right-to-left.</summary>
	RightToLeft = 2,
}

/// <summary>
/// How lines are positioned horizontally within the layout width.
/// </summary>
/// <remarks>
/// Alignment has no effect unless <see cref="TextLayoutOptions.MaxWidth"/> is set: with no width
/// there is no box to align within, and every line starts at zero.
/// </remarks>
public enum TextAlign
{
	/// <summary>Align to the left edge.</summary>
	Left = 0,

	/// <summary>Centre within the layout width.</summary>
	Center = 1,

	/// <summary>Align to the right edge.</summary>
	Right = 2,
}

/// <summary>
/// The weight of a font face, using the usual OpenType numeric scale.
/// </summary>
public enum TextFontWeight
{
	/// <summary>Weight 100.</summary>
	Thin = 100,

	/// <summary>Weight 200.</summary>
	ExtraLight = 200,

	/// <summary>Weight 300.</summary>
	Light = 300,

	/// <summary>Weight 400, the default.</summary>
	Normal = 400,

	/// <summary>Weight 500.</summary>
	Medium = 500,

	/// <summary>Weight 600.</summary>
	SemiBold = 600,

	/// <summary>Weight 700.</summary>
	Bold = 700,

	/// <summary>Weight 800.</summary>
	ExtraBold = 800,

	/// <summary>Weight 900.</summary>
	Black = 900,
}

/// <summary>
/// The slant of a font face.
/// </summary>
public enum TextFontStyle
{
	/// <summary>Upright.</summary>
	Normal = 0,

	/// <summary>Slanted, using a synthesised or true oblique face.</summary>
	Oblique = 1,

	/// <summary>Slanted, using a true italic face where one exists.</summary>
	Italic = 2,
}

/// <summary>
/// The width of a font face.
/// </summary>
public enum TextFontStretch
{
	/// <summary>Unspecified; treated as <see cref="Normal"/>.</summary>
	Undefined = 0,

	/// <summary>The narrowest width.</summary>
	UltraCondensed = 1,

	/// <summary>Narrower than <see cref="Condensed"/>.</summary>
	ExtraCondensed = 2,

	/// <summary>Narrower than <see cref="SemiCondensed"/>.</summary>
	Condensed = 3,

	/// <summary>Narrower than <see cref="Normal"/>.</summary>
	SemiCondensed = 4,

	/// <summary>The normal width.</summary>
	Normal = 5,

	/// <summary>Wider than <see cref="Normal"/>.</summary>
	SemiExpanded = 6,

	/// <summary>Wider than <see cref="SemiExpanded"/>.</summary>
	Expanded = 7,

	/// <summary>Wider than <see cref="Expanded"/>.</summary>
	ExtraExpanded = 8,

	/// <summary>The widest width.</summary>
	UltraExpanded = 9,
}
