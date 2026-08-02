using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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

    private readonly Stopwatch _instrumentLoadTimer = new();

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

    // ===== Third player: MIDI music through an SFZ instrument =====

    /// <summary>
    /// Sets the instrument and the sequence together, which is what starts the load. The UI stays
    /// live while it runs - that is the point of MidiPlayer loading in the background - so the
    /// button is disabled rather than the window freezing.
    /// </summary>
    private void LoadMidiButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMidiButton.IsEnabled = false;
        MidiStatus.Text = "Loading the instrument - 226 samples, decoded once and then shared...";
        _instrumentLoadTimer.Restart();

        MidiMusic.Instrument = InstrumentSource;
        MidiMusic.Source = MidiSource;
    }

    private void MidiMusic_MediaOpened(object sender, EventArgs e)
    {
        _instrumentLoadTimer.Stop();
        LoadMidiButton.IsEnabled = true;

        // Worth showing rather than hiding: an instrument loads even when parts of it could not be
        // built, and an SFZ library may use opcodes the synthesizer does not implement. Both being
        // zero is what "this instrument is fully supported" looks like.
        MidiStatus.Text =
            $"Loaded in {_instrumentLoadTimer.Elapsed.TotalSeconds:F1} s - {MidiMusic.Duration:mm\\:ss} of music, " +
            $"{MidiMusic.InstrumentProblems.Count} instrument problem(s), " +
            $"{MidiMusic.UnsupportedInstrumentOpcodes.Count} unsupported opcode(s). Press Play.";
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
