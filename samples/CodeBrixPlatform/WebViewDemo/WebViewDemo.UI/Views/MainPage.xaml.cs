using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;

namespace WebViewDemo.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        // To send a custom User-Agent string (on any head), set it here - empty restores
        // the native engine's default:
        //Browser.CoreWebView2.Settings.UserAgent = "MyApp/1.0";

        Browser.NavigationCompleted += Browser_NavigationCompleted;
    }

    private void GoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Browser.Source = new Uri(AddressBox.Text);
        }
        catch (Exception)
        {
            StatusText.Text = $"Invalid address: '{AddressBox.Text}'";
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoBack)
        {
            Browser.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoForward)
        {
            Browser.GoForward();
        }
    }

    private void Browser_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        // Reading the current URL: use CoreWebView2.Source, NOT the XAML Source property.
        //   - Source (the DependencyProperty) is for *setting* / binding a target URL. It is a
        //     mirror the control updates from CoreWebView2.SourceChanged, so inside a navigation
        //     callback - and after server redirects or user/link navigations - it can still hold
        //     the previously requested URL.
        //   - CoreWebView2.Source is the engine's own live value: the authoritative current
        //     top-level document URL. Read it here to know "where am I now?".
        // (This is true of the native WinUI/WPF WebView2 as well - it is the shared WebView2
        //  API contract, not specific to CodeBrix.Platform.)
        AddressBox.Text = sender.CoreWebView2?.Source ?? Browser.Source?.ToString() ?? string.Empty;

        var title = Browser.CoreWebView2?.DocumentTitle;
        StatusText.Text = args.IsSuccess
            ? $"Loaded: {(string.IsNullOrEmpty(title) ? "(no title)" : title)}"
            : $"Navigation failed ({args.WebErrorStatus}): {(string.IsNullOrEmpty(title) ? "(no title)" : title)}";
    }
}
