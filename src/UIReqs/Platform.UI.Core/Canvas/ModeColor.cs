using System.Collections.Generic;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Core.UIReqs.Canvas;

/// <summary>
/// The mode of a set of pixels, the way the harness measures ink and dominant colours: pixels are counted per
/// quantized colour bucket (four bits per channel, <see cref="ColorMatch.Quantize"/>), the most common bucket wins,
/// and the colour reported for it is the bucket's MOST FREQUENT exact colour - not the first pixel met in scan order,
/// which can be an anti-aliased edge pixel of the same bucket (#FF0909 ink for a #FF0000 glyph, outside the match
/// tolerance). The CodeBrix.Android copy of this harness found and fixed the same defect (its ModeColor.cs).
/// </summary>
public sealed class ModeColor
{
	private readonly Dictionary<uint, int> _bucketCounts = new();
	private readonly Dictionary<uint, Dictionary<uint, int>> _exact = new();

	/// <summary>How many pixels were counted.</summary>
	public int Total { get; private set; }

	/// <summary>Counts one (already composited) pixel.</summary>
	/// <param name="color">The pixel's colour.</param>
	public void Add(SKColor color)
	{
		var bucket = (uint) ColorMatch.Quantize(color);
		_bucketCounts.TryGetValue(bucket, out var count);
		_bucketCounts[bucket] = count + 1;

		if (!_exact.TryGetValue(bucket, out var colors))
		{
			_exact[bucket] = colors = new Dictionary<uint, int>();
		}

		var key = (uint) color;
		colors.TryGetValue(key, out var exactCount);
		colors[key] = exactCount + 1;
		Total++;
	}

	/// <summary>
	/// The most common bucket (the first one counted wins a tie, as the harness always did) and its most frequent
	/// exact colour (the smaller colour value wins a tie, so the answer does not depend on scan order).
	/// </summary>
	/// <param name="color">The representative colour.</param>
	/// <param name="count">How many pixels fell into the winning bucket.</param>
	/// <returns><see langword="false"/> when no pixel was counted.</returns>
	public bool TryGetMode(out SKColor color, out int count)
	{
		var bestBucket = 0u;
		count = 0;
		foreach (var pair in _bucketCounts)
		{
			// Dictionary enumeration follows insertion order while nothing is removed: the first bucket met keeps a tie,
			// as OrderByDescending(...).First() did.
			if (pair.Value > count)
			{
				bestBucket = pair.Key;
				count = pair.Value;
			}
		}

		if (count == 0)
		{
			color = default;
			return false;
		}

		var best = 0u;
		var bestCount = -1;
		foreach (var pair in _exact[bestBucket])
		{
			if (pair.Value > bestCount || (pair.Value == bestCount && pair.Key < best))
			{
				best = pair.Key;
				bestCount = pair.Value;
			}
		}

		color = new SKColor(best);
		return true;
	}
}
