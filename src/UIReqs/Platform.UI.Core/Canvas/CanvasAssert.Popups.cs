using System;
using System.Globalization;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using SilverAssertions;

namespace CodeBrix.Platform.UI.Core.UIReqs.Canvas;

/// <summary>
/// The vocabulary the Popups group's requirements need. A modal dialog dims what is behind it,
/// and "dimmed" is not a colour a feature file can name: it is the same picture, darker. That
/// is a statement about a region's average brightness in two frames rather than about any one
/// colour, so it gets a primitive of its own.
/// </summary>
public static partial class CanvasAssert
{
	/// <summary>
	/// How much darker, on a 0-255 scale, a region must become before it counts as dimmed.
	/// The smoke layer a dialog paints is far darker than this; the margin keeps a repaint
	/// that merely shifts a shade from passing for one.
	/// </summary>
	public const double DimmedDrop = 20.0;

	/// <summary>
	/// The average brightness of a region, on a 0-255 scale, judged as a person sees it: a
	/// semi-transparent pixel is composited over the panel background first.
	/// </summary>
	/// <param name="region">The region to measure.</param>
	/// <returns>The average brightness.</returns>
	public static double MeanBrightness(Region region)
	{
		ArgumentNullException.ThrowIfNull(region);

		var background = Background;
		var total = 0.0;
		foreach (var pixel in region.Pixels())
		{
			var composited = ColorMatch.Composite(pixel, background);
			total += (0.299 * composited.Red) + (0.587 * composited.Green) + (0.114 * composited.Blue);
		}

		return total / region.PixelCount;
	}

	/// <summary>Asserts that a region became darker than it was in another frame.</summary>
	/// <param name="region">The region as it looks now.</param>
	/// <param name="other">The same region in the earlier frame.</param>
	/// <param name="minimumDrop">How much darker it must have become; the default is <see cref="DimmedDrop"/>.</param>
	public static void IsDimmedComparedTo(this Region region, Region other, double minimumDrop = DimmedDrop)
	{
		ArgumentNullException.ThrowIfNull(region);
		ArgumentNullException.ThrowIfNull(other);

		var before = MeanBrightness(other);
		var now = MeanBrightness(region);
		var drop = before - now;

		if (drop < minimumDrop)
		{
			drop.Should().BeGreaterThanOrEqualTo(minimumDrop, "{0}", Explain(region, string.Create(
				CultureInfo.InvariantCulture,
				$"{region.Description} must be dimmed: its average brightness was {before:0.0} and is now {now:0.0}")));
		}
	}

	/// <summary>The rectangle of the panel a scenario looks at to see whether it was dimmed.</summary>
	/// <remarks>
	/// It is a square in the panel's top-left corner, sized from the panel rather than from a
	/// constant, so the same requirement holds on both the landscape and the portrait panel.
	/// A dialog is centred, so this square is never underneath one.
	/// </remarks>
	/// <returns>The rectangle, in device pixels.</returns>
	public static DeviceRect PanelCorner()
	{
		var (width, height) = TestTargetFixture.PanelSize;
		var side = Math.Min(width, height) / 8;
		var inset = side / 4;
		return new DeviceRect(inset, inset, side, side);
	}
}
