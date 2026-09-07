================================================================================
AGENT-README: CodeBrix.Platform.AudioPlayer
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.AudioPlayer.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
Audio playback (WAV, MP3, Ogg Vorbis, FLAC - and Opus once the application
registers the separate codec package) and MIDI music for CodeBrix.Platform
applications, delivered as XAML-declarable elements plus one static class.
Target: .NET 10 or later. Five public types, all in one namespace:

  AudioPlayer     A non-visual [Bindable] FrameworkElement: a file player with
                  Play/Pause/Stop/Seek, volume, looping, and a position that
                  two-way binds to a Slider for scrubbing.
  MidiPlayer      The same transport, member for member, for a MIDI file
                  rendered through a SoundFont (.sf2), an SFZ (.sfz) or a
                  Decent Sampler (.dspreset, .dslibrary, .dsbundle, or a folder
                  holding a preset) instrument; plus tempo, a beat clock, the
                  MPE settings, a Decent Sampler preset's own control model,
                  per-channel mixing and an observe-only note hook.
  SoundEffect     Static fire-and-forget sound effects: decoded once, mixed as
                  overlapping voices in the application's shared output.
  MidiInstrumentKind
                  Which of the three instrument formats a MidiPlayer loaded.
  AudioPlayerFailedEventArgs
                  The payload of both players' MediaFailed event.

"Decent Sampler" is Decidedly LLC's name for an instrument format and for the
player that reads it; it appears here only to say what a file is.

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
                                            SoundFont, SFZ and Decent Sampler
                                            synthesis; the bundled
                                            codebrix_miniaudio backend

NOT dependencies - two add-on packages the APPLICATION references and registers.
Both follow the same rule: this add-in depends on nothing outside its own
licence bar, and both add-ons reach it anyway, because the engine resolves
codecs through the shared audio output and looks up instrument features in a
process-wide registry.

  CodeBrix.Audio.Opus.BsdLicenseForever
      Opus playback. Opus is BSD-3-Clause rather than MIT, so it ships as its
      own package. An application that plays .opus files references it and calls
      CodeBrixAudioOpus.Register() once at start-up. See FORMATS AND CODECS.
  CodeBrix.Audio.ModestSynth.MitLicenseForever
      The oscillators and creative effects a Decent Sampler preset may ask for.
      An application that plays such a preset references it and calls
      ModestSynth.Register() once at start-up, BEFORE the first instrument
      loads: the engine looks those factories up when an instrument is BUILT, so
      a later registration does not reach an instrument that is already loaded.
      Without it a preset still loads and every sample group still plays; what
      it asked for and could not have is named in InstrumentProblems and
      UnsupportedInstrumentFeatures, and the element logs one warning carrying
      the engine's own advice line.

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

INSTRUMENTS THAT MUST NAME A FILE ON DISK are the one exception. An .sfz
instrument, and every Decent Sampler form (.dspreset, .dslibrary, .dsbundle, or
a folder holding a preset), take ONLY a filesystem path or an ms-appx:/// URI -
never embedded://, never a Stream. A .sf2 SoundFont takes every form above. See
MidiPlayer below for why, and for the MediaFailed message that says so.

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
synthesizes a MIDI file through a SoundFont, an SFZ or a Decent Sampler
instrument.

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

    string   Instrument          the instrument to render through; what it
                                 names decides which synthesizer runs (see
                                 INSTRUMENT FORMS below). Loading starts once
                                 BOTH Source and Instrument are non-empty.
    string   InstrumentPreset    which preset to play from a Decent Sampler
                                 container that holds several. Empty, the
                                 default, takes the container's first. Changing
                                 it queues a load exactly as Instrument does;
                                 ignored by the other two formats.
    MidiInstrumentKind InstrumentKind
                                 read-only; which format the loaded instrument
                                 turned out to be - None, SoundFont, Sfz or
                                 DecentSampler. None until a load completes.
    bool     IsLoading           read-only; true while the background load runs
    double   Speed               tempo multiplier, default 1.0 (0.5 half speed,
                                 2.0 double); pitch does not change. Negative
                                 values clamp to 0, which freezes the transport
                                 while sounding notes ring out.
    int      ActiveVoiceCount    read-only; synthesizer voices sounding now,
                                 refreshed with Position while playing
    double   BeatsPerMinute      read-only; the music's own tempo where the
                                 transport is now - a sequence with tempo
                                 changes reports the one in force rather than
                                 the one it started at. Takes no notice of
                                 Speed, which changes how fast the transport
                                 travels rather than what the music says.
    double   BeatPosition        read-only; how far in, in beats - the musical
                                 counterpart of PositionSeconds, for a beat
                                 indicator or for lining something up with the
                                 bar. Both refresh with Position while playing.
    bool     DropAuxiliaryOutputs
                                 false by default, so nothing an instrument
                                 makes goes unheard. A Decent Sampler preset can
                                 route a group, a zone or a bus to an auxiliary
                                 stereo output; this player has one stereo pair,
                                 so it folds them into the mix. Set this when a
                                 preset uses them for something a listener
                                 should not hear. Formats without auxiliary
                                 outputs are unaffected.

