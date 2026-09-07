using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls.Primitives;
using CodeBrix.Audio.Synth.DecentSampler;
using CodeBrix.Audio.Synth.Mpe;
using CodeBrix.Platform.UI.AudioPlayer.Skia;

namespace AudioPlayerDemo.Views;

public sealed partial class MainPage : Page
{
    private const string EmbeddedSource = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.demo_song.mp3";
    private const string MsAppxSource = "ms-appx:///Assets/demo_song.mp3";

    private const string ChimeEffect = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.chime.wav";
    private const string ClickEffect = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.click.wav";

    // The same chime, compressed three ways. SoundEffect needs no hint about which is which - the
    // format is read from the bytes - and .opus works only because App registered that codec.
    private const string ChimeOggEffect = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.chime.ogg";
    private const string ChimeFlacEffect = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.chime.flac";
    private const string ChimeOpusEffect = "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.chime.opus";

    // The MIDI music and the instrument it is rendered through. Both are loose files in the
    // output's Assets folder; the instrument has to be, because its samples live beside it.
    private const string MidiSource = "ms-appx:///Assets/debussy_Ste_Bergamesq_Clair.mid";
    private const string InstrumentSource = "ms-appx:///Assets/SplendidGrandPiano/Splendid Grand Piano.sfz";

    // The demo's own Decent Sampler instrument: a hand-written preset over copies of the chime and
    // click above (samples/assets/DemoSampler). A .dspreset is a folder instrument exactly as an
    // .sfz is - its samples sit beside it in Samples/ - so it is addressed through ms-appx:///
    // rather than embedded://, and the whole tree is copied to the output (see the .Core csproj).
    private const string DemoSamplerSource = "ms-appx:///Assets/DemoSampler/Demo Sampler.dspreset";

    // The same preset named as an embedded resource. Nothing packs it that way, and nothing could:
    // it is here only so the demo (and the self-test) can show what happens when a format that must
    // be a real file on disk is given a form that names no file - MediaFailed, with a message that
    // says which format and why. The name is never opened, so the resource's absence is not the
    // reason it fails.
    private const string DemoSamplerEmbeddedSource =
        "embedded://AudioPlayerDemo.Core/AudioPlayerDemo.Assets.DemoSampler.dspreset";

    private readonly Stopwatch _instrumentLoadTimer = new();

    // The knob the control slider drives - the first of the loaded instrument's controls, or null
    // when what is loaded has no control model (a SoundFont or an SFZ instrument).
    private DecentSamplerControl _boundControl;

    // Set while the slider is being written FROM the instrument, so writing it back is not read as
    // a user's drag and sent round again.
    private bool _updatingControlSlider;

    public MainPage()
    {
        InitializeComponent();

        // Optional: preload the effects so even the very first press starts with no file access.
        SoundEffect.Preload(ChimeEffect);
        SoundEffect.Preload(ClickEffect);

        // Load the initial second-player selection (Sample Song 1 / MP3, per the drop-down
        // defaults set in XAML) once the page has loaded, so the synchronous file load happens
        // off the constructor / first-render path rather than blocking the window from showing.
        Loaded += (_, _) => LoadSecondPlayerSource();

        // Optional self-test hook: exercise the whole player from a script and exit (used by
        // the repo's scripted X11 smoke verification).
        if (Environment.GetEnvironmentVariable("AUDIOPLAYERDEMO_SELFTEST") == "1")
        {
            Loaded += (_, _) => RunSelfTest();
        }
    }

    private int _selfTestFailures;

    private void Check(string step, bool ok, string detail)
    {
        Console.WriteLine($"APD-SELFTEST: {(ok ? "PASS" : "FAIL")} {step} ({detail})");
        if (!ok)
        {
            _selfTestFailures++;
        }
    }

