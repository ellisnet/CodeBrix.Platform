#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Hosting; //Was previously: Uno.UI.Runtime.Skia.Wpf.Hosting

internal interface IWpfApplicationHost : ISkiaApplicationHost
{
	bool IgnorePixelScaling { get; }
}
