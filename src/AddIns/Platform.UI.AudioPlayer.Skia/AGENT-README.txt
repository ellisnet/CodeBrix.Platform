================================================================================
AGENT-README: CodeBrix.Platform.AudioPlayer
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.AudioPlayer.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
Audio playback (WAV, MP3, Ogg Vorbis, FLAC - and Opus once the application
registers the separate codec package) and MIDI music for CodeBrix.Platform
applications, delivered as XAML-declarable elements plus one static class.
Target: .NET 10 or later. Four public types, all in one namespace:

  AudioPlayer     A non-visual [Bindable] FrameworkElement: a file player with
                  Play/Pause/Stop/Seek, volume, looping, and a position that
                  two-way binds to a Slider for scrubbing.
  MidiPlayer      The same transport, member for member, for a MIDI file
                  rendered through a SoundFont (.sf2) or SFZ (.sfz) instrument;
                  plus tempo, per-channel mixing and an observe-only note hook.
  SoundEffect     Static fire-and-forget sound effects: decoded once, mixed as
                  overlapping voices in the application's shared output.
  AudioPlayerFailedEventArgs
                  The payload of both players' MediaFailed event.

There is no native setup at all: unlike the WebView and MediaPlayer add-ins
there is no per-OS engine and nothing to apt install. Playback is fully managed
via the CodeBrix.Audio.MitLicenseForever package (whose bundled
codebrix_miniaudio backend covers Windows, Linux and macOS, x64 + arm64), so the
add-in is live on all six heads - Windows Win32-Skia, Windows WPF-Skia, Linux
X11, Linux Wayland, Linux FrameBuffer and macOS.

CONSUMPTION PATTERN: unlike the "invisible" WebView/MediaPlayer add-ins, this
one follows the Lottie pattern - application code references the add-in's own
public types directly (there is no WinUI contract control for audio):

    xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia"
    <audio:AudioPlayer x:Name="Player"
        Source="embedded://MyApp.Core/MyApp.Assets.song.mp3" />

Both players are NON-VISUAL elements: they render nothing and take no space,
so declare them anywhere in a page's tree (typically as the first child of the
root Grid) and give them no Width/Height/Margin.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.AudioPlayer.ApacheLicenseForever

