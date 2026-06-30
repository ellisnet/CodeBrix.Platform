using CodeBrix.Platform.Simple;
using JustBetweenUs.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace JustBetweenUs.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        //Doing this before InitializeComponent() - in case InitializeComponent()
        //  is the thing that sets the data context.
        DataContextChanged += (sender, args) =>
        {
            (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

            if (DataContext is ICopyToClipboard copy)
            {
                copy.CopyTextToClipboard = (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        var clipData = new DataPackage();
                        clipData.SetText(text);
                        Clipboard.SetContent(clipData);
                    }
                };
            }
        };

        InitializeComponent();

        //TODO: Take this out, when I am sure that the CodeBrix.Platform.Fonts.OpenSans package is working correctly

        // Verify that the CodeBrix.Platform.Fonts.OpenSans NuGet package is present and being
        // utilized correctly. The Page's FontFamily should resolve to the OpenSans
        // .ttf file via the StaticResource OpenSansFont defined in App.xaml.
        // If the package is missing or misconfigured, the font will silently fall
        // back to the system default — this check makes that visible in Debug output.
        Loaded += (_, _) =>
        {
            var fontFamily = FontFamily?.Source ?? "(null)";
            System.Diagnostics.Debug.WriteLine($"[MainPage] FontFamily = '{fontFamily}'");

            if (fontFamily.Contains("OpenSans", System.StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] ✓ Open Sans font is active.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] ✗ Open Sans font is NOT active — check App.xaml OpenSansFont resource.");
                //Paranoid throw to make sure that the font package is working correctly
                throw new InvalidOperationException("[MainPage] ✗ Open Sans font is NOT active — check App.xaml OpenSansFont resource.");
            }
        };
    }
}
