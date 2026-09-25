#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.IO;
using CodeBrix.Platform.UI.Composition.Contracts;

namespace Microsoft.UI.Composition
{
	internal partial class PlatformCompositionSurface : CompositionObject, ICompositionSurface
	{
		private ICompositionSurfacePlatform? _platform;

		/// <summary>
		/// Gets the platform image store of this surface, created the first time it is needed.
		/// </summary>
		internal ICompositionSurfacePlatform Platform => _platform ??= CompositionPlatformServices.Composition.CreateSurfacePlatform(this);

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
			=> Platform.LoadFromStream(targetWidth, targetHeight, imageStream);

		/// <summary>
		/// Copies the provided pixels to the composition surface
		/// </summary>
		internal void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
			=> Platform.CopyPixels(pixelWidth, pixelHeight, data);

		/// <summary>
		/// Raises the change of <paramref name="propertyName"/> on this surface; the platform image store calls it
		/// when the image it holds changes, which invalidates whatever renders the surface.
		/// </summary>
		/// <param name="propertyName">The name of the property that changed.</param>
		internal void RaiseSurfacePropertyChanged(string propertyName) => OnPropertyChanged(propertyName, isSubPropertyChange: false);

		~PlatformCompositionSurface()
		{
			Platform.Dispose();
		}
	}
}
#endif
