using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CodeBrix.Platform.UI.CommandBar.Contracts;

/// <summary>
/// Builds the platform image source behind an SVG icon: told what size to rasterise the artwork at and handed the
/// stylesheet that carries the tint, the platform parses and rasterises the document in the background.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once by <see cref="SvgImageSourceFactory"/> (lazily, on the first SVG icon) through
/// <see cref="PlatformContract"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.CommandBar.Skia.IconRasterizationSkiaPlatform</c> (assembly CodeBrix.Platform.UI.CommandBar),
/// which hands the work to the Svg add-in over CodeBrix.SkiaSvg, and is registered by that assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IIconRasterizationPlatform
{
	/// <summary>
	/// Creates one SVG image source and starts loading it.
	/// </summary>
	/// <param name="artwork">The artwork's URI, used only when <paramref name="document"/> is null; null when there is
	/// nothing to load.</param>
	/// <param name="document">The SVG document itself (UTF-8), when the icon's artwork was written inline or found as
	/// an embedded resource; otherwise null.</param>
	/// <param name="size">The icon's edge length in LOGICAL pixels. The platform multiplies it by the display scale
	/// to get the bitmap it rasterises.</param>
	/// <param name="css">The stylesheet carrying the tint, or null. It must be in place before the document is
	/// parsed.</param>
	/// <returns>The image source, which loads in the background.</returns>
	SvgImageSource CreateSvgImageSource(Uri? artwork, byte[]? document, double size, string? css);
}
