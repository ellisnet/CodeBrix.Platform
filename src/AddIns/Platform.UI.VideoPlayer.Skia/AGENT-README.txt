================================================================================
AGENT-README: CodeBrix.Platform.VideoPlayer
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.VideoPlayer.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
Video playback for CodeBrix.Platform applications, delivered as one
XAML-declarable element. Plays AV1 video from WebM and Matroska containers and
from CodeBrix ".cbv" video files, with Ogg Vorbis or Opus sound. Target: .NET 10
or later. Three public types, all in one namespace:

  VideoPlayer     A [Bindable] Panel that shows the picture: a file player with
                  Play/Pause/Stop/Seek, volume, looping, a position that two-way
                  binds to a Slider for scrubbing, Stretch letterboxing, an
                  effect chain, drawable layers, and captions and chapters as
                  data.
  VideoPlayerFailedEventArgs
                  The payload of the MediaFailed event.
  VideoPlayerRenderPathChangedEventArgs
                  The payload of the RenderPathChanged event.

The transport surface is the AudioPlayer add-in's element, member for member, so
ONE scrubber markup drives either kind of player.

There is no per-OS engine and nothing to apt install: the container readers, the
demultiplexer, the clock and the sound are fully managed, and the picture is
composed with SkiaSharp. The add-in is live on all six heads — Windows
Win32-Skia, Windows WPF-Skia, Linux X11, Linux Wayland, Linux FrameBuffer and
macOS.

TWO PACKAGES THE APPLICATION SUPPLIES (this add-in does not, and must not)
-------------------------------------------------------------------------
  AV1 decoding is BSD-2-Clause, and Opus is BSD-3-Clause; this package is
  Apache-2.0 and takes neither as a dependency. An application that plays video
  therefore references and registers them itself:

      CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever   CodeBrixVideoPlaybackDav1d.Register()
      CodeBrix.Audio.Opus.BsdLicenseForever            CodeBrixAudioOpus.Register()

  The first is needed for every AV1 file — which is every file this family
  AUTHORS; no coded video decodes without it. The one thing that plays with
  neither package is an uncompressed (V_UNCOMPRESSED) track, whose decoder is
  built into the playback core: a test-clip and tooling format, enormous on
  disk, never something to ship. The second is needed only for a soundtrack
  encoded as Opus (Vorbis needs nothing). Both calls go in the application's start-up, once, before a source is
  opened; there is deliberately no module initializer doing it for you, because
  that works in a debug build and silently does not run in a trimmed publish.
  Until the calls are made, MediaFailed carries a message naming the package and
  the call.

CONSUMPTION PATTERN: the Lottie / AudioPlayer pattern — application code
references this add-in's own public types directly (there is no framework
contract control for video that this add-in extends):

    xmlns:video="using:CodeBrix.Platform.UI.VideoPlayer.Skia"
    <video:VideoPlayer x:Name="Player"
                       Source="ms-appx:///Assets/video/clip.cbv"
                       Stretch="Uniform" />

Unlike the AudioPlayer element, this one IS visual: give it a place with a size
(a Grid cell, or Width/Height) or you will see nothing.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.VideoPlayer.ApacheLicenseForever
    dotnet add package CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever
    dotnet add package CodeBrix.Audio.Opus.BsdLicenseForever        (Opus sound only)

