#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Converts the null-ness of a binding value to a <see cref="Visibility"/>: a non-null
/// value becomes <see cref="Visibility.Visible"/> and <see langword="null"/> becomes
/// <see cref="Visibility.Collapsed"/> - i.e. "show this element once its data exists".
/// Set <see cref="Invert"/> to flip the mapping ("show this placeholder until the data
/// exists"). Only <see langword="null"/> itself counts as null: an empty string or an
/// empty collection is a value, and is Visible by default.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:NullToVisibilityConverter x:Key="VisibleWhenSet" /&gt;
///     &lt;conv:NullToVisibilityConverter x:Key="VisibleWhenNull" Invert="True" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;!-- A loading placeholder that hides once the thumbnail arrives --&gt;
/// &lt;TextBlock Text="…" Visibility="{Binding Thumbnail, Converter={StaticResource VisibleWhenNull}}" /&gt;
/// &lt;Image Source="{Binding Thumbnail}" /&gt;
/// </code>
/// </example>
public class NullToVisibilityConverter : IValueConverter
{
	/// <summary>
	/// When <see langword="true"/>, flips the mapping: <see langword="null"/> becomes
	/// <see cref="Visibility.Visible"/> and a non-null value becomes
	/// <see cref="Visibility.Collapsed"/>. Defaults to <see langword="false"/>.
	/// </summary>
	public bool Invert { get; set; }

	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, string? language)
		=> ((value != null) != Invert) ? Visibility.Visible : Visibility.Collapsed;

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown; the original value cannot be
	/// reconstructed from its null-ness.</exception>
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> throw new NotSupportedException(
			$"{nameof(NullToVisibilityConverter)} does not support two-way bindings.");
}
