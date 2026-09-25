#nullable enable

namespace CodeBrix.Platform.Foundation.Contracts;

/// <summary>
/// The platform's font source for the shared text engine: the application's font configuration (default, symbols and
/// fallback families, font isolation) and the typefaces it resolves them to, from ONE typeface cache per process.
/// </summary>
/// <typeparam name="TTypeface">The platform's typeface type; <c>SkiaSharp.SKTypeface</c> on every current platform
/// (the engine registers and resolves <c>IFontSourcePlatform&lt;SKTypeface&gt;</c>). This assembly does not reference
/// SkiaSharp, so the contract takes the type as a parameter instead of naming it.</typeparam>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The text engine (<c>UnicodeText</c>, <c>FontDetailsCache</c> and the types around them) is ONE source compiled into two
/// assemblies: the framework's Skia assembly (CodeBrix.Platform.UI, which lays out every TextBlock and TextBox) and
/// CodeBrix.Platform.UI.TextLayout.Core (which lays out TextLayout, and through it AdvancedTextEdit and TerminalView,
/// and supplies PlotterView's typefaces). Neither copy names a XAML type; everything the engine used to read from the
/// XAML side (FeatureConfiguration.Font, application font URIs through the package storage) comes through this contract,
/// so both copies resolve every family to the same typeface instance and measure identically.
/// </para>
/// <para>
/// Platform (Skia): <c>CodeBrix.Platform.UI.Skia.FontSourceSkiaPlatform</c> in CodeBrix.Platform.UI, registered by that
/// assembly's platform bootstrap; the framework's own engine copy uses the same instance directly. The TextLayout.Core
/// copy resolves it once, through its <c>PlatformContract</c> (which loads CodeBrix.Platform.UI, CodeBrix.Android.UI,
/// CodeBrix.Android.UI.TextLayout or CodeBrix.Mobile.UI.TextLayout by name when nothing is registered yet).
/// </para>
/// </remarks>
internal interface IFontSourcePlatform<TTypeface>
	where TTypeface : class
{
	/// <summary>The application's default text font family (a family name, or an application font URI).</summary>
	string DefaultTextFontFamily { get; }

	/// <summary>The font family of the framework's symbol glyphs, tried first for a character the run's font lacks.</summary>
	string SymbolsFont { get; }

	/// <summary>
	/// Whether font isolation is on: a bare family name resolves only against the application's own fonts, and fallback
	/// never reaches the host's installed fonts.
	/// </summary>
	bool RestrictToEmbeddedFonts { get; }

	/// <summary>The application's own fallback families, in order (null or empty when none are configured).</summary>
	IReadOnlyList<string>? FallbackFontFamilies { get; }

	/// <summary>
	/// Resolves a family (a family name, or an application font URI, optionally with a <c>#</c> suffix) and a style to a
	/// typeface, through the process-wide typeface cache: every caller asking for the same family and style gets the same
	/// task, and so the same typeface instance.
	/// </summary>
	/// <param name="familyName">The family name or font URI; never null (the engine substitutes the default first).</param>
	/// <param name="weight">The numeric weight (400 normal, 700 bold).</param>
	/// <param name="stretch">The stretch, as the value of <c>Windows.UI.Text.FontStretch</c> (0 undefined .. 9 ultra-expanded).</param>
	/// <param name="style">The style, as the value of <c>Windows.UI.Text.FontStyle</c> (0 normal, 1 oblique, 2 italic).</param>
	/// <returns>The typeface load: already completed for a family resolved synchronously; its result is null when the family
	/// cannot be resolved.</returns>
	Task<TTypeface?> GetTypefaceAsync(string familyName, ushort weight, int stretch, int style);

	/// <summary>
	/// Under font isolation, the application's own default font when its load has finished; otherwise (isolation off, the
	/// default is a bare family name, or it is still loading) null.
	/// </summary>
	/// <param name="weight">The numeric weight.</param>
	/// <param name="stretch">The stretch value (see <see cref="GetTypefaceAsync"/>).</param>
	/// <param name="style">The style value (see <see cref="GetTypefaceAsync"/>).</param>
	/// <returns>The loaded default typeface, or null.</returns>
	TTypeface? GetLoadedEmbeddedDefaultTypeface(ushort weight, int stretch, int style);
}
