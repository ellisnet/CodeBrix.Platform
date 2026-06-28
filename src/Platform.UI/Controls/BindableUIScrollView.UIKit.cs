using System;
using System.Drawing;
using CodeBrix.Platform.Extensions.Disposables;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.UI.DataBinding;
using Microsoft.UI.Xaml;
using ObjCRuntime;

using Foundation;
using UIKit;

namespace CodeBrix.Platform.UI.Views.Controls //Was previously: Uno.UI.Views.Controls
{
	public partial class BindableUIScrollView : UIScrollView, DependencyObject
	{
		public BindableUIScrollView(NativeHandle handle)
			: base(handle)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(NSCoder coder)
			: base(coder)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(NSObjectFlag t)
			: base(t)
		{
			InitializeBinder();
		}

		public BindableUIScrollView(RectangleF frame)
			: base(frame)
		{
			InitializeBinder();
		}

		public BindableUIScrollView()
		{
			InitializeBinder();
		}
	}
}

