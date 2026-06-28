using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup; //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup

public sealed partial class MarkupExtension_FullNameFirst : Page
{
	public MarkupExtension_FullNameFirst()
	{
		this.InitializeComponent();
	}
}

public class FullNameFirstMarkupExtension : MarkupExtension
{
	protected override object ProvideValue() => nameof(FullNameFirstMarkupExtension);
}

public class FullNameFirstMarkup : MarkupExtension
{
	protected override object ProvideValue() => nameof(FullNameFirstMarkup);
}

public class ShortNameMarkup : MarkupExtension
{
	protected override object ProvideValue() => nameof(ShortNameMarkup);
}