Reference them from the project that carries your framework package references
(the application's .Core project in the standard CodeBrix.Platform layout); the
XAML in the shared .UI project then resolves the video: namespace.

Dependencies of this package (flow in automatically, no separate install):
  CodeBrix.Platform.ApacheLicenseForever            the core framework
  CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
                                                    the off-screen GPU Skia
                                                    context this element takes
                                                    wherever a head can give one
  CodeBrix.VideoPlayback.MitLicenseForever          containers, demultiplexer,
                                                    clock, transport, captions,
                                                    chapters, the effect chain
                                                    and the colour-shader source

That is the whole list: THREE dependencies. In particular there is no
CodeBrix.VideoPlayback.Skia - that package is the playback engine's own
SkiaSharp presenter, for hosts outside this family (WPF, WinUI, MAUI, Avalonia),
and it pins its own SkiaSharp version. This family publishes as one unit and
pins ONE SkiaSharp, and an assembly compiled against one SkiaSharp and run
against another fails the moment SkiaSharp changes a signature it uses. So the
composing presenter and its colour shader are this add-in's own internal code,
built against the family's pin, and your application carries exactly one
SkiaSharp - the framework's.

Bringing Graphics3DGL in is what makes <video:VideoPlayer Source="…"/> need zero
GPU wiring from the application. The cost, stated plainly: a consumer also gets
CodeBrix.Platform.OpenGL.MitLicenseForever and the vendored macOS ANGLE dylibs
(~16 MB under runtimes/osx/native).

License: Apache-2.0. Requirements: none beyond the framework's own.

KEY NAMESPACES / USINGS
=======================
    xmlns:video="using:CodeBrix.Platform.UI.VideoPlayer.Skia"      (XAML)
    using CodeBrix.Platform.UI.VideoPlayer.Skia;                    (C#)

Every type this package defines lives in that one namespace - including the two
drawing seams, IVideoLayer and VideoComposingEventArgs, which are this package's
own because they name the framework's SKCanvas. Several other types on the
element's surface belong to the playback engine that flows in with it, and are
surfaced deliberately rather than wrapped, so nothing has to be converted:

    using CodeBrix.VideoPlayback.Rendering;        // VideoRenderPath, VideoRenderBackend,
                                                  //   VideoCompositionContext
    using CodeBrix.VideoPlayback.Effects;          // IVideoFrameEffect, LutEffect
    using CodeBrix.VideoPlayback.Captions;         // CaptionTrack, CaptionCue
    using CodeBrix.VideoPlayback.Chapters;         // Chapter
    using CodeBrix.VideoPlayback.Playback;         // ChapterChangedEventArgs
    using CodeBrix.VideoPlayback.Sources;          // FileSourceMode
    using CodeBrix.VideoPlayback.Containers;       // MediaTrackInfo
    using CodeBrix.VideoPlayback.Presentation;     // VideoFramePresenterStatistics

Stretch is the XAML one (Microsoft.UI.Xaml.Media.Stretch), so it reads exactly as
it does on an Image.

CORE API REFERENCE
==================

VideoPlayer — the source
------------------------
  string Source
      A file path, a file:// URI, an http:// or https:// address, an
      ms-appx:///Assets/... application-asset URI, or an
      embedded://AssemblyName/Manifest.Resource.Name embedded resource. In the
      embedded form "." means the application's own assembly and "(assembly)"
      inside the resource name is replaced with the resolved assembly's name.
      Setting it opens the file — Duration is available the moment it returns —
      and, when AutoPlay is true, starts playback. Set "" to unload.

  void SetSourceStream(Stream stream)
      For bytes that are neither a file nor an embedded resource. The stream
      should be seekable (a forward-only one plays but cannot seek). Clears
      Source.

  FileSourceMode SourceMode        Streaming (default) | MemoryMapped | Preloaded
      How a LOCAL FILE is read. Preloaded reads the whole file into memory once,
      which is what makes a short clip loop with no disk access at all; it costs
      the file's size in memory, and the read happens while Source is being set.
      Ignored for streams and addresses. Read AT OPEN, so set it BEFORE Source.

  bool AutoPlay                    default false

VideoPlayer — the transport (identical to the AudioPlayer element)
------------------------------------------------------------------
  TimeSpan Position         (two-way)   TimeSpan Duration          (read-only)
  double   PositionSeconds  (two-way)   double   DurationSeconds   (read-only)
  bool     IsPlaying        (read-only) double   Volume            0.0 .. 1.0
  bool     IsMuted                      bool     IsLooping
  TimeSpan PositionUpdateInterval       default 150 ms

  void Play()      void Pause()      void Stop()      void Seek(TimeSpan)

  Position/PositionSeconds are refreshed on the UI thread by a dispatcher timer
  while playing, and writing either one seeks — debounced, so a whole slider drag
  lands ONE seek when the thumb is released. Seek(TimeSpan) is immediate and not
  debounced. A seek while paused still puts the new frame on screen.

  events  MediaOpened, PlaybackEnded, MediaFailed

VideoPlayer — the picture
-------------------------
  Stretch Stretch                  None | Fill | Uniform (default) | UniformToFill
      Applied at paint time, so changing it costs a repaint and nothing else.
      Uniform letterboxes; UniformToFill covers and crops. The picture keeps its
      DISPLAY aspect ratio, so a portrait recording is drawn portrait inside a
      landscape window with bars either side.

  SKImage? CapturePresentedFrame()
      An independent copy of the picture on screen — the COMPOSED frame, with the
      effect chain and every layer already in it — which the CALLER owns and must
      dispose. Null before the first frame. Safe from any thread. This is the
      screenshot hook, and the way a headless verification proves pixels flowed.

  VideoFramePresenterStatistics FrameStatistics   (read-only)
      Posted / Presented / Superseded / Late / Dropped frame counters.

VideoPlayer — the render path
-----------------------------
  VideoRenderPath RenderPath       GpuAuto (default) | GpuNoFallback | Cpu
      What this player wants, and what to do when the graphics device cannot be
      had. GpuAuto takes the graphics device wherever a context can be created
      and quietly falls back to the processor where it cannot — no exception, no
      user-facing error. GpuNoFallback fails with a message instead of degrading,
      for an application whose picture is meaningless without its effect chain.
      Cpu forces the processor path even where a graphics device exists.

      MUST BE SET BEFORE A SOURCE IS OPENED. Changing it while a source is open
      throws InvalidOperationException; close the source (Source = "") first.

  bool AllowEffectsOnCpu           default false
      True to apply the effect chain on the processor path too, at the cost of a
      table lookup per pixel of every frame. Left false, a configured chain stays
      configured but is not applied when the processor path is running.

  VideoRenderBackend ActiveRenderPath   (read-only)   Gpu | Cpu
  bool               EffectsActive      (read-only)
  event RenderPathChanged
      What is ACTUALLY running, and whether the chain is actually being applied.
      Bind a status line to these rather than assuming what RenderPath got you.

VideoPlayer — effects and composition
-------------------------------------
  ObservableCollection<IVideoFrameEffect> Effects
      The ordered chain applied to the picture — colour lookup tables first and
      foremost (LutEffect.FromCubeFile(path, percent)). However many are in the
      chain they are composed into ONE resultant table and cost a single lookup
      per pixel. The collection belongs to the element and keeps its identity for
      the element's whole life, so a binding to it never goes stale.
      A change reaches the screen straight away whether playback is running or
      not: while playing the next frame is composed through the new chain, and
      while PAUSED the element composes the frame already on screen again. No
      seek and no Play is needed to see a grade you just dialled in. The same is
      true of Layers and of AllowEffectsOnCpu.

  ObservableCollection<IVideoLayer> Layers
  event EventHandler<VideoComposingEventArgs> Composing
      Drawn OVER the video, INSIDE the composition, in VIDEO coordinates — so
      what they draw is part of the picture and CapturePresentedFrame() captures
      it. Subtitles, a heads-up display, annotation, a webcam picture-in-picture.
      Use Composing for a one-off; write an IVideoLayer for anything reusable.

VideoPlayer — captions and chapters (data only)
-----------------------------------------------
  IReadOnlyList<CaptionTrack> CaptionTracks     (read-only)
  CaptionTrack? SelectedCaptionTrack            null = off
  bool ShowForcedCaptions                       default true
  IReadOnlyList<CaptionCue> ActiveCues          (read-only)
  event EventHandler CaptionCuesChanged

  IReadOnlyList<Chapter> Chapters               (read-only)
  Chapter? CurrentChapter                       (read-only)
  event EventHandler<ChapterChangedEventArgs> ChapterChanged
  void SeekToChapter(int index)
  bool NextChapter()      bool PreviousChapter()

  IReadOnlyList<MediaTrackInfo> Tracks          (read-only)

  Captions are DATA: the player says which cues are current and the application
  decides how — and whether — to draw them. An IVideoLayer is the natural place.

VideoPlayer — teardown
----------------------
  void Close()
      Stops playback and releases everything the player owns: the decode threads,
      the soundtrack, the composition surface and the graphics context. The
      element cannot play again afterwards.

      OPTIONAL. Leaving the visual tree already releases the graphics resources
      (the only moment the underlying window is reliably still alive) and pauses
      playback, and re-entering the tree brings the picture back. Call Close()
      when a page wants the decode threads and the audio device gone at a moment
      of its choosing. The name is Close rather than Shutdown because the
      framework's UIElement already has an internal Shutdown that nothing may
      hide.

FORMATS AND CONTAINERS
======================
  Video      AV1 ("av01"), 8/10/12-bit, 4:2:0 / 4:2:2 / 4:4:4, through the
             separately-registered Dav1d package; and uncompressed video
             (V_UNCOMPRESSED), which the playback core decodes itself with no
             package at all — a test and tooling format, enormous on disk.
             There is no H.264/HEVC and no MP4: that is the point of this
             family — nothing royalty-bearing and nothing LGPL/GPL in a shipped
             application.
  Sound      Ogg Vorbis (built in) and Opus (register CodeBrix.Audio.Opus).
  Containers WebM and Matroska (.webm, .mkv), and CodeBrix ".cbv" video in both
             its flavours: the WebM-profile one (which any browser also opens)
             and the bespoke one.
  Captions   WebVTT / SRT text tracks, carried as data.
  Chapters   Flat, single-edition, with per-language titles.

WHAT DECIDES WHICH RENDER PATH RUNS
===================================
On the graphics path the three colour planes are uploaded as single-channel
textures and ONE shader does the colour conversion and the whole effect chain in
a single pass at full precision. On the processor path the core's vector
converter turns the frame into BGRA pixels. Neither is a degraded version of the
other: the processor path is the right answer on a machine with no usable
graphics device — the frame-buffer head, for instance — and it is a tested,
benchmarked configuration in its own right.

The element creates its off-screen graphics context LAZILY, on the first frame
after it is loaded and has a live XamlRoot; one informational log line names the
backend it chose (OpenGL/GLES on the Windows and Linux heads, Metal on macOS),
and one warning explains a failure. On Windows a missing OpenGL driver is the
usual cause of that failure, and Microsoft's free "OpenCL and OpenGL
Compatibility Pack" (https://apps.microsoft.com/detail/9NQPSL29BFFF) can supply
one; the warning says so.

The context is torn down when the element LEAVES THE VISUAL TREE, not when it is
disposed, and rebuilt lazily when it comes back. That is not an optimization: on
WGL the off-screen context is built on the window's own device context, so once
the window is destroyed the graphics resources can never be released at all.

COMPLETE EXAMPLES
=================

1. The smallest thing that plays a video
----------------------------------------
App.xaml.cs, once at start-up:

    using CodeBrix.VideoPlayback.Dav1d;
    using CodeBrix.Audio.Opus;

    public App()
    {
        CodeBrixVideoPlaybackDav1d.Register();   // AV1 - needed for every coded file
        CodeBrixAudioOpus.Register();            // only for Opus soundtracks
        InitializeComponent();
    }

MainPage.xaml:

    <Page xmlns:video="using:CodeBrix.Platform.UI.VideoPlayer.Skia" ...>
        <Grid>
            <video:VideoPlayer Source="ms-appx:///Assets/video/clip.cbv"
                               AutoPlay="True" Stretch="Uniform" />
        </Grid>
    </Page>

2. Transport, scrubber and a duration readout — the AudioPlayer markup
----------------------------------------------------------------------
    <Grid RowDefinitions="*,Auto,Auto">
        <video:VideoPlayer x:Name="Player" Grid.Row="0"
                           Source="ms-appx:///Assets/video/clip.webm"
                           MediaFailed="Player_MediaFailed" />

        <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
            <TextBlock Text="{Binding Position, ElementName=Player}" Width="80" />
            <Slider Width="420"
                    Maximum="{Binding DurationSeconds, ElementName=Player}"
                    Value="{Binding PositionSeconds, ElementName=Player, Mode=TwoWay}"
                    StepFrequency="0.1" />
            <TextBlock Text="{Binding Duration, ElementName=Player}" Width="80" />
        </StackPanel>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="8">
            <Button Content="Play"  Click="Play_Click" />
            <Button Content="Pause" Click="Pause_Click" />
            <Button Content="Stop"  Click="Stop_Click" />
            <ToggleSwitch OnContent="Looping" OffContent="Loop"
                          IsOn="{Binding IsLooping, ElementName=Player, Mode=TwoWay}" />
            <TextBlock Text="{Binding ActiveRenderPath, ElementName=Player}"
                       VerticalAlignment="Center" />
        </StackPanel>
    </Grid>

3. A colour grade, and proving it is running
--------------------------------------------
    using CodeBrix.VideoPlayback.Effects;

    // Before Play, or while paused: the chain is composed once, not per frame.
    Player.Effects.Add(LutEffect.FromCubeFile(lutPath, applyAtPercent: 40));

    Player.RenderPathChanged += (_, e) =>
        Status.Text = e.EffectsActive
            ? $"grading on the {e.ActiveRenderPath} path"
            : $"running on the {e.ActiveRenderPath} path - the grade is not applied";

  EffectsActive is false, deliberately, when the processor path is running and
  AllowEffectsOnCpu was left false. Say so in the interface rather than quietly
  costing every pixel a table lookup.

4. Demanding the graphics device
--------------------------------
    // Before any Source is set.
    Player.RenderPath = VideoRenderPath.GpuNoFallback;
    Player.MediaFailed += (_, e) => Status.Text = e.Message;   // says why, plainly

5. A short clip looping out of memory
-------------------------------------
    Player.SourceMode = FileSourceMode.Preloaded;   // BEFORE Source
    Player.IsLooping = true;
    Player.Source = "ms-appx:///Assets/video/logo_sting.cbv";
    Player.Play();

6. Drawing on the picture
-------------------------
    Player.Composing += (_, e) =>
    {
        // e.Canvas is the composition surface; its coordinates are the VIDEO's, and
        // e.Context carries the video rect, the timestamp, the frame number, the
        // running backend and whether effects are active.
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        e.Canvas.DrawCircle(e.Context.VideoRect.MidX, e.Context.VideoRect.MidY, 24, paint);
    };

7. A screenshot of what is on screen
------------------------------------
    using SKImage? shot = Player.CapturePresentedFrame();
    if (shot is not null)
    {
        using SKData png = shot.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
    }

MINIMUM VIABLE PROJECT
======================
  MyApp.Core.csproj
      <PackageReference Include="CodeBrix.Platform.VideoPlayer.ApacheLicenseForever" Version="…" />
      <PackageReference Include="CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever"     Version="…" />
      <PackageReference Include="CodeBrix.Audio.Opus.BsdLicenseForever"              Version="…" />
      <Content Include="..\assets\clip.cbv" Link="Assets\video\clip.cbv"
               CopyToOutputDirectory="PreserveNewest" />

  App.xaml.cs        CodeBrixVideoPlaybackDav1d.Register(); CodeBrixAudioOpus.Register();
  MainPage.xaml      xmlns:video + one <video:VideoPlayer/> in a Grid cell

  That is the whole integration. No head project changes, no native prerequisite,
  no GPU wiring.

PERFORMANCE TIPS
================
  - Let the graphics path have the effect chain. A LUT there is one texture
    sample per pixel; the same chain under AllowEffectsOnCpu is a table lookup
    per pixel on the processor, every frame.
  - Preloaded is for SHORT clips. A feature-length file read into memory is
    exactly as expensive as it sounds; Streaming is the default for a reason.
  - Set PositionUpdateInterval no finer than a scrubber can show. 150 ms is
    already 6-7 layout passes a second.
  - Resizing the window suppresses live presents for half a second after the last
    size change and re-blits the picture letterboxed instead, deliberately: a
    backlog of full-size blits is what makes a resize go chunky and then catch
    up. Nothing to configure; just do not be surprised by it.
  - A repaint (overlap, theme change, resize) re-blits the picture already
    presented. It never asks the decoder for anything.

COMMON PITFALLS TO AVOID
========================
  - Forgetting CodeBrixVideoPlaybackDav1d.Register(). No AV1 file plays without
    it — and every file this family authors is AV1 — so it is the single most
    likely reason a first attempt shows a black rectangle. An uncompressed test
    clip DOES play without it, which is exactly how a missing registration can
    hide until the first real file. Handle MediaFailed while developing: its
    message names the package and the call.
  - Setting RenderPath after Source. It throws, on purpose — the path is chosen
    once, before anything is opened. Set it first.
  - Setting SourceMode after Source. It does not throw; it simply has no effect
    until the NEXT open, because the mode is read when the file is opened.
  - Giving the element no size. It is a visual element: in a StackPanel with no
    Height it is measured to nothing and shows nothing. Put it in a Grid cell, or
    give it Width/Height.
  - Adding children to it. It is a Panel because it hosts its own picture
    surface; put an overlay in a Grid cell above it, or draw inside the picture
    with Layers / Composing.
  - Disposing what CurrentImage-style properties hand you. CapturePresentedFrame()
    returns an image the CALLER owns and must dispose; everything else the element
    exposes belongs to the element.
  - Assuming ActiveRenderPath == Gpu because RenderPath == GpuAuto. GpuAuto is
    permission, not a promise. Read ActiveRenderPath.
  - Expecting an Effects change to be applied on the PROCESSOR path. It is not,
    unless AllowEffectsOnCpu is true — that is deliberate and silent, and
    EffectsActive is what says which is happening. (A change while paused DOES
    reach the screen; the element recomposes the frame it is holding.)
  - Expecting captions to appear on screen. They are data. Drawing them is the
    application's, through an IVideoLayer.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No MP4/ISOBMFF, no H.264/HEVC/AAC. By design; see FORMATS.
  - No hardware (GPU/VA-API) video DECODING. The graphics device is used for
    colour conversion, grading and compositing, not for decoding.
  - No playback rate other than 1.0, no HDR tone-mapping, no rotation metadata
    for foreign files, no DRM.
  - No caption RENDERING, no built-in transport chrome. Both are the
    application's; the element gives it everything it needs.
  - No playlist or queue: one source per element. Chain PlaybackEnded, or declare
    several elements.
  - No streaming protocol beyond plain HTTP(S) progressive/range.
  - It does not decode AV1 or Opus itself; those arrive as the two packages the
    application registers. Uncompressed video is the one codec the playback core
    decodes on its own.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/VideoPlayerDemo
      The reference application for this package (all six heads). Its main page
      declares one VideoPlayer, plays the seven sample clips (three container
      flavours, one of them chaptered), switches render path, applies a LUT, and shows ActiveRenderPath /
      EffectsActive live. Setting CODEBRIX_VIDEOPLAYER_SMOKE to an asset name
      before launching runs a scripted end-to-end verification that captures the
      presented frame to a PNG and exits PASS/FAIL. Start with
      VideoPlayerDemo.UI/Views/MainPage.xaml and its code-behind.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.VideoPlayer.Skia
      The add-in's own source (VideoPlayer.cs and the Internal/ folder), fully
      XML-documented.

QUICK REFERENCE CARD
====================
namespace CodeBrix.Platform.UI.VideoPlayer.Skia
xmlns:video="using:CodeBrix.Platform.UI.VideoPlayer.Skia"

[Bindable] sealed class VideoPlayer : Panel
    string   Source                     TimeSpan Duration          (ro)
    FileSourceMode SourceMode           double   DurationSeconds   (ro)
    bool     AutoPlay                   bool     IsPlaying         (ro)
    TimeSpan Position          (2-way)  double   Volume            0..1
    double   PositionSeconds   (2-way)  bool     IsMuted
    bool     IsLooping                  TimeSpan PositionUpdateInterval
    Stretch  Stretch                    VideoRenderPath RenderPath  (before Source!)
    bool     AllowEffectsOnCpu          VideoRenderBackend ActiveRenderPath (ro)
                                        bool     EffectsActive     (ro)
    ObservableCollection<IVideoFrameEffect> Effects
    ObservableCollection<IVideoLayer>       Layers
    CaptionTrack? SelectedCaptionTrack   bool ShowForcedCaptions
    IReadOnlyList<CaptionTrack> CaptionTracks   (ro)
    IReadOnlyList<CaptionCue>   ActiveCues      (ro)
    IReadOnlyList<Chapter>      Chapters        (ro)
    Chapter? CurrentChapter                     (ro)
    IReadOnlyList<MediaTrackInfo> Tracks        (ro)
    VideoFramePresenterStatistics FrameStatistics (ro)
    void Play();  void Pause();  void Stop();  void Seek(TimeSpan position);
    void SetSourceStream(Stream stream);        void Close();
    void SeekToChapter(int index);  bool NextChapter();  bool PreviousChapter();
    SKImage? CapturePresentedFrame();           // caller owns it
    event EventHandler MediaOpened;             event EventHandler PlaybackEnded;
    event EventHandler<VideoPlayerFailedEventArgs> MediaFailed;
    event EventHandler<VideoPlayerRenderPathChangedEventArgs> RenderPathChanged;
    event EventHandler CaptionCuesChanged;
    event EventHandler<ChapterChangedEventArgs> ChapterChanged;
    event EventHandler<VideoComposingEventArgs> Composing;

sealed class VideoPlayerFailedEventArgs : EventArgs
    string Message { get; }     Exception? Error { get; }

sealed class VideoPlayerRenderPathChangedEventArgs : EventArgs
    VideoRenderBackend ActiveRenderPath { get; }    bool EffectsActive { get; }

Source forms: path | file:// | http(s):// | ms-appx:///Assets/x
              | embedded://Asm/Res.Name ("." = app assembly; "(assembly)"
              placeholder) | Stream

The application registers the two optional codecs, once, at start-up:
    CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever → CodeBrixVideoPlaybackDav1d.Register()
    CodeBrix.Audio.Opus.BsdLicenseForever          → CodeBrixAudioOpus.Register()
