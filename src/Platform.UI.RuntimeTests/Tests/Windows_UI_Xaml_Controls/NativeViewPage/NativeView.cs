using System;
using System.Collections.Generic;
using System.Text;
#if false
using _NativeBase = UIKit.UISwitch;
#elif false
using _NativeBase = AndroidX.AppCompat.Widget.AppCompatCheckBox;
#else
using _NativeBase = Microsoft.UI.Xaml.Controls.CheckBox; // No native views on other platforms
#endif

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class NativeView : _NativeBase
	{
#if false
		public NativeView() : base(ContextHelper.Current)
		{

		}
#endif
	}
}
