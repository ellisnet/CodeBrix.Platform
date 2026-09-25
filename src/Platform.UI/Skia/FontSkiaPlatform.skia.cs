#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using CodeBrix.Platform.UI.Contracts;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IFontPlatform"/>: loads typefaces through the font details cache of the
/// Skia text engine.
/// </summary>
internal sealed class FontSkiaPlatform : IFontPlatform
{
	/// <inheritdoc />
	public Task<bool> PreloadAsync(FontFamily family, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		// size doesn't matter here, we're just preloading the typeface
		// Default value of the font is of type double and boxed in object
		var fontSize = (float)(double)TextBlock.FontSizeProperty.Metadata.DefaultValue;
		return FontDetailsCache.GetFont(family.Source, fontSize, weight, stretch, style)
			.loadedTask
			.ContinueWith(t => t is { IsCompletedSuccessfully: true, Result: not null });
	}
}
