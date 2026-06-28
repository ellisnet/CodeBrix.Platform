using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.UserControlTests //Was previously: Uno.UI.Tests.Windows_UI_XAML_Controls.UserControlTests
{
	public sealed partial class UserControl_WriteOnlyProperty_UserControl : UserControl
	{
		public string Text
		{
			set => TextDisplay.Text = value;
		}

		public UserControl_WriteOnlyProperty_UserControl()
		{
			InitializeComponent();
		}
	}
}
