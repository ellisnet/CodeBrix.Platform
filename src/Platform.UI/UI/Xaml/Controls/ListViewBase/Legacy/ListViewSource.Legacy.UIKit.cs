using System;
using System.Linq;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;

using Foundation;
using UIKit;
using CoreGraphics;

namespace CodeBrix.Platform.UI.Controls.Legacy //Was previously: Uno.UI.Controls.Legacy
{
	public partial class ListViewSource : ListViewBaseSource
	{
		public ListViewSource(ListView owner) : base(owner)
		{
		}

		protected override SelectorItem CreateSelectorItem()
		{
			return new ListViewItem() { ShouldHandlePressed = false };
		}

		protected override ListViewBaseHeaderItem CreateSectionHeaderItem()
		{
			return new ListViewHeaderItem();
		}
	}
}

