#nullable enable
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.UI.Skia;
using SkiaSharp;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// The framework-only part of the engine's font cache (WPE1 C5): the XAML-typed entry points the framework's own callers
// (TextBlock, the inline model, ParsedText, the font preload) use, and the font source of the framework's copy.
internal static partial class FontDetailsCache
{
	// The framework's copy of the engine uses the framework's font source directly (it is also registered for the
	// TextLayout copy and for the other platforms' use by this assembly's platform bootstrap).
	private static partial IFontSourcePlatform<SKTypeface> ResolveFontSource() => FontSourceSkiaPlatform.Instance;

	/// <summary>
	/// <see cref="GetFont(string?, float, ushort, EngineFontStretch, EngineFontStyle)"/> for the framework's XAML-typed
	/// font properties.
	/// </summary>
	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => GetFont(name, fontSize, weight.Weight, stretch.ToEngine(), style.ToEngine());

	/// <summary>
	/// <see cref="GetEmbeddedFallback(int, float, ushort, EngineFontStretch, EngineFontStyle)"/> for the framework's
	/// XAML-typed font properties.
	/// </summary>
	internal static FontDetails? GetEmbeddedFallback(
		int codepoint,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => GetEmbeddedFallback(codepoint, fontSize, weight.Weight, stretch.ToEngine(), style.ToEngine());
}
