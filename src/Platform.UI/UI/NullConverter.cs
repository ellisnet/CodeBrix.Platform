using System;
using System.Globalization;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters //Was previously: Uno.UI.Converters
{
	/// <summary>
	/// A converter which always return null.
	/// </summary>
	internal class NullConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			this.Log().Warn("Convert a value using the NullConverter (Usually you get this when you specify a converter on a binding, and it does not implement IValueConverter).");

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			this.Log().Warn("Convert BACK a value using the NullConverter (Usually you get this when you specify a converter on a binding, and it does not implement IValueConverter).");

			return null;
		}
	}
}
