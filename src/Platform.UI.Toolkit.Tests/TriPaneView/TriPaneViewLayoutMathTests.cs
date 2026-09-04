using CodeBrix.Platform.UI.Toolkit.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests;

[TestClass]
public class TriPaneViewLayoutMathTests
{
	private const double Tolerance = 1e-9;

	[TestMethod]
	public void SanitizeWeight_positive_value_is_kept() =>
		TriPaneViewLayoutMath.SanitizeWeight(42d).Should().Be(42d);

	[TestMethod]
	public void SanitizeWeight_negative_value_becomes_zero() =>
		TriPaneViewLayoutMath.SanitizeWeight(-5d).Should().Be(0d);

	[TestMethod]
	public void SanitizeWeight_nan_becomes_zero() =>
		TriPaneViewLayoutMath.SanitizeWeight(double.NaN).Should().Be(0d);

	[TestMethod]
	public void SanitizeWeight_infinity_becomes_zero() =>
		TriPaneViewLayoutMath.SanitizeWeight(double.PositiveInfinity).Should().Be(0d);

	[TestMethod]
	public void SanitizeLength_negative_value_becomes_zero() =>
		TriPaneViewLayoutMath.SanitizeLength(-1d).Should().Be(0d);

	[TestMethod]
	public void NormalizePair_equal_weights_split_evenly()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(60d, 60d);