    private async void RunSelfTest()
    {
        try
        {
            var playbackEnded = false;
            Player.PlaybackEnded += (_, _) => playbackEnded = true;

            Check("duration", Math.Abs(Player.DurationSeconds - 90) < 2, $"DurationSeconds={Player.DurationSeconds:F1}");

            Player.Play();
            await System.Threading.Tasks.Task.Delay(2000);
            Check("position-advances", Player.PositionSeconds is > 1.2 and < 3.5, $"PositionSeconds={Player.PositionSeconds:F2}");
            Check("position-pair-sync", Math.Abs(Player.Position.TotalSeconds - Player.PositionSeconds) < 0.001, $"Position={Player.Position}");
            Check("is-playing", Player.IsPlaying, $"IsPlaying={Player.IsPlaying}");

            Player.Seek(TimeSpan.FromMinutes(1));
            await System.Threading.Tasks.Task.Delay(700);
            Check("seek-jump", Player.PositionSeconds is > 59.5 and < 62.5, $"PositionSeconds={Player.PositionSeconds:F2}");

            // A "slider drag": an external write to PositionSeconds must seek (debounced).
            Player.PositionSeconds = 30;
            await System.Threading.Tasks.Task.Delay(900);
            Check("two-way-scrub", Player.PositionSeconds is > 29.5 and < 32.5, $"PositionSeconds={Player.PositionSeconds:F2}");

            Check("sfx-chime", SoundEffect.Play(ChimeEffect), "SoundEffect.Play returned");
            Check("sfx-click", SoundEffect.Play(ClickEffect, volume: 0.8), "SoundEffect.Play returned");

            SwitchSource(MsAppxSource);
            Player.Play();
            await System.Threading.Tasks.Task.Delay(1200);
            Check("ms-appx-source", Player.IsPlaying && Player.PositionSeconds > 0.5, $"PositionSeconds={Player.PositionSeconds:F2}");

            Player.Seek(Player.Duration - TimeSpan.FromSeconds(1.5));
            await System.Threading.Tasks.Task.Delay(3000);
            Check("playback-ended", playbackEnded && !Player.IsPlaying, $"ended={playbackEnded} IsPlaying={Player.IsPlaying}");

            await CheckEveryFormat();
            await CheckCompressedSoundEffects();
            await CheckMidiMusic();
            await CheckDecentSamplerInstrument();

            Console.WriteLine($"APD-SELFTEST: RESULT {(_selfTestFailures == 0 ? "PASS" : $"FAIL ({_selfTestFailures})")}");
            Environment.Exit(_selfTestFailures == 0 ? 0 : 1);
        }
        catch (Exception e)
        {
            Console.WriteLine($"APD-SELFTEST: RESULT FAIL (exception: {e.Message})");
            Environment.Exit(2);
        }
    }

    /// <summary>
    /// Plays the same song in all five formats through the second player: each must report the
    /// right duration, actually advance, and land a seek where it was sent.
    /// </summary>
    private async Task CheckEveryFormat()
    {
        // Every file is the same 3:36 recording, so one expected duration covers the lot. Opus is
        // a few milliseconds longer because its encoder pads the tail.
        const double songSeconds = 216.04;

        foreach (var extension in FormatExtensions)
        {
            Player2.Stop();
            Player2.Source = $"ms-appx:///Assets/sample_song_1.{extension}";
            Check($"{extension}-duration", Math.Abs(Player2.DurationSeconds - songSeconds) < 2.0,
                $"DurationSeconds={Player2.DurationSeconds:F2}");

            Player2.Play();
            await Task.Delay(1200);
            Check($"{extension}-plays", Player2.IsPlaying && Player2.PositionSeconds > 0.4,
                $"PositionSeconds={Player2.PositionSeconds:F2}");

            Player2.Seek(TimeSpan.FromMinutes(2));
            await Task.Delay(700);
            Check($"{extension}-seek", Player2.PositionSeconds is > 119.0 and < 124.0,
                $"PositionSeconds={Player2.PositionSeconds:F2}");

            Player2.Stop();
            await Task.Delay(200);
        }
    }

    /// <summary>
    /// The decode-once effect path, in the four formats the effect buttons offer.
    /// </summary>
    private async Task CheckCompressedSoundEffects()
    {
        foreach (var (label, source) in new[]
                 {
                     ("wav", ChimeEffect),
                     ("ogg", ChimeOggEffect),
                     ("flac", ChimeFlacEffect),
                     ("opus", ChimeOpusEffect),
                 })
        {
            Check($"sfx-{label}", SoundEffect.Play(source, volume: 0.7), "SoundEffect.Play returned");
            await Task.Delay(400);
        }
    }

