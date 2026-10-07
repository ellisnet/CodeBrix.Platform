#nullable enable

using CodeBrix.Platform.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Xaml.Media; //Was previously: Uno.UI.Xaml.Media

internal static class DefaultBrushes
{
	private const string DefaultTextForegroundThemeBrushKey = "DefaultTextForegroundThemeBrush";

	private static Brush? _textForegroundBrush;
	private static Brush? _lightTextForegroundBrush;
	private static Brush? _darkTextForegroundBrush;

	/// <summary>
	/// The default text brush of the application's theme.
	/// </summary>
	internal static Brush TextForegroundBrush => GetDefaultTextBrush(DefaultTextForegroundThemeBrushKey, ref _textForegroundBrush);

	internal static SolidColorBrush SelectionHighlightColor { get; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));

	internal static SolidColorBrush SelectedTextForegroundColor { get; } = new SolidColorBrush(Colors.White);

	internal static void ResetDefaultThemeBrushes()
	{
		_textForegroundBrush = null;
		_lightTextForegroundBrush = null;
		_darkTextForegroundBrush = null;
	}

	/// <summary>
	/// The default text brush for <paramref name="referenceObject"/>: that of the theme its element (or the nearest
	/// element above it) gets from RequestedTheme, else the application's.
	/// </summary>
	/// <param name="referenceObject">The object whose default text foreground is requested.</param>
	/// <returns>The theme's default text brush.</returns>
	internal static Brush GetTextForegroundBrush(DependencyObject? referenceObject)
	{
		if (!FrameworkElement.HasElementThemeOverrides || referenceObject is null)
		{
			return TextForegroundBrush;
		}

		return FrameworkElement.FindThemeContext(referenceObject)?.GetEffectiveThemeOverride() switch
		{
			ElementTheme.Light => GetThemeTextBrush(ElementTheme.Light, ref _lightTextForegroundBrush),
			ElementTheme.Dark => GetThemeTextBrush(ElementTheme.Dark, ref _darkTextForegroundBrush),
			_ => TextForegroundBrush,
		};
	}

	private static Brush GetThemeTextBrush(ElementTheme theme, ref Brush? brush)
	{
		if (brush is null && Application.Current is not null)
		{
			using var themeScope = ResourceDictionary.PushThemeScope(FrameworkElement.GetThemeKeyForResources(theme));
			brush = Application.Current.Resources.TryGetValue(DefaultTextForegroundThemeBrushKey, out var defaultBrushObject)
				&& defaultBrushObject is Brush defaultBrush
					? defaultBrush
					: theme == ElementTheme.Dark ? SolidColorBrushHelper.White : SolidColorBrushHelper.Black;
		}

		return brush ?? (theme == ElementTheme.Dark ? SolidColorBrushHelper.White : SolidColorBrushHelper.Black);
	}

	private static Brush GetDefaultTextBrush(string key, ref Brush? brush)
	{
		if (Application.Current is null)
		{
			// Called too early or within unit tests, fallback
			return SolidColorBrushHelper.Black;
		}

		if (brush is null)
		{
			// The application's theme, even when an element theme scope is open.
			using var themeScope = FrameworkElement.HasElementThemeOverrides
				? ResourceDictionary.PushThemeScope(ResourceDictionary.GetApplicationTheme())
				: default;

			if (Application.Current.Resources.TryGetValue(key, out var defaultBrushObject) &&
				defaultBrushObject is Brush defaultBrush)
			{
				brush = defaultBrush;
			}
			else
			{
				// Fallback to black/white
				brush = CoreApplication.RequestedTheme == SystemTheme.Dark ?
					SolidColorBrushHelper.White : SolidColorBrushHelper.Black;
			}
		}

		return brush;
	}
}
