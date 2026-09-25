using System;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The platform's pixel store and image encoders behind <see cref="SoftwareBitmap"/> and
/// <see cref="BitmapEncoder"/>. A bitmap crosses this contract as an opaque platform object (on Skia, an
/// <c>SKBitmap</c>) that <see cref="SoftwareBitmap"/> holds and never looks into.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once by <see cref="SoftwareBitmap"/> and once by <see cref="BitmapEncoder"/> (lazily, on first use)
/// through <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.Skia.GraphicsImagingSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IGraphicsImagingPlatform
{
	/// <summary>
	/// Creates an empty bitmap with the platform's default alpha mode.
	/// </summary>
	/// <param name="format">The pixel format.</param>
	/// <param name="width">The width, in pixels.</param>
	/// <param name="height">The height, in pixels.</param>
	/// <returns>The platform bitmap.</returns>
	/// <exception cref="NotSupportedException"><paramref name="format"/> has no platform equivalent.</exception>
	object CreateBitmap(BitmapPixelFormat format, int width, int height);

	/// <summary>
	/// Creates an empty bitmap.
	/// </summary>
	/// <param name="format">The pixel format.</param>
	/// <param name="width">The width, in pixels.</param>
	/// <param name="height">The height, in pixels.</param>
	/// <param name="alphaMode">The alpha mode.</param>
	/// <returns>The platform bitmap.</returns>
	/// <exception cref="NotSupportedException"><paramref name="format"/> or <paramref name="alphaMode"/> has no platform equivalent.</exception>
	object CreateBitmap(BitmapPixelFormat format, int width, int height, BitmapAlphaMode alphaMode);

	/// <summary>
	/// Creates a bitmap over <paramref name="pixels"/>, which the bitmap keeps pinned and uses as its pixel store.
	/// </summary>
	/// <param name="pixels">The pixel data, laid out for <paramref name="format"/>, <paramref name="width"/> and <paramref name="height"/>.</param>
	/// <param name="format">The pixel format of <paramref name="pixels"/>.</param>
	/// <param name="alphaMode">The alpha mode of <paramref name="pixels"/>.</param>
	/// <param name="width">The width, in pixels.</param>
	/// <param name="height">The height, in pixels.</param>
	/// <returns>The platform bitmap, or <see langword="null"/> when the platform cannot install the pixels.</returns>
	/// <exception cref="NotSupportedException"><paramref name="format"/> or <paramref name="alphaMode"/> has no platform equivalent.</exception>
	object CreateBitmapFromPixels(byte[] pixels, BitmapPixelFormat format, BitmapAlphaMode alphaMode, int width, int height);

	/// <summary>
	/// Returns the pixel format of a platform bitmap.
	/// </summary>
	/// <param name="bitmap">The platform bitmap.</param>
	/// <returns>The pixel format.</returns>
	BitmapPixelFormat GetPixelFormat(object bitmap);

	/// <summary>
	/// Returns the alpha mode of a platform bitmap.
	/// </summary>
	/// <param name="bitmap">The platform bitmap.</param>
	/// <returns>The alpha mode.</returns>
	BitmapAlphaMode GetAlphaMode(object bitmap);

	/// <summary>
	/// Returns the width of a platform bitmap.
	/// </summary>
	/// <param name="bitmap">The platform bitmap.</param>
	/// <returns>The width, in pixels.</returns>
	int GetPixelWidth(object bitmap);

	/// <summary>
	/// Returns the height of a platform bitmap.
	/// </summary>
	/// <param name="bitmap">The platform bitmap.</param>
	/// <returns>The height, in pixels.</returns>
	int GetPixelHeight(object bitmap);

	/// <summary>
	/// Draws <paramref name="source"/> onto <paramref name="destination"/> at the origin.
	/// </summary>
	/// <param name="source">The platform bitmap to copy from.</param>
	/// <param name="destination">The platform bitmap to copy into.</param>
	void CopyPixels(object source, object destination);

	/// <summary>
	/// Creates an independent copy of a platform bitmap.
	/// </summary>
	/// <param name="source">The platform bitmap to copy.</param>
	/// <returns>The new platform bitmap.</returns>
	object CopyBitmap(object source);

	/// <summary>
	/// Releases a platform bitmap.
	/// </summary>
	/// <param name="bitmap">The platform bitmap.</param>
	void DisposeBitmap(object bitmap);

	/// <summary>
	/// Returns whether the platform can encode to the format that <paramref name="encoderId"/> names
	/// (one of the <see cref="BitmapEncoder"/> encoder ids).
	/// </summary>
	/// <param name="encoderId">The encoder id.</param>
	/// <returns><see langword="true"/> when <see cref="Encode"/> accepts <paramref name="encoderId"/>.</returns>
	bool IsEncoderSupported(Guid encoderId);

	/// <summary>
	/// Encodes a platform bitmap at full quality and writes the result to <paramref name="destination"/>.
	/// Writes nothing when <paramref name="bitmap"/> is <see langword="null"/> or the platform produces no data.
	/// </summary>
	/// <param name="bitmap">The platform bitmap, or <see langword="null"/>.</param>
	/// <param name="encoderId">An encoder id for which <see cref="IsEncoderSupported"/> returned <see langword="true"/>.</param>
	/// <param name="destination">The stream that receives the encoded image.</param>
	void Encode(object bitmap, Guid encoderId, IRandomAccessStream destination);
}
