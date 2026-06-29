using SkiaSharp;
using CodeBrix.Platform.UI.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		internal override SKPath GetSKPath()
		{
			using var path = new SKPathBuilder();

			foreach (var geometry in Children)
			{
				var geometryPath = geometry.GetSKPath();
				path.AddPath(geometryPath);
			}

			path.FillType = FillRule.ToSkiaFillType();
			return path.Snapshot();
		}
	}
}
