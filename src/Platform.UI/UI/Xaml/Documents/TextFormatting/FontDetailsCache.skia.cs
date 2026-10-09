#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.Foundation.Logging;
using SKFontStyleWidth = SkiaSharp.SKFontStyleWidth;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <remarks>
/// Skia uses the word "typeface" to mean a specific style of a typographic family (e.g. OpenSans with Bold weight, Normal width and Italic slant)
/// and the word "font" to mean a typeface + a specific font size. This is different from the literature where "typeface"
/// means a typographic family (e.g. OpenSans or Segoe UI) and "font" means what Skia means by "typeface".
/// We try to use Skia's wording for code and the accurate wording for logging.
/// <para>
/// Part of the shared text engine (compiled into the framework's Skia assembly and into
/// CodeBrix.Platform.UI.TextLayout.Core, WPE1 C5): it names no XAML or WinRT type. The application's font configuration
/// and the typefaces themselves come from the platform's font source (<see cref="Source"/>), whose typeface cache is
/// shared by both copies; each copy keeps its own sized fonts (<see cref="FontDetails"/>) over those typefaces. Each
/// assembly supplies <c>ResolveFontSource</c> in its own partial (the framework: its own font source; TextLayout.Core:
/// the registered one).
/// </para>
/// </remarks>
internal static partial class FontDetailsCache
{
	private readonly record struct FontEntry(
		string Name,
		SKFontStyleWeight Weight,
		SKFontStyleWidth Width,
		SKFontStyleSlant Slant);

	private static IFontSourcePlatform<SKTypeface>? _source;

	/// <summary>The platform's font source (the font configuration and the process-wide typeface cache), resolved once.</summary>
	internal static IFontSourcePlatform<SKTypeface> Source => _source ??= ResolveFontSource();

	private static partial IFontSourcePlatform<SKTypeface> ResolveFontSource();

