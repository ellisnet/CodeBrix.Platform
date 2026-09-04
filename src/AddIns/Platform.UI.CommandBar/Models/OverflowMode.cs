namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// What a <see cref="ToolBar"/> does with the items that do not fit across its measured width
/// (its height, when the bar is vertical).
/// </summary>
public enum OverflowMode
{
	/// <summary>Nothing: the items keep their places and the bar is simply clipped by its
	/// parent.</summary>
	None,

	/// <summary>The items that do not fit continue on a further line.</summary>
	Wrap,

	/// <summary>The trailing items that do not fit move, in order, into a flyout behind a chevron
	/// button at the end of the bar, and come back when the space returns. The default.</summary>
	Chevron,
}
