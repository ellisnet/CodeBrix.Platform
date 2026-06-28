using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32; //Was previously: Uno.UI.Runtime.Skia.Win32

internal class Win32NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot) => new Win32WindowWrapper(window, xamlRoot);
}
