using System;
using System.Linq;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

using Foundation;
using UIKit;
using CoreGraphics;

namespace CodeBrix.Platform.UI.Controls.Legacy //Was previously: Uno.UI.Controls.Legacy
{
	public partial class GridViewSource : ListViewBaseSource
	{
		public GridViewSource(GridView owner = null) : base(owner)
		{

		}
		protected override SelectorItem CreateSelectorItem()
		{
			return new GridViewItem() { ShouldHandlePressed = false };
		}

		protected override ListViewBaseHeaderItem CreateSectionHeaderItem()
		{
			return new GridViewHeaderItem();
		}
	}
}

