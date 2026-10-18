using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

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
        Browser.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // Optional self-test hook: navigate straight to a download URL and exit once the
        // download completes (used by the repo's scripted X11 smoke verification).
        if (Environment.GetEnvironmentVariable("WEBVIEWDEMO_SELFTEST_DOWNLOAD_URL") is { Length: > 0 } url)
        {
            _selfTest = true;
            Browser.Source = new Uri(url);
        }

        // Optional self-test hook for host-to-page messaging: load the echo page, post one string
        // and one JSON message once the page says it is listening, and exit 0 when both echoes
        // come back through WebMessageReceived (1 on a timeout). This is the scripted check of
        // PostWebMessageAsString / PostWebMessageAsJson on a head.
        if (Environment.GetEnvironmentVariable("WEBVIEWDEMO_SELFTEST_MESSAGING") == "1")
        {
            _messagingSelfTest = true;
            Browser.CoreWebView2.NavigateToString(EchoPageHtml);
            _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ =>
            {
                Console.WriteLine($"WVD-SELFTEST: RESULT FAIL messaging timed out; echoes received: string={_stringEchoReceived} json={_jsonEchoReceived}");
                Environment.Exit(1);
            }, TaskScheduler.Default);
        }
    }

    private bool _selfTest;
    private bool _messagingSelfTest;
    private bool _stringEchoReceived;
    private bool _jsonEchoReceived;

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

    // ---- Host-to-page messaging ---------------------------------------------------------------
    // The page half of the WebView2 messaging contract: it listens on window.chrome.webview for
    // the 'message' events that PostWebMessageAsString / PostWebMessageAsJson raise (e.data is the
    // string, or the parsed JSON value), lists each one, and answers through
    // window.chrome.webview.postMessage so the host hears it in WebMessageReceived. On load it
    // posts "listening": the cue that it is safe to post to it, because a message posted before
    // the page's channel exists is dropped.
    private const string EchoPageHtml =
        """
        <!DOCTYPE html><html><head><meta charset="utf-8"><title>Echo</title></head>
        <body style="font-family:sans-serif;padding:16px">
        <h3>Echo page</h3>
        <p>Messages the host posts are listed here and echoed back to it.</p>
        <ul id="log"></ul>
        <script>
        window.chrome.webview.addEventListener('message', function (e) {
            var isString = typeof e.data === 'string';
            var text = (isString ? 'string:' : 'json:') + (isString ? e.data : JSON.stringify(e.data));
            var item = document.createElement('li');
            item.textContent = text;
            document.getElementById('log').appendChild(item);
            window.chrome.webview.postMessage(text);
        });
        window.addEventListener('load', function () { window.chrome.webview.postMessage('listening'); });
        </script></body></html>
        """;

    // Awkward on purpose: quotes, a closing script tag and non-trivial JSON must come back intact.
    // The JSON has single-key objects only: Edge hands the page a parsed object with its keys in
    // Edge's own (sorted) order, so a multi-key object would not stringify back to the text that
    // was posted, and the self-test compares the echo text for text.
    private const string StringMessage = "he said \"hi\" </script> done";
    private const string JsonMessage = "{\"list\":[true,null,\"two\",{\"n\":1}]}";

    private void EchoPageButton_Click(object sender, RoutedEventArgs e)
    {
        MessageText.Text = "Echo page loading; it posts \"listening\" when ready";
        Browser.CoreWebView2.NavigateToString(EchoPageHtml);
    }

    private void PostStringButton_Click(object sender, RoutedEventArgs e)
    {
        // The page's listener receives e.data === StringMessage, quotes and "</script>" included.
        Browser.CoreWebView2.PostWebMessageAsString(StringMessage);
    }

    private void PostJsonButton_Click(object sender, RoutedEventArgs e)
    {
        // The page's listener receives the parsed value (e.data.list[3].n === 1). Text that is not
        // JSON throws ArgumentException on every head that implements the channel.
        Browser.CoreWebView2.PostWebMessageAsJson(JsonMessage);
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var text = args.TryGetWebMessageAsString();
        MessageText.Text = $"Received: {text}";
        Console.WriteLine($"WVD-SELFTEST: WEB_MESSAGE {text}");

        if (!_messagingSelfTest)
        {
            return;
        }

        if (text == "listening")
        {
            Browser.CoreWebView2.PostWebMessageAsString(StringMessage);
            Browser.CoreWebView2.PostWebMessageAsJson(JsonMessage);
        }
        else if (text == "string:" + StringMessage)
        {
            _stringEchoReceived = true;
        }
        else if (text == "json:" + JsonMessage)
        {
            _jsonEchoReceived = true;
        }

        if (_stringEchoReceived && _jsonEchoReceived)
        {
            Console.WriteLine("WVD-SELFTEST: RESULT PASS messaging (both echoes arrived)");
            Environment.Exit(0);
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
