using System;
using CodeBrix.Platform.Simple;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WebGpuDemo.Helpers;

namespace WebGpuDemo;

public partial class App : Application
{
    private Window _window;

    public App()
    {
        // Open Sans for all text in the application.
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily =
            "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            //Register my custom services here
        });
        SimpleViewModel.SetIsDesignMode(false);

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var frame = new Frame();
        frame.NavigationFailed += OnNavigationFailed;
        _window = new Window { Title = "WebGpu Demo", Content = frame };
        frame.Navigate(typeof(Views.MainPage), args.Arguments);
        _window.Activate();
    }

    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}", e.Exception);

    // Gated on DEBUG like every other sample. The WebGpu add-in's own category is let through at Information: it
    // logs one line when a view gets its device - the adapter, the backend and the render path (Readback, or a direct
    // path when Program.cs opts in to UseDirectGpuRenderingMode()) - and one line whenever a direct path falls back.
    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("CodeBrix.Platform", LogLevel.Warning);
            builder.AddFilter("CodeBrix.Platform.UI.WebGpu", LogLevel.Information);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });
        global::CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
        global::CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    }
}
