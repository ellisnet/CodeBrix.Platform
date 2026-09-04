#nullable enable

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// Identifies one of the two dividers of a <see cref="TriPaneView"/>.
/// </summary>
public enum TriPaneViewDividerKind
{
	/// <summary>
	/// The side divider: the full-height divider between the side pane and the stack.
	/// </summary>
	Side,

	/// <summary>
	/// The stack divider: the divider between the upper pane and the lower pane.
	/// </summary>
	Stack
}
