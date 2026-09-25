#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.UI.Xaml.Media;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using SKFontStyleWidth = SkiaSharp.SKFontStyleWidth;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IFontSourcePlatform{TTypeface}"/> for the shared text engine: the application's
/// font configuration (<see cref="FeatureConfiguration.Font"/>) and the process-wide typeface cache, which loads
/// application font URIs (and their font manifests) from the package and every other family from the host through
/// Skia.
/// </summary>
/// <remarks>
/// The typeface cache and the loading are the code of the engine's <c>FontDetailsCache</c> before the text-engine
/// split (WPE1 C5), moved verbatim: the engine's two copies (this assembly's and CodeBrix.Platform.UI.TextLayout.Core's)
/// both resolve their typefaces here, so a family resolves to the same typeface instance in a TextBlock, a TextLayout
/// and a chart. The framework's copy uses <see cref="Instance"/> directly; the platform bootstrap registers the same
/// instance for the TextLayout copy.
/// </remarks>
internal sealed class FontSourceSkiaPlatform : IFontSourcePlatform<SKTypeface>
{
	/// <summary>The process-wide font source.</summary>
	internal static FontSourceSkiaPlatform Instance { get; } = new();

	private readonly record struct FontEntry(
		string Name,
		SKFontStyleWeight Weight,
		SKFontStyleWidth Width,
		SKFontStyleSlant Slant);

	private static readonly Dictionary<FontEntry, Task<SKTypeface?>> _fontCache = new();
	private static readonly object _fontCacheGate = new();

	private FontSourceSkiaPlatform()
	{
	}

	/// <inheritdoc />
	public string DefaultTextFontFamily => FeatureConfiguration.Font.DefaultTextFontFamily;

	/// <inheritdoc />
	public string SymbolsFont => FeatureConfiguration.Font.SymbolsFont;

	/// <inheritdoc />
	public bool RestrictToEmbeddedFonts => FeatureConfiguration.Font.RestrictToEmbeddedFonts;

	/// <inheritdoc />
	public IReadOnlyList<string>? FallbackFontFamilies => FeatureConfiguration.Font.FallbackFontFamilies;

	/// <inheritdoc />
	public Task<SKTypeface?> GetTypefaceAsync(string familyName, ushort weight, int stretch, int style)
	{
		var (fontWeight, fontStretch, fontStyle) = (new FontWeight(weight), (FontStretch)stretch, (FontStyle)style);
		var key = new FontEntry(familyName, fontWeight.ToSkiaWeight(), fontStretch.ToSkiaWidth(), fontStyle.ToSkiaSlant());

		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var nullableTask))
			{
				_fontCache[key] = nullableTask = GetFontInternal(familyName, fontWeight, fontStretch, fontStyle);
			}
			return nullableTask;
		}
	}

	/// <inheritdoc />
	public SKTypeface? GetLoadedEmbeddedDefaultTypeface(ushort weight, int stretch, int style) =>
		GetLoadedEmbeddedDefaultTypeface(new FontWeight(weight), (FontStretch)stretch, (FontStyle)style);

	//Moved verbatim from FontDetailsCache (Documents/TextFormatting/FontDetailsCache.skia.cs) at the text-engine split.
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

	//Moved verbatim from FontDetailsCache at the text-engine split.
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
		// the cache's own lock (GetFontInternal, called while GetTypefaceAsync holds the gate).
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var task))
			{
				_fontCache[key] = task = GetFontInternal(name, weight, stretch, style);
			}
			return task;
		}
	}
}
