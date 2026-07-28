#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using CodeBrix.Platform.Helpers;
using SKFontStyleWidth = SkiaSharp.SKFontStyleWidth;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <remarks>
/// Skia uses the word "typeface" to mean a specific style of a typographic family (e.g. OpenSans with Bold weight, Normal width and Italic slant)
/// and the word "font" to mean a typeface + a specific font size. This is different from the literature where "typeface"
/// means a typographic family (e.g. OpenSans or Segoe UI) and "font" means what Skia means by "typeface".
/// We try to use Skia's wording for code and the accurate wording for logging.
/// </remarks>
internal static class FontDetailsCache
{
	private readonly record struct FontEntry(
		string Name,
		SKFontStyleWeight Weight,
		SKFontStyleWidth Width,
		SKFontStyleSlant Slant);

	private static readonly Dictionary<FontEntry, Task<SKTypeface?>> _fontCache = new();
	private static readonly object _fontCacheGate = new();

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				using var manifestStream = await manifestFile.OpenStreamForReadAsync();
				uri = new Uri(FontManifestHelpers.GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
			}
		}
		catch (Exception e)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font manifest for {uri}: {e}");
			}
		}

		if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(FontDetailsCache).Log().LogDebug($"Fetching font from {uri}");
		}

		try
		{
			using var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
			return SKTypeface.FromStream(stream);
		}
		catch (Exception e)
		{
			typeof(FontDetailsCache).LogError()?.Error($"Loading font from {uri} failed: {e}");
			return null;
		}
	}

	private static Task<SKTypeface?> GetFontInternal(
		string name,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		var skWidth = stretch.ToSkiaWidth();
		var skSlant = style.ToSkiaSlant();

		var hashIndex = name.IndexOf('#');
		if (hashIndex > 0)
		{
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
		{
			return LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch);
		}
		else if (FeatureConfiguration.Font.RestrictToEmbeddedFonts)
		{
			// Font isolation: a bare family name ("Segoe UI", "Arial") can only ever be
			// satisfied by the host's installed fonts, so there is nothing to resolve it
			// against here. The application's own default font stands in — the same thing
			// a device carrying only the application's fonts would fall back to.
			return GetEmbeddedDefaultTypefaceTask(weight, stretch, style)
				?? Task.FromResult<SKTypeface?>(null);
		}
		else
		{
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			return Task.FromResult<SKTypeface?>(SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant));
		}
	}

	/// <summary>
	/// The first font in <see cref="FeatureConfiguration.Font.FallbackFontFamilies"/> that
	/// has a glyph for <paramref name="codepoint"/>, or null when none does (or none are
	/// configured). These are the application's OWN fonts, so they are consulted whether or
	/// not font isolation is on — an application that declares companion faces is extending
	/// its own script coverage, not reaching for the host's fonts.
	/// <para>
	/// A font still loading is skipped rather than waited on: the caller falls through for
	/// that one measure pass, and the next one picks it up once the load completes.
	/// </para>
	/// </summary>
	internal static FontDetails? GetEmbeddedFallback(
		int codepoint,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var families = FeatureConfiguration.Font.FallbackFontFamilies;
		if (families is null || families.Count == 0)
		{
			return null;
		}

		for (var i = 0; i < families.Count; i++)
		{
			var family = families[i];
			if (string.IsNullOrWhiteSpace(family))
			{
				continue;
			}

			var details = GetFont(family, fontSize, weight, stretch, style).details;
			if (details.SKFont.ContainsGlyph(codepoint))
			{
				return details;
			}
		}

		return null;
	}

	/// <summary>
	/// The application's own default font for the last-resort path, or null when font
	/// isolation is off or that font has not finished loading. Every other branch of that
	/// path asks the HOST for a typeface, so under isolation this is the only acceptable
	/// answer — and it is available whenever the font's load has already finished, which
	/// the preload in Application startup makes the ordinary case. A font still loading
	/// falls through for that one measure pass; the continuation in the caller replaces
	/// what was measured once the real typeface arrives.
	/// </summary>
	private static SKTypeface? GetLoadedEmbeddedDefaultTypeface(
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		if (!FeatureConfiguration.Font.RestrictToEmbeddedFonts)
		{
			return null;
		}
		return GetEmbeddedDefaultTypefaceTask(weight, stretch, style) is { IsCompletedSuccessfully: true } task
			? task.Result
			: null;
	}

	/// <summary>
	/// The load of the application's own default font, taken from (and seeded into) the
	/// same cache as any other font so one typeface instance is shared by every caller
	/// that lands on it. Null when the application's default is itself a bare family name
	/// — the built-in "Segoe UI" — which under font isolation is unresolvable by
	/// definition, and never recurses for that same reason.
	/// </summary>
	private static Task<SKTypeface?>? GetEmbeddedDefaultTypefaceTask(
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var name = FeatureConfiguration.Font.DefaultTextFontFamily;
		if (!Uri.TryCreate(name, UriKind.Absolute, out _))
		{
			return null;
		}

		var key = new FontEntry(name, weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant());
		// Monitor is reentrant, so this is safe on the path that reaches it from inside
		// the cache's own lock (GetFontInternal, called while _getFont holds the gate).
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var task))
			{
				_fontCache[key] = task = GetFontInternal(name, weight, stretch, style);
			}
			return task;
		}
	}

	private static readonly Func<string?, float, FontWeight, FontStretch, FontStyle, (FontDetails details, Task<FontDetails> loadedTask)> _getFont = FuncMemoizeExtensions.AsLockedMemoized((
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) =>
	{
		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = FeatureConfiguration.Font.DefaultTextFontFamily;
		}

		var (skWeight, skWidth, skSlant) = (weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant());
		var key = new FontEntry(name, skWeight, skWidth, skSlant);

		Task<SKTypeface?> typefaceTask;
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var nullableTask))
			{
				_fontCache[key] = nullableTask = GetFontInternal(name, weight, stretch, style);
			}
			typefaceTask = nullableTask;
		}

		var canChange = !typefaceTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var typeface = !canChange ? typefaceTask.Result : null;

		if (typeface == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Debug))
			{
				if (canChange)
				{
					typeof(Inline).Log().LogDebug($"{key} is still loading, using system default for now.");
				}
				else
				{
					typeof(Inline).Log().LogDebug($"{key} could not be found, using system default");
				}
			}

			typeface = GetLoadedEmbeddedDefaultTypeface(weight, stretch, style)
						?? SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
						?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
						?? SKTypeface.FromFamilyName(null);
		}

		var details = FontDetails.Create(typeface, fontSize);

		var detailsTask = typefaceTask.ContinueWith(t =>
		{
			var loadedTypeface = t.IsCompletedSuccessfully ? t.Result : null;

			if (loadedTypeface is null)
			{
				if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
				{
					typeof(FontDetailsCache).Log().LogError($"Failed to load {key}", t.Exception);
				}

				return details;
			}
			else
			{
				return FontDetails.Create(loadedTypeface, details.SKFontSize);
			}
		});
		return (details, detailsTask);
	});

	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => _getFont(name, fontSize, weight, stretch, style);
}
