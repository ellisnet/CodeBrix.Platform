#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Input; //Was previously: Uno.UI.Xaml.Input

internal struct TabStopProcessingResult
{
	public TabStopProcessingResult(bool isOverriden, DependencyObject? newTabStop)
	{
		IsOverriden = isOverriden;
		NewTabStop = newTabStop;
	}

	public bool IsOverriden { get; set; }

	public DependencyObject? NewTabStop { get; set; }
}
