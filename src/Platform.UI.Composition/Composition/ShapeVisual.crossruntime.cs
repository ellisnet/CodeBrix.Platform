#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Linq;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	internal bool _needsContinuousUpdates;

	/// <summary>
	/// Gets the shape collection if it has been created, without creating it (<see cref="Shapes"/> creates it).
	/// </summary>
	internal CompositionShapeCollection? ShapesIfCreated => _shapes;

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		if (propertyName == nameof(Shapes))
		{
			_needsContinuousUpdates = _shapes?.OfType<CompositionSpriteShape>().Any(s => s.FillBrush?.RequiresRepaintOnEveryFrame ?? false) ?? false;
		}
	}

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal override bool HitTest(Point point)
	{
		if (_shapes is null)
		{
			return false;
		}

		foreach (var shape in _shapes)
		{
			if (shape.HitTest(point))
			{
				return true;
			}
		}

		// Do not check the child visuals. On WinUI, if you add a child visual (e.g. using ContainerVisual.Children.InsertAtTop),
		// the child doesn't factor at all in hit-testing. The children of the UIElement that owns this visual will be checked
		// separately in VisualTreeHelper.HitTest

		return false;
	}
}
#endif
