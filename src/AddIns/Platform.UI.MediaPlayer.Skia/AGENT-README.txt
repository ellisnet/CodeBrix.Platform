================================================================================
AGENT-README: CodeBrix.Platform.MediaPlayer
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.MediaPlayer.LgplLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.MediaPlayer.LgplLicenseForever is the optional media-player
add-on for CodeBrix.Platform desktop apps (.NET 10 or later). It makes the
XAML MediaPlayerElement control (audio and video playback) work on every Skia
head except macOS: Windows (Win32), Skia-on-WPF, Linux X11, Linux native
Wayland, and Linux FrameBuffer. ONE package covers all five heads.

How it works: LibVLC decodes the media, and decoded video frames are delivered
into memory (libvlc's windowing-system-agnostic "vmem" output, surfaced by the
MediaPlayerCore library's VideoFrameSink). The add-in copies each frame into a
Skia image and paints it directly into the Skia scene, exactly like any other
XAML content. Consequences that matter to you:
  - No native child windows, so no "airspace" problems: clipping, transforms,
    opacity and z-order all behave normally, and XAML content can be drawn on
    top of the video.
  - No XWayland: the Wayland head stays native.
  - The FrameBuffer head (no windowing system at all) is covered too.
  - The XAML Stretch mode is applied at paint time by the add-in, not by VLC.

You program against the standard WinUI contract - MediaPlayerElement,
Windows.Media.Playback.MediaPlayer and Windows.Media.Core.MediaSource - which
live in the core framework package. This add-on contributes the playback engine
behind that contract; its own two public types are instantiated by the
framework and are not something app code normally touches (see "THE ADD-IN'S
OWN TYPES").

Head coverage summary:
  Win32, WPF, X11, Wayland, FrameBuffer   this package (LibVLC engine)
  macOS                                   NOT this package - the macOS head has
                                          built-in AVFoundation media support;
                                          the add-in is inert there and needs
                                          no libvlc.

INSTALLATION
============
PackageId:   CodeBrix.Platform.MediaPlayer.LgplLicenseForever
License:     LGPL-2.1-or-later   (see "WHY LGPL" below)

    dotnet add package CodeBrix.Platform.MediaPlayer.LgplLicenseForever

NuGet dependencies (pulled in automatically):
  - CodeBrix.Platform.ApacheLicenseForever            the framework itself
  - CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever
                                                      managed LibVLC binding
                                                      (a port of LibVLCSharp)
  - SkiaSharp

WHERE TO REFERENCE IT (the only correct placement)
--------------------------------------------------
Reference this package ONCE, in your app's .Core project - the same rule as the
WebView and other extension add-ons. Every head project inherits it
transitively. Do NOT reference it from a head project, and do not look for a
per-head variant: there is none.
  - The Windows (Win32 and Skia-on-WPF) and Linux (X11, Wayland, FrameBuffer)
    heads activate it automatically (OS-gated ApiExtension registrations that
    the XAML source generator emits into your app).
  - On the macOS head neither registration matches, so the add-in is inert and
    the head's built-in AVFoundation media support is used instead.

THE NATIVE libvlc RUNTIME IS NOT IN THE PACKAGE
-----------------------------------------------
The package ships managed code only. You must provide the native LibVLC
runtime per OS:

  Linux (all three Linux heads) - install via apt; the base plugin set is
  enough, the full vlc application is NOT needed:

      sudo apt install libvlc5 vlc-plugin-base

  Windows (Win32 and WPF heads) - add the "VideoLAN.LibVLC.Windows" NuGet
  package to the WINDOWS HEAD project(s) only (not to .Core - it is a Windows
  native payload). It copies libvlc.dll, libvlccore.dll and the plugins folder
  into the head's output.

  macOS - nothing to install; this package is not used there.

Optional Linux hardware decoding: see PERFORMANCE TIPS.

WHY LGPL (truth-in-labeling)
----------------------------
This is the ONLY published CodeBrix.Platform package that is NOT Apache-2.0.
Playback is delivered via LibVLC, so the package depends on
CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever (a managed port of
LibVLCSharp) - all LGPL-2.1-or-later. The ".LgplLicenseForever" suffix in the
package id is deliberate truth-in-labeling so that nobody adds an LGPL
dependency to an app by accident. If your app must stay LGPL-free, do not use
this package; on macOS you do not need it at all, and for audio-only playback
consider the Apache-2.0 CodeBrix.Platform.AudioPlayer.ApacheLicenseForever
add-on instead (see its AGENT-README under src/AddIns/Platform.UI.AudioPlayer
.Skia in the repository).

Legacy projects: the repository also contains two superseded native-child-window
add-ins (X11 and Win32 only, incompatible with Wayland and FrameBuffer), whose
package ids are CodeBrix.Platform.WinUI.MediaPlayer.Skia.Win32.LgplLicenseForever
and CodeBrix.Platform.WinUI.MediaPlayer.Skia.X11.LgplLicenseForever. Both projects
are packable and self-pack on a Release build - and both pack THIS file as their
AGENT-README - but they are excluded from the central pack driver, so neither id
is published to nuget.org and neither is available to install. Use the
CodeBrix.Platform.MediaPlayer.LgplLicenseForever package documented here.

KEY NAMESPACES / USINGS
=======================
Consumer contract (all in the core framework package; the XAML default xmlns
already covers the controls):

    using Microsoft.UI.Xaml.Controls;   // MediaPlayerElement, MediaTransportControls
    using Microsoft.UI.Xaml.Media;      // Stretch
    using Windows.Media.Playback;       // MediaPlayer, MediaPlaybackSession,
                                        // MediaPlaybackState, MediaPlaybackItem,
                                        // MediaPlaybackList, MediaPlayerFailedEventArgs
    using Windows.Media.Core;           // MediaSource

The add-in's own namespace (only needed for the optional PreloadVlc() call):

    using CodeBrix.Platform.UI.MediaPlayer.Skia;   // SkiaMediaPlayerExtension

Remember: package ids carry the license suffix, namespaces do not.

CORE API REFERENCE
==================

MediaPlayerElement (Microsoft.UI.Xaml.Controls) - the XAML control
------------------------------------------------------------------
    public IMediaPlaybackSource Source { get; set; }      // MediaSource,
                                                           // MediaPlaybackItem
                                                           // or MediaPlaybackList
    public bool AutoPlay { get; set; }                     // copied to
                                                           // MediaPlayer.AutoPlay
                                                           // when the element loads
    public bool AreTransportControlsEnabled { get; set; }  // show the built-in
                                                           // play/pause/seek bar
    public Stretch Stretch { get; set; }                   // None, Fill, Uniform
                                                           // (default), UniformToFill
    public ImageSource PosterSource { get; set; }          // shown while loading,
                                                           // when no/invalid source,
                                                           // and for audio-only media
    public bool IsFullWindow { get; set; }                 // see FULL SCREEN below
    public MediaPlayer MediaPlayer { get; set; }           // created by the control
                                                           // in OnApplyTemplate when
                                                           // null - see PITFALLS
    public void SetMediaPlayer(MediaPlayer mediaPlayer)
    public MediaTransportControls TransportControls { get; set; }
    public void ToggleCompactOverlay(bool showCompactOverlay)   // no-op on the
                                                                // Skia heads

Windows.Media.Playback.MediaPlayer - the playback object
--------------------------------------------------------
    public IMediaPlaybackSource Source { get; set; }   // setting it stops the
                                                        // current media, loads the
                                                        // new one, and plays it
                                                        // when AutoPlay is true
    public bool AutoPlay { get; set; }
    public double Volume { get; set; }                  // 0.0 .. 1.0
    public bool IsMuted { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan NaturalDuration { get; }
    public double PlaybackRate { get; set; }
    public bool IsLoopingEnabled { get; set; }          // replay the same media
    public bool IsLoopingAllEnabled { get; set; }       // wrap a MediaPlaybackList
    public bool CanPause { get; }
    public bool CanSeek { get; }
    public bool IsVideo { get; }                        // true once the loaded
                                                        // media is known to have
                                                        // a video track
    public MediaPlaybackSession PlaybackSession { get; }
    public void Play()
    public void Pause()
    public void Stop()
    public void StepForwardOneFrame()                   // VLC steps forward only;
                                                        // StepBackwardOneFrame()
                                                        // throws NotImplementedException
    public void NextTrack()                             // MediaPlaybackList only
    public void PreviousTrack()
    public void Dispose()

Events this engine raises (all on the UI thread):
    public event TypedEventHandler<MediaPlayer, object> SourceChanged;
    public event TypedEventHandler<MediaPlayer, object> MediaOpened;
    public event TypedEventHandler<MediaPlayer, object> MediaEnded;
    public event TypedEventHandler<MediaPlayer, MediaPlayerFailedEventArgs> MediaFailed;
    public event TypedEventHandler<MediaPlayer, object> VolumeChanged;
    public event TypedEventHandler<MediaPlayer, object> IsMutedChanged;
    public event TypedEventHandler<MediaPlayer, object> NaturalVideoDimensionChanged;
Other events declared on MediaPlayer (SeekCompleted, BufferingStarted/
BufferingEnded, CurrentStateChanged, MediaPlayerRateChanged,
PlaybackMediaMarkerReached, VideoFrameAvailable, SubtitleFrameChanged) are not
driven by this engine; watch PlaybackSession.PositionChanged after a seek
instead of SeekCompleted.

MediaPlayerFailedEventArgs:
    public MediaPlayerError Error { get; }        // always MediaPlayerError.Unknown
                                                  // with this engine
    public string ErrorMessage { get; }
    public Exception ExtendedErrorCode { get; }

MediaPlaybackSession (MediaPlayer.PlaybackSession)
--------------------------------------------------
    public TimeSpan Position { get; set; }        // clamped to 0..NaturalDuration
    public TimeSpan NaturalDuration { get; }
    public double PlaybackRate { get; set; }
    public double BufferingProgress { get; }
    public MediaPlaybackState PlaybackState { get; }
        // enum MediaPlaybackState { None, Opening, Buffering, Playing, Paused }
    public event TypedEventHandler<MediaPlaybackSession, object> PositionChanged;
    public event TypedEventHandler<MediaPlaybackSession, object> PlaybackStateChanged;
    public event TypedEventHandler<MediaPlaybackSession, object> NaturalDurationChanged;
    public event TypedEventHandler<MediaPlaybackSession, object> PlaybackRateChanged;
    public event TypedEventHandler<MediaPlaybackSession, object> BufferingProgressChanged;

NOT available: MediaPlaybackSession.NaturalVideoWidth / NaturalVideoHeight
throw NotImplementedException on the Skia heads. The natural size is used
internally by the presenter for Stretch, but there is no public readback; use
MediaPlayer.IsVideo and the NaturalVideoDimensionChanged event to learn that a
video track exists.

Windows.Media.Core.MediaSource and playlists
--------------------------------------------
    public static MediaSource CreateFromUri(Uri uri)   // the ONLY implemented
                                                        // factory on the Skia heads
    public Uri Uri { get; }

    public MediaPlaybackItem(MediaSource source)        // wraps one MediaSource
    public MediaSource Source { get; }

    public partial class MediaPlaybackList : IMediaPlaybackList, IMediaPlaybackSource
    public IObservableVector<MediaPlaybackItem> Items { get; }

MediaSource.CreateFromStream(...), CreateFromStreamReference(...) and
CreateFromStorageFile(...) throw NotImplementedException: this engine plays
from URIs only. There is no stream or embedded-resource source form; write
embedded content to a file first (e.g. under ApplicationData) and play it by
URI.

SUPPORTED SOURCE URI FORMS
--------------------------
The add-in hands the Uri to libvlc after resolving these schemes:
  - http:// and https://       network streams and progressive-download files
                               (plus any other scheme libvlc itself can open)
  - file:///absolute/path      a local file
  - ms-appx:///Assets/x.mp4    a file under the app's install folder (the
                               executable's directory, i.e. a Content item
                               copied to the output)
  - ms-appdata://local/x.mp4   a file under the app data folders
  - a relative or scheme-less Uri is treated as ms-appx:///<value>
Codecs and containers are whatever the installed libvlc plugin set decodes
(vlc-plugin-base on Linux; the VideoLAN.LibVLC.Windows plugin folder on
Windows).

Playlist behaviour: setting a MediaPlaybackList as Source loads its first item;
when an item ends the next one starts automatically; NextTrack()/PreviousTrack()
move explicitly; IsLoopingAllEnabled wraps from the last item to the first.

STRETCH AND FRAME PRESENTATION
------------------------------
The add-in paints the latest decoded frame into the presenter's area, centred,
on a black background, with linear filtering:
    Stretch.Uniform        fit inside, keep aspect (default)
    Stretch.UniformToFill  cover the area, keep aspect (edges clipped)
    Stretch.Fill           distort to the area
    Stretch.None           1:1 pixels, centred
While nothing is playing, or for audio-only media, the video area is left
transparent and the video element is collapsed (PosterSource shows if set).
After the media's metadata is parsed the engine seeks to the first frame so a
freshly loaded video shows its first picture instead of black.

FULL SCREEN AND COMPACT OVERLAY
-------------------------------
Setting MediaPlayerElement.IsFullWindow = true asks the host window to enter
full-screen mode (ApplicationView.TryEnterFullScreenMode on the window that
hosts the element) and moves the player's layout root into the XamlRoot's
full-window media root; setting it back to false exits full-screen mode and
restores the element to its place. The element must already be loaded in the
visual tree, otherwise a warning is logged and nothing happens. The add-in's
own RequestFullScreen/ExitFullScreen hooks are intentionally empty - the
framework does all of the work. ToggleCompactOverlay(bool) is a no-op on the
Skia heads.

THE ADD-IN'S OWN TYPES (namespace CodeBrix.Platform.UI.MediaPlayer.Skia)
------------------------------------------------------------------------
    public class SkiaMediaPlayerExtension : IMediaPlayerExtension
        public SkiaMediaPlayerExtension(MediaPlayer player)  // framework-created
        public static void PreloadVlc()                       // the one member
                                                              // app code may call
        public bool? IsVideo { get; }                         // null until known
    public class SkiaMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
        public SkiaMediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
        // framework-created; RequestFullScreen/ExitFullScreen/
        // RequestCompactOverlay/ExitCompactOverlay are no-ops here.

Both are instantiated by the framework through the ApiExtension registrations;
never construct them yourself.

PreloadVlc(): call it once, early in startup (e.g. at the top of Main, or in
App's constructor), to warm up the LibVLC runtime on a background thread and
cut the latency of the first playback. It plays a tiny embedded sample to
force libvlc's plugin cache to load. It is entirely optional.

    CodeBrix.Platform.UI.MediaPlayer.Skia.SkiaMediaPlayerExtension.PreloadVlc();

HOST-BUILDER PreloadMediaPlayer(bool) DOES NOT APPLY TO THIS PACKAGE: the X11
and Win32 host builders expose X11HostBuilder.PreloadMediaPlayer(bool) and
Win32HostBuilder.PreloadMediaPlayer(bool), but those look up the legacy,
never-published add-ins' types by name and do nothing when this package is the
one installed. Call SkiaMediaPlayerExtension.PreloadVlc() directly instead. The
Wayland, FrameBuffer and WPF host builders have no such option at all.

WHAT HAPPENS WHEN libvlc IS MISSING
-----------------------------------
The engine is created the first time a MediaPlayer is constructed - which
MediaPlayerElement does itself when its template is applied (first layout).
If the native runtime cannot be loaded, that construction throws
PlatformNotSupportedException whose message names the fix, with the
MediaPlayerCore VLCException as InnerException:
  Linux:   "The native libvlc runtime was not found. Install it via the system
            package manager: sudo apt install libvlc5 vlc-plugin-base
            (Debian/Ubuntu)."
  Windows: "The native libvlc runtime was not found. Add the
            VideoLAN.LibVLC.Windows package to your Windows head project(s)."
The same exception is logged (not thrown) from PreloadVlc(), which runs on a
background thread.

LINUX AUDIO NOTES (X11, Wayland AND FrameBuffer)
------------------------------------------------
This package contains no head-specific audio code: libvlc picks its own audio
output (PulseAudio/PipeWire/ALSA) on all three Linux heads, including the
FrameBuffer head, which has no desktop session. One Linux-only diagnostic is
built in: the OS audio server (PulseAudio/PipeWire via WirePlumber
stream-restore) may restore a saved per-application or per-media-role
("Movie"/video) mute onto libvlc's output stream, so the media plays silently
although the app never asked for mute. The add-in detects this once per media
and logs a warning:
    "MediaPlayer audio is muted at the OS audio layer (PulseAudio/PipeWire),
     but the application did not request mute - so this media will play with
     no sound. ... Unmute it in your system sound settings (e.g. 'pavucontrol'
     -> Playback) to restore audio; the app will not override your choice."
It deliberately does NOT unmute (the user may have muted it on purpose).

COMPLETE EXAMPLES
=================

1. XAML: a player with the built-in transport controls
------------------------------------------------------
    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        <Grid>
            <MediaPlayerElement x:Name="Player"
                                AutoPlay="True"
                                AreTransportControlsEnabled="True"
                                Stretch="Uniform" />
        </Grid>
    </Page>

    // MainPage.xaml.cs
    using Microsoft.UI.Xaml.Controls;
    using Windows.Media.Core;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Player.Source = MediaSource.CreateFromUri(
                new Uri("https://example.com/clip.mp4"));
        }
    }

2. Code-driven playback with events and a position readout
----------------------------------------------------------
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Windows.Media.Core;
    using Windows.Media.Playback;

    public sealed partial class MainPage : Page
    {
        private MediaPlayer _player;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Player.MediaPlayer exists once the element's template is applied.
            _player = Player.MediaPlayer;
            _player.MediaOpened += (p, _) =>
                Status.Text = $"Opened, {p.NaturalDuration:mm\\:ss}, video={p.IsVideo}";
            _player.MediaEnded += (p, _) => Status.Text = "Ended";
            _player.MediaFailed += (p, args) =>
                Status.Text = $"Failed: {args.ErrorMessage}";
            _player.PlaybackSession.PositionChanged += (s, _) =>
                Elapsed.Text = s.Position.ToString(@"mm\:ss");
            _player.PlaybackSession.PlaybackStateChanged += (s, _) =>
                State.Text = s.PlaybackState.ToString();

            _player.Volume = 0.8;
            _player.IsLoopingEnabled = true;
            _player.Source = MediaSource.CreateFromUri(
                new Uri("ms-appx:///Assets/intro.mp4"));
            _player.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
            => _player.Pause();

        private void SeekButton_Click(object sender, RoutedEventArgs e)
            => _player.PlaybackSession.Position = TimeSpan.FromSeconds(30);
    }

3. Your own MediaPlayer, shared by the element
----------------------------------------------
    var player = new MediaPlayer { AutoPlay = false };   // engine created here
    player.Source = MediaSource.CreateFromUri(new Uri("file:///home/me/a.mp3"));
    Player.SetMediaPlayer(player);      // or: Player.MediaPlayer = player;
    player.Play();

4. A playlist
-------------
    var list = new MediaPlaybackList();
    foreach (var url in new[] { "https://example.com/1.mp4", "https://example.com/2.mp4" })
    {
        list.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(url))));
    }
    Player.MediaPlayer.IsLoopingAllEnabled = true;   // wrap at the end
    Player.Source = list;                           // first item loads; AutoPlay plays it
    // later: Player.MediaPlayer.NextTrack();

