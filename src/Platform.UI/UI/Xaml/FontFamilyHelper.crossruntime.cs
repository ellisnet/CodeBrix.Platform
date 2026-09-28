#if !__NETSTD_REFERENCE__
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

internal static partial class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(
		FontFamily family,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
		=> PlatformServices.Fonts.PreloadAsync(family, weight, stretch, style);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(
		string familyName,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
		=> PreloadAsync(new FontFamily(familyName), weight, stretch, style);

	/// <summary>
	/// Decides whether a default font family value names a font FILE whose ".manifest" can be preloaded: only an
	/// absolute URI does (e.g. "ms-appx:///Assets/Fonts/OpenSans.ttf"). A plain family NAME ("Segoe UI", the Core
	/// default) is nothing to preload as a manifest: it goes straight to the by-name preload. (Before WPE1-5 a name
	/// was parsed as a relative URI and the manifest lookup threw UriFormatException, logged as an error at every start.)
	/// </summary>
	/// <param name="fontFamily">The font family value (FeatureConfiguration.Font.DefaultTextFontFamily).</param>
	/// <param name="uri">The font file URI, when the method returns <see langword="true"/>.</param>
	/// <returns><see langword="true"/> when <paramref name="fontFamily"/> is an absolute URI.</returns>
	internal static bool TryGetFontManifestUri(string fontFamily, out Uri uri)
		=> Uri.TryCreate(fontFamily, UriKind.Absolute, out uri);

	/// <summary>
	/// Builds the URI of a font file's ".manifest" companion. A "#Family" fragment names a family inside the font file
	/// (e.g. "ms-appx:///Fonts/Roboto.ttf#Roboto"): it is not part of the file path, so it is dropped before ".manifest"
	/// is appended (appending after it put ".manifest" into the fragment and opened the font file itself as the manifest).
	/// A URI without a fragment gets ".manifest" appended to its original string unchanged.
	/// </summary>
	/// <param name="uri">The URI of the font file, optionally followed by a "#Family" fragment.</param>
	/// <returns>The URI of the font's manifest file.</returns>
	internal static Uri GetFontManifestUri(Uri uri)
	{
		var fontFile = uri.OriginalString;
		var fragmentStart = fontFile.IndexOf('#');
		if (fragmentStart >= 0)
		{
			fontFile = fontFile.Substring(0, fragmentStart);
		}

		return new Uri(fontFile + ".manifest");
	}

	/// <param name="uri">The URI of the font (ending with.ttf without .manifest)</param>
	public static async Task<bool> PreloadAllFontsInManifest(Uri uri)
	{
		var manifestUri = GetFontManifestUri(uri);
		var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
		if (!await StorageFileHelper.ExistsInPackage(path))
		{
			return false;
		}

		var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
		FontManifest manifest = null;
		using (var manifestStream = await manifestFile.OpenStreamForReadAsync())
		{
			manifest = FontManifestHelpers.DeserializeManifest(manifestStream);
		}

		if (manifest is null)
		{
			return false;
		}

		var tasks = manifest.Fonts
			.Select(fontInfo => PreloadAsync(fontInfo.FamilyName, new FontWeight(fontInfo.FontWeight), fontInfo.FontStretch, fontInfo.FontStyle));

		return await Task.WhenAll(tasks).ContinueWith(combinedTask => combinedTask.Result.All(t => t));
	}
}
#endif
