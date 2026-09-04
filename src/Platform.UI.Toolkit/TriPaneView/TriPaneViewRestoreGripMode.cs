#nullable enable

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// Controls when a <see cref="TriPaneView"/> leaves a divider visible at the edge of a minimized
/// pane so that the user can restore it. That divider is called the restore grip: clicking it
/// restores the pane, dragging it reopens the pane live.
/// </summary>
public enum TriPaneViewRestoreGripMode
{
	/// <summary>
	/// A restore grip is shown only for a pane the user minimized by dragging a divider, or for a
	/// pane minimized because a percent property was set to zero. A pane minimized from code - by
	/// calling one of the <c>Minimize</c> methods or by setting one of the <c>IsMinimized</c>
	/// properties to <see langword="true"/> - gets no grip. This is the default.
	/// </summary>
	Auto,

	/// <summary>
	/// A restore grip is shown for every minimized pane, no matter how it was minimized.
	/// </summary>
	Always,

	/// <summary>
	/// No restore grip is ever shown. The divider next to a minimized pane is hidden as well and
	/// the sibling pane takes all of the space, so a minimized pane can only be restored from code.
	/// </summary>
	Never
}
