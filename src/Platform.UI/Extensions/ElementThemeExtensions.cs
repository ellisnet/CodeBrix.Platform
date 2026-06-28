using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Extensions //Was previously: Uno.UI.Extensions
{
	internal static class ElementThemeExtensions
	{
		public static ApplicationTheme? ToApplicationThemeOrDefault(this ElementTheme elementTheme)
			=> elementTheme switch
			{
				ElementTheme.Default => null,
				ElementTheme.Light => ApplicationTheme.Light,
				ElementTheme.Dark => ApplicationTheme.Dark,
				_ => throw new ArgumentException()
			};
	}
}
