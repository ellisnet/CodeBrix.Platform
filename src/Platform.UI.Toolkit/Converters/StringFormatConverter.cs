#nullable enable

using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Formats a binding value into a string using a standard .NET composite format string
/// supplied as the <c>ConverterParameter</c> (the binding value is argument <c>{0}</c>).
/// With no parameter, the value's <see cref="object.ToString"/> is returned. A
/// <see langword="null"/> value converts to <see langword="null"/> so target-null styling
/// (e.g. <c>TargetNullValue</c>) still applies. Formatting uses
/// <see cref="CultureInfo.CurrentCulture"/>; an invalid format string throws
/// <see cref="FormatException"/> - fail fast rather than silently showing wrong text.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:StringFormatConverter x:Key="Format" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;!-- Note the {} escape so XAML doesn't read {0} as a markup extension --&gt;
/// &lt;TextBlock Text="{Binding DownloadCount, Converter={StaticResource Format},
///                    ConverterParameter='{}{0:N0} downloads'}" /&gt;
/// </code>
/// </example>
public class StringFormatConverter : IValueConverter
{
	/// <inheritdoc />
	public object? Convert(object? value, Type targetType, object? parameter, string? language)
	{
		if (value == null) { return null; }

		return parameter is string format && format.Length > 0
			? string.Format(CultureInfo.CurrentCulture, format, value)
			: value.ToString();
	}

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown; formatted text cannot be
	/// reliably parsed back to the source value.</exception>
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> throw new NotSupportedException(
			$"{nameof(StringFormatConverter)} does not support two-way bindings.");
}
