#if !__NETSTD_REFERENCE__
#nullable enable

using CodeBrix.Platform.UI.Contracts;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Media
{
	partial class Geometry
	{
		/// <summary>
		/// Builds the outline of this geometry as a new platform path (see <see cref="IGeometryPlatform.CreateGeometrySource"/>).
		/// </summary>
		internal IGeometrySource2D GetGeometrySource2D() => PlatformServices.Geometry.CreateGeometrySource(this);

		/// <summary>
		/// Builds the filled part of this geometry as a new platform path, or returns <see langword="null"/> when the
		/// geometry has no separate filled outline (see <see cref="IGeometryPlatform.CreateFilledGeometrySource"/>).
		/// </summary>
		/// <remarks>
		/// Note: Try not to depend on this. See the note on <c>CompositionSpriteShape.FillGeometry</c>.
		/// </remarks>
		internal IGeometrySource2D? GetFilledGeometrySource2D() => PlatformServices.Geometry.CreateFilledGeometrySource(this);
	}
}
#endif
