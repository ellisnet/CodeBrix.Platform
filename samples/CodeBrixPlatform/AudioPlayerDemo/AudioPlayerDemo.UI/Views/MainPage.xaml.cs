using System;
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

    private async void RunSelfTest()
    {
        var failures = 0;
        void Check(string step, bool ok, string detail)
        {
            Console.WriteLine($"APD-SELFTEST: {(ok ? "PASS" : "FAIL")} {step} ({detail})");
            if (!ok)
            {
                failures++;
            }
        }

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

            Console.WriteLine($"APD-SELFTEST: RESULT {(failures == 0 ? "PASS" : $"FAIL ({failures})")}");
            Environment.Exit(failures == 0 ? 0 : 1);
        }
        catch (Exception e)
        {
            Console.WriteLine($"APD-SELFTEST: RESULT FAIL (exception: {e.Message})");
            Environment.Exit(2);
        }
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

    private void Player_PlaybackEnded(object sender, EventArgs e) => StatusText.Text = "Playback ended";

    private void Player_MediaFailed(object sender, AudioPlayerFailedEventArgs e) => StatusText.Text = $"Media failed: {e.Message}";

    // ===== Second player: song/format drop-downs + its own transport =====

    // The four sample songs live loose in the output's Assets folder (see AudioPlayerDemo.Core.csproj)
    // and are addressed through the ms-appx:/// asset scheme.
    private static readonly string[] SongFileStems = { "sample_song_1", "sample_song_2" };

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
        // FormatSelector items are WAV (index 0), MP3 (index 1).
        var extension = FormatSelector.SelectedIndex == 1 ? "mp3" : "wav";
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
