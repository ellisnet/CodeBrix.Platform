using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests; //Was previously: Uno.UI.RuntimeTests.Tests

public sealed partial class TargetNullValueThemeResource : Page
{
	public TargetNullValueThemeResource()
	{
		this.InitializeComponent();
		myBtn.DataContext = myBtn;
	}
}
