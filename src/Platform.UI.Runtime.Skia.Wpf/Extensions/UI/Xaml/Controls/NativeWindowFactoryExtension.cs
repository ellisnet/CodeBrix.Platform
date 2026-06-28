#nullable enable

using System.DirectoryServices.ActiveDirectory;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.UI.Controls;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls; //Was previously: Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	internal NativeWindowFactoryExtension()
	{
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var codebrixWpfWindow = new CodeBrixWpfWindow(window, xamlRoot);
		return new WpfWindowWrapper(codebrixWpfWindow, window, xamlRoot);
	}
}
