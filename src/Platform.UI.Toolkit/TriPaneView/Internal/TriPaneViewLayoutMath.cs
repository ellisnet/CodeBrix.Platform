#nullable enable

using System;

namespace CodeBrix.Platform.UI.Toolkit.Internal;

/// <summary>
/// The whole of the arithmetic behind <see cref="TriPaneView"/>: weight sanitizing and
/// normalization, minimum-length clamping, the drag-to-minimize snap rule, restore-grip visibility
/// and the portrait test. Every method here is a pure function of its arguments, so the control's
/// state model can be exercised with no window, no template and no visual tree.
/// </summary>
internal static class TriPaneViewLayoutMath
{
	/// <summary>The percent value a pair of weights always adds up to once normalized.</summary>
	internal const double TotalPercent = 100d;

	/// <summary>The default weight of the side pane, in the side-pane/stack pair.</summary>
	internal const double DefaultSidePanePercent = 33.3d;

	/// <summary>The default weight of the stack, in the side-pane/stack pair.</summary>
	internal const double DefaultStackPercent = 66.7d;

	/// <summary>The default weight of the upper pane, in the upper/lower pair.</summary>
	internal const double DefaultUpperPanePercent = 50d;

	/// <summary>The default weight of the lower pane, in the upper/lower pair.</summary>
	internal const double DefaultLowerPanePercent = 50d;

	/// <summary>
	/// The distance, in pixels, a pointer may travel while still counting as a tap rather than a
	/// drag. A tap on a restore grip restores the pane it belongs to.
	/// </summary>
	internal const double TapDistanceThreshold = 2d;

	/// <summary>
	/// Reduces a raw weight to a usable star weight. Negative values, <see cref="double.NaN"/> and
	/// the infinities are all treated as zero, which is the value that means "minimized".
	/// </summary>
	/// <param name="value">The raw weight, as set on the control.</param>
	/// <returns>The sanitized weight: either a positive finite number or zero.</returns>
	internal static double SanitizeWeight(double value) => double.IsFinite(value) && value > 0d ? value : 0d;

	/// <summary>
	/// Reduces a raw length in pixels to a usable one. Negative values, <see cref="double.NaN"/>
	/// and the infinities all become zero.
	/// </summary>
	/// <param name="value">The raw length, as set on the control or measured from the tree.</param>
	/// <returns>The sanitized length: either a positive finite number or zero.</returns>
	internal static double SanitizeLength(double value) => double.IsFinite(value) && value > 0d ? value : 0d;

	/// <summary>
	/// Normalizes a pair of raw weights to the effective star weights the layout uses: two numbers
	/// that sum to <see cref="TotalPercent"/> and keep the ratio of the sanitized inputs. So
	/// <c>60, 60</c> becomes <c>50, 50</c> and <c>40, 120</c> becomes <c>25, 75</c>. A pair in
	/// which both weights sanitize to zero is laid out evenly - <c>50, 50</c> - and therefore has
	/// neither member minimized; a pair with exactly one zero leaves that member minimized.
	/// </summary>
	/// <param name="first">The raw weight of the first member of the pair.</param>
	/// <param name="second">The raw weight of the second member of the pair.</param>
	/// <returns>The effective weights of the two members, summing to <see cref="TotalPercent"/>.</returns>
	internal static (double First, double Second) NormalizePair(double first, double second)
	{
		var sanitizedFirst = SanitizeWeight(first);
		var sanitizedSecond = SanitizeWeight(second);
		var total = sanitizedFirst + sanitizedSecond;

		if (total <= 0d)
		{
			return (TotalPercent / 2d, TotalPercent / 2d);
		}

		var firstPercent = sanitizedFirst / total * TotalPercent;

		return (firstPercent, TotalPercent - firstPercent);
	}

