using System;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationViewItem
{
	// For perf considerations, we defer the pressed and over visual state on Uno.
	// This highly improves scrolling experience by avoiding freeze of UI thread (due to measure/arrange)
	// at the begging of the scroll, or when flicking during scroll.
	// Note: This is enabled only if flag CODEBRIX_USE_DEFERRED_VISUAL_STATES is set in NavigationViewItem.cs

	private bool _codebrix_isDefferingOverState = false;
	private bool _codebrix_isDefferingPressedState = false;

#pragma warning disable CS0649 // Field 'NavigationViewItem._uno_pointerDeferring' is never assigned to, and will always have its default value null
	private DispatcherQueueTimer _codebrix_pointerDeferring;
#pragma warning restore CS0649 // Field 'NavigationViewItem._uno_pointerDeferring' is never assigned to, and will always have its default value null

#if false
	private void DeferUpdateVisualStateForPointer()
	{
		// Note: As we use only one timer for both pressed and over state, we stop this timer only if cancelled / capture lost
		//		 Other cases will be handle the "normal" way using the m_isPointerOver and m_isPressed flags.

		if (_codebrix_isDefferingOverState || _codebrix_isDefferingPressedState)
		{
			if (_codebrix_pointerDeferring is null)
			{
				_codebrix_pointerDeferring = global::Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
				_codebrix_pointerDeferring.Interval = TimeSpan.FromMilliseconds(200);
				_codebrix_pointerDeferring.IsRepeating = false;
				_codebrix_pointerDeferring.Tick += (snd, e) =>
				{
					if (_codebrix_isDefferingOverState || _codebrix_isDefferingPressedState)
					{
						_codebrix_isDefferingOverState = false;
						_codebrix_isDefferingPressedState = false;
						UpdateVisualStateForPointer();
					}
				};
			}

			if (!_codebrix_pointerDeferring.IsRunning)
			{
				_codebrix_pointerDeferring.Start();
			}
		}
	}
#endif
}
