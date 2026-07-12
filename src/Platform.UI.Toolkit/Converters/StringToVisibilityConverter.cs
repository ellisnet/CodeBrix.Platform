#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Converts a string binding value to a <see cref="Visibility"/> by its content: a
/// non-empty string becomes <see cref="Visibility.Visible"/>; <see langword="null"/> or an
/// empty string becomes <see cref="Visibility.Collapsed"/> - i.e. "show this element only
/// when there is text". Set <see cref="Invert"/> to flip the mapping. A non-string,
/// non-null value counts as having content. Whitespace counts as content; trim in the
/// view model if whitespace should hide the element.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:StringToVisibilityConverter x:Key="VisibleWhenText" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;!-- The error banner only exists while there is an error message --&gt;
/// &lt;TextBlock Text="{Binding ErrorMessage}"
///            Visibility="{Binding ErrorMessage, Converter={StaticResource VisibleWhenText}}" /&gt;
/// </code>
/// </example>
public class StringToVisibilityConverter : IValueConverter
{
	/// <summary>
	/// When <see langword="true"/>, flips the mapping: <see langword="null"/> or an empty
	/// string becomes <see cref="Visibility.Visible"/> and a non-empty string becomes
	/// <see cref="Visibility.Collapsed"/>. Defaults to <see langword="false"/>.
	/// </summary>
	public bool Invert { get; set; }

	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, string? language)
	{
		var hasContent = value is string stringValue ? stringValue.Length > 0 : value != null;
		return (hasContent != Invert) ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown; the original string cannot be
	/// reconstructed from its visibility.</exception>
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> throw new NotSupportedException(
			$"{nameof(StringToVisibilityConverter)} does not support two-way bindings.");
}
