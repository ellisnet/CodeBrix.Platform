#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Converts a <see cref="bool"/> (or nullable <see cref="bool"/>) binding value to a
/// <see cref="Visibility"/>: <see langword="true"/> becomes <see cref="Visibility.Visible"/>;
/// <see langword="false"/> - and also <see langword="null"/> or any non-boolean value -
/// becomes <see cref="Visibility.Collapsed"/>. Set <see cref="Invert"/> to flip the mapping.
/// Supports two-way bindings: <see cref="ConvertBack"/> maps the <see cref="Visibility"/>
/// back to a <see cref="bool"/> honoring the same <see cref="Invert"/> setting.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:BoolToVisibilityConverter x:Key="VisibleWhenTrue" /&gt;
///     &lt;conv:BoolToVisibilityConverter x:Key="VisibleWhenFalse" Invert="True" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;ProgressBar Visibility="{Binding IsBusy, Converter={StaticResource VisibleWhenTrue}}" /&gt;
/// </code>
/// </example>
public class BoolToVisibilityConverter : IValueConverter
{
	/// <summary>
	/// When <see langword="true"/>, flips the mapping: <see langword="true"/> becomes
	/// <see cref="Visibility.Collapsed"/> and <see langword="false"/> (or null, or a
	/// non-boolean value) becomes <see cref="Visibility.Visible"/>. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	public bool Invert { get; set; }

	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, string? language)
	{
		var isTrue = value is bool boolValue && boolValue;
		return (isTrue != Invert) ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
	{
		var isVisible = value is Visibility visibility && visibility == Visibility.Visible;
		return isVisible != Invert;
	}
}
