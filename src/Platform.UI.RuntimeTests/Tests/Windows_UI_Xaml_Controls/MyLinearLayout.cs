using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
#if true
	public partial class MyLinearLayout : StackPanel
	{ }
#else
	[ContentProperty(Name = "Child")]
	public partial class MyLinearLayout : Android.Widget.LinearLayout
	{
		public Android.Views.View Child
		{
			get => GetChildAt(0);
			set
			{
				RemoveAllViews();
				AddView(value);
			}
		}
		public MyLinearLayout() : base(CodeBrix.Platform.UI.ContextHelper.Current)
		{
			Orientation = Android.Widget.Orientation.Horizontal;
		}
	}
#endif
}
