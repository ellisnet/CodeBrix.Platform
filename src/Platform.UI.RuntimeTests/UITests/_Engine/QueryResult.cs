using Microsoft.UI.Xaml;
using CodeBrix.Platform.UI.Extensions;
using Windows.Foundation;

namespace CodeBrix.Platform.UITest.Helpers.Queries; //Was previously: Uno.UITest.Helpers.Queries

internal class QueryResult
{
	private readonly FrameworkElement _element;

	public QueryResult(FrameworkElement element)
	{
		_element = element;
	}

	public FrameworkElement Element => _element;

	public IAppRect Rect => new AppRectAdapter(_element.TransformToVisual(null).TransformBounds(new Rect(default, _element.RenderSize)));

#if HAS_CODEBRIX
	/// <inheritdoc />
	public override string ToString()
		=> Element.GetDebugIdentifier();
#endif

	private sealed class AppRectAdapter : IAppRect
	{
		private readonly Rect _rect;

		public AppRectAdapter(Rect rect)
		{
			_rect = rect;
		}

		public float Width => (float)_rect.Width;
		public float Height => (float)_rect.Height;
		public float X => (float)_rect.X;
		public float Y => (float)_rect.Y;
		public float CenterX => (float)(_rect.X + _rect.Width / 2);
		public float CenterY => (float)(_rect.Y + _rect.Height / 2);
		public float Right => (float)(_rect.X + _rect.Width);
		public float Bottom => (float)(_rect.Y + _rect.Height);
	}
}
