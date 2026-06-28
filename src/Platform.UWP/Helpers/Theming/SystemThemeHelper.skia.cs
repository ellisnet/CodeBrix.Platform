#nullable enable

using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.Helpers.Theming; //Was previously: Uno.Helpers.Theming

internal static partial class SystemThemeHelper
{
	private static ISystemThemeHelperExtension? _systemThemeHelperExtension;

	internal static SystemTheme GetSystemTheme() => GetExtension() is { } extension ?
		extension.GetSystemTheme() : SystemTheme.Light;

	static partial void ObserveThemeChangesPlatform()
	{
		if (GetExtension() is { } extension)
		{
			extension.SystemThemeChanged += OnSystemThemeChanged;
		}
	}

	private static void OnSystemThemeChanged(object? sender, System.EventArgs e) =>
		RefreshSystemTheme();

	private static ISystemThemeHelperExtension? GetExtension()
	{
		if (_systemThemeHelperExtension is null)
		{
			ApiExtensibility.CreateInstance(typeof(SystemThemeHelper), out _systemThemeHelperExtension);
		}

		return _systemThemeHelperExtension;
	}
}
