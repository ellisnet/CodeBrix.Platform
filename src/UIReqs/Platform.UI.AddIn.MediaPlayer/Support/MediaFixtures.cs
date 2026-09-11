using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;
using Windows.UI;

namespace CodeBrix.Platform.UI.AddIn.MediaPlayer.UIReqs.Support;

/// <summary>
/// The clips the scenarios play, and the poster they show when there is no picture.
/// <para>
/// Every clip is a two-second file beside the test executable: red for its first second and
/// blue for its second, in saturated primaries, so that WHICH second of the clip is on the
/// panel is something a person can see. They are handed to the engine as absolute
/// <c>file:///</c> URIs, which the add-in passes to the native player untouched - that is the
/// one source shape with no ambiguity about which folder a relative name is relative to.
/// </para>
/// <para>
/// The poster is made here instead of carried as a file: a scenario that says the poster is
/// showing has to name the colour it expects, and a colour the test itself painted cannot drift
/// away from the colour the scenario asserts.
/// </para>
/// </summary>
public static class MediaFixtures
{
	/// <summary>The clip a scenario names to get the two-colour clip with no audio track.</summary>
	public const string TwoColour = "twocolour";

	/// <summary>The clip a scenario names to get the two-colour clip that also carries silence.</summary>
	public const string TwoColourWithSound = "twocolour-with-sound";

	/// <summary>The clip a scenario names to get the taller-than-wide two-colour clip.</summary>
	public const string Portrait = "portrait";

	/// <summary>The clip a scenario names to get a clip with sound and no picture at all.</summary>
	public const string AudioOnly = "audio-only";

	/// <summary>The name a scenario uses for a source that names a file which is not there.</summary>
	public const string Missing = "missing";

	/// <summary>The width and height of the poster picture, in pixels.</summary>
	public const int PosterSize = 64;

	private static readonly Dictionary<string, string> FileNames = new(StringComparer.OrdinalIgnoreCase)
	{
		[TwoColour] = "twocolour_videoonly.mp4",
		[TwoColourWithSound] = "twocolour.mp4",
		[Portrait] = "portrait.mp4",
		[AudioOnly] = "audio_only.m4a",

		// Nothing of this name is copied to the output folder, which is the point: the source is
		// well formed and the file behind it does not exist.
		[Missing] = "no-such-clip.mp4",
	};

	/// <summary>The names a feature file may write, in alphabetical order.</summary>
	public static IReadOnlyCollection<string> Names
	{
		get
		{
			var names = new List<string>(FileNames.Keys);
			names.Sort(StringComparer.OrdinalIgnoreCase);
			return names;
		}
	}

	/// <summary>The absolute path of a clip, beside the test executable.</summary>
	/// <param name="name">The name a feature file writes.</param>
	/// <returns>The path.</returns>
	/// <exception cref="NotSupportedException">No clip goes by that name.</exception>
	public static string PathOf(string name)
	{
		if (!FileNames.TryGetValue(name, out var fileName))
		{
			throw new NotSupportedException(
				$"There is no clip called \"{name}\". The scenarios have: {string.Join(", ", Names)}.");
		}

		return Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
	}

	/// <summary>The absolute <c>file:///</c> URI of a clip, as the engine is given it.</summary>
	/// <param name="name">The name a feature file writes.</param>
	/// <returns>The URI.</returns>
	public static Uri UriOf(string name) => new(PathOf(name));

	/// <summary>Encodes the poster as a PNG of one flat colour.</summary>
	/// <param name="color">The colour to paint it.</param>
	/// <returns>The PNG bytes.</returns>
	public static byte[] EncodePosterPng(Color color)
	{
		using var bitmap = new SKBitmap(PosterSize, PosterSize, SKColorType.Rgba8888, SKAlphaType.Premul);
		using (var canvas = new SKCanvas(bitmap))
		{
			canvas.Clear(new SKColor(color.R, color.G, color.B, color.A));
		}

		using var image = SKImage.FromBitmap(bitmap);
		using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
			?? throw new InvalidOperationException("The poster picture could not be encoded as a PNG.");

		return encoded.ToArray();
	}

	/// <summary>The poster as a stream a bitmap source can be given.</summary>
	/// <param name="color">The colour to paint it.</param>
	/// <returns>A stream over the PNG bytes.</returns>
	public static Stream OpenPosterPng(Color color) => new MemoryStream(EncodePosterPng(color), false);

	/// <summary>A description of a clip, for a failure message.</summary>
	/// <param name="name">The name a feature file writes.</param>
	/// <returns>The name and the file behind it.</returns>
	public static string Describe(string name) => string.Create(CultureInfo.InvariantCulture,
		$"the clip \"{name}\" ({PathOf(name)})");
}
