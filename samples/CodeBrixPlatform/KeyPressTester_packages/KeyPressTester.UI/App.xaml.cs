using CodeBrix.Platform.Simple;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using KeyPressTester.Helpers;

// ReSharper disable RedundantExtendsListEntry
// ReSharper disable RedundantNameQualifier
// ReSharper disable CheckNamespace

namespace KeyPressTester;

public partial class App : Application
{
    public App()
    {
        //Set Open Sans as the default font for all text in application
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

        //Opt in to menu items showing their keyboard accelerator text ("Ctrl+P") automatically
        global::CodeBrix.Platform.UI.FeatureConfiguration.MenuFlyoutItem.ShowKeyboardAcceleratorText = true;

        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            //Register my custom services here
        });
        SimpleViewModel.SetIsDesignMode(false);

        InitializeComponent();
    }

    protected Window MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Title = "Key Press Tester"
        };

        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(Views.MainPage), args.Arguments);
        }

        //Forward window activation to the page: it logs it, and (like Doom.Brix) gives the
        //  game surface keyboard focus again when the window comes back to the foreground.
        MainWindow.Activated += (_, e) =>
            ((MainWindow.Content as Frame)?.Content as Views.MainPage)?.OnWindowActivated(e.WindowActivationState);

        MainWindow.Activate();
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
