#nullable enable

using System;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.Converters;

/// <summary>
/// Converts a collection binding value to a <see cref="Visibility"/> by its content: a
/// collection with at least one item becomes <see cref="Visibility.Visible"/>;
/// <see langword="null"/> or an empty collection becomes <see cref="Visibility.Collapsed"/>.
/// Set <see cref="Invert"/> to flip the mapping - the classic "show the 'no results'
/// message when the list is empty" case. Any <see cref="IEnumerable"/> works
/// (<see cref="ICollection"/> counts are used without enumerating); a non-enumerable,
/// non-null value counts as having content.
/// </summary>
/// <example>
/// <code>
/// &lt;Page.Resources&gt;
///     &lt;conv:CollectionToVisibilityConverter x:Key="VisibleWhenAny" /&gt;
///     &lt;conv:CollectionToVisibilityConverter x:Key="VisibleWhenEmpty" Invert="True" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;ListView ItemsSource="{Binding Results}"
///           Visibility="{Binding Results, Converter={StaticResource VisibleWhenAny}}" /&gt;
/// &lt;TextBlock Text="No results found."
///            Visibility="{Binding Results, Converter={StaticResource VisibleWhenEmpty}}" /&gt;
/// </code>
/// </example>
public class CollectionToVisibilityConverter : IValueConverter
{
	/// <summary>
	/// When <see langword="true"/>, flips the mapping: <see langword="null"/> or an empty
	/// collection becomes <see cref="Visibility.Visible"/> and a collection with items
	/// becomes <see cref="Visibility.Collapsed"/>. Defaults to <see langword="false"/>.
	/// </summary>
	public bool Invert { get; set; }

	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, string? language)
		=> (HasItems(value) != Invert) ? Visibility.Visible : Visibility.Collapsed;

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown; the original collection cannot
	/// be reconstructed from its visibility.</exception>
	public object ConvertBack(object? value, Type targetType, object? parameter, string? language)
		=> throw new NotSupportedException(
			$"{nameof(CollectionToVisibilityConverter)} does not support two-way bindings.");

	private static bool HasItems(object? value)
	{
		switch (value)
		{
			case null:
				return false;
			case ICollection collection:
				return collection.Count > 0;
			case IEnumerable enumerable:
			{
				var enumerator = enumerable.GetEnumerator();
				try
				{
					return enumerator.MoveNext();
				}
				finally
				{
					(enumerator as IDisposable)?.Dispose();
				}
			}
			default:
				//A non-enumerable value can't be an empty collection; treat it as content.
				return true;
		}
	}
}
