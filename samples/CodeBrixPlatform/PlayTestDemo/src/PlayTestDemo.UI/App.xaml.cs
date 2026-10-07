using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace PlayTestDemo;

public partial class App : Application
{
    private Window _window;

    public App()
    {
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily =
            "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var frame = new Frame();
        frame.NavigationFailed += OnNavigationFailed;
        _window = new Window { Title = "PlayTestDemo", Content = frame };
        WindowLifecycle.Attach(_window);
        frame.Navigate(typeof(Views.MainPage), args.Arguments);
        _window.Activate();
    }

    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}", e.Exception);

    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        global::CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
        global::CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    }
}
