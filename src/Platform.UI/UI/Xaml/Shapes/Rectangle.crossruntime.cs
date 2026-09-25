#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using CodeBrix.Platform.UI.Contracts;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (_, renderingArea) = ArrangeRelativeShape(finalSize);
			var path = renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null;

			Render(path);

			return finalSize;
		}

		private IGeometrySource2D GetGeometry(Rect finalRect)
			=> PlatformServices.Geometry.CreateRectangle(finalRect, RadiusX, RadiusY);
	}
}
#endif
