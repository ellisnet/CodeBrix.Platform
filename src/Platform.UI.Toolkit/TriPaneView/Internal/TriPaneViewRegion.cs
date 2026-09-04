#nullable enable

namespace CodeBrix.Platform.UI.Toolkit.Internal;

/// <summary>
/// Names the four regions of a <see cref="TriPaneView"/> whose minimized state is tracked
/// separately: the three panes and the stack that holds two of them.
/// </summary>
internal enum TriPaneViewRegion
{
	/// <summary>The full-height side pane.</summary>
	Side,

	/// <summary>The column holding the upper and lower panes.</summary>
	Stack,

	/// <summary>The upper pane of the stack.</summary>
	Upper,

	/// <summary>The lower pane of the stack.</summary>
	Lower
}
