#nullable enable

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// Identifies which edge of a <see cref="TriPaneView"/> the full-height side pane occupies.
/// </summary>
public enum TriPaneViewSidePanePlacement
{
	/// <summary>
	/// The side pane occupies the left edge and the stack (the upper and lower panes) fills the
	/// remaining space on the right. This is the default.
	/// </summary>
	Left,

	/// <summary>
	/// The side pane occupies the right edge and the stack (the upper and lower panes) fills the
	/// remaining space on the left.
	/// </summary>
	Right
}
