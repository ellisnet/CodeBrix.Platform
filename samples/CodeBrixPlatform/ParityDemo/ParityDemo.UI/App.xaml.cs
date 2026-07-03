using CodeBrix.Platform.Simple;
using ParityDemo.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

// ReSharper disable RedundantExtendsListEntry
// ReSharper disable RedundantNameQualifier
// ReSharper disable CheckNamespace

namespace ParityDemo;

public partial class App : Application
{
    public App()
    {
        //Set Open Sans as the default font for all text in application
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            //Register my custom services here
        });
        SimpleViewModel.SetIsDesignMode(false);

        InitializeComponent();
    }

    internal static Window MainWindowInstance { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new Window
        {
            Title = "Wayland/X11 Parity Demo"
        };

        if (MainWindowInstance.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindowInstance.Content = rootFrame;
            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(Views.MainPage), args.Arguments);
        }

        MainWindowInstance.Activate();
    }

    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("CodeBrix.Platform", LogLevel.Warning);
            builder.AddFilter("CodeBrix.Platform.WinUI.Runtime.Skia.Wayland", LogLevel.Debug);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_CODEBRIX
        global::CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