    /// <summary>
    /// The MIDI player: the instrument loads in the background, the sequence plays through it,
    /// tempo changes how fast the sequence advances, and the transport behaves like the others.
    /// </summary>
    private async Task CheckMidiMusic()
    {
        var opened = false;
        var failure = "";
        MidiMusic.MediaOpened += (_, _) => opened = true;
        MidiMusic.MediaFailed += (_, e) => failure = e.Message;

        var loadTimer = Stopwatch.StartNew();
        MidiMusic.Instrument = InstrumentSource;
        MidiMusic.Source = MidiSource;

        Check("midi-loads-in-background", MidiMusic.IsLoading, $"IsLoading={MidiMusic.IsLoading} immediately after the set");

        while (!opened && failure.Length == 0 && loadTimer.Elapsed < TimeSpan.FromSeconds(90))
        {
            await Task.Delay(250);
        }
        loadTimer.Stop();

        Check("midi-instrument-loaded", opened, failure.Length == 0
            ? $"MediaOpened after {loadTimer.Elapsed.TotalSeconds:F1} s"
            : $"MediaFailed: {failure}");

        if (!opened)
        {
            return;
        }

        Check("midi-instrument-supported",
            MidiMusic.InstrumentProblems.Count == 0 && MidiMusic.UnsupportedInstrumentOpcodes.Count == 0,
            $"problems={MidiMusic.InstrumentProblems.Count} unsupported={MidiMusic.UnsupportedInstrumentOpcodes.Count}");

        // The sequence is 5:22.5 long.
        Check("midi-duration", Math.Abs(MidiMusic.DurationSeconds - 322.5) < 2.0,
            $"DurationSeconds={MidiMusic.DurationSeconds:F2}");

        MidiMusic.Play();
        await Task.Delay(2500);
        Check("midi-plays", MidiMusic.IsPlaying && MidiMusic.PositionSeconds > 1.0,
            $"PositionSeconds={MidiMusic.PositionSeconds:F2}");
        Check("midi-voices-sounding", MidiMusic.ActiveVoiceCount > 0,
            $"ActiveVoiceCount={MidiMusic.ActiveVoiceCount}");

        var beforeSpeedUp = MidiMusic.PositionSeconds;
        MidiMusic.Speed = 2.0;
        await Task.Delay(2000);
        var advanced = MidiMusic.PositionSeconds - beforeSpeedUp;
        Check("midi-tempo", advanced > 3.0, $"advanced {advanced:F2} s of sequence in 2 s at 2x");
        MidiMusic.Speed = 1.0;

        MidiMusic.Seek(TimeSpan.FromMinutes(2));
        await Task.Delay(700);
        Check("midi-seek", MidiMusic.PositionSeconds is > 119.0 and < 124.0,
            $"PositionSeconds={MidiMusic.PositionSeconds:F2}");

        MidiMusic.Stop();
        await Task.Delay(300);
        Check("midi-stop", !MidiMusic.IsPlaying && MidiMusic.PositionSeconds < 0.5,
            $"IsPlaying={MidiMusic.IsPlaying} PositionSeconds={MidiMusic.PositionSeconds:F2}");
    }

