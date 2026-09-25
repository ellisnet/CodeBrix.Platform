using System.Collections.Generic;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using Reqnroll;
using SilverAssertions;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// Requirements on the harness's own measuring vocabulary that no rendered frame can pin down reliably: they feed the
/// measure a set of pixels directly. (A rendered glyph's anti-aliased edge only sometimes lands in the same colour
/// bucket as its core, which is why the first-pixel defect stayed latent on these heads.)
/// </summary>
[Binding]
public sealed class HarnessSelfTestSteps
{
	private readonly ModeColor _mode = new();

	/// <summary>Counts a run of pixels, in scan order.</summary>
	/// <param name="firstCount">How many pixels of the first colour come first.</param>
	/// <param name="first">The first colour.</param>
	/// <param name="thenCount">How many pixels of the second colour follow.</param>
	/// <param name="then">The second colour.</param>
	[Given("pixels of one colour shade: {int} of {string} first, then {int} of {string}")]
	public void GivenPixelsOfOneShade(int firstCount, string first, int thenCount, string then)
	{
		foreach (var (count, color) in new List<(int, string)> { (firstCount, first), (thenCount, then) })
		{
			var parsed = SKColor.Parse(color);
			for (var i = 0; i < count; i++)
			{
				_mode.Add(parsed);
			}
		}
	}

	/// <summary>Asserts the colour the harness's mode reports.</summary>
	/// <param name="expected">The exact colour.</param>
	[Then("the harness reports their colour as exactly {string}")]
	public void ThenTheColourIs(string expected)
	{
		_mode.TryGetMode(out var color, out _).Should().BeTrue();
		ColorMatch.Describe(color).Should().Be(ColorMatch.Describe(SKColor.Parse(expected)));
	}

	/// <summary>Asserts how many pixels fell into the reported colour's bucket.</summary>
	/// <param name="expected">The count.</param>
	[Then("the harness reports that colour for {int} of them")]
	public void ThenTheCountIs(int expected)
	{
		_mode.TryGetMode(out _, out var count).Should().BeTrue();
		count.Should().Be(expected);
	}
}
