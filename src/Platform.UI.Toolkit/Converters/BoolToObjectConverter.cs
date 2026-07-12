#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Converts a <see cref="bool"/> binding value to one of two arbitrary values:
/// <see cref="TrueValue"/> when the value is <see langword="true"/>, otherwise
/// <see cref="FalseValue"/> (a <see langword="null"/> or non-boolean value counts as
/// <see langword="false"/>). The general-purpose sibling of
/// <see cref="BoolToVisibilityConverter"/> - use it for brushes, strings, glyphs,
/// opacities and anything else that flips with a flag. Two-way bindings are supported:
/// <see cref="ConvertBack"/> returns <see langword="true"/> when the value equals
/// <see cref="TrueValue"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:BoolToObjectConverter x:Key="OnlineBrush" TrueValue="Green" FalseValue="Gray" /&gt;
///     &lt;conv:BoolToObjectConverter x:Key="OnlineText" TrueValue="Online" FalseValue="Offline" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;Ellipse Fill="{Binding IsOnline, Converter={StaticResource OnlineBrush}}" /&gt;
/// &lt;TextBlock Text="{Binding IsOnline, Converter={StaticResource OnlineText}}" /&gt;
/// </code>
/// </example>
public class BoolToObjectConverter : IValueConverter
{
	/// <summary>The value returned when the bound value is <see langword="true"/>.</summary>
	public object? TrueValue { get; set; }

	/// <summary>
	/// The value returned when the bound value is <see langword="false"/>,
	/// <see langword="null"/> or not a <see cref="bool"/>.
	/// </summary>
	public object? FalseValue { get; set; }

	/// <inheritdoc />
	public object? Convert(object? value, Type targetType, object? parameter, string? language)
		=> (value is bool boolValue && boolValue) ? TrueValue : FalseValue;

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> Equals(value, TrueValue);
}