    /// <summary>
    /// The Decent Sampler half of the MIDI player: the demo's own hand-written preset loads in the
    /// background like any other instrument, reports itself as a Decent Sampler instrument, is
    /// fully supported, exposes its knobs as live parameters that round-trip and report their own
    /// movement on the UI thread, plays, and refuses a form that names no file on disk. The MPE
    /// settings and the musical clock are checked here too, because they need something loaded.
    /// </summary>
    /// <remarks>
    /// Runs after CheckMidiMusic, so the player already holds the SFZ piano when it starts: the
    /// first thing it does is supersede that load, which is also the shape a real page's instrument
    /// chooser has.
    /// </remarks>
    private async Task CheckDecentSamplerInstrument()
    {
        var opened = false;
        var failure = "";
        void OnOpened(object s, EventArgs e) => opened = true;
        void OnFailed(object s, AudioPlayerFailedEventArgs e) => failure = e.Message;

        MidiMusic.Stop();
        MidiMusic.MediaOpened += OnOpened;
        MidiMusic.MediaFailed += OnFailed;

        var loadTimer = Stopwatch.StartNew();
        MidiMusic.Instrument = DemoSamplerSource;
        MidiMusic.Source = MidiSource;

        Check("ds-loads-in-background", MidiMusic.IsLoading,
            $"IsLoading={MidiMusic.IsLoading} immediately after the set");

        while (!opened && failure.Length == 0 && loadTimer.Elapsed < TimeSpan.FromSeconds(60))
        {
            await Task.Delay(100);
        }
        loadTimer.Stop();

        if (!opened)
        {
            Check("ds-kind", false, failure.Length == 0
                ? $"nothing opened after {loadTimer.Elapsed.TotalSeconds:F1} s"
                : $"MediaFailed: {failure}");
            MidiMusic.MediaOpened -= OnOpened;
            MidiMusic.MediaFailed -= OnFailed;
            return;
        }

        Check("ds-kind", MidiMusic.InstrumentKind == MidiInstrumentKind.DecentSampler,
            $"InstrumentKind={MidiMusic.InstrumentKind} after {loadTimer.Elapsed.TotalSeconds:F1} s, " +
            $"memory=\"{MidiMusic.InstrumentMemorySummary}\"");

        Check("ds-supported",
            MidiMusic.InstrumentProblems.Count == 0 && MidiMusic.UnsupportedInstrumentFeatures.Count == 0,
            $"problems={MidiMusic.InstrumentProblems.Count} " +
            $"unsupported={MidiMusic.UnsupportedInstrumentFeatures.Count} " +
            $"{DescribeFirst(MidiMusic.InstrumentProblems)}{DescribeFirst(MidiMusic.UnsupportedInstrumentFeatures)}");

        Check("source-problems-empty", MidiMusic.SourceProblems.Count == 0,
            $"SourceProblems={MidiMusic.SourceProblems.Count}{DescribeFirst(MidiMusic.SourceProblems)}");

        await CheckInstrumentControls();

        // Voices are WATCHED rather than sampled once. The demo's instrument is built from two very
        // short recordings, so a note lasts as long as its sample does - about a second and a half
        // at the pitches this piece opens with - and the music has rests in it. Asking at one
        // arbitrary instant would be asking whether a note happens to be sounding right then.
        MidiMusic.Play();
        var voicesSeen = 0;
        var voiceWatch = Stopwatch.StartNew();
        while (voiceWatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            voicesSeen = Math.Max(voicesSeen, MidiMusic.ActiveVoiceCount);
            if (voicesSeen > 0 && MidiMusic.PositionSeconds > 2.0)
            {
                break;
            }

            await Task.Delay(50);
        }

        Check("ds-plays", MidiMusic.IsPlaying && voicesSeen > 0,
            $"IsPlaying={MidiMusic.IsPlaying} voices peaked at {voicesSeen} " +
            $"PositionSeconds={MidiMusic.PositionSeconds:F2}");

        // The musical clock: beats rather than seconds, refreshed on the same timer as Position.
        var beatsBefore = MidiMusic.BeatPosition;
        await Task.Delay(1500);
        Check("beat-position-advances", MidiMusic.BeatPosition > beatsBefore,
            $"BeatPosition {beatsBefore:F2} -> {MidiMusic.BeatPosition:F2} at {MidiMusic.BeatsPerMinute:F1} BPM");

        MidiMusic.Stop();
        await Task.Delay(300);

        CheckMpeRoundTrip();

        MidiMusic.MediaOpened -= OnOpened;
        MidiMusic.MediaFailed -= OnFailed;

        await CheckEmbeddedInstrumentFails();
    }

