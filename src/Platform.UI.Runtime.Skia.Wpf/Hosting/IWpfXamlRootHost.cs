#nullable enable

using CodeBrix.Platform.UI.Hosting;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Hosting; //Was previously: Uno.UI.Runtime.Skia.Wpf.Hosting

internal interface IWpfXamlRootHost : IXamlRootHost
{
	WpfCanvas? NativeOverlayLayer { get; }

	bool IgnorePixelScaling { get; }

	RenderSurfaceType? RenderSurfaceType { get; }
}
