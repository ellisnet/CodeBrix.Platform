#nullable enable

namespace CodeBrix.Platform.UI.Toolkit.Internal;

/// <summary>
/// Records why a region of a <see cref="TriPaneView"/> is currently minimized. The cause decides
/// whether a restore grip is offered while <see cref="TriPaneViewRestoreGripMode.Auto"/> is in
/// effect.
/// </summary>
internal enum TriPaneViewMinimizeCause
{
	/// <summary>
	/// The region was minimized by the user dragging a divider, or by a percent property being set
	/// to zero. Both are treated as a user-visible act, so a restore grip is offered under
	/// <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </summary>
	Drag,

	/// <summary>
	/// The region was minimized from code, by one of the <c>Minimize</c> methods or by setting one
	/// of the <c>IsMinimized</c> properties. No restore grip is offered under
	/// <see cref="TriPaneViewRestoreGripMode.Auto"/>; the application is expected to restore it.
	/// </summary>
	Code
}
