#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Input //Was previously: Uno.UI.Xaml.Input
{
	internal struct ChangingFocusEventRaiseResult
	{
		public ChangingFocusEventRaiseResult(bool canceled, DependencyObject? finalGettingFocusElement = null)
		{
			Canceled = canceled;
			FinalGettingFocusElement = finalGettingFocusElement;
		}

		public bool Canceled { get; }

		public DependencyObject? FinalGettingFocusElement { get; }
	}
}
