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
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Browser.EnsureCoreWebView2Async();
        Browser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

        // Optional self-test hook: navigate straight to a download URL and exit once the
        // download completes (used by the repo's scripted X11 smoke verification).
        if (Environment.GetEnvironmentVariable("WEBVIEWDEMO_SELFTEST_DOWNLOAD_URL") is { Length: > 0 } url)
        {
            _selfTest = true;
            Browser.Source = new Uri(url);
        }
    }

    private bool _selfTest;

    // Demonstrates the DownloadStarting API: leave args untouched to accept the default
    // (a collision-free name in the user's Downloads folder), or set args.ResultFilePath /
    // args.Cancel - possibly after awaiting a deferral - to redirect or refuse the download.
    private void CoreWebView2_DownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
    {
        var operation = args.DownloadOperation;
        StatusText.Text = $"Downloading to {args.ResultFilePath}";
        Console.WriteLine($"WVD-SELFTEST: DOWNLOAD_STARTING uri={operation.Uri} path={args.ResultFilePath} mime={operation.MimeType} total={operation.TotalBytesToReceive}");

        operation.BytesReceivedChanged += (op, _) =>
        {
            StatusText.Text = $"Downloading {op.ResultFilePath}: {op.BytesReceived}/{op.TotalBytesToReceive} bytes";
            Console.WriteLine($"WVD-SELFTEST: DOWNLOAD_PROGRESS bytes={op.BytesReceived} total={op.TotalBytesToReceive} eta={op.EstimatedEndTime}");
        };
        operation.StateChanged += (op, _) =>
        {
            StatusText.Text = $"Download {op.State}: {op.ResultFilePath}";
            Console.WriteLine($"WVD-SELFTEST: DOWNLOAD_STATE state={op.State} reason={op.InterruptReason} bytes={op.BytesReceived}");
            if (_selfTest && op.State != CoreWebView2DownloadState.InProgress)
            {
                var success = op.State == CoreWebView2DownloadState.Completed && System.IO.File.Exists(op.ResultFilePath);
                Console.WriteLine($"WVD-SELFTEST: RESULT {(success ? "PASS" : "FAIL")} file={op.ResultFilePath}");
                Environment.Exit(success ? 0 : 1);
            }
        };
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

    // Simulates WikipediaPublisher's completion alert: a modal ContentDialog (the same control
    // SimpleDialog is built on) shown over the WebView2. On the WinWpfSkia head this exercises the
    // airspace fix - while the dialog dims the app, the native WebView must hide (not stay lit on
    // top of the dialog), and must reappear when OK is clicked.
    private async void UrlButton_Click(object sender, RoutedEventArgs e)
    {
        var url = Browser.CoreWebView2?.Source ?? Browser.Source?.ToString() ?? "(no URL yet)";

        var dialog = new ContentDialog
        {
            Title = "Current URL",
            Content = new TextBlock { Text = url, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
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