	/// <summary>
	/// The first font in the application's fallback families (<see cref="IFontSourcePlatform{TTypeface}.FallbackFontFamilies"/>)
	/// that has a glyph for <paramref name="codepoint"/>, or null when none does (or none are
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
		ushort weight,
		EngineFontStretch stretch,
		EngineFontStyle style)
	{
		var families = Source.FallbackFontFamilies;
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
	/// The font to draw <paramref name="codepoint"/> in when the font the text asked for has no glyph for it, or null
	/// when the character should stay in the requested font (a control character, or nothing else has it). This is the
	/// ONE missing-character lookup of the text engine; both text paths (<c>UnicodeText</c> and the run segmenter of
	/// <c>RunTextEngineExtensions</c>) call it, so they resolve a missing character identically.
	/// </summary>
	/// <remarks>
	/// The order is: the framework's symbols font (<see cref="IFontSourcePlatform{TTypeface}.SymbolsFont"/>), then the
	/// application's own fallback families (<see cref="GetEmbeddedFallback"/>), then - only while
	/// <see cref="IFontSourcePlatform{TTypeface}.RestrictToEmbeddedFonts"/> is off - the host's fonts.
	/// </remarks>
	/// <param name="codepoint">The Unicode code point the requested font cannot draw.</param>
	/// <param name="fontSize">The font size of the text.</param>
	/// <param name="fontWeight">The font weight of the text.</param>
	/// <param name="fontStretch">The font stretch of the text.</param>
	/// <param name="fontStyle">The font style of the text.</param>
	/// <returns>The fallback font, or null to keep the requested font.</returns>
	internal static FontDetails? GetFallbackFont(int codepoint, float fontSize, ushort fontWeight, EngineFontStretch fontStretch, EngineFontStyle fontStyle)
	{
		// Line-break and other control characters have no visible glyph, so they must never trigger
		// font fallback. On some hosts (e.g. Linux with the LyX math fonts installed) SKFontManager's
		// MatchCharacter(U+000A) resolves to a math font such as esint10, whose shaped glyph paints a
		// stray "elongated f"/integral stroke at the end of every broken line. Returning null keeps the
		// character in the caller's own font (via "?? inline.FontDetails"), where it maps to an inkless
		// .notdef. This is a no-op on platforms where fallback already resolved to nothing visible.
		if (codepoint <= 0xFFFF && char.IsControl((char)codepoint))
		{
			return null;
		}

		var symbolsFont = GetFont(Source.SymbolsFont, fontSize, fontWeight, fontStretch, fontStyle).details;
		if (symbolsFont.SKFont.ContainsGlyph(codepoint))
		{
			return symbolsFont;
		}
		// The application's own declared fallbacks, in order. These are its fonts, shipped
		// in its package, so they are consulted whether or not isolation is on — and they
		// are checked BEFORE the host's fonts so text renders the same on a desktop as on
		// a device that has nothing else installed.
		if (GetEmbeddedFallback(codepoint, fontSize, fontWeight, fontStretch, fontStyle) is { } embedded)
		{
			return embedded;
		}
		// Font isolation: everything below this point looks outside the application — the
		// device's own font directory on Android, the host's installed fonts everywhere
		// else — so under isolation there is deliberately nowhere left to look. Returning
		// null keeps the character in the caller's own font (via "?? inline.FontDetails"),
		// where it renders as that font's missing-glyph, which is what a device carrying
		// only the application's fonts would show. The symbols font above is checked first
		// and stays exempt: the framework depends on it, so it is present on a real device
		// exactly as it is here.
		if (Source.RestrictToEmbeddedFonts)
		{
			return null;
		}
		if (OperatingSystem.IsAndroid())
		{
			foreach (var file in Directory.EnumerateFiles("/system/fonts"))
			{
				var font = GetFont(file, fontSize, fontWeight, fontStretch, fontStyle).details;
				if (font.SKFont.ContainsGlyph(codepoint))
				{
					return font;
				}
			}
		}
		var typeface = SKFontManager.Default.MatchCharacter(codepoint);
		return typeface is not null ? GetFont(typeface.FamilyName, fontSize, fontWeight, fontStretch, fontStyle).details : null;
	}

	private static readonly Func<string?, float, ushort, EngineFontStretch, EngineFontStyle, (FontDetails details, Task<FontDetails> loadedTask)> _getFont = FuncMemoizeExtensions.AsLockedMemoized((
		string? name,
		float fontSize,
		ushort weight,
		EngineFontStretch stretch,
		EngineFontStyle style) =>
	{
		var source = Source;
		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = source.DefaultTextFontFamily;
		}

		var (skWeight, skWidth, skSlant) = (weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant());
		var key = new FontEntry(name, skWeight, skWidth, skSlant);

		// The typeface comes from the font source's process-wide cache (one task per family and style, shared by every
		// copy of the engine); only the sized font below is this copy's own.
		var typefaceTask = source.GetTypefaceAsync(name, weight, (int)stretch, (int)style);

		var canChange = !typefaceTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var typeface = !canChange ? typefaceTask.Result : null;

		if (typeface == null)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
			{
				if (canChange)
				{
					typeof(FontDetailsCache).Log().LogDebug($"{key} is still loading, using system default for now.");
				}
				else
				{
					typeof(FontDetailsCache).Log().LogDebug($"{key} could not be found, using system default");
				}
			}

			typeface = source.GetLoadedEmbeddedDefaultTypeface(weight, (int)stretch, (int)style)
						?? SKTypeface.FromFamilyName(source.DefaultTextFontFamily, skWeight, skWidth, skSlant)
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

	/// <summary>
	/// The sized font for a family and style, and the task that completes with the family's own font once its typeface
	/// has loaded (an interim font stands in until then).
	/// </summary>
	/// <param name="name">The family name or font URI; null (or "XamlAutoFontFamily") for the application's default.</param>
	/// <param name="fontSize">The font size.</param>
	/// <param name="weight">The numeric weight.</param>
	/// <param name="stretch">The stretch.</param>
	/// <param name="style">The style.</param>
	/// <returns>The font to use now, and the load of the family's own font.</returns>
	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
		string? name,
		float fontSize,
		ushort weight,
		EngineFontStretch stretch,
		EngineFontStyle style) => _getFont(name, fontSize, weight, stretch, style);
}
