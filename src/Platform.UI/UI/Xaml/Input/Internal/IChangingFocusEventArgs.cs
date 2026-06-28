#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Input //Was previously: Uno.UI.Xaml.Input
{
	internal interface IChangingFocusEventArgs
	{
		DependencyObject? NewFocusedElement { get; set; }

		bool Cancel { get; }
	}
}
