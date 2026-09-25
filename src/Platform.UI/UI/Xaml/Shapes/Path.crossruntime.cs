#if !__NETSTD_REFERENCE__
#nullable enable
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Path : Shape
	{
		private CompositionPathGeometry? _fillGeometry;

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private IGeometrySource2D? GetPath() => Data?.GetGeometrySource2D();

		private protected override void Render(IGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null,
			double? renderOriginY = null)
		{
			base.Render(path, scaleX, scaleY, renderOriginX, renderOriginY);

			_fillGeometry ??= Visual.Compositor.CreatePathGeometry();
			SpriteShape.FillGeometry = _fillGeometry;
			if (Data?.GetFilledGeometrySource2D() is { } filledPath)
			{
				_fillGeometry.Path = new CompositionPath(filledPath);
			}
			else
			{
				_fillGeometry.Path = null;
			}
		}
	}
}
#endif
