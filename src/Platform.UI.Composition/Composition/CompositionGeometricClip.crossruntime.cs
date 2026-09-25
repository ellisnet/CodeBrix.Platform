#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class CompositionGeometricClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		if (Geometry is not null)
		{
			var geometry = Geometry.BuildGeometry();

			return CompositionPlatformServices.Geometry.GetClipBounds(geometry);
		}

		return null;
	}
}
#endif
