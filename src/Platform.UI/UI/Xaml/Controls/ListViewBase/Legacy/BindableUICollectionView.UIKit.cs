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
	public partial class BindableUICollectionView : UICollectionView, DependencyObject
	{
		public BindableUICollectionView(RectangleF frame, UICollectionViewLayout layout)
			: base(frame, layout)
		{
			Initialize();
		}

		public BindableUICollectionView(NSCoder coder)
			 : base(coder)
		{
			Initialize();
		}

		public BindableUICollectionView(NSObjectFlag t)
			 : base(t)
		{
			Initialize();
		}

		public BindableUICollectionView(NativeHandle handle)
			 : base(handle)
		{
			Initialize();
		}

		void Initialize()
		{
			DelaysContentTouches = true;
		}
	}
}
