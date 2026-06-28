using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Graphics; //Was previously: Uno.UI.Graphics

internal interface SKCanvasVisualBaseFactory
{
	SKCanvasVisualBase CreateInstance(Action<object, Size> renderCallback, Compositor compositor);
}
