#nullable enable

using System;
using System.IO;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform image store behind one <see cref="PlatformCompositionSurface"/>, the composition surface that image
/// sources, loaded image surfaces and writeable bitmaps draw from. It decodes images (including animated ones,
/// whose frames it advances on its own) and holds the decoded pixels. One instance per surface, created by
/// <see cref="ICompositionPlatform.CreateSurfacePlatform"/> the first time the surface needs it and kept in a
/// field. Disposed from the surface's finalizer, which releases the decoded image.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// When the image changes (a new decode, new pixels, or the next frame of an animated image) the implementation
/// raises the surface's property change through <c>PlatformCompositionSurface.RaiseSurfacePropertyChanged</c>, which
/// invalidates whatever renders the surface. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Composition.Skia.CompositionSurfaceSkiaPlatform</c> (an <c>SKImage</c> per frame),
/// created by <c>CompositionSkiaPlatform</c>.
/// </para>
/// </remarks>
internal interface ICompositionSurfacePlatform : IDisposable
{
	/// <summary>
	/// Decodes the image in <paramref name="imageStream"/> into the surface, replacing what it held.
	/// </summary>
	/// <param name="targetWidth">The width to decode to, or <see langword="null"/> for the image's own size.</param>
	/// <param name="targetHeight">The height to decode to, or <see langword="null"/> for the image's own size.</param>
	/// <param name="imageStream">The encoded image.</param>
	/// <returns>Whether the decode succeeded, and the platform's own result object (a status or a message).</returns>
	(bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream);

	/// <summary>
	/// Replaces what the surface held with a copy of premultiplied BGRA pixels.
	/// </summary>
	/// <param name="pixelWidth">The width in pixels.</param>
	/// <param name="pixelHeight">The height in pixels.</param>
	/// <param name="data">The pixels, four bytes each, rows packed.</param>
	void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data);
}