		//Assert
		first.Should().BeApproximately(50d, Tolerance);
		second.Should().BeApproximately(50d, Tolerance);
	}

	[TestMethod]
	public void NormalizePair_unequal_weights_keep_their_ratio()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(40d, 120d);

		//Assert
		first.Should().BeApproximately(25d, Tolerance);
		second.Should().BeApproximately(75d, Tolerance);
	}

	[TestMethod]
	public void NormalizePair_always_sums_to_one_hundred()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(33.3d, 66.7d);

		//Assert
		(first + second).Should().BeApproximately(100d, Tolerance);
	}

	[TestMethod]
	public void NormalizePair_negative_weight_is_treated_as_zero()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(-10d, 50d);

		//Assert
		first.Should().Be(0d);
		second.Should().BeApproximately(100d, Tolerance);
	}

	[TestMethod]
	public void NormalizePair_nan_weight_is_treated_as_zero()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(50d, double.NaN);

		//Assert
		first.Should().BeApproximately(100d, Tolerance);
		second.Should().Be(0d);
	}

	[TestMethod]
	public void NormalizePair_both_weights_zero_splits_evenly()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.NormalizePair(0d, 0d);

		//Assert
		first.Should().Be(50d);
		second.Should().Be(50d);
	}

	[TestMethod]
	public void IsMinimized_zero_weight_is_minimized() =>
		TriPaneViewLayoutMath.IsMinimized(0d).Should().BeTrue();

	[TestMethod]
	public void IsMinimized_positive_weight_is_not_minimized() =>
		TriPaneViewLayoutMath.IsMinimized(0.5d).Should().BeFalse();

	[TestMethod]
	public void ResolveDragLengths_moves_the_boundary_by_the_delta()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, 50d, 0d, 0d, false);

		//Assert
		first.Should().Be(150d);
		second.Should().Be(250d);
	}

	[TestMethod]
	public void ResolveDragLengths_negative_delta_moves_the_boundary_back()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, -40d, 0d, 0d, false);

		//Assert
		first.Should().Be(60d);
		second.Should().Be(340d);
	}

	[TestMethod]
	public void ResolveDragLengths_clamps_to_the_available_space()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, 5000d, 0d, 0d, false);

		//Assert
		first.Should().Be(400d);
		second.Should().Be(0d);
	}

	[TestMethod]
	public void ResolveDragLengths_reaches_zero_when_the_minimum_length_is_zero()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, -500d, 0d, 0d, false);

		//Assert
		first.Should().Be(0d);
		second.Should().Be(400d);
	}

	[TestMethod]
	public void ResolveDragLengths_stops_at_the_first_minimum_when_drag_to_minimize_is_off()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, -90d, 80d, 0d, false);

		//Assert
		first.Should().Be(80d);
		second.Should().Be(320d);
	}

	[TestMethod]
	public void ResolveDragLengths_stops_at_the_second_minimum_when_drag_to_minimize_is_off()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, 250d, 0d, 120d, false);

		//Assert
		first.Should().Be(280d);
		second.Should().Be(120d);
	}

	[TestMethod]
	public void ResolveDragLengths_refuses_the_drag_when_both_minimums_do_not_fit()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(40d, 60d, -30d, 80d, 80d, false);

		//Assert
		first.Should().Be(40d);
		second.Should().Be(60d);
	}

	[TestMethod]
	public void ResolveDragLengths_snaps_below_the_minimum_to_zero_when_drag_to_minimize_is_on()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, -60d, 80d, 0d, true);

		//Assert
		first.Should().Be(0d);
		second.Should().Be(400d);
	}

	[TestMethod]
	public void ResolveDragLengths_reopens_at_the_minimum_when_drag_to_minimize_is_on()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(0d, 400d, 80d, 80d, 0d, true);

		//Assert
		first.Should().Be(80d);
		second.Should().Be(320d);
	}

	[TestMethod]
	public void ResolveDragLengths_never_leaves_a_pane_between_zero_and_its_minimum()
	{
		//Act
		var (first, _) = TriPaneViewLayoutMath.ResolveDragLengths(0d, 400d, 79d, 80d, 0d, true);

		//Assert
		first.Should().Be(0d);
	}

	[TestMethod]
	public void ResolveDragLengths_snaps_the_second_pane_to_zero_when_drag_to_minimize_is_on()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, 250d, 0d, 120d, true);

		//Assert
		first.Should().Be(400d);
		second.Should().Be(0d);
	}

	[TestMethod]
	public void ResolveDragLengths_returns_the_start_lengths_when_there_is_no_space()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(0d, 0d, 40d, 0d, 0d, false);

		//Assert
		first.Should().Be(0d);
		second.Should().Be(0d);
	}

	[TestMethod]
	public void ResolveDragLengths_ignores_a_non_finite_delta()
	{
		//Act
		var (first, second) = TriPaneViewLayoutMath.ResolveDragLengths(100d, 300d, double.NaN, 0d, 0d, false);

		//Assert
		first.Should().Be(100d);
		second.Should().Be(300d);
	}

	[TestMethod]
	public void LengthsToPercent_normalizes_to_one_hundred()
	{
		//Act
		var percent = TriPaneViewLayoutMath.LengthsToPercent(150d, 450d);

		//Assert
		percent.Should().NotBeNull();
		percent!.Value.First.Should().BeApproximately(25d, Tolerance);
		percent.Value.Second.Should().BeApproximately(75d, Tolerance);
	}

	[TestMethod]
	public void LengthsToPercent_returns_null_when_both_lengths_are_zero() =>
		TriPaneViewLayoutMath.LengthsToPercent(0d, 0d).Should().BeNull();

	[TestMethod]
	public void IsRestoreGripVisible_auto_shows_a_grip_for_a_drag_cause() =>
		TriPaneViewLayoutMath
			.IsRestoreGripVisible(TriPaneViewRestoreGripMode.Auto, true, TriPaneViewMinimizeCause.Drag)
			.Should().BeTrue();

	[TestMethod]
	public void IsRestoreGripVisible_auto_hides_the_grip_for_a_code_cause() =>
		TriPaneViewLayoutMath
			.IsRestoreGripVisible(TriPaneViewRestoreGripMode.Auto, true, TriPaneViewMinimizeCause.Code)
			.Should().BeFalse();

	[TestMethod]
	public void IsRestoreGripVisible_always_shows_a_grip_for_a_code_cause() =>
		TriPaneViewLayoutMath
			.IsRestoreGripVisible(TriPaneViewRestoreGripMode.Always, true, TriPaneViewMinimizeCause.Code)
			.Should().BeTrue();

	[TestMethod]
	public void IsRestoreGripVisible_never_hides_the_grip_for_a_drag_cause() =>
		TriPaneViewLayoutMath
			.IsRestoreGripVisible(TriPaneViewRestoreGripMode.Never, true, TriPaneViewMinimizeCause.Drag)
			.Should().BeFalse();

	[TestMethod]
	public void IsRestoreGripVisible_is_false_when_nothing_is_minimized() =>
		TriPaneViewLayoutMath
			.IsRestoreGripVisible(TriPaneViewRestoreGripMode.Always, false, TriPaneViewMinimizeCause.Drag)
			.Should().BeFalse();

	[TestMethod]
	public void IsPortrait_taller_than_wide_is_portrait() =>
		TriPaneViewLayoutMath.IsPortrait(420d, 900d).Should().BeTrue();

	[TestMethod]
	public void IsPortrait_wider_than_tall_is_not_portrait() =>
		TriPaneViewLayoutMath.IsPortrait(900d, 420d).Should().BeFalse();

	[TestMethod]
	public void IsPortrait_square_is_not_portrait() =>
		TriPaneViewLayoutMath.IsPortrait(500d, 500d).Should().BeFalse();

	[TestMethod]
	public void ShouldEnableHorizontalScrolling_disabled_is_always_off() =>
		TriPaneViewLayoutMath
			.ShouldEnableHorizontalScrolling(TriPaneViewHorizontalScrollMode.Disabled, true)
			.Should().BeFalse();

	[TestMethod]
	public void ShouldEnableHorizontalScrolling_enabled_is_always_on() =>
		TriPaneViewLayoutMath
			.ShouldEnableHorizontalScrolling(TriPaneViewHorizontalScrollMode.Enabled, false)
			.Should().BeTrue();

	[TestMethod]
	public void ShouldEnableHorizontalScrolling_auto_on_portrait_follows_the_shape()
	{
		//Assert
		TriPaneViewLayoutMath
			.ShouldEnableHorizontalScrolling(TriPaneViewHorizontalScrollMode.AutoOnPortrait, true)
			.Should().BeTrue();
		TriPaneViewLayoutMath
			.ShouldEnableHorizontalScrolling(TriPaneViewHorizontalScrollMode.AutoOnPortrait, false)
			.Should().BeFalse();
	}

	[TestMethod]
	public void ResolveDividerTrackLength_visible_divider_takes_its_thickness() =>
		TriPaneViewLayoutMath.ResolveDividerTrackLength(true, 6d).Should().Be(6d);

	[TestMethod]
	public void ResolveDividerTrackLength_hidden_divider_takes_no_space() =>
		TriPaneViewLayoutMath.ResolveDividerTrackLength(false, 6d).Should().Be(0d);

	[TestMethod]
	public void ResolveDividerTrackLength_negative_thickness_takes_no_space() =>
		TriPaneViewLayoutMath.ResolveDividerTrackLength(true, -6d).Should().Be(0d);

	[TestMethod]
	public void IsTap_a_barely_moved_pointer_is_a_tap() =>
		TriPaneViewLayoutMath.IsTap(1d).Should().BeTrue();

	[TestMethod]
	public void IsTap_a_moved_pointer_is_not_a_tap() =>
		TriPaneViewLayoutMath.IsTap(-40d).Should().BeFalse();

	[TestMethod]
	public void IsTap_a_non_finite_travel_is_not_a_tap()
	{
		//Assert
		TriPaneViewLayoutMath.IsTap(double.NaN).Should().BeFalse();
		TriPaneViewLayoutMath.IsTap(double.PositiveInfinity).Should().BeFalse();
		TriPaneViewLayoutMath.IsTap(double.NegativeInfinity).Should().BeFalse();
	}

	[TestMethod]
	public void ResolveRestoreWeight_uses_the_snapshot_when_there_is_one() =>
		TriPaneViewLayoutMath.ResolveRestoreWeight(27d, 33.3d).Should().Be(27d);

	[TestMethod]
	public void ResolveRestoreWeight_falls_back_when_there_is_no_snapshot() =>
		TriPaneViewLayoutMath.ResolveRestoreWeight(null, 33.3d).Should().Be(33.3d);

	[TestMethod]
	public void ResolveRestoreWeight_falls_back_when_the_snapshot_is_zero() =>
		TriPaneViewLayoutMath.ResolveRestoreWeight(0d, 33.3d).Should().Be(33.3d);
}
