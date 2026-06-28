using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests; //Was previously: Uno.UI.RuntimeTests.Tests

public sealed partial class XBindConstPage : Page
{
	private const double MyWidth = 200;
	private const double MyHeight = 200;

	public XBindConstPage()
	{
		this.InitializeComponent();
	}

	public Border XBoundBorder => BoundBorder;
}
