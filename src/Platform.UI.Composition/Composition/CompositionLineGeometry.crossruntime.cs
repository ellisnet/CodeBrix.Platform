#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLineGeometry : CompositionGeometry
	{
		private IGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private IGeometrySource2D? InternalBuildGeometry()
			=> CompositionPlatformServices.Geometry.CreateLine(Start, End);

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Start) or nameof(End))
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
