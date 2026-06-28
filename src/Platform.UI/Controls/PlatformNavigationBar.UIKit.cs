using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Microsoft.UI.Xaml;
using Foundation;
using UIKit;
using ObjCRuntime;

namespace CodeBrix.Platform.UI.Controls //Was previously: Uno.UI.Controls
{
	public partial class CodeBrixNavigationBar : UINavigationBar, DependencyObject
	{
		internal event Action SizeChanged;

		public CodeBrixNavigationBar()
		{
			InitializeBinder();
		}

		public CodeBrixNavigationBar(CGRect frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public CodeBrixNavigationBar(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public CodeBrixNavigationBar(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public CodeBrixNavigationBar(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				if (value != Frame)
				{
					base.Frame = value;
					SizeChanged?.Invoke();
				}
			}
		}

		public override CGRect Bounds
		{
			get => base.Bounds;
			set
			{
				if (value != Bounds)
				{
					base.Bounds = value;
					SizeChanged?.Invoke();
				}
			}
		}
	}
}
