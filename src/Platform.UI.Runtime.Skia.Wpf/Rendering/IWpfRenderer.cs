using System;
using System.Windows.Media;
using CodeBrix.Platform.UI.Rendering;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Rendering; //Was previously: Uno.UI.Runtime.Skia.Wpf.Rendering

internal interface IWpfRenderer : IRenderer, IDisposable
{
	bool TryInitialize();

	void Render(DrawingContext drawingContext);
}
