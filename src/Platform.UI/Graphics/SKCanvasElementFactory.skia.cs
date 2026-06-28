using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Graphics; //Was previously: Uno.UI.Graphics

internal class SKCanvasVisualFactory : SKCanvasVisualBaseFactory
{
	public SKCanvasVisualFactory() { }

	public SKCanvasVisualBase CreateInstance(Action<object, Size> renderCallback, Compositor compositor) => new SKCanvasVisual(renderCallback, compositor);
}
