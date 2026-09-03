using CodeBrix.Audio.Opus;
using CodeBrix.VideoPlayback.Dav1d;
using CodeBrix.Platform.Simple;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using VideoPlayerDemo.Helpers;

// ReSharper disable RedundantExtendsListEntry
// ReSharper disable RedundantNameQualifier
// ReSharper disable CheckNamespace

namespace VideoPlayerDemo;

public partial class App : Application
{
    public App()
    {
        //Set Open Sans as the default font for all text in application
        global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

        // Turn on AV1 video and .opus audio. These two calls are the whole integration: after
        // them, every clip this demo ships plays through the VideoPlayer AddIn's VideoPlayer
        // element exactly like a format the playback engine read natively.
        //
        // The dependencies belong to THIS application, not to the AddIn. AV1 decoding is
        // BSD-2-Clause and Opus is BSD-3-Clause while the AddIn is Apache-2.0, so each codec ships
        // as its own package; the AddIn resolves codecs through the playback session's registries
        // and so needs neither a reference to them nor a code change. There is deliberately no
        // module initializer doing these calls for us - that would work in a debug build and
        // silently not run in a trimmed publish.
        //
        // Dav1d is not optional in practice: every clip this demo ships carries AV1, so nothing here plays
        // without it. (Only an uncompressed V_UNCOMPRESSED track would, through the decoder built into the core.)
        // Opus is needed only for the WebM and CodeBrix-Mode1 clips; the Mode2 clips carry Vorbis.
        CodeBrixVideoPlaybackDav1d.Register();
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
            Title = "VideoPlayer Demo"
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

    // Gated on DEBUG like every other sample (the house-style App.xaml.cs). Inside the block the
    // VideoPlayer AddIn's own category is let through at Information: it logs the graphics backend
    // it chose (OpenGL/GLES or Metal) exactly once, so a Debug run says which graphics API carried
    // the picture. A more specific category filter wins in Microsoft.Extensions.Logging, so the
    // platform stays at Warning while the add-in is heard. The scripted smoke verification does not
    // depend on logging - its VPD-SMOKE facts come from the element itself.
    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("CodeBrix.Platform", LogLevel.Warning);
            builder.AddFilter("CodeBrix.Platform.UI.VideoPlayer", LogLevel.Information);
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
