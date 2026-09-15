using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// A bar that is measured with no width limit at all, which is what a horizontal StackPanel, an
/// unbounded Grid column and a content host that passes an infinity on all do to it.
/// </summary>
/// <remarks>
/// The partition is decided at measure time so that no panel gains or loses a child while it is
/// measuring; an unbounded measure has nothing to decide it from, and the bar used to keep every
/// item, grow past whatever clipped it and show no chevron. The arrange pass knows the width the
/// bar really got, so these tests state what it does with it - and, just as importantly, that it
/// stops doing it once the bar has settled.
/// </remarks>
public class ToolBarUnboundedWidthTests
{
	/// <summary>Prepares the process the way an application head would have.</summary>
	public ToolBarUnboundedWidthTests() => TestHost.EnsureReady();

	[Fact]
	public void A_bar_measured_without_a_limit_takes_its_overflow_from_the_width_it_is_arranged_at()
	{
		//Arrange
		//Five 50-pixel items and the gaps between them need far more than the 200 the bar is
		//arranged at, so a bar that read the arranged width has to show a chevron.
		var bar = CreateBar(5);

		//Act
		Unbounded(bar, arrangedWidth: 200);

		//Assert
		bar.HasOverflowItems.Should().BeTrue();
		InFlyout(bar).Should().BeGreaterThan(0);
	}

	[Fact]
	public void A_bar_measured_without_a_limit_and_arranged_wide_keeps_every_item()
	{
		//Arrange
		var bar = CreateBar(5);

		//Act
		Unbounded(bar, arrangedWidth: 900);

		//Assert
		//The recompute must not invent an overflow: an unbounded bar with room for everything is
		//exactly the case the old behaviour got right.
		bar.HasOverflowItems.Should().BeFalse();
		InFlyout(bar).Should().Be(0);
	}

	[Fact]
	public void The_partition_of_an_unbounded_bar_does_not_move_once_it_has_settled()
	{
		//Arrange
		//The width the bar's own comment records as the oscillating case: one pixel too narrow for
		//the last item, so the partition sits right on the boundary the chevron is reserved at.
		var bar = CreateBar(5);
		Unbounded(bar, arrangedWidth: 265);
		var settled = InFlyout(bar);
		var settledOverflow = bar.HasOverflowItems;

		//Act
		//Four further layout passes, each one measuring without a limit exactly as the container
		//that caused this does. A bar that traded the chevron back and forth would change here.
		for (var pass = 0; pass < 4; pass++)
		{
			Unbounded(bar, arrangedWidth: 265);
			InFlyout(bar).Should().Be(settled);
			bar.HasOverflowItems.Should().Be(settledOverflow);
		}

		//Assert
		settledOverflow.Should().BeTrue();
		settled.Should().BeGreaterThan(0);
	}

	[Fact]
	public void An_unbounded_bar_follows_a_later_change_of_arranged_width()
	{
		//Arrange
		var bar = CreateBar(5);
		Unbounded(bar, arrangedWidth: 200);
		bar.HasOverflowItems.Should().BeTrue();

		//Act
		//The window is widened. The offer is still unbounded, so only the arranged width says so.
		Unbounded(bar, arrangedWidth: 900);

		//Assert
		bar.HasOverflowItems.Should().BeFalse();
		InFlyout(bar).Should().Be(0);
	}

	[Fact]
	public void A_bar_measured_with_a_real_limit_is_unaffected()
	{
		//Arrange
		var bar = CreateBar(5);

		//Act
		//The ordinary case, stated here so that a change to the unbounded path cannot quietly
		//change the bounded one: the limit the bar is MEASURED with is what decides the partition.
		bar.Measure(new Size(900, 100));
		bar.Arrange(new Rect(0, 0, 200, 100));

		//Assert
		bar.HasOverflowItems.Should().BeFalse();
		InFlyout(bar).Should().Be(0);
	}

	/// <summary>Builds a chevron bar of fixed-size items.</summary>
	/// <param name="items">How many items to put in it.</param>
	/// <returns>The bar.</returns>
	private static ToolBar CreateBar(int items)
	{
		var bar = new ToolBar { OverflowMode = OverflowMode.Chevron };
		for (var i = 0; i < items; i++)
		{
			bar.Items.Add(new Border { Width = 50, Height = 20 });
		}

		return bar;
	}

	/// <summary>
	/// Lays the bar out the way an unbounded container does: measured with no width limit, then
	/// arranged at the width it really got. The second pass is the one a real layout manager runs
	/// after the arrange pass invalidates the measure.
	/// </summary>
	/// <param name="bar">The bar to lay out.</param>
	/// <param name="arrangedWidth">The width the container arranges it at.</param>
	private static void Unbounded(ToolBar bar, double arrangedWidth)
	{
		for (var pass = 0; pass < 2; pass++)
		{
			bar.Measure(new Size(double.PositiveInfinity, 100));
			bar.Arrange(new Rect(0, 0, arrangedWidth, 100));
		}
	}

	/// <summary>How many of the bar's items are currently in the overflow flyout.</summary>
	/// <param name="bar">The bar to look at.</param>
	/// <returns>The count.</returns>
	private static int InFlyout(ToolBar bar) => bar.OverflowHost.Children.Count;
}
