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
        AddressBox.Text = Browser.Source?.ToString() ?? string.Empty;

        var title = Browser.CoreWebView2?.DocumentTitle;
        StatusText.Text = args.IsSuccess
            ? $"Loaded: {(string.IsNullOrEmpty(title) ? "(no title)" : title)}"
            : $"Navigation failed ({args.WebErrorStatus}): {(string.IsNullOrEmpty(title) ? "(no title)" : title)}";
    }
}