5. Full screen toggle
---------------------
    private void FullScreen_Click(object sender, RoutedEventArgs e)
        => Player.IsFullWindow = !Player.IsFullWindow;

MINIMUM VIABLE PROJECT
======================
The app follows the standard CodeBrix.Platform layout (.Core class library +
.UI shared project + one head project per OS; see the framework's root
AGENT-README for the head packages and Program.cs of each head). The parts
specific to this add-on:

MyApp.Core/MyApp.Core.csproj (references go HERE, once):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever"
                          Version="<current family version>" />
        <PackageReference Include="CodeBrix.Platform.MediaPlayer.LgplLicenseForever"
                          Version="<same family version>" />
      </ItemGroup>
    </Project>

MyApp.Win32Skia/MyApp.Win32Skia.csproj and MyApp.WinWpfSkia (Windows heads
ONLY) additionally carry the native runtime:

      <ItemGroup>
        <PackageReference Include="VideoLAN.LibVLC.Windows"
                          Version="<current VideoLAN release>" />
      </ItemGroup>

Linux heads: nothing extra in the csproj; install the runtime on the machine:

    sudo apt install libvlc5 vlc-plugin-base

MyApp.LinuxX11/Program.cs (optional warm-up shown):

    using CodeBrix.Platform.UI.Hosting;
    using CodeBrix.Platform.UI.MediaPlayer.Skia;

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SkiaMediaPlayerExtension.PreloadVlc();   // optional: warm up libvlc

            var host = CodeBrixPlatformHostBuilder.Create()
                .App(() => new App())
                .UseLinuxX11()
                .Build();
            host.Run();
        }
    }

