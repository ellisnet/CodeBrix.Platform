#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The platform side of the imaging pipeline: decoding and measuring encoded images, the backing stores of
/// <see cref="WriteableBitmap"/> and <see cref="RenderTargetBitmap"/>, and turning platform images (a rendered
/// element, a vector picture from the SVG provider) into composition surfaces. The <c>ImageSource</c> pipeline
/// (opening, subscriptions, caching, <see cref="ImageData"/>) stays platform-neutral; decoded images cross this
/// contract inside a <see cref="PlatformCompositionSurface"/>, the composition surface every image source draws from.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once through <see cref="PlatformServices.Imaging"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Skia.ImagingSkiaPlatform</c> (<c>SKCodec</c>, <c>SKImage</c>, <c>SKSurface</c>),
/// registered by <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>. The SVG provider itself is still found
/// through the <see cref="CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg.ISvgProvider"/> registration.
/// </para>
/// </remarks>
internal interface IImagingPlatform
{
	/// <summary>
	/// On a browser, decodes <paramref name="encodedImage"/> with the browser's own image decoder. Only called when
	/// the application runs in a browser.
	/// </summary>
	/// <param name="encodedImage">The encoded image.</param>
	/// <returns>
	/// The decoded image (or the failure), or <see langword="null"/> when the browser could not decode it and the
	/// caller should decode it into a composition surface itself.
	/// </returns>
	Task<ImageData?> TryDecodeWithBrowserAsync(byte[] encodedImage);

	/// <summary>
	/// Reads the pixel size of an encoded image without decoding its pixels.
	/// </summary>
	/// <param name="encodedImage">The encoded image.</param>
	/// <returns>The width and height in pixels.</returns>
	(int Width, int Height) GetEncodedImageSize(Stream encodedImage);

	/// <summary>
	/// Decodes an encoded image into BGRA pixels (four bytes each).
	/// </summary>
	/// <param name="encodedImage">The encoded image.</param>
	/// <param name="destination">The pixel buffer to fill.</param>
	/// <param name="rowBytes">The number of bytes per row of <paramref name="destination"/>.</param>
	void DecodeToBgra8(Stream encodedImage, Span<byte> destination, int rowBytes);

	/// <summary>
	/// Wraps a copy of premultiplied BGRA pixels into an image.
	/// </summary>
	/// <param name="pixels">The first pixel.</param>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <returns>The image, or the failure.</returns>
	ImageData CreateImageFromBgra8Premul(IntPtr pixels, int width, int height);

	/// <summary>
	/// Renders <paramref name="element"/> (its whole render size, at the current rasterization scale) into
	/// premultiplied BGRA pixels, growing <paramref name="buffer"/> when needed.
	/// </summary>
	/// <param name="element">The element to render.</param>
	/// <param name="buffer">The pixel buffer, replaced by a larger one when it is too small.</param>
	/// <param name="scaledSize">The size to scale the rendered pixels to, or <see langword="null"/> to keep them.</param>
	/// <returns>The number of bytes written, and the pixel size.</returns>
	(int ByteCount, int Width, int Height) RenderToBgra8Premul(UIElement element, ref RenderTargetBitmap.UnmanagedArrayOfBytes? buffer, Size? scaledSize);

	/// <summary>
	/// Rasterizes a vector picture produced by the SVG provider into a new composition surface.
	/// </summary>
	/// <param name="picture">The picture returned by the SVG provider.</param>
	/// <param name="size">The pixel size of the picture.</param>
	/// <returns>The surface, or <see langword="null"/> when <paramref name="picture"/> is not a picture of this platform.</returns>
	PlatformCompositionSurface? CreateSurfaceFromPicture(object picture, Size size);

	/// <summary>
	/// Reads the pixel size of the image a composition surface holds.
	/// </summary>
	/// <param name="surface">The surface.</param>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <returns><see langword="true"/> when the surface holds an image.</returns>
	bool TryGetImageSize(PlatformCompositionSurface surface, out int width, out int height);

	/// <summary>
	/// Releases the image a composition surface holds, if any.
	/// </summary>
	/// <param name="surface">The surface.</param>
	void DisposeImage(PlatformCompositionSurface surface);
}
