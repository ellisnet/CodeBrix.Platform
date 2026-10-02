using System;
using CodeBrix.Platform.Helpers.Theming;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest.Hosting;

internal sealed class VirtualSystemTheme : ISystemThemeHelperExtension
{
    private readonly SystemTheme _theme;

    internal VirtualSystemTheme(ApplicationTheme theme) =>
        _theme = theme == ApplicationTheme.Dark ? SystemTheme.Dark : SystemTheme.Light;

    public SystemTheme GetSystemTheme() => _theme;

    // The virtual OS preference is immutable; never subscribe to the real desktop.
    public event EventHandler SystemThemeChanged { add { } remove { } }
}
