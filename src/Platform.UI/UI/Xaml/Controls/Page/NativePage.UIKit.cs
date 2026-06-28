using CodeBrix.Platform.UI.Views.Controls;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Extensions;
using System;
using CodeBrix.Platform.UI;
using UIKit;

namespace Microsoft.UI.Xaml.Controls
{
	public abstract class NativePage : UIViewController
	{
		public NativePage()
		{
			Initialize();
		}

		void Initialize()
		{
			AutomaticallyAdjustsScrollViewInsets = false;
			InitializeComponent();
		}

		/// <summary>
		/// Initializes the content of the current UIViewController.
		/// </summary>
		protected abstract void InitializeComponent();

		UIView _content;

		public UIView Content
		{
			get
			{
				return _content;
			}
			set
			{
				if (_content != value)
				{
					if (_content != null)
					{
						_content.RemoveFromSuperview();
					}

					_content = value;
					if (_content != null)
					{

						_content.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
						_content.Frame = View.Frame;

						View.AddSubview(_content);
						View.SetNeedsLayout();
					}
				}
			}
		}
	}
}
