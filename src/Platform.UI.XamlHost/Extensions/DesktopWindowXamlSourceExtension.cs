#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace CodeBrix.Platform.UI.XamlHost.Extensions; //Was previously: Uno.UI.XamlHost.Extensions

public static class DesktopWindowXamlSourceExtension
{
	public static UIElement? GetVisualTreeRoot(this DesktopWindowXamlSource source) =>
		source.Content?.XamlRoot?.VisualTree.RootElement;
}
