namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// How far an icon's tint brush reaches into the artwork.
/// </summary>
/// <remarks>
/// <para>
/// Icon sets differ in how they express "the icon's colour". Most modern sets paint with
/// <c>currentColor</c>, which is exactly the hook a tint needs. Older sets, and anything exported
/// from a drawing tool, often carry hard-coded black or white strokes and fills instead, and those
/// need replacing before the icon can follow a theme.
/// </para>
/// <para>
/// The tint is applied by handing the SVG parser a CSS snippet at load, so the file on disk is
/// never rewritten and one file can be drawn in as many colours as an application asks for.
/// </para>
/// </remarks>
public enum IconTintMode
{
	/// <summary>
	/// Recolours only what the artwork left open: <c>currentColor</c> fills and strokes take the
	/// tint, and every colour the file states outright is kept. This is the default, and the right
	/// choice for an icon set drawn to be themed.
	/// </summary>
	CurrentColorOnly,

	/// <summary>
	/// Recolours <c>currentColor</c> AND any fill or stroke ATTRIBUTE naming pure black or pure
	/// white - <c>#000000</c>, <c>#000</c>, <c>black</c>, <c>#ffffff</c>, <c>#fff</c>, <c>white</c> -
	/// so a monochrome icon exported with hard-coded strokes still follows the theme. A colour
	/// inside an inline <c>style</c> attribute is NOT replaced, because an inline style outranks a
	/// stylesheet; neither is an element that states no colour at all and simply inherits the SVG
	/// default of black.
	/// </summary>
	ReplaceBlackAndWhite,

	/// <summary>
	/// Leaves the artwork exactly as drawn. Use it for a multi-colour icon, or where the tint brush
	/// is set for some other reason and must not reach the picture.
	/// </summary>
	None
}