	/// <summary>
	/// Tests whether an effective weight - one produced by <see cref="NormalizePair"/> - means the
	/// pane it belongs to is minimized.
	/// </summary>
	/// <param name="effectiveWeight">The effective weight to test.</param>
	/// <returns><see langword="true"/> when the weight is zero.</returns>
	internal static bool IsMinimized(double effectiveWeight) => effectiveWeight <= 0d;

	/// <summary>
	/// Resolves the two pane lengths a divider drag asks for, honoring the minimum lengths and the
	/// drag-to-minimize rule. The two panes share a fixed amount of space - the sum of the lengths
	/// they had when the drag started - so a positive <paramref name="delta"/> grows the first pane
	/// by exactly as much as it shrinks the second.
	/// </summary>
	/// <param name="firstStartLength">
	/// The length, in pixels, of the pane before the divider when the drag started.
	/// </param>
	/// <param name="secondStartLength">
	/// The length, in pixels, of the pane after the divider when the drag started.
	/// </param>
	/// <param name="delta">
	/// The total distance, in pixels, the divider has moved since the drag started. Positive values
	/// move it away from the first pane, growing it.
	/// </param>
	/// <param name="firstMinLength">The minimum length, in pixels, of the first pane.</param>
	/// <param name="secondMinLength">The minimum length, in pixels, of the second pane.</param>
	/// <param name="isDragToMinimizeEnabled">
	/// <see langword="true"/> to snap a pane the drag pushes below its minimum length all the way to
	/// zero - minimizing it - and to reopen it at its minimum length when the drag comes back past
	/// that minimum; <see langword="false"/> to stop the drag dead at the minimum length instead.
	/// </param>
	/// <returns>
	/// The resolved lengths of the two panes, in pixels. They always sum to the same total the two
	/// start lengths did.
	/// </returns>
	/// <remarks>
	/// A pane whose minimum length is zero can always be dragged to zero, with or without
	/// <paramref name="isDragToMinimizeEnabled"/>; that is the only way a pane reaches zero when
	/// drag-to-minimize is off. When the shared space is too small to honor both minimum lengths at
	/// once and drag-to-minimize is off, the drag is refused and the start lengths are returned
	/// unchanged.
	/// </remarks>
	internal static (double First, double Second) ResolveDragLengths(
		double firstStartLength,
		double secondStartLength,
		double delta,
		double firstMinLength,
		double secondMinLength,
		bool isDragToMinimizeEnabled)
	{
		var firstStart = SanitizeLength(firstStartLength);
		var secondStart = SanitizeLength(secondStartLength);
		var total = firstStart + secondStart;

		if (total <= 0d)
		{
			return (firstStart, secondStart);
		}

		var firstMin = Math.Min(SanitizeLength(firstMinLength), total);
		var secondMin = Math.Min(SanitizeLength(secondMinLength), total);
		var moved = double.IsFinite(delta) ? delta : 0d;
		var first = Math.Clamp(firstStart + moved, 0d, total);

		if (isDragToMinimizeEnabled)
		{
			if (first > 0d && first < firstMin)
			{
				first = 0d;
			}

			var second = total - first;

			if (second > 0d && second < secondMin)
			{
				first = total;
			}

			return (first, total - first);
		}

		if (firstMin + secondMin > total)
		{
			return (firstStart, secondStart);
		}

		first = Math.Clamp(first, firstMin, total - secondMin);

		return (first, total - first);
	}

	/// <summary>
	/// Converts a pair of resolved pane lengths in pixels into the pair of percent weights the
	/// control stores, normalized to sum to <see cref="TotalPercent"/>.
	/// </summary>
	/// <param name="firstLength">The length, in pixels, of the first pane.</param>
	/// <param name="secondLength">The length, in pixels, of the second pane.</param>
	/// <returns>
	/// The percent weights of the two panes, or <see langword="null"/> when the two lengths carry no
	/// usable ratio - both zero - and the caller should leave the stored weights alone.
	/// </returns>
	internal static (double First, double Second)? LengthsToPercent(double firstLength, double secondLength)
	{
		var first = SanitizeLength(firstLength);
		var second = SanitizeLength(secondLength);
		var total = first + second;

		if (total <= 0d)
		{
			return null;
		}

		var firstPercent = first / total * TotalPercent;

		return (firstPercent, TotalPercent - firstPercent);
	}

