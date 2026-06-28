#nullable enable

using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Xaml.Core;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Xaml.Islands; //Was previously: Uno.UI.Xaml.Islands

partial class XamlIslandRoot : IRootElement
{
	private readonly CodeBrixRootElementLogic _rootElementLogic;

	void IRootElement.SetBackgroundColor(Color backgroundColor) =>
		SetValue(Panel.BackgroundProperty, new SolidColorBrush(backgroundColor));
}
