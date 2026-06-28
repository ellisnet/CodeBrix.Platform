using System;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.Extensions;

namespace CodeBrix.Platform.UI.Helpers //Was previously: Uno.UI.Helpers
{
#if false
	public static partial class ListViewHelper
	{
		public static NativeListViewBase GetNativePanel(ListViewBase lvb) => lvb.NativePanel;

#if false
		public static void InstantScrollToIndex(this ListViewBase lvb, int index, UIKit.UICollectionViewScrollPosition scrollPosition = UIKit.UICollectionViewScrollPosition.Top)
		{
			var indexPath = lvb.GetIndexPathFromIndex(index)?.ToNSIndexPath() ?? throw new IndexOutOfRangeException();

			lvb.NativePanel?.ScrollToItem(indexPath, scrollPosition, animated: false);
		}

		public static void SmoothScrollToIndex(this ListViewBase lvb, int index, UIKit.UICollectionViewScrollPosition scrollPosition = UIKit.UICollectionViewScrollPosition.Top)
		{
			var indexPath = lvb.GetIndexPathFromIndex(index)?.ToNSIndexPath() ?? throw new IndexOutOfRangeException();

			lvb.NativePanel?.ScrollToItem(indexPath, scrollPosition, animated: true);
		}
#elif false
		public static void InstantScrollToIndex(this ListViewBase lvb, int index)
		{
			lvb.NativePanel?.ScrollToPosition(index);
		}

		public static void SmoothScrollToIndex(this ListViewBase lvb, int index)
		{
			lvb.NativePanel?.SmoothScrollToPosition(index);
		}
#endif
	}
#endif
}
