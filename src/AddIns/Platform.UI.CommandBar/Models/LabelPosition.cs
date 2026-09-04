namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Where a tool bar item's text sits relative to its icon when both are shown.
/// </summary>
/// <remarks>
/// Only meaningful when the effective <see cref="LabelMode"/> is
/// <see cref="LabelMode.IconAndText"/>.
/// </remarks>
public enum LabelPosition
{
	/// <summary>The text follows the icon on the same line. The default.</summary>
	Right,

	/// <summary>The text sits under the icon, both centred.</summary>
	Bottom,
}
