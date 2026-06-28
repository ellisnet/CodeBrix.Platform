#nullable enable
#if !__SKIA__

using System;
using CodeBrix.Platform.Foundation.Extensibility;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

partial class NativeWindowFactory
{
	public static bool SupportsClosingCancellation => false;

	public static bool SupportsMultipleWindows => false;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		NativeWindowWrapper.Instance.SetWindow(window, xamlRoot);
		return NativeWindowWrapper.Instance;
	}
}
#endif
