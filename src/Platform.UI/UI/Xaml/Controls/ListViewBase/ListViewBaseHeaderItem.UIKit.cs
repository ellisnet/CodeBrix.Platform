using System;
using CodeBrix.Platform.UI.Views.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewBaseHeaderItem : ContentControl
	{
		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			SetNeedsLayout();
		}
	}
}