Plain property, read at the NEXT load (not bindable - it is configuration for
the load rather than something a page binds to):

    DecentSamplerLoadOptions InstrumentLoadOptions
        How to load a Decent Sampler instrument: its memory budget, the size
        above which a sample is streamed from disk instead of decoded, whether
        samples are decoded at all, and where the decode cache lives. Null, the
        default, takes the engine's own defaults (the CodeBrix.Audio
        AGENT-README's "PLAYING DECENT SAMPLER INSTRUMENTS" section documents
        them). The element loads from a COPY, so changing the object afterwards
        affects only later loads, and a preset name inside it is overridden by
        InstrumentPreset whenever that is set. Ignored by the other two formats.

Plain properties, valid from MediaOpened onward:

    IReadOnlyList<string>       InstrumentProblems
        What the loaded instrument could not make sense of (a referenced sample
        file that is missing, for example), for all three formats. The
        instrument still loads; the regions it could not build are silent.
    IReadOnlyCollection<string> UnsupportedInstrumentOpcodes
        SFZ opcodes the instrument uses that the synthesizer does not implement.
        Empty for the other two formats.
    IReadOnlyCollection<string> UnsupportedInstrumentFeatures
        The Decent Sampler equivalent: elements, attributes, effect types and
        waveforms the engine did not understand. Empty for the other two
        formats. Some entries name the ModestSynth add-on package (see
        INSTALLATION); the preset still plays what it can.
    string                      InstrumentMemorySummary
        How the loaded Decent Sampler instrument decided to hold its samples -
        what was decoded, what streams, and against which budget - in the
        engine's own words, for a diagnostics panel or a log. Empty for the
        other two formats.
    IReadOnlyList<string>       SourceProblems
        Anything the MIDI FILE itself needed forgiving. A Standard MIDI File
        that breaks a rule is read leniently rather than refused and says so
        here; the sequence plays either way. Empty for a file that is exactly to
        specification, which is the normal case.
    All of them empty means "fully supported, read exactly as written". Show
    them rather than guessing when an instrument or a file sounds wrong.

Additional event (UI thread):

    event EventHandler MediaOpened
        The instrument and sequence have loaded and the transport is live.
        Duration is set by the time this is raised.

INSTRUMENT FORMS. What Instrument names decides the synthesizer - the file
extension first, then a folder that holds a preset:

    .sf2                            a SoundFont           InstrumentKind.SoundFont
    .sfz                            an SFZ instrument     InstrumentKind.Sfz
    .dspreset                       a Decent Sampler preset       .DecentSampler
    .dslibrary / .dsbundle          a Decent Sampler container    .DecentSampler
    a folder holding a .dspreset    the same - the shape a .dsbundle has on
                                    macOS, and the shape an unpacked library has
                                    everywhere                    .DecentSampler

