using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Views.Controls //Was previously: Uno.UI.Views.Controls
{
	public partial class StyleSelector2 : DependencyObject
	{
		Style _style;

		public Style Style
		{
			get
			{
				return _style;
			}
			set
			{
				_style = value;
			}
		}
	}
}