Reference it from the project that carries your framework package references
(the application's .Core project in the standard CodeBrix.Platform layout); the
XAML in the shared .UI project then resolves the audio: namespace.

Dependencies (flow in automatically, no separate install):
  CodeBrix.Platform.ApacheLicenseForever    the core framework
  CodeBrix.Audio.MitLicenseForever          the managed audio engine: WAV, MP3,
                                            Ogg Vorbis and FLAC decoding; MIDI;
                                            SoundFont and SFZ synthesis; the
                                            bundled codebrix_miniaudio backend

NOT a dependency - Opus. Opus is BSD-3-Clause rather than MIT, so it ships as
the separate CodeBrix.Audio.Opus.BsdLicenseForever package. An application that
plays .opus files references that package itself and calls
CodeBrixAudioOpus.Register() once at start-up; this add-in needs no change and
no reference to it, because playback resolves codecs through the shared audio
output. See FORMATS AND CODECS below.

License: Apache-2.0. Requirements: none beyond the framework's own.

KEY NAMESPACES / USINGS
=======================
    xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia"      (XAML)
    using CodeBrix.Platform.UI.AudioPlayer.Skia;                    (C#)

Every public type of this package lives in that one namespace. Two namespaces
of the CodeBrix.Audio package (which flows in with this one) come up only at
the edges:

    using CodeBrix.Audio.Wave;     // SharedAudioOutput - only if you pin the
                                   // output sample rate yourself (see SAMPLE
                                   // RATES below)
    using CodeBrix.Audio.Synth;    // CodeBrix.Audio's MIDI-music types; a
                                   // lambda assigned to MidiMessageProcessed
                                   // needs no using at all

SOURCE FORMS (all three types)
==============================
Source strings are resolved the same way by AudioPlayer.Source, MidiPlayer's
Source and .sf2 Instrument, and SoundEffect.Play/Preload:

  a filesystem path                 "/home/me/music/song.mp3", "C:\...\song.mp3"
  a file:// URI                     "file:///home/me/music/song.mp3"
  an ms-appx:/// asset URI          "ms-appx:///Assets/theme.mid" - resolved
                                    under the application's installed folder.
                                    The two-slash form ms-appx://LibraryName/x
                                    (an asset that arrived in a library package)
                                    names the same thing. Spaces and other
                                    escaped characters are unescaped, so an
                                    asset called "My Song.mp3" resolves.
  an embedded:// URI                "embedded://AssemblyName/Manifest.Resource.Name"
                                    - an embedded resource, the same scheme the
                                    SVG and Lottie add-ins use. "." as the
                                    assembly name means the application
                                    assembly; "(assembly)" inside the resource
                                    name is replaced with the resolved assembly
                                    name.
  a Stream                          AudioPlayer.SetSourceStream(Stream) and
                                    SoundEffect.Play(Stream, double).

An .sfz instrument is the one exception: it takes ONLY a filesystem path or an
ms-appx:/// URI (see MidiPlayer below for why).

CORE API REFERENCE
==================

AudioPlayer  ([Bindable] sealed, : FrameworkElement)
----------------------------------------------------
Dependency properties (every one is bindable; read-only ones have private
setters):

    string   Source                  file path / URI as above; setting it loads
                                     the file synchronously (Duration is valid
                                     immediately) and, when AutoPlay is true,
                                     starts playback. "" unloads.
    bool     AutoPlay                default false
    TimeSpan Position                two-way; see SCRUBBER BINDING
    double   PositionSeconds         Position in seconds, two-way, no converter
    TimeSpan Duration                read-only; TimeSpan.Zero while unloaded
    double   DurationSeconds         read-only; bind a Slider.Maximum to it
    bool     IsPlaying               read-only
    double   Volume                  0.0..1.0, default 1.0 (clamped)
    bool     IsLooping               default false; restarts at the end
    TimeSpan PositionUpdateInterval  refresh cadence of Position while
                                     playing; default 150 ms

Methods:

    void Play()                      no-op until a source is loaded; a start
                                     failure raises MediaFailed instead of
                                     throwing
    void Pause()                     keeps the position
    void Stop()                      rewinds to the beginning
    void Seek(TimeSpan position)     immediate (no debounce); clamped to
                                     0..Duration; no-op while unloaded
    void SetSourceStream(Stream stream)
                                     loads any supported format from a stream
                                     (should be seekable). The player TAKES
                                     OWNERSHIP and disposes it when another
                                     source is loaded. Clears Source. Throws
                                     ArgumentNullException on null.

Events (both raised on the UI thread):

    event EventHandler PlaybackEnded
        The natural end of the file. NOT raised when IsLooping is true, when
        Stop() is called, or when playback fails.
    event EventHandler<AudioPlayerFailedEventArgs> MediaFailed
        A source failed to load or play (missing file, unsupported format,
        unreadable stream). Load/play failures raise this event and log at
        Error; they never throw into a binding path.

Lifecycle: the element pauses itself when it is Unloaded from the visual tree
(navigating away from its page pauses the audio). Keep a player that must
outlive a page on a page that stays loaded, e.g. the shell.

SCRUBBER BINDING (the headline feature)
---------------------------------------
Position/PositionSeconds update on the UI thread while playing AND are two-way
bindable: writes seek the audio, debounced 200 ms so a Slider drag lands ONE
seek where the user releases the thumb ("seek on release"):

    <Slider Maximum="{Binding DurationSeconds, ElementName=Player}"
            Value="{Binding PositionSeconds, ElementName=Player, Mode=TwoWay}" />

Position and PositionSeconds always mirror each other; write whichever is
convenient. Seek(TimeSpan) bypasses the debounce.

AudioPlayerFailedEventArgs  (sealed, : EventArgs)
-------------------------------------------------
    string    Message    what failed - for an .opus file without the Opus
                         package registered, the message also says so and what
                         to do (see FORMATS AND CODECS)
    Exception Error      the underlying exception

The constructor is internal: the players create these; consumers only read
them. Both AudioPlayer.MediaFailed and MidiPlayer.MediaFailed carry this type.

SoundEffect  (static class)
---------------------------
    static bool Play(string source, double volume = 1.0)
    static bool Play(Stream stream, double volume = 1.0)
    static void Preload(string source)
    static void ClearCache()

Play(string) is fire-and-forget: each call is one voice in the application's
single shared output device, so effects overlap each other and the AudioPlayer
cheaply. An effect is DECODED ONCE, on its first play, and the decoded audio is
kept, so a sound triggered repeatedly costs nothing but mixing, and no file
access or decoding ever happens on the real-time audio thread. Preload(source)
reads the bytes ahead of time (it does not decode, and does not start the
output device); ClearCache() releases every cached byte array and decoded clip.
Volume is clamped to 0..1.

Play returns FALSE (and logs at Error) instead of throwing when an effect fails
to resolve, decode or start, so a missing effect never crashes the app.

Play(Stream) reads the stream in full before returning, so the caller can
dispose it immediately afterwards. It throws ArgumentNullException on null. A
stream has no identity to cache under, so it DECODES EVERY CALL - use the
string overload for anything played repeatedly.

MidiPlayer  ([Bindable] sealed, : FrameworkElement)
---------------------------------------------------
Same namespace, same shape as AudioPlayer: a non-visual element that
synthesizes a MIDI file through a SoundFont (.sf2) or an SFZ (.sfz) instrument.

    <audio:MidiPlayer x:Name="Music"
        Source="ms-appx:///Assets/theme.mid"
        Instrument="ms-appx:///Assets/Piano/Piano.sfz" />

Transport surface - AudioPlayer's, member for member: Position/PositionSeconds
(two-way, debounced), Duration/DurationSeconds, IsPlaying, Volume, IsLooping,
AutoPlay, PositionUpdateInterval, Play()/Pause()/Stop()/Seek(TimeSpan),
PlaybackEnded, MediaFailed - so THE SAME SCRUBBER MARKUP DRIVES EITHER PLAYER.
Differences in detail: IsLooping repeats from the sequence's own loop point
when it carries one (from the beginning otherwise); Stop() also silences every
sounding voice and clears the controller state the sequence had set; Seek
replays controller state up to the target so instruments sound right, but
notes already sounding there do not resume - a seek into the middle of a held
chord starts from silence.

Additional dependency properties:

    string   Instrument          the .sf2 or .sfz to render through; the file
                                 EXTENSION decides which synthesizer runs.
                                 Loading starts once BOTH Source and Instrument
                                 are non-empty.
    bool     IsLoading           read-only; true while the background load runs
    double   Speed               tempo multiplier, default 1.0 (0.5 half speed,
                                 2.0 double); pitch does not change. Negative
                                 values clamp to 0, which freezes the transport
                                 while sounding notes ring out.
    int      ActiveVoiceCount    read-only; synthesizer voices sounding now,
                                 refreshed with Position while playing

Plain properties, valid from MediaOpened onward:

    IReadOnlyList<string>       InstrumentProblems
        What the loaded instrument could not make sense of (a referenced sample
        file that is missing, for example). The instrument still loads; the
        regions it could not build are silent.
    IReadOnlyCollection<string> UnsupportedInstrumentOpcodes
        SFZ opcodes the instrument uses that the synthesizer does not implement.
        Empty for a SoundFont.
    Both empty means fully supported. Show them rather than guessing when an
    instrument sounds wrong.

Additional event (UI thread):

    event EventHandler MediaOpened
        The instrument and sequence have loaded and the transport is live.
        Duration is set by the time this is raised.

Mixing and playing alongside the sequence (safe from any thread; all no-ops
until a load has completed):

    void SendMidiMessage(int channel, int command, int data1, int data2)
        channel 0-15; command is the command nibble: 0x80 note-off, 0x90
        note-on, 0xB0 control change, 0xC0 program change, 0xE0 pitch bend;
        data1/data2 0-127 (data2 ignored by one-byte commands). Throws
        ArgumentOutOfRangeException for a channel outside 0-15.
    void SetChannelVolume(int channel, double volume)
        MIDI control change 7; volume 0.0..1.0, clamped. The sequence's own
        volume automation still applies - a track that writes CC7 overwrites
        this the next time it does so.
    void SetChannelPan(int channel, double pan)
        MIDI control change 10; pan -1.0 (full left) .. 0.0 (centre) .. 1.0
        (full right), clamped.
    void SetChannelProgram(int channel, int program)
        MIDI program change; program 0-127. Which sound a number selects is the
        loaded instrument's business. Throws ArgumentOutOfRangeException for a
        channel outside 0-15 or a program outside 0-127.

The OBSERVE-ONLY message hook, for driving something on screen off the notes:

    MidiMessageObserver? MidiMessageProcessed { get; set; }

MidiMessageObserver is CodeBrix.Audio's observe-only delegate; it is raised
AFTER each MIDI message has reached the synthesizer, so it cannot break
playback. Assign a lambda of the shape
    (channel, command, note, velocity) => { ... }
It runs on the real-time AUDIO THREAD: keep it fast and allocation-free, do not
touch the UI in it, and do not call back into the player - hand what it sees to
the UI thread through the element's DispatcherQueue (see COMPLETE EXAMPLES).
CodeBrix.Audio's other hook - the modifying one, which REPLACES delivery and
silences the music if a caller does not re-deliver - is deliberately not
exposed by this element.

LOADING IS ASYNCHRONOUS, and that is the one real difference from AudioPlayer.
Instruments are big - a sampled piano is hundreds of MB of decoded audio - so
setting Source or Instrument raises IsLoading (bind a status line or a
ProgressRing to it), loads on a thread-pool thread, and raises MediaOpened when
the transport is live. Duration is valid from MediaOpened onward, NOT from the
property set; Play() before that is a no-op (set AutoPlay instead). Setting
Source and Instrument one after the other queues ONE load that covers both;
a newer set while a load is running supersedes it. Instruments are cached
process-wide (an .sfz by its resolved path; an .sf2 by path when given as a
path or ms-appx:/// URI - an .sf2 from an embedded:// resource is read each
time), so a second player sharing one pays nothing.

SOURCE FORMS FOR AN INSTRUMENT: a .sf2 takes every form Source does (path,
ms-appx:///, embedded://). A .sfz takes ONLY a file path or an ms-appx:///
URI. An .sfz is not one file - it references its samples as separate files
beside it (and may #include others), so it needs a real directory to resolve
against; an embedded resource has none, and MediaFailed says exactly that.

FORMATS AND CODECS
------------------
WAV, MP3, Ogg Vorbis and FLAC, for AudioPlayer and SoundEffect alike - all
decoded by the CodeBrix.Audio package that flows in with this one. Ogg Vorbis
matters for anything consuming free game-asset packs (kenney.nl audio is 100%
.ogg).

OPUS is not included (BSD-3-Clause, separate package - see INSTALLATION). An
application that needs it references CodeBrix.Audio.Opus.BsdLicenseForever and
calls CodeBrixAudioOpus.Register() once at start-up; from then on .opus plays
through AudioPlayer and SoundEffect like any other format, because both resolve
codecs through the shared audio output.

An .opus file played WITHOUT that registration fails with a MediaFailed
message (or SoundEffect log line) naming Opus and saying what to do. This
add-in supplies that explanation itself, because the engine's own message
names the CONTAINER instead: "No registered and working codec factory found
for decoding format 'ogg'". Ogg is a container, so that message is the same
for Vorbis, Opus and Ogg FLAC; the add-in sniffs the failed source and appends
the Opus explanation only where it applies.

SAMPLE RATES AND SharedAudioOutput.Configure
--------------------------------------------
Effects do NOT have to share one sample rate. Each is converted to the output's
format when it is decoded, so an asset pack mixing 22 kHz and 44.1 kHz files
just works, and so does AudioPlayer. That is a property of this add-in's
SoundEffect (which decodes through CodeBrix.Audio's SoundEffectClip, converting
on load). Feeding CodeBrix.Audio's WaveOutEvent yourself is different: it has
no resampler and rejects a source whose rate differs from the running output.
If you drive WaveOutEvent directly alongside this add-in, pin the output
format ONCE at start-up, before the first sound plays:

    using CodeBrix.Audio.Wave;                       // CodeBrix.Audio.MitLicenseForever
    SharedAudioOutput.Configure(sampleRate: 48000);  // Configure(sampleRate[, channels])

SharedAudioOutput lives in the CodeBrix.Audio.Wave namespace of the
CodeBrix.Audio.MitLicenseForever package; it is the one shared output device
every player, effect and WaveOutEvent in the process mixes into. Configure is
optional - left alone, the output adopts the format of the first sound played.

SHIPPING AN INSTRUMENT AS A NUGET PACKAGE
-----------------------------------------
Possible today with no new machinery, because library assets already flow
through the framework's library-asset step (_CodeBrixAddLibraryAssets). Build
the package the way the CodeBrix.Platform.Fonts.OpenSans package is built: a
library project with GenerateLibraryLayout=true, the instrument tree as Content
items with target paths, which packs to lib/<tfm>/<AssemblyName>/... beside an
(empty) <AssemblyName>.uprimarker file. At head-build time the framework's
asset expansion (ExpandPackageAssets_v0) globs that whole folder recursively
and copies it into the app output with its shape intact, so the .sfz keeps its
Samples/ and Data/ neighbours and is addressed as
ms-appx:///<AssemblyName>/<name>.sfz. Sample formats follow CodeBrix.Audio -
WAV/FLAC/Ogg work as they are; an instrument built on .opus samples
additionally needs the app to register the Opus package.

COMPLETE EXAMPLES
=================

1. A page with a player, transport buttons and a scrubber
--------------------------------------------------------
    <Page ...
        xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia">
      <StackPanel Spacing="8" Padding="16">
        <audio:AudioPlayer x:Name="Player"
            Source="ms-appx:///Assets/song.mp3"
            Volume="0.8"
            PlaybackEnded="OnPlaybackEnded"
            MediaFailed="OnMediaFailed" />

        <StackPanel Orientation="Horizontal" Spacing="8">
          <Button Content="Play"  Click="OnPlay" />
          <Button Content="Pause" Click="OnPause" />
          <Button Content="Stop"  Click="OnStop" />
          <ToggleSwitch Header="Loop"
              IsOn="{Binding IsLooping, ElementName=Player, Mode=TwoWay}" />
        </StackPanel>

        <Slider
            Maximum="{Binding DurationSeconds, ElementName=Player}"
            Value="{Binding PositionSeconds, ElementName=Player, Mode=TwoWay}" />

        <TextBlock x:Name="Status"
            Text="{Binding Position, ElementName=Player}" />
      </StackPanel>
    </Page>

    using System;
    using Microsoft.UI.Xaml;
    using CodeBrix.Platform.UI.AudioPlayer.Skia;

    public sealed partial class MainPage : Page
    {
        public MainPage() => InitializeComponent();

        private void OnPlay(object sender, RoutedEventArgs e)  => Player.Play();
        private void OnPause(object sender, RoutedEventArgs e) => Player.Pause();
        private void OnStop(object sender, RoutedEventArgs e)  => Player.Stop();

        private void OnPlaybackEnded(object sender, EventArgs e)
            => Status.Text = "Finished";

        private void OnMediaFailed(object sender, AudioPlayerFailedEventArgs e)
            => Status.Text = e.Message;     // e.Error holds the exception

        // Jump 10 s ahead, immediately (no debounce):
        private void OnSkip(object sender, RoutedEventArgs e)
            => Player.Seek(Player.Position + TimeSpan.FromSeconds(10));
    }

2. Loading from a stream
------------------------
    // Any readable, seekable stream; the player owns it from here on.
    Player.SetSourceStream(File.OpenRead(pathChosenByUser));
    Player.Play();

3. Sound effects
----------------
    using CodeBrix.Platform.UI.AudioPlayer.Skia;

    // Optional: read the bytes during a loading screen so the first play
    // does no file access.
    SoundEffect.Preload("ms-appx:///Assets/Sfx/laser.ogg");
    SoundEffect.Preload("embedded://./MyApp.Assets.Sfx.explosion.wav");

    // Fire and forget; overlapping calls each get their own voice.
    if (!SoundEffect.Play("ms-appx:///Assets/Sfx/laser.ogg", volume: 0.6))
    {
        // Already logged at Error; decide whether to tell the user.
    }

    // Releasing everything (e.g. when leaving a game level):
    SoundEffect.ClearCache();

4. MIDI music with an SFZ piano, a status line and an on-screen reaction
-----------------------------------------------------------------------
    <audio:MidiPlayer x:Name="Music"
        Source="ms-appx:///Assets/theme.mid"
        Instrument="ms-appx:///Assets/Piano/Piano.sfz"
        AutoPlay="True"
        MediaOpened="OnMusicOpened"
        MediaFailed="OnMediaFailed" />
    <ProgressRing IsActive="{Binding IsLoading, ElementName=Music}" />
    <Ellipse x:Name="BeatIndicator" Width="24" Height="24" Fill="Orange" Opacity="0.2" />
    <TextBlock x:Name="Status" />
    <Slider Maximum="{Binding DurationSeconds, ElementName=Music}"
            Value="{Binding PositionSeconds, ElementName=Music, Mode=TwoWay}" />
    <Slider Minimum="0.25" Maximum="2" StepFrequency="0.05"
            Value="{Binding Speed, ElementName=Music, Mode=TwoWay}" />

    private void OnMusicOpened(object sender, EventArgs e)
    {
        Status.Text = $"Loaded, {Music.Duration:mm\\:ss}, "
            + $"{Music.InstrumentProblems.Count} problems, "
            + $"{Music.UnsupportedInstrumentOpcodes.Count} unsupported opcodes";

        // Observe-only hook. Runs on the AUDIO THREAD: do the minimum here and
        // marshal to the UI thread through the element's DispatcherQueue.
        Music.MidiMessageProcessed = (channel, command, note, velocity) =>
        {
            if (command == 0x90 && velocity > 0 && channel == 9)   // drums
            {
                Music.DispatcherQueue.TryEnqueue(() => BeatIndicator.Opacity = 1.0);
            }
        };
    }

    // Mixing a layered arrangement live while it plays:
    Music.SetChannelVolume(3, 0.0);      // drop the lead layer out...
    Music.SetChannelVolume(3, 1.0);      // ...and bring it back
    Music.SetChannelPan(1, -0.5);        // bass a little to the left
    Music.SetChannelProgram(2, 48);      // strings on channel 3 (0-based 2)
    Music.SendMidiMessage(0, 0x90, 60, 100);   // middle C, note-on, channel 1

5. Enabling Opus
----------------
    // In the application project: reference CodeBrix.Audio.Opus.BsdLicenseForever
    // and register it once, before the first .opus source is set.
    CodeBrixAudioOpus.Register();
    Player.Source = "ms-appx:///Assets/voice.opus";   // now plays

MINIMUM VIABLE PROJECT
======================
A CodeBrix.Platform application already has a .Core project holding its package
references and a shared .UI project holding its XAML. The only addition is one
PackageReference in the .Core project:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.AudioPlayer.ApacheLicenseForever" />
        <!-- only if you play .opus: -->
        <!-- <PackageReference Include="CodeBrix.Audio.Opus.BsdLicenseForever" /> -->
      </ItemGroup>
      <ItemGroup>
        <!-- Assets/song.mp3 must be a Content item so ms-appx:///Assets/song.mp3 exists -->
        <Content Include="Assets\**" />
      </ItemGroup>
    </Project>

and a page in the .UI project:

    <Page x:Class="MyApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia">
      <Grid>
        <audio:AudioPlayer x:Name="Player" Source="ms-appx:///Assets/song.mp3" AutoPlay="True" />
        <Slider VerticalAlignment="Bottom" Margin="16"
            Maximum="{Binding DurationSeconds, ElementName=Player}"
            Value="{Binding PositionSeconds, ElementName=Player, Mode=TwoWay}" />
      </Grid>
    </Page>

Nothing else: no engine to install, no head-specific code, no start-up call
(unless you add Opus).

PERFORMANCE TIPS
================
  - SoundEffect decodes each source ONCE and keeps the decoded audio; every
    later Play is mixing only. Preload during a loading screen to move even
    the first file read off the moment of play. Use the string overload for
    anything repeated - Play(Stream) decodes on every call.
  - ClearCache() releases decoded audio; call it at level boundaries in an
    asset-heavy game rather than letting every effect ever played stay resident.
  - AudioPlayer streams the file in chunks rather than reading it into memory,
    so a very large WAV opens in milliseconds; loading is synchronous and cheap.
  - MidiPlayer loads on a thread-pool thread precisely because a sampled
    instrument takes seconds and hundreds of MB to decode. Instruments are
    cached process-wide: a second MidiPlayer pointing at the same .sfz (or the
    same .sf2 by path) pays nothing. Keep one instrument per app where you can.
  - PositionUpdateInterval (default 150 ms) is the UI refresh cadence; raise
    it for a page that only shows a coarse indicator, lower it (say 50 ms) for
    a tight visualizer. Position writes are debounced 200 ms anyway.
  - MidiMessageProcessed runs on the real-time audio thread for every message:
    no allocations, no locks held long, no UI - flag and enqueue, nothing more.
  - ActiveVoiceCount shows what an arrangement actually costs; watch it when an
    instrument with long releases makes the synthesizer work hard.

COMMON PITFALLS TO AVOID
========================
  - Writing Position/PositionSeconds seeks with a 200 ms DEBOUNCE - that is what
    makes slider drags land one seek. For an immediate jump call Seek(TimeSpan).
  - The players pause themselves on Unloaded: audio that must survive page
    navigation belongs to an element on a page that stays in the tree.
  - AudioPlayer.Duration is valid the moment Source is set; MidiPlayer.Duration
    is valid only from MediaOpened. Play() on a MidiPlayer before MediaOpened is
    a silent no-op - set AutoPlay="True" or call Play() in the MediaOpened
    handler.
  - A MidiPlayer needs BOTH Source and Instrument before anything loads; with
    one of them empty it simply sits unloaded (IsLoading stays false).
  - An .sfz must be a real file on disk (path or ms-appx:///). An embedded://
    .sfz fails with MediaFailed saying so; a .sf2 works in every form.
  - SetSourceStream hands the stream to the player - do not dispose it yourself
    afterwards, and do not reuse it for another player.
  - SoundEffect.Play(Stream) is uncached and re-decodes every call; a hot
    effect through that overload costs decoding on every trigger.
  - An .opus source without the Opus package registered fails; read
    AudioPlayerFailedEventArgs.Message - it names Opus and the fix. The engine's
    raw message only says "format 'ogg'".
  - SharedAudioOutput.Configure is only needed when YOU feed WaveOutEvent
    directly with mixed sample rates; this add-in's own types never need it.
  - Speed below 0 clamps to 0 and freezes the transport; use Pause() to pause.
  - SetChannelVolume is MIDI CC7 - a track that automates its own volume
    overwrites your value at its next CC7 event.
  - Load and play failures never throw from the players; they raise
    MediaFailed and log. Subscribe to MediaFailed or you will not know.
  - MidiMessageProcessed lambdas that touch UI directly crash or corrupt state:
    always DispatcherQueue.TryEnqueue.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No Opus decoding on its own: that is the separate
    CodeBrix.Audio.Opus.BsdLicenseForever package plus one Register() call.
  - No modifying MIDI hook (transpose/re-channel as it plays): only the
    observe-only MidiMessageProcessed is exposed. Use CodeBrix.Audio's
    MidiMusicPlayer directly if you need to rewrite messages.
  - No recording, no microphone input, no audio analysis or DSP - playback
    only. CodeBrix.Audio carries those primitives; this add-in does not surface
    them.
  - No visual chrome: no transport control, no waveform, no volume slider. The
    players are non-visual elements; you compose the UI from ordinary controls
    bound to their properties.
  - No playlist/queue: one source per element. Chain PlaybackEnded handlers or
    declare several elements.
  - No streaming from http(s) URLs: sources are files, application assets,
    embedded resources or streams you open.
  - Not a per-element output device: everything mixes into the process's one
    shared output.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/AudioPlayerDemo
      The reference application for this package (all six heads). Its main page
      declares two AudioPlayer elements and a MidiPlayer, drives all five audio
      formats (WAV, MP3, Ogg Vorbis, FLAC, and Opus through the registered
      CodeBrix.Audio.Opus.BsdLicenseForever package), compressed sound effects,
      and the MIDI player from background load through tempo and seek.
      Start with AudioPlayerDemo.UI/Views/MainPage.xaml and its code-behind.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.AudioPlayer.Skia
      The add-in's own source (AudioPlayer.cs, MidiPlayer.cs, SoundEffect.cs),
      fully XML-documented.

QUICK REFERENCE CARD
====================
namespace CodeBrix.Platform.UI.AudioPlayer.Skia
xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia"

[Bindable] sealed class AudioPlayer : FrameworkElement
    string   Source                     TimeSpan Duration          (ro)
    bool     AutoPlay                   double   DurationSeconds   (ro)
    TimeSpan Position          (2-way)  bool     IsPlaying         (ro)
    double   PositionSeconds   (2-way)  double   Volume            0..1
    bool     IsLooping                  TimeSpan PositionUpdateInterval
    void Play();  void Pause();  void Stop();  void Seek(TimeSpan position);
    void SetSourceStream(Stream stream);
    event EventHandler PlaybackEnded;
    event EventHandler<AudioPlayerFailedEventArgs> MediaFailed;

[Bindable] sealed class MidiPlayer : FrameworkElement
    ...everything AudioPlayer has except SetSourceStream, plus:
    string   Instrument                 bool     IsLoading         (ro)
    double   Speed                      int      ActiveVoiceCount  (ro)
    IReadOnlyList<string>       InstrumentProblems              (ro)
    IReadOnlyCollection<string> UnsupportedInstrumentOpcodes    (ro)
    MidiMessageObserver? MidiMessageProcessed { get; set; }   // audio thread
    void SendMidiMessage(int channel, int command, int data1, int data2);
    void SetChannelVolume(int channel, double volume);       // CC7,  0..1
    void SetChannelPan(int channel, double pan);             // CC10, -1..1
    void SetChannelProgram(int channel, int program);        // 0..127
    event EventHandler MediaOpened;

static class SoundEffect
    static bool Play(string source, double volume = 1.0);    // cached decode
    static bool Play(Stream stream, double volume = 1.0);    // decodes each call
    static void Preload(string source);
    static void ClearCache();

sealed class AudioPlayerFailedEventArgs : EventArgs
    string Message { get; }     Exception Error { get; }

Source forms: path | file:// | ms-appx:///Assets/x | embedded://Asm/Res.Name
              ("." = app assembly; "(assembly)" placeholder) | Stream
.sfz instruments: path or ms-appx:/// ONLY.

Elsewhere (CodeBrix.Audio.MitLicenseForever, flows in automatically):
    CodeBrix.Audio.Wave.SharedAudioOutput.Configure(sampleRate[, channels])
Opus (separate CodeBrix.Audio.Opus.BsdLicenseForever): CodeBrixAudioOpus.Register()
