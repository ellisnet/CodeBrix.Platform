#nullable enable

using CodeBrix.Platform.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

partial class ContentManager
{
	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement)
	{
		xamlRoot.InvalidateMeasure();
		xamlRoot.InvalidateArrange();
	}
}