    /// <summary>
    /// The preset's knobs, as MidiPlayer exposes them: a live control list, a value that round-trips
    /// through the instrument's own parameter model, and the change reported back on the UI thread.
    /// </summary>
    private async Task CheckInstrumentControls()
    {
        var control = MidiMusic.InstrumentControls.Count > 0 ? MidiMusic.InstrumentControls[0] : null;
        var byName = control is null ? null : MidiMusic.GetInstrumentControl(control.Name);

        var changedOnUiThread = false;
        var observed = 0;
        void OnChanged(object s, DecentSamplerControlChangedEventArgs e)
        {
            observed++;
            changedOnUiThread = DispatcherQueue is not null && DispatcherQueue.HasThreadAccess;
        }

        MidiMusic.InstrumentControlChanged += OnChanged;

        if (control is not null)
        {
            // A quarter of the way along the knob's own travel, and away from where it sits now.
            var started = control.Value;
            var range = control.MaxValue - control.MinValue;
            var target = Math.Abs(started - (control.MinValue + range * 0.25)) > range * 0.05
                ? control.MinValue + range * 0.25
                : control.MinValue + range * 0.75;
            control.SetValue(target);

            // The event is coalesced onto the dispatcher, so give it a pass to arrive on.
            await Task.Delay(300);

            Check("ds-controls",
                MidiMusic.InstrumentControls.Count > 0
                && ReferenceEquals(byName, control)
                && Math.Abs(control.Value - target) < 1e-6
                && observed > 0
                && changedOnUiThread,
                $"controls={MidiMusic.InstrumentControls.Count} tags={MidiMusic.InstrumentTagStates.Count} " +
                $"\"{control.Name}\" set to {target:F2}, reads {control.Value:F2}, " +
                $"{observed} change(s) raised, onUiThread={changedOnUiThread}");

            // Put the knob back. The first control of this preset is its level, and the checks that
            // follow listen for what the instrument sounds - a test must not leave it turned down.
            control.SetValue(started);
            await Task.Delay(200);
        }
        else
        {
            Check("ds-controls", false, "the loaded instrument reported no controls");
        }

        MidiMusic.InstrumentControlChanged -= OnChanged;
    }

    /// <summary>
    /// MPE is a property path rather than a sound here - the Debussy file is ordinary MIDI - so what
    /// is checked is that the setting reaches the player, survives being read back, and that the
    /// zone information is readable while an instrument is loaded.
    /// </summary>
    private void CheckMpeRoundTrip()
    {
        MidiMusic.MpeMode = MpeMode.Auto;
        var readBack = MidiMusic.MpeMode;
        var lower = MidiMusic.MpeLowerZone;
        var upper = MidiMusic.MpeUpperZone;

        Check("mpe-mode-roundtrip", readBack == MpeMode.Auto,
            $"MpeMode={readBack} lowerZone(active={lower.IsActive} master={lower.MasterChannel} " +
            $"members={lower.MemberCount}) upperZone(active={upper.IsActive} master={upper.MasterChannel}) " +
            $"bendRange={MidiMusic.MpeMemberBendRange:F0}");

        MidiMusic.MpeMode = MpeMode.Off;
    }

    /// <summary>
    /// A Decent Sampler instrument names files that have to be on disk beside it, so a form that
    /// names no file cannot work - and the failure has to SAY so rather than reporting a missing
    /// resource, because the fix is to ship the instrument as content rather than to add a resource.
    /// </summary>
    private async Task CheckEmbeddedInstrumentFails()
    {
        var failure = "";
        var opened = false;
        void OnOpened(object s, EventArgs e) => opened = true;
        void OnFailed(object s, AudioPlayerFailedEventArgs e) => failure = e.Message;

        MidiMusic.MediaOpened += OnOpened;
        MidiMusic.MediaFailed += OnFailed;

        MidiMusic.Instrument = DemoSamplerEmbeddedSource;
        MidiMusic.Source = MidiSource;

        var timer = Stopwatch.StartNew();
        while (!opened && failure.Length == 0 && timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            await Task.Delay(100);
        }

        Check("ds-embedded-fails",
            !opened && failure.Contains("Decent Sampler", StringComparison.Ordinal)
                    && failure.Contains(".dspreset", StringComparison.Ordinal),
            failure.Length == 0 ? $"opened={opened}, no MediaFailed in {timer.Elapsed.TotalSeconds:F1} s" : failure);

        MidiMusic.MediaOpened -= OnOpened;
        MidiMusic.MediaFailed -= OnFailed;
    }

    /// <summary>The first line of a report list, for a check's detail text; empty when there is none.</summary>
    private static string DescribeFirst(System.Collections.Generic.IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            return $" first=\"{line}\"";
        }

