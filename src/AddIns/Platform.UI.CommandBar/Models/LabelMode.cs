namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Which parts of a tool bar item are shown: its icon, its text, or both.
/// </summary>
/// <remarks>
/// Set on a <see cref="ToolBar"/> (or anywhere above it, through the inherited attached property
/// <see cref="ToolBarProperties.LabelModeProperty"/>) to drive every item in the bar, and override
/// it on a single button where that button needs to differ. Switching the value at run time is
/// supported and is the usual way an application offers a "show button text" preference.
/// </remarks>
public enum LabelMode
{
	/// <summary>Show the icon only. The default; the text is still used for the tooltip and for
	/// the accessibility name, so an icon-only bar remains fully described.</summary>
	IconOnly,

	/// <summary>Show the text only, even when the item has an icon.</summary>
	TextOnly,

	/// <summary>Show both the icon and the text, arranged by
	/// <see cref="ToolBarProperties.LabelPositionProperty"/>.</summary>
	IconAndText,
}
