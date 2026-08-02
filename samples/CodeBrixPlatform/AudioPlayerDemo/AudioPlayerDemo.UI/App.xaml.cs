using CodeBrix.Audio.Opus;
using CodeBrix.Platform.Simple;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using AudioPlayerDemo.Helpers;

// ReSharper disable RedundantExtendsListEntry
// ReSharper disable RedundantNameQualifier
// ReSharper disable CheckNamespace

namespace AudioPlayerDemo;

public partial class App : Application
{
    public App()
    {
        //Set Open Sans as the default font for all text in application
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

        // Turn on .opus playback. This one call is the whole integration: after it, .opus files
        // play through the AudioPlayer AddIn's AudioPlayer element and SoundEffect exactly like
        // the formats CodeBrix.Audio reads natively.
        //
        // The dependency belongs to THIS application, not to the AddIn. Opus is BSD-3-Clause and
        // CodeBrix.Audio holds an MIT-or-better bar, so the codec ships as its own package; the
        // AddIn resolves codecs through the shared audio output and so needs neither a reference
        // to it nor a code change. There is deliberately no module initializer doing this call for
        // us - that would work in a debug build and silently not run in a trimmed publish.
        CodeBrixAudioOpus.Register();

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
            Title = "AudioPlayer Demo"
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
