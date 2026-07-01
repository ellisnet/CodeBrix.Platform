#nullable enable

using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandNativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal WaylandNativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
		=> new WaylandWindowWrapper(window, xamlRoot);
}