MyApp.UI/Views/MainPage.xaml: as in COMPLETE EXAMPLES 1. Put media files you
ship as <Content Include="Assets\intro.mp4" CopyToOutputDirectory="PreserveNewest" />
in the .Core project and address them as ms-appx:///Assets/intro.mp4.

PERFORMANCE TIPS
================
  - Warm-up: call SkiaMediaPlayerExtension.PreloadVlc() at startup. Loading
    libvlc's plugin cache is the dominant first-play cost.
  - Linux hardware decoding (optional): on Linux the add-in creates libvlc
    with software decoding requested ("--avcodec-hw=none"), because frames must
    land in system memory for the memory output. Debian's libvlc still probes
    VAAPI/VDPAU, and with only vlc-plugin-base installed those probes fail (no
    GPU-surface-to-CPU converter) and VLC falls back to software decoding -
    playback works, at the cost of roughly two seconds of extra startup and
    "Failed to adapt decoder format to display" log noise. Installing
    vlc-plugin-video-output adds the VAAPI converter (libvaapi_filters) so a
    hardware decode-with-copyback succeeds on the first attempt:
        sudo apt install vlc-plugin-video-output
  - Every video frame is a full BGRA copy into a Skia image on a libvlc thread,
    then a blit on the UI thread; cost scales with the video's pixel size, not
    the element's size. Prefer sources encoded near the display size for
    many simultaneous players or very high resolutions.
  - Position updates arrive on libvlc's TimeChanged cadence (not per frame);
    do not poll Position on a timer, subscribe to PlaybackSession.PositionChanged.
  - Playback state and volume are also polled by a 16 ms engine timer to catch
    changes libvlc does not signal (buffering end, external volume changes).

