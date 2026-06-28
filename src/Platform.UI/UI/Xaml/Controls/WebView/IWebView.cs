using Windows.UI.Core;

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

internal interface IWebView
{
	bool SwitchSourceBeforeNavigating { get; }

	bool IsLoaded { get; }

	CoreDispatcher Dispatcher { get; }
}
