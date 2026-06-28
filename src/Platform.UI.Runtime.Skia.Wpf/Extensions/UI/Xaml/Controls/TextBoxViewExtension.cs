#nullable enable

using CodeBrix.Platform.UI.Xaml.Controls.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls; //Was previously: Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls

internal class TextBoxViewExtension : OverlayTextBoxViewExtension
{
	public TextBoxViewExtension(TextBoxView owner) :
		base(owner, WpfTextBoxView.Create)
	{
	}

	public override bool IsOverlayLayerInitialized(XamlRoot xamlRoot) =>
		WpfTextBoxView.GetOverlayLayer(xamlRoot) is not null;
}