COMMON PITFALLS TO AVOID
========================
  - Referencing the package in a head project, or looking for a per-head
    package: reference it ONCE in .Core. The Windows heads still need the
    separate VideoLAN.LibVLC.Windows package; the Linux machines need apt.
  - Forgetting the native runtime: creating the first MediaPlayer (which
    MediaPlayerElement does at first layout) throws PlatformNotSupportedException
    with the install hint quoted above.
  - Player.MediaPlayer is null before the element's template is applied.
    Subscribe to its events from Loaded (or later), or create your own
    MediaPlayer and assign it with SetMediaPlayer / the MediaPlayer property.
  - MediaSource.CreateFromStream / CreateFromStorageFile throw
    NotImplementedException; only CreateFromUri is implemented. Embedded
    resources must be written to a file and played by URI.
  - MediaPlaybackSession.NaturalVideoWidth/Height throw NotImplementedException.
  - StepBackwardOneFrame() throws NotImplementedException (VLC steps forward
    only). SetUriSource/SetFileSource/SetStreamSource/SetMediaSource are
    deprecated and throw; set MediaPlayer.Source instead.
  - AutoPlay on the element is copied to the MediaPlayer when the element
    LOADS; if you swap in your own MediaPlayer later, set its AutoPlay yourself.
  - Windows heads: MediaPlayer.Stop() is dispatched to a thread-pool thread
    (calling libvlc's stop on the UI thread deadlocks there), so Stop() returns
    before playback has actually stopped. Do not immediately assume the state
    is None after Stop() on Windows; watch PlaybackStateChanged instead.
  - Linux: silent playback with no error is usually an OS-level mute (see LINUX
    AUDIO NOTES) - check the log for the "muted at the OS audio layer" warning
    and unmute in the system mixer; the app will not override it.
  - X11HostBuilder.PreloadMediaPlayer(true) / Win32HostBuilder
    .PreloadMediaPlayer(true) have no effect with this package; call
    SkiaMediaPlayerExtension.PreloadVlc() yourself.
  - IsFullWindow before the element is in the visual tree does nothing (a
    warning is logged). Toggle it from a handler that runs after Loaded.
  - MediaFailed always reports MediaPlayerError.Unknown; inspect ErrorMessage
    and the libvlc log output for the real cause.
  - Do not construct SkiaMediaPlayerExtension or
    SkiaMediaPlayerPresenterExtension yourself; the framework does.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - macOS: not used there; the macOS head plays media with AVFoundation on its
    own, without libvlc.
  - No native libvlc binaries are shipped: Linux needs apt packages, Windows
    heads need VideoLAN.LibVLC.Windows.
  - No stream, storage-file or embedded-resource sources (URI only).
  - No public natural-video-size readback (MediaPlaybackSession
    .NaturalVideoWidth/Height throw); no backward frame stepping; no compact
    overlay; no subtitle/marker/video-frame-available events; no
    CurrentState/buffering-progress reporting (BufferingProgress is always 0).
  - No hardware-accelerated zero-copy presentation: frames always pass through
    system memory (that is what makes the add-in head-agnostic).
  - Full-screen is the host window's full-screen mode; there is no separate
    full-screen window or multi-monitor targeting.
  - The legacy X11/Win32 native-child-window add-ins in the repository are not
    published and are not an alternative.

WORKING EXAMPLES ON GITHUB
==========================
  - MediaPlayerDemo (all six heads, URL box + Stretch selector +
    MediaPlayerElement with transport controls):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/MediaPlayerDemo
    The shared page is MediaPlayerDemo.UI/Views/MainPage.xaml(.cs): it sets
    Player.Source = MediaSource.CreateFromUri(uri) and switches Player.Stretch
    from a ComboBox. The Windows heads carry the VideoLAN.LibVLC.Windows
    reference; the Linux heads rely on the apt-installed runtime.
  - The add-in source itself (small; two public types):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.MediaPlayer.Skia

QUICK REFERENCE CARD
====================
Package        CodeBrix.Platform.MediaPlayer.LgplLicenseForever   LGPL-2.1-or-later
Reference in   .Core ONLY (once); heads inherit it
Heads          Win32, WPF, X11, Wayland, FrameBuffer (macOS: inert, AVFoundation)
Native runtime Linux: sudo apt install libvlc5 vlc-plugin-base
               Windows heads: VideoLAN.LibVLC.Windows package (head csproj)
               Optional Linux HW copy-back: sudo apt install vlc-plugin-video-output
Warm-up        CodeBrix.Platform.UI.MediaPlayer.Skia.SkiaMediaPlayerExtension.PreloadVlc()
Missing libvlc PlatformNotSupportedException (inner VLCException) at first MediaPlayer

XAML           <MediaPlayerElement x:Name="Player" AutoPlay="True"
                   AreTransportControlsEnabled="True" Stretch="Uniform" />
Source         Player.Source = MediaSource.CreateFromUri(new Uri(...));
               schemes: http(s)://, file:///, ms-appx:///, ms-appdata://, relative=ms-appx
               NOT: CreateFromStream / CreateFromStorageFile (throw)
Playlist       new MediaPlaybackList { Items = { new MediaPlaybackItem(MediaSource) } }
Transport      MediaPlayer.Play() / Pause() / Stop() / NextTrack() / PreviousTrack()
State          MediaPlayer.PlaybackSession.PlaybackState (None/Opening/Buffering/Playing/Paused)
Position       MediaPlayer.PlaybackSession.Position (get/set) + PositionChanged
Duration       MediaPlayer.NaturalDuration (valid after MediaOpened)
Volume         MediaPlayer.Volume 0..1, IsMuted; VolumeChanged / IsMutedChanged
Looping        MediaPlayer.IsLoopingEnabled (one media), IsLoopingAllEnabled (list)
Rate           MediaPlayer.PlaybackRate
Video?         MediaPlayer.IsVideo (+ NaturalVideoDimensionChanged event)
Events         MediaOpened, MediaEnded, MediaFailed(args.ErrorMessage), SourceChanged,
               VolumeChanged, IsMutedChanged, NaturalVideoDimensionChanged - all on
               the UI thread (SeekCompleted is NOT raised by this engine)
Stretch        None | Fill | Uniform | UniformToFill (applied at paint time)
Full screen    Player.IsFullWindow = true/false (element must be loaded)
No-ops/throws  ToggleCompactOverlay (no-op); StepBackwardOneFrame,
               MediaPlaybackSession.NaturalVideoWidth/Height (throw)
Host builders  X11/Win32 PreloadMediaPlayer(bool) has NO effect with this package
