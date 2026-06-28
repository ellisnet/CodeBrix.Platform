using System;
using CodeBrix.Platform.UI.Views.Controls;
using CodeBrix.Platform.UI.Contracts;

using Foundation;
using UIKit;

namespace CodeBrix.Platform.UI.Controls //Was previously: Uno.UI.Controls
{
	public class ViewControllerTransitioningDeligate : UIViewControllerTransitioningDelegate
	{
		readonly IUIViewControllerAnimatedTransitioning _showTransition;

		readonly IUIViewControllerAnimatedTransitioning _hideTransition;

		public ViewControllerTransitioningDeligate(IUIViewControllerAnimatedTransitioning show, IUIViewControllerAnimatedTransitioning hide)
		{
			this._showTransition = show;
			_hideTransition = hide;
		}

		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForPresentedController(UIViewController presented, UIViewController presenting, UIViewController source)
		{
			return _showTransition;
		}

		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
		{
			return _hideTransition;
		}
	}
}

