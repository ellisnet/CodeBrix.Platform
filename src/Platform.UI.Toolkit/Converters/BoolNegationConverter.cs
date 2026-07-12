#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Negates a <see cref="bool"/> binding value: <see langword="true"/> becomes
/// <see langword="false"/> and vice versa. A <see langword="null"/> or non-boolean value
/// is treated as <see langword="false"/>, so it converts to <see langword="true"/>.
/// Two-way bindings are supported; <see cref="ConvertBack"/> negates the same way.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:BoolNegationConverter x:Key="Negate" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;Button Content="Save" IsEnabled="{Binding IsBusy, Converter={StaticResource Negate}}" /&gt;
/// </code>
/// </example>
public class BoolNegationConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, string? language)
		=> !(value is bool boolValue && boolValue);

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> !(value is bool boolValue && boolValue);
}
