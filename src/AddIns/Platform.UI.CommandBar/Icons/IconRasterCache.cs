using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The process-wide cache of prepared icon images, one entry per distinct look.
/// </summary>
/// <remarks>
/// <para>
/// An SVG icon is parsed and rasterised, which is far too expensive to repeat for every button that
/// shows it - a tool bar commonly shows the same icon in several places, and re-renders on every
/// theme change and every display-scale change. What makes one rendering different from another is
/// exactly five things: the artwork, the theme (which chooses between an icon's light and dark
/// artwork), the icon size, the display scale, and the tint. Those five are the key.
/// </para>
/// <para>
/// Entries are held WEAKLY. An icon nothing shows any more is collected like any other object, so a
/// long-running application that walks through thousands of icons does not accumulate them; an icon
/// that is still on screen is still in the cache, which is the case that matters.
/// </para>
/// </remarks>
public static class IconRasterCache
{
	private static readonly object _gate = new();
	private static readonly Dictionary<IconCacheKey, WeakReference<SvgImageSource>> _entries = new();

	private static long _hits;
	private static long _misses;

	/// <summary>How many icon renderings the cache is holding, live entries only.</summary>
	public static int Count
	{
		get
		{
			lock (_gate)
			{
				Prune();
				return _entries.Count;
			}
		}
	}

	/// <summary>
	/// Empties the cache.
	/// </summary>
	/// <remarks>
	/// Icons already on screen keep working - each one holds its own image - so this only costs the
	/// next icon of each kind a re-render. Worth calling when an application swaps its whole icon
	/// set, and useful in a test.
	/// </remarks>
	public static void Clear()
	{
		lock (_gate)
		{
			_entries.Clear();
			_hits = 0;
			_misses = 0;
		}
	}

	/// <summary>How many lookups have been answered from the cache.</summary>
	internal static long Hits
	{
		get
		{
			lock (_gate)
			{
				return _hits;
			}
		}
	}

	/// <summary>How many lookups have had to render.</summary>
	internal static long Misses
	{
		get
		{
			lock (_gate)
			{
				return _misses;
			}
		}
	}

	/// <summary>
	/// The image for one look, rendered through <paramref name="factory"/> the first time it is
	/// asked for and reused afterwards.
	/// </summary>
	/// <param name="key">What makes this rendering different from every other.</param>
	/// <param name="factory">Builds the image. Called at most once per live key.</param>
	/// <returns>The cached image.</returns>
	internal static SvgImageSource GetOrCreate(IconCacheKey key, Func<SvgImageSource> factory)
	{
		lock (_gate)
		{
			if (_entries.TryGetValue(key, out var weak) && weak.TryGetTarget(out var cached))
			{
				_hits++;
				return cached;
			}

			_misses++;
			var created = factory();
			_entries[key] = new WeakReference<SvgImageSource>(created);
			return created;
		}
	}

	private static void Prune()
	{
		List<IconCacheKey>? dead = null;
		foreach (var entry in _entries)
		{
			if (!entry.Value.TryGetTarget(out _))
			{
				(dead ??= new List<IconCacheKey>()).Add(entry.Key);
			}
		}

		if (dead is not null)
		{
			foreach (var key in dead)
			{
				_entries.Remove(key);
			}
		}
	}
}

/// <summary>
/// Everything that makes one icon rendering different from another.
/// </summary>
/// <param name="Source">The artwork: a URI, or the inline markup itself.</param>
/// <param name="Theme">The theme in force, which chose <paramref name="Source"/> in the first place
/// and is kept in the key so a light and a dark rendering never collide.</param>
/// <param name="Size">The icon's edge length in logical pixels.</param>
/// <param name="Scale">The display scale the rendering was made for.</param>
/// <param name="Tint">The stylesheet applied at parse, or the empty string for none.</param>
internal readonly record struct IconCacheKey(
	string Source,
	ElementTheme Theme,
	double Size,
	double Scale,
	string Tint)
{
	/// <summary>A short, stable description, for a log line or a test failure message.</summary>
	/// <returns>The key written out.</returns>
	public override string ToString()
		=> string.Create(
			CultureInfo.InvariantCulture,
			$"{Source}|{Theme}|{Size:0.##}|{Scale:0.##}|{Tint}");
}
