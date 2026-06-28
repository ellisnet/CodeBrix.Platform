#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	void InvalidateRender();
}