SOURCE FORMS FOR AN INSTRUMENT: a .sf2 takes every form Source does (path,
ms-appx:///, embedded://). An .sfz and every Decent Sampler form take ONLY a
file path or an ms-appx:/// URI, because each of them needs a real place on
disk: an .sfz references its samples as separate files beside it (and may
#include others), a .dspreset references its samples as separate files beside
it, and a .dslibrary or .dsbundle, though it IS one file, is read in place from
the archive on disk rather than unpacked. An embedded:// instrument in any of
those formats fails with MediaFailed, and the message names the format it was
handed and says why. Nothing is opened to work that out, so the failure is about
the FORM, not about a resource that happens to be missing.

INSTRUMENT SHARING AND THE CACHES. Instruments are cached process-wide, so a
second player naming the same instrument pays nothing: an .sfz by its resolved
path, an .sf2 by path when given as a path or ms-appx:/// URI (an .sf2 from an
embedded:// resource is read each time), and a Decent Sampler instrument through
the audio engine's OWN process-wide cache - the same one CodeBrix.Audio's
path-based loading uses, so an application that pre-loaded a library and a XAML
MidiPlayer naming the same path share one copy, and two presets of one library
share that library's decoded samples. InstrumentPreset is part of what that
cache keys on.

THE CONTROL MODEL: A DECENT SAMPLER PRESET'S KNOBS. A preset's interface section
is a LIVE control surface, and this element exposes it as the instrument's own
objects rather than wrapping them:

    IReadOnlyList<DecentSamplerControl>   InstrumentControls
        Every element of the preset's interface, in the order a binding counts
        them - knobs, sliders, menus, buttons, labels and images alike. Do not
        filter it: an index in a binding counts them all. Setting a control's
        value fires the bindings behind it AT ONCE, exactly as turning that knob
        in a player would. Empty for the other two formats.
    IReadOnlyList<DecentSamplerTagState>  InstrumentTagStates
        The live state of every tag the preset names - enabled, volume, pan and
        polyphony. Empty for the other two formats.
    DecentSamplerControl GetInstrumentControl(string name)
        The first control of that name, case-insensitively, or null.
    event EventHandler<DecentSamplerControlChangedEventArgs> InstrumentControlChanged
        Raised on the UI THREAD when a control moves - because something set it,
        or because a modulator or a MIDI binding inside the preset moved it
        while the music played. Those arrive on the real-time audio thread and
        can move one knob on every rendered block, so changes are COALESCED: one
        raise per control per dispatcher pass, carrying the last change seen for
        it. Read the current value from the control the event args carry.

    Both the control types and the event args are CodeBrix.Audio types
    (CodeBrix.Audio.Synth.DecentSampler), which is the point: what a host binds
    to is the instrument's own parameter model.

MIDI POLYPHONIC EXPRESSION. A performance recorded from an expressive controller
spreads each note onto its own MIDI channel so that it can bend, brighten and
swell alone, and all three instrument formats read such a file the same way. The
element adds nothing to those semantics - the CodeBrix.Audio AGENT-README's "MPE
FROM MIDI FILES" section is the contract - it only surfaces the settings:

    MpeMode  MpeMode             Off (default), LowerZone, UpperZone, Both, Auto.
                                 An exporter usually leaves out the message that
                                 configures the zones, which is what Auto is for.
    double   MpeMemberBendRange  how far a member channel's bend reaches when the
                                 music never says, in semitones. A bend-range
                                 message in the music overrides it.
    int      MpeLowerZoneMemberCount / MpeUpperZoneMemberCount
                                 0, the default, means automatic. Pin one when a
                                 file is read differently from how it was played.
    MpeZoneInfo MpeLowerZone / MpeUpperZone     (read-only, plain properties)
                                 the zones as the loaded instrument reads them
                                 now: whether each is active, its master and
                                 member channels and the bend ranges in force.
    int GetReleaseVelocity(int channel, int key)
                                 the "lift" velocity of the last note-off for a
                                 key on a channel (0-based channel), or -1 when
                                 nothing is loaded. No instrument format defines
                                 what release velocity should DO, so it changes
                                 nothing about how a preset sounds; it is there
                                 for an application to react to. To watch lifts
                                 as they happen, use MidiMessageProcessed - a
                                 note-off carries the lift as its second data
                                 byte.

    The four settings are settable in XAML before a load, changeable while the
    music plays, and survive a load; they reach whichever instrument is loaded.

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
A sampled instrument is a large amount of decoded audio and takes seconds to
read, so setting Source or Instrument raises IsLoading (bind a status line or a
ProgressRing to it), loads on a thread-pool thread, and raises MediaOpened when
the transport is live. Duration is valid from MediaOpened onward, NOT from the
property set; Play() before that is a no-op (set AutoPlay instead). Setting
Source, Instrument and InstrumentPreset one after the other queues ONE load that
covers them all; a newer set while a load is running supersedes it.

FORMATS AND CODECS
------------------
WAV, MP3, Ogg Vorbis and FLAC, for AudioPlayer and SoundEffect alike - all
decoded by the CodeBrix.Audio package that flows in with this one. Ogg Vorbis
matters for anything consuming free game-asset packs (kenney.nl audio is 100%
.ogg).

INSTRUMENT FORMATS, for MidiPlayer: SoundFont (.sf2), SFZ (.sfz) and Decent
Sampler (.dspreset, .dslibrary, .dsbundle, or a folder holding a preset). The
sample files INSIDE an instrument follow the same codec list as everything else,
so an instrument built on WAV, Ogg Vorbis or FLAC samples plays as it is, and
one built on .opus samples additionally needs the application to register the
Opus package.

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

The three formats pack differently, and the difference is only ever how many
FILES there are - all of them are addressed by path once they are laid out:

    .sf2          one file. It is also the only format that can be an embedded
                  resource instead, if you would rather not have a loose file.
    .sfz          the .sfz plus its Samples/ (and any folder it #includes), as a
                  tree with its shape intact.
    .dspreset     the .dspreset plus its Samples/ (and Resources/, when the
                  preset draws on one), the same way.
    .dslibrary    ONE file, and it packs trivially - but it is still Content
    .dsbundle     rather than an embedded resource, because the engine reads
                  the container IN PLACE from the archive on disk.

The same rule covers an instrument that ships with the APPLICATION rather than
in a package: make it Content with CopyToOutputDirectory, keep the folder shape,
and address it as ms-appx:///Assets/<folder>/<name>.<extension>.

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

6. A Decent Sampler instrument, with a knob on screen
-----------------------------------------------------
    <audio:MidiPlayer x:Name="Music"
        Source="ms-appx:///Assets/theme.mid"
        Instrument="ms-appx:///Assets/Choir/Choir.dslibrary"
        InstrumentPreset="Cantores Oohs"
        MpeMode="Auto"
        MediaOpened="OnMusicOpened"
        InstrumentControlChanged="OnInstrumentControlChanged"
        MediaFailed="OnMediaFailed" />
    <Slider x:Name="Knob" IsEnabled="False" ValueChanged="OnKnobMoved" />
    <TextBlock x:Name="KnobName" />

    using CodeBrix.Audio.Synth.DecentSampler;
    using CodeBrix.Platform.UI.AudioPlayer.Skia;

    private DecentSamplerControl _attack;
    private bool _writingFromInstrument;

    private void OnMusicOpened(object sender, EventArgs e)
    {
        // What actually loaded, and what it could not do. All three are valid
        // only from here on.
        Status.Text = $"{Music.InstrumentKind}: "
            + $"{Music.InstrumentProblems.Count} problem(s), "
            + $"{Music.UnsupportedInstrumentFeatures.Count} unsupported feature(s). "
            + Music.InstrumentMemorySummary;

        // A named knob of the preset. Setting its value fires the preset's own
        // bindings at once, exactly as turning it in a player would.
        _attack = Music.GetInstrumentControl("ATTACK");
        if (_attack is not null)
        {
            KnobName.Text = _attack.Name;
            Knob.Minimum = _attack.MinValue;
            Knob.Maximum = _attack.MaxValue;
            Knob.Value = _attack.Value;
            Knob.IsEnabled = true;
        }
    }

    private void OnKnobMoved(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_writingFromInstrument)
        {
            _attack?.SetValue(e.NewValue);
        }
    }

    // The other direction: the preset's own modulators and MIDI bindings move
    // its knobs while the music plays. Already on the UI thread, and already
    // coalesced to one raise per control per dispatcher pass.
    private void OnInstrumentControlChanged(
        object sender, DecentSamplerControlChangedEventArgs e)
    {
        if (ReferenceEquals(e.Control, _attack))
        {
            _writingFromInstrument = true;
            Knob.Value = e.Control.Value;
            _writingFromInstrument = false;
        }
    }

    // Tuning the load, when the default memory policy is not what you want.
    // Read at the NEXT load, so set it before Instrument.
    Music.InstrumentLoadOptions = new DecentSamplerLoadOptions
    {
        InstrumentMemoryBudgetBytes = 512L * 1024 * 1024,
        DecodeSamples = true,
    };

    // In the application project, if a preset asks for an oscillator or a
    // creative effect: reference CodeBrix.Audio.ModestSynth.MitLicenseForever
    // and register it at start-up, BEFORE the first instrument loads.
    //   using CodeBrix.Audio.ModestSynth;
    //   ModestSynth.Register();

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
    instrument takes seconds to read and a great deal of memory to decode.
    Instruments are cached process-wide: a second MidiPlayer pointing at the
    same instrument pays nothing. Keep one instrument per app where you can.
  - A Decent Sampler instrument decides PER FILE whether to hold a sample in
    memory or stream it from disk, against a per-sample threshold and an
    instrument-wide budget. Both are on InstrumentLoadOptions, and
    InstrumentMemorySummary says what was decided - read it before tuning
    anything. A folder instrument streams better than the same library inside a
    .dslibrary, because seeking backwards inside a zip entry costs a walk from
    the start of it.
  - InstrumentLoadOptions.DecodeSamples = false means LOAD ON FIRST USE, not
    "never": every path is resolved and every problem is still found, and no
    audio file is opened until a note wants one. That is what an instrument
    browser wants, and what a page that only needs to validate an instrument
    wants. The first note that needs a file is silent and the instrument says so
    in InstrumentProblems; every note after it sounds.
  - A knob is not free to turn during playback in only one way: a control whose
    binding moves a sample's start or loop points takes effect on the NEXT note,
    so dragging it under a held chord does nothing you can hear until then.
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
  - An .sfz and every Decent Sampler form must be a real file or folder on disk
    (path or ms-appx:///). An embedded:// one fails with MediaFailed, and the
    message names the format and says why; only a .sf2 works in every form.
    Packing one as an EmbeddedResource is the mistake this is here to catch.
  - TWO PLAYERS NAMING ONE DECENT SAMPLER PRESET SHARE ITS KNOBS. A preset keeps
    its knob positions, controller values and modulated parameters on the
    INSTRUMENT, because that is where the format puts them - a binding writes the
    group's volume, not the synthesizer's - and instruments are shared. That is
    right for two players of the same sound and wrong when two parts must move
    their own knobs. There is no per-element instrument option; load the second
    part through CodeBrix.Audio's own API with an instrument of its own if you
    need one.
  - InstrumentControls, InstrumentTagStates and the rest of the reporting
    properties are empty until MediaOpened. Reading them from the handler that
    SET Instrument reads the previous load's state, or nothing at all.
  - ModestSynth.Register() has to run BEFORE the instrument loads (see
    INSTALLATION). Registering afterwards throws nothing and changes nothing:
    the oscillator group stays silent and the effect stays bypassed until the
    instrument is loaded again.
  - A control's value is the INSTRUMENT's, not the element's, so it survives
    the element and outlives an unload. Read it from InstrumentControls after
    MediaOpened rather than assuming a preset starts where it did last time.
  - InstrumentControlChanged is coalesced onto the dispatcher, so a control set
    from code reports back on a LATER pass, not inside the SetValue call. Do not
    write a round-trip test that reads the event synchronously.
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
  - No rendering of a Decent Sampler preset's own interface. Its knobs, images,
    background and keyboard colours are exposed as a live control model with all
    the geometry and colour a renderer would need, and nothing here draws them -
    a page composes ordinary controls over InstrumentControls instead. A
    preset's own files (cover art, a text file) are reachable through the
    instrument's container if a page wants to show one.
  - No stems or multi-track element: one sequence per MidiPlayer, one file per
    AudioPlayer. CodeBrix.Audio's own MultiTrackPlayer plays a multi-track song
    (including a download of separated stems) directly.
  - No MIDI device input. MidiPlayer plays MIDI FILES; nothing here opens a
    controller. Expressive performances arrive as recorded files (see MPE).
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
      and the MIDI player from background load through tempo and seek. Its MIDI
      pane plays one piece through either of two instruments - a sampled SFZ
      piano or a small hand-written Decent Sampler preset that ships with the
      sample - with a slider driving the preset's first knob through
      InstrumentControls and InstrumentControlChanged, a status line showing
      InstrumentKind, InstrumentMemorySummary and the MIDI file's own problems,
      and an MPE drop-down two-way bound to MpeMode.
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

  Dependency properties
    string   Instrument                 string   InstrumentPreset   ("")
    double   Speed                      bool     IsLoading          (ro)
    bool     DropAuxiliaryOutputs       int      ActiveVoiceCount   (ro)
    MidiInstrumentKind InstrumentKind   (ro)     // None until a load completes
    double   BeatsPerMinute  (ro)       double   BeatPosition       (ro)
    MpeMode  MpeMode                    double   MpeMemberBendRange (48.0)
    int      MpeLowerZoneMemberCount    int      MpeUpperZoneMemberCount   (0 = auto)

  Plain properties
    DecentSamplerLoadOptions InstrumentLoadOptions { get; set; }  // next load
    IReadOnlyList<string>       InstrumentProblems               (ro)
    IReadOnlyCollection<string> UnsupportedInstrumentOpcodes     (ro)  // SFZ
    IReadOnlyCollection<string> UnsupportedInstrumentFeatures    (ro)  // DS
    string                      InstrumentMemorySummary          (ro)  // DS
    IReadOnlyList<string>       SourceProblems                   (ro)  // the .mid
    IReadOnlyList<DecentSamplerControl>  InstrumentControls      (ro)
    IReadOnlyList<DecentSamplerTagState> InstrumentTagStates     (ro)
    MpeZoneInfo MpeLowerZone (ro)       MpeZoneInfo MpeUpperZone (ro)

  Methods
    DecentSamplerControl GetInstrumentControl(string name);   // case-insensitive
    int  GetReleaseVelocity(int channel, int key);            // -1 = unavailable
    MidiMessageObserver? MidiMessageProcessed { get; set; }   // audio thread
    void SendMidiMessage(int channel, int command, int data1, int data2);
    void SetChannelVolume(int channel, double volume);        // CC7,  0..1
    void SetChannelPan(int channel, double pan);              // CC10, -1..1
    void SetChannelProgram(int channel, int program);         // 0..127

  Events
    event EventHandler MediaOpened;                           // UI thread
    event EventHandler<DecentSamplerControlChangedEventArgs>
          InstrumentControlChanged;    // UI thread, coalesced per dispatcher pass

enum MidiInstrumentKind { None, SoundFont, Sfz, DecentSampler }

static class SoundEffect
    static bool Play(string source, double volume = 1.0);    // cached decode
    static bool Play(Stream stream, double volume = 1.0);    // decodes each call
    static void Preload(string source);
    static void ClearCache();

sealed class AudioPlayerFailedEventArgs : EventArgs
    string Message { get; }     Exception Error { get; }

Source forms: path | file:// | ms-appx:///Assets/x | embedded://Asm/Res.Name
              ("." = app assembly; "(assembly)" placeholder) | Stream
Instruments:  .sf2 every form; .sfz, .dspreset, .dslibrary, .dsbundle and a
              folder holding a .dspreset are path or ms-appx:/// ONLY.

Elsewhere (CodeBrix.Audio.MitLicenseForever, flows in automatically):
    CodeBrix.Audio.Wave.SharedAudioOutput.Configure(sampleRate[, channels])
    CodeBrix.Audio.Synth.Mpe.MpeMode / MpeZoneInfo
    CodeBrix.Audio.Synth.DecentSampler.DecentSamplerControl / DecentSamplerTagState
        / DecentSamplerLoadOptions / DecentSamplerControlChangedEventArgs
Application-side add-ons, each one Register() call at start-up:
    Opus         CodeBrix.Audio.Opus.BsdLicenseForever  -> CodeBrixAudioOpus.Register()
    Oscillators  CodeBrix.Audio.ModestSynth.MitLicenseForever -> ModestSynth.Register()
                 (before the first instrument loads)
