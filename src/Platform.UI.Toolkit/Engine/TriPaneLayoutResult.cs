#nullable enable

namespace CodeBrix.Platform.UI.Toolkit.Engine;

/// <summary>
/// What one TriPaneView state pass computed (WPE1 C12): the four effective weights (zero = minimized), the side pane's
/// placement, and which dividers show, which act as restore grips and which way their chevrons point.
/// </summary>
/// <param name="SideWeight">The side pane's effective weight.</param>
/// <param name="StackWeight">The stack's effective weight.</param>
/// <param name="UpperWeight">The upper pane's effective weight.</param>
/// <param name="LowerWeight">The lower pane's effective weight.</param>
/// <param name="IsPlacedLeft">Whether the side pane is on the left.</param>
/// <param name="IsSideDividerVisible">Whether the side divider is shown.</param>
/// <param name="IsSideGripVisible">Whether the side divider acts as a restore grip.</param>
/// <param name="IsSideGripTowardStart">Whether the side grip's chevron points left.</param>
/// <param name="IsStackDividerVisible">Whether the stack divider is shown.</param>
/// <param name="IsStackGripVisible">Whether the stack divider acts as a restore grip.</param>
/// <param name="IsStackGripTowardStart">Whether the stack grip's chevron points up.</param>
internal readonly record struct TriPaneLayoutResult(
	double SideWeight,
	double StackWeight,
	double UpperWeight,
	double LowerWeight,
	bool IsPlacedLeft,
	bool IsSideDividerVisible,
	bool IsSideGripVisible,
	bool IsSideGripTowardStart,
	bool IsStackDividerVisible,
	bool IsStackGripVisible,
	bool IsStackGripTowardStart);
