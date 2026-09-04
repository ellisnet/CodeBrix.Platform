#nullable enable

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// Controls horizontal scrolling for a single pane of a <see cref="TriPaneView"/>. Every pane
/// scrolls vertically by default; horizontal scrolling is opt-in because content that is allowed
/// to grow sideways cannot stretch to fill the pane.
/// </summary>
public enum TriPaneViewHorizontalScrollMode
{
	/// <summary>
	/// Horizontal scrolling is off: the pane's content is measured to the pane width and stretches
	/// to fill it. This is the default.
	/// </summary>
	Disabled,

	/// <summary>
	/// Horizontal scrolling is on: the pane's content is measured unbounded horizontally and a
	/// horizontal scroll bar appears when the content is wider than the pane.
	/// </summary>
	Enabled,

	/// <summary>
	/// Horizontal scrolling follows the shape of the <see cref="TriPaneView"/>: it behaves like
	/// <see cref="Enabled"/> while the control is taller than it is wide (portrait), and like
	/// <see cref="Disabled"/> otherwise (landscape). The decision is re-evaluated every time the
	/// control's size changes, so it also tracks a window being resized.
	/// </summary>
	AutoOnPortrait
}