	/// <summary>
	/// Decides whether a minimized region should be given a restore grip.
	/// </summary>
	/// <param name="mode">The control's restore-grip mode.</param>
	/// <param name="isMinimized">Whether the region is minimized at all.</param>
	/// <param name="cause">Why the region is minimized.</param>
	/// <returns><see langword="true"/> when a restore grip should be shown.</returns>
	internal static bool IsRestoreGripVisible(TriPaneViewRestoreGripMode mode, bool isMinimized, TriPaneViewMinimizeCause cause)
		=> isMinimized && mode switch
		{
			TriPaneViewRestoreGripMode.Always => true,
			TriPaneViewRestoreGripMode.Never => false,
			_ => cause == TriPaneViewMinimizeCause.Drag
		};

	/// <summary>
	/// Tests whether a control of the given size counts as portrait. Desktop heads expose no
	/// display orientation, so the control's own shape is the rule.
	/// </summary>
	/// <param name="width">The control's actual width.</param>
	/// <param name="height">The control's actual height.</param>
	/// <returns><see langword="true"/> when the control is taller than it is wide.</returns>
	internal static bool IsPortrait(double width, double height)
		=> double.IsFinite(width) && double.IsFinite(height) && height > width;

	/// <summary>
	/// Resolves a pane's horizontal scroll mode against the current shape of the control.
	/// </summary>
	/// <param name="mode">The pane's horizontal scroll mode.</param>
	/// <param name="isPortrait">Whether the control is currently portrait.</param>
	/// <returns><see langword="true"/> when the pane should scroll horizontally.</returns>
	internal static bool ShouldEnableHorizontalScrolling(TriPaneViewHorizontalScrollMode mode, bool isPortrait)
		=> mode switch
		{
			TriPaneViewHorizontalScrollMode.Enabled => true,
			TriPaneViewHorizontalScrollMode.AutoOnPortrait => isPortrait,
			_ => false
		};

	/// <summary>
	/// Resolves the length of a divider track: the divider's thickness while the divider is shown,
	/// and zero while it is hidden.
	/// </summary>
	/// <param name="isVisible">Whether the divider is shown.</param>
	/// <param name="thickness">The configured divider thickness.</param>
	/// <returns>The track length in pixels.</returns>
	internal static double ResolveDividerTrackLength(bool isVisible, double thickness)
		=> isVisible ? SanitizeLength(thickness) : 0d;

	/// <summary>
	/// Tests whether a completed pointer interaction counts as a tap rather than a drag. A tap on a
	/// restore grip restores the pane the grip belongs to.
	/// </summary>
	/// <param name="totalDelta">The total distance, in pixels, the pointer travelled.</param>
	/// <returns><see langword="true"/> when the pointer barely moved.</returns>
	internal static bool IsTap(double totalDelta)
		=> !double.IsNaN(totalDelta) && Math.Abs(totalDelta) < TapDistanceThreshold;

	/// <summary>
	/// Picks the weight to restore a region to: its snapshot when there is a usable one, and the
	/// supplied default otherwise.
	/// </summary>
	/// <param name="snapshot">The weight the region had when it was minimized, if it was recorded.</param>
	/// <param name="fallback">The weight to use when there is no usable snapshot.</param>
	/// <returns>A positive weight.</returns>
	internal static double ResolveRestoreWeight(double? snapshot, double fallback)
	{
		if (snapshot.HasValue)
		{
			var sanitized = SanitizeWeight(snapshot.Value);

			if (sanitized > 0d)
			{
				return sanitized;
			}
		}

		return fallback;
	}
}
