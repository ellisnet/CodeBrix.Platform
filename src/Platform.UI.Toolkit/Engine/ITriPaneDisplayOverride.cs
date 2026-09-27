#nullable enable

using CodeBrix.Platform.UI.Toolkit.Internal;

namespace CodeBrix.Platform.UI.Toolkit.Engine;

/// <summary>
/// The four weights of a TriPaneView as one value: SidePanePercent, StackPercent, UpperPanePercent, LowerPanePercent.
/// </summary>
/// <param name="Side">The side pane's width weight.</param>
/// <param name="Stack">The stack's width weight.</param>
/// <param name="Upper">The upper pane's height weight.</param>
/// <param name="Lower">The lower pane's height weight.</param>
internal readonly record struct TriPaneDisplayWeights(double Side, double Stack, double Upper, double Lower);

/// <summary>
/// A platform's override of the pane layout a TriPaneView DISPLAYS (WPE1-13): an adaptive form (Android's Compact and
/// Medium window size classes, for one) shows fewer panes than the application's weights describe, WITHOUT writing the
/// application's percent or IsMinimized properties.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): none - the displayed layout is the application's weights, as before.
/// <para>
/// Set per control through the internal <c>TriPaneView.DisplayOverride</c> (or <see cref="TriPaneLayoutState.DisplayOverride"/>
/// for a host without the control); call <c>TriPaneView.RefreshDisplayOverride()</c> whenever the answer of
/// <see cref="GetDisplayWeights"/> changes (a new window size class). On every state pass the engine publishes the
/// minimized flags of the APPLICATION's weights (unchanged by the override) and lays out the weights this override
/// returns. A region the override hides (display weight zero while the application's weight is open) is shown minimized
/// with a restore grip (unless RestoreGripMode is Never); a tap on that grip calls <see cref="RestoreRequested"/> instead
/// of restoring anything itself, and a drag on that grip does not move it. A divider whose axis has no hidden region
/// drags as usual - the drag writes the application's weights, which is what the user asked for.
/// </para>
/// </remarks>
internal interface ITriPaneDisplayOverride
{
	/// <summary>
	/// Returns the weights to display for the application's weights. Return <paramref name="applicationWeights"/> itself
	/// to display them unchanged; a region set to zero is hidden. Called on every state pass; keep it cheap.
	/// </summary>
	/// <param name="applicationWeights">The application's weights (the four percent properties, as set).</param>
	/// <returns>The weights to lay out.</returns>
	TriPaneDisplayWeights GetDisplayWeights(TriPaneDisplayWeights applicationWeights);

	/// <summary>
	/// The user tapped the restore grip of a region this override hides. Show it (change what
	/// <see cref="GetDisplayWeights"/> returns - e.g. switch the Compact form to that pane) and return
	/// <see langword="true"/>; return <see langword="false"/> to leave the layout as it is. The engine runs a state pass
	/// afterwards either way.
	/// </summary>
	/// <param name="region">The hidden region behind the grip.</param>
	/// <returns>Whether the display changed.</returns>
	bool RestoreRequested(TriPaneViewRegion region);
}
