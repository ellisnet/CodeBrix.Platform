using CodeBrix.Platform.UI.Hosting;
using System;

// ReSharper disable CheckNamespace

namespace AudioPlayerDemo;

internal class Program
{
    // Must be a synchronous STA Main: WebView2 (CoreWebView2Environment.CreateAsync) requires the
    // UI thread to be an STA. With 'async Task Main' the [STAThread] attribute is ignored and the
    // thread runs as MTA, so WebView2 creation throws RPC_E_CHANGED_MODE ("Cannot change thread mode
    // after it is set."). host.Run() pumps the Win32 message loop synchronously on this STA thread.
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWindowsWin32()
            .Build();

        host.Run();
    }
}
