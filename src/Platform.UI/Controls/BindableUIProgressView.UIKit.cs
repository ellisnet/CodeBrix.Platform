using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Views.Controls //Was previously: Uno.UI.Views.Controls
{
	public partial class BindableUIProgressView : UIProgressView, DependencyObject
	{
		public BindableUIProgressView()
		{
			InitializeBinder();
		}

		public Brush Foreground
		{
			get
			{
				return new SolidColorBrush(base.ProgressTintColor);
			}
			set
			{
				var scb = value as SolidColorBrush;

				if (scb != null)
				{
					base.ProgressTintColor = scb.Color;
				}
			}
		}

		public Brush Background
		{
			get
			{
				return new SolidColorBrush(base.BackgroundColor);
			}
			set
			{
				var scb = value as SolidColorBrush;

				if (scb != null)
				{
					base.TrackTintColor = scb.Color;
				}
			}
		}

		public override float Progress
		{
			get
			{
				return base.Progress;
			}
			set
			{
				base.SetProgress(value, true);
			}
		}
	}
}
