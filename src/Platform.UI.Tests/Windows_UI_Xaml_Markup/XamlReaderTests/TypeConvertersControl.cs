using System;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests //Was previously: Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public class TypeConvertersControl : FrameworkElement
	{
		public Type TypeProperty { get; set; }

		public Uri UriProperty { get; set; }
	}
}