        return "";
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Player.Play();
        StatusText.Text = $"Playing ({Player.Duration:mm\\:ss} total)";
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        Player.Pause();
        StatusText.Text = $"Paused at {Player.Position:mm\\:ss}";
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        Player.Stop();
        StatusText.Text = "Stopped";
    }

    private void JumpButton_Click(object sender, RoutedEventArgs e)
    {
        // An immediate programmatic jump to a specific timecode (no debounce).
        Player.Seek(TimeSpan.FromMinutes(1));
        StatusText.Text = "Jumped to 1:00";
    }

    private void EmbeddedSourceButton_Click(object sender, RoutedEventArgs e) => SwitchSource(EmbeddedSource);

    private void MsAppxSourceButton_Click(object sender, RoutedEventArgs e) => SwitchSource(MsAppxSource);

    private void SwitchSource(string source)
    {
        var wasPlaying = Player.IsPlaying;
        Player.Source = source;
        if (wasPlaying)
        {
            Player.Play();
        }
        StatusText.Text = $"Source: {source}";
    }

    private void ChimeButton_Click(object sender, RoutedEventArgs e) => SoundEffect.Play(ChimeEffect);

    private void ClickButton_Click(object sender, RoutedEventArgs e) => SoundEffect.Play(ClickEffect, volume: 0.8);

    private void ChimeOggButton_Click(object sender, RoutedEventArgs e) => PlayEffect(ChimeOggEffect, "OGG/Vorbis");

    private void ChimeFlacButton_Click(object sender, RoutedEventArgs e) => PlayEffect(ChimeFlacEffect, "FLAC");

    private void ChimeOpusButton_Click(object sender, RoutedEventArgs e) => PlayEffect(ChimeOpusEffect, "Opus");

    // SoundEffect.Play reports failure by returning false rather than throwing, so a missing or
    // unplayable effect never takes the app down - which makes it worth saying so on screen.
    private void PlayEffect(string source, string format)
        => StatusText.Text = SoundEffect.Play(source)
            ? $"Played the {format} chime"
            : $"The {format} chime could not be played";

    private void Player_PlaybackEnded(object sender, EventArgs e) => StatusText.Text = "Playback ended";

    private void Player_MediaFailed(object sender, AudioPlayerFailedEventArgs e) => StatusText.Text = $"Media failed: {e.Message}";

    // ===== Second player: song/format drop-downs + its own transport =====

    // The ten sample songs live loose in the output's Assets folder (see AudioPlayerDemo.Core.csproj)
    // and are addressed through the ms-appx:/// asset scheme.
    private static readonly string[] SongFileStems = { "sample_song_1", "sample_song_2" };

    // In the order of the Format drop-down's items. The player is told nothing about the format:
    // every one of these is just a Source it reads the container and codec out of. Only .opus
    // needs anything of the app at all, and that is one Register() call in App.xaml.cs.
    private static readonly string[] FormatExtensions = { "wav", "mp3", "ogg", "flac", "opus" };

    /// <summary>
    /// Reads the two drop-downs and loads the matching sample song into the second player,
    /// stopping any current playback. The user then presses Play to start it.
    /// </summary>
    private void LoadSecondPlayerSource()
    {
        // May fire from SelectionChanged during XAML load, before every element is created.
        if (Player2 is null || SongSelector is null || FormatSelector is null)
        {
            return;
        }

        var stem = SongFileStems[SongSelector.SelectedIndex < 1 ? 0 : 1];
        var extension = FormatExtensions[Math.Clamp(FormatSelector.SelectedIndex, 0, FormatExtensions.Length - 1)];
        var fileName = $"{stem}.{extension}";

        // Stop whatever is currently loaded/playing before switching. Skip this on the very first
        // load, when no source has been set yet (calling Stop on a never-loaded player is pointless
        // and best avoided).
        if (!string.IsNullOrEmpty(Player2.Source))
        {
            Player2.Stop();
        }

        Player2.Source = $"ms-appx:///Assets/{fileName}";
        Player2Status.Text = $"Loaded {fileName} - press Play";
    }

    private void SecondPlayerSelection_Changed(object sender, SelectionChangedEventArgs e) => LoadSecondPlayerSource();

    private void Play2Button_Click(object sender, RoutedEventArgs e)
    {
        Player2.Play();
        Player2Status.Text = $"Playing ({Player2.Duration:mm\\:ss} total)";
    }

    private void Pause2Button_Click(object sender, RoutedEventArgs e)
    {
        Player2.Pause();
        Player2Status.Text = $"Paused at {Player2.Position:mm\\:ss}";
    }

    private void Stop2Button_Click(object sender, RoutedEventArgs e)
    {
        Player2.Stop();
        Player2Status.Text = "Stopped";
    }

    private void Player2_PlaybackEnded(object sender, EventArgs e) => Player2Status.Text = "Playback ended";

    private void Player2_MediaFailed(object sender, AudioPlayerFailedEventArgs e) => Player2Status.Text = $"Media failed: {e.Message}";

    // ===== Third player: MIDI music through an SFZ or Decent Sampler instrument =====

    /// <summary>
    /// The instrument the MIDI pane renders through, as the drop-down currently reads. The music
    /// never changes - only what synthesizes it, which is what the extension in this string decides.
    /// </summary>
    private string SelectedInstrument
        => InstrumentSelector is not null && InstrumentSelector.SelectedIndex == 1
            ? DemoSamplerSource
            : InstrumentSource;

    /// <summary>
    /// Changing the instrument does not load anything by itself: the two instruments are very
    /// different sizes, and starting a three-second load from a drop-down would be a surprise.
    /// Pressing the load button afterwards is what picks the new one up.
    /// </summary>
    private void InstrumentSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Fires from SelectionChanged during XAML load, before every element on the page exists.
        if (MidiStatus is null)
        {
            return;
        }

        MidiStatus.Text = "Press \"Load the instrument and the music\" to load the chosen instrument";
    }

    /// <summary>
    /// Sets the instrument and the sequence together, which is what starts the load. The UI stays
    /// live while it runs - that is the point of MidiPlayer loading in the background - so the
    /// button is disabled rather than the window freezing.
    /// </summary>
    private void LoadMidiButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMidiButton.IsEnabled = false;
        MidiStatus.Text = InstrumentSelector.SelectedIndex == 1
            ? "Loading the demo sampler - two samples, decoded once and then shared..."
            : "Loading the piano - 226 samples, decoded once and then shared...";
        _instrumentLoadTimer.Restart();

        MidiMusic.Instrument = SelectedInstrument;
        MidiMusic.Source = MidiSource;
    }

    private void MidiMusic_MediaOpened(object sender, EventArgs e)
    {
        _instrumentLoadTimer.Stop();
        LoadMidiButton.IsEnabled = true;

        // Worth showing rather than hiding: an instrument loads even when parts of it could not be
        // built, an SFZ library may use opcodes the synthesizer does not implement, and a Decent
        // Sampler preset may ask for a feature only the ModestSynth add-on package supplies. All
        // three being zero is what "this instrument is fully supported" looks like.
        MidiStatus.Text =
            $"Loaded in {_instrumentLoadTimer.Elapsed.TotalSeconds:F1} s - {MidiMusic.Duration:mm\\:ss} of music, " +
            $"{MidiMusic.InstrumentProblems.Count} instrument problem(s), " +
            $"{MidiMusic.UnsupportedInstrumentOpcodes.Count} unsupported opcode(s), " +
            $"{MidiMusic.UnsupportedInstrumentFeatures.Count} unsupported feature(s). Press Play.";

        ShowInstrumentDetail();
        BindFirstInstrumentControl();
    }

    /// <summary>
    /// What the loaded instrument turned out to be, how it decided to hold its samples, and
    /// anything the MIDI file itself needed forgiving. A Standard MIDI File that breaks a rule is
    /// read leniently rather than refused, and SourceProblems is where that is visible.
    /// </summary>
    private void ShowInstrumentDetail()
    {
        var memory = string.IsNullOrEmpty(MidiMusic.InstrumentMemorySummary)
            ? "the format reports no memory policy"
            : MidiMusic.InstrumentMemorySummary;

        InstrumentStatus.Text =
            $"InstrumentKind: {MidiMusic.InstrumentKind} - {memory} - " +
            $"{MidiMusic.SourceProblems.Count} MIDI file problem(s)";
    }

    /// <summary>
    /// Points the control slider at the first of the loaded instrument's controls. A Decent Sampler
    /// preset carries its knobs on the instrument itself, so this is the instrument's own live
    /// parameter rather than a copy of it - and because instruments are shared, two players naming
    /// one preset move the same knob.
    /// </summary>
    private void BindFirstInstrumentControl()
    {
        _boundControl = MidiMusic.InstrumentControls.Count > 0 ? MidiMusic.InstrumentControls[0] : null;

        if (_boundControl is null)
        {
            ControlName.Text = "Instrument control";
            ControlValue.Text = "-";
            ControlSlider.IsEnabled = false;
            return;
        }

        ControlName.Text = string.IsNullOrEmpty(_boundControl.Label) ? _boundControl.Name : _boundControl.Label;
        ControlSlider.Minimum = _boundControl.MinValue;
        ControlSlider.Maximum = _boundControl.MaxValue;
        ControlSlider.StepFrequency = Math.Max((_boundControl.MaxValue - _boundControl.MinValue) / 100.0, 0.001);
        ControlSlider.IsEnabled = true;
        WriteControlValueToSlider(_boundControl.Value);
    }

    /// <summary>
    /// A drag on the control slider. Setting a control's value fires the bindings behind it at
    /// once - exactly as turning that knob in a player would - so this one line is the whole
    /// integration.
    /// </summary>
    private void ControlSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingControlSlider || _boundControl is null)
        {
            return;
        }

        _boundControl.SetValue(e.NewValue);
        ControlValue.Text = $"{e.NewValue:F2}";
    }

    /// <summary>
    /// The other direction: the instrument's own modulators and MIDI bindings move its knobs while
    /// the music plays, and MidiPlayer reports every movement on the UI thread (coalesced to one
    /// raise per control per dispatcher pass, because a modulator can move one knob on every
    /// rendered block).
    /// </summary>
    private void MidiMusic_InstrumentControlChanged(object sender, DecentSamplerControlChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Control, _boundControl))
        {
            return;
        }

        WriteControlValueToSlider(e.Control.Value);
    }

    private void WriteControlValueToSlider(double value)
    {
        _updatingControlSlider = true;
        ControlSlider.Value = value;
        _updatingControlSlider = false;
        ControlValue.Text = $"{value:F2}";
    }

    private void PlayMidiButton_Click(object sender, RoutedEventArgs e)
    {
        MidiMusic.Play();
        MidiStatus.Text = MidiMusic.IsPlaying
            ? $"Playing ({MidiMusic.Duration:mm\\:ss} total)"
            : "Nothing is loaded yet - press \"Load the piano and the music\" first";
    }

    private void PauseMidiButton_Click(object sender, RoutedEventArgs e)
    {
        MidiMusic.Pause();
        MidiStatus.Text = $"Paused at {MidiMusic.Position:mm\\:ss}";
    }

    private void StopMidiButton_Click(object sender, RoutedEventArgs e)
    {
        MidiMusic.Stop();
        MidiStatus.Text = "Stopped";
    }

    private void MidiMusic_PlaybackEnded(object sender, EventArgs e) => MidiStatus.Text = "Playback ended";

    private void MidiMusic_MediaFailed(object sender, AudioPlayerFailedEventArgs e)
    {
        _instrumentLoadTimer.Stop();
        LoadMidiButton.IsEnabled = true;
        MidiStatus.Text = $"Media failed: {e.Message}";

        // A failed load unloads whatever was reported before it, so the detail line and the knob go
        // back to their nothing-is-loaded state too.
        InstrumentStatus.Text = "";
        BindFirstInstrumentControl();
    }
}

/// <summary>
/// Formats a TimeSpan position/duration as m:ss for the indicator TextBlocks.
/// </summary>
public sealed class TimecodeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is TimeSpan time ? $"{(int)time.TotalMinutes}:{time.Seconds:00}" : "0:00";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>
/// Two-way binds a ComboBox's SelectedIndex to MidiPlayer.MpeMode, which is an enumeration whose
/// members are in the drop-down's own order (Off, LowerZone, UpperZone, Both, Auto).
/// </summary>
public sealed class MpeModeIndexConverter : IValueConverter
{
    /// <summary>Turns the player's MpeMode into the index of the matching drop-down item.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is MpeMode mode ? (int)mode : 0;

    /// <summary>
    /// Turns the selected index back into an MpeMode. A ComboBox reports -1 while nothing is
    /// selected, which is read as Off rather than as an invalid enumeration value.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is int index && Enum.IsDefined(typeof(MpeMode), index) ? (MpeMode)index : MpeMode.Off;
}
