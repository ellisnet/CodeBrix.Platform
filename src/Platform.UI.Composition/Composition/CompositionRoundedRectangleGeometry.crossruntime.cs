#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Numerics;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private IGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private IGeometrySource2D? InternalBuildGeometry()
		{
			IGeometrySource2D? path;

			Vector2 cornerRadius = CornerRadius;
			if (cornerRadius.X == 0 || cornerRadius.Y == 0)
			{
				// Simple rectangle
				path = CompositionPlatformServices.Geometry.CreateRectangle(Offset, Size);
			}
			else
			{
				// Complex rectangle
				path = CompositionPlatformServices.Geometry.CreateRoundedRectangle(Offset, Size, CornerRadius);
			}

			return path;
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Offset) or nameof(Size) or nameof(CornerRadius))
			{
				(_geometrySource2D as IDisposable)?.Dispose();
				_geometrySource2D = InternalBuildGeometry();
			}

			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		}

		private protected override void DisposeInternal()
		{
			(_geometrySource2D as IDisposable)?.Dispose();
			base.DisposeInternal();
		}
	}
}
#endif
