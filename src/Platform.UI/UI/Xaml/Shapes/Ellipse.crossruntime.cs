#if !__NETSTD_REFERENCE__
using Windows.Foundation;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Contracts;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Ellipse : Shape
	{
		public Ellipse()
		{
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var (_, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null);

			return finalSize;
		}

		private IGeometrySource2D GetGeometry(Rect renderingArea)
			=> PlatformServices.Geometry.CreateEllipse(renderingArea);
	}
}
#endif
