#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Loads fonts for the platform's text engine.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Called by <c>Microsoft.UI.Xaml.FontFamilyHelper</c>, which the application uses at launch to preload the symbols
/// font and the default text font. The Skia implementation is <c>CodeBrix.Platform.UI.Skia.FontSkiaPlatform</c>
/// (the font details cache of the Skia text engine), registered by <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>.
/// The text work package may fold this contract into its text platform contract.
/// </para>
/// </remarks>
internal interface IFontPlatform
{
	/// <summary>
	/// Loads a typeface ahead of its first use, so that text does not have to be laid out again once it arrives.
	/// </summary>
	/// <param name="family">The font family.</param>
	/// <param name="weight">The font weight.</param>
	/// <param name="stretch">The font stretch.</param>
	/// <param name="style">The font style.</param>
	/// <returns>A task that completes with <see langword="true"/> when the typeface loaded, otherwise <see langword="false"/>.</returns>
	Task<bool> PreloadAsync(FontFamily family, FontWeight weight, FontStretch stretch, FontStyle style);
}
