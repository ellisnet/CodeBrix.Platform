using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.UI.VideoPlayer.Skia;
using CodeBrix.VideoPlayback.Effects;
using CodeBrix.VideoPlayback.Rendering;
using CodeBrix.VideoPlayback.Sources;
using SkiaSharp;

namespace VideoPlayerDemo.Views;

public sealed partial class MainPage : Page
{
    // The seven clips this demo ships, in the order of the drop-down. Keys double as the value the
    // smoke mode accepts, so a scripted run names a clip the same way a person picks one.
    private static readonly (string Key, string Asset)[] Clips =
    {
        ("landscape_720p_webm", "ms-appx:///Assets/video/landscape_720p_webm.webm"),
        ("portrait_720p_webm", "ms-appx:///Assets/video/portrait_720p_webm.webm"),
        ("landscape_720p_mode1", "ms-appx:///Assets/video/landscape_720p_mode1.cbv"),
        ("portrait_720p_mode1", "ms-appx:///Assets/video/portrait_720p_mode1.cbv"),
        ("landscape_720p_mode2", "ms-appx:///Assets/video/landscape_720p_mode2.cbv"),
        ("portrait_720p_mode2", "ms-appx:///Assets/video/portrait_720p_mode2.cbv"),
        ("landscape_720p_mode2_chapters", "ms-appx:///Assets/video/landscape_720p_mode2_chapters.cbv"),
    };

    public MainPage()
    {
        InitializeComponent();

        // Optional headless hook: exercise the whole player from a script and exit (used by the
        // repo's scripted X11 smoke verification).
        if (SmokeOptions.FromEnvironment() is { } smoke)
        {
            Loaded += (_, _) => RunSmoke(smoke);
        }
    }

    #region | The demo's own controls |

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource();
        if (string.IsNullOrWhiteSpace(source))
        {
            StatusText.Text = "Pick a clip, or type a path in the box.";
            return;
        }

        // Both of these are read WHEN A SOURCE IS OPENED, so they are applied here rather than
        // live: RenderPath refuses to change while something is open, and SourceMode simply would
        // not take effect until the next open.
        Player.Source = "";
        Player.RenderPath = SelectedRenderPath();
        Player.SourceMode = PreloadedLoopCheck.IsChecked == true ? FileSourceMode.Preloaded : FileSourceMode.Streaming;
        Player.IsLooping = PreloadedLoopCheck.IsChecked == true;

        Player.Source = source;
        StatusText.Text = $"Opened {source}";
    }

    private string SelectedSource()
    {
        var index = ClipSelector.SelectedIndex;
        if (index >= 0 && index < Clips.Length)
        {
            return Clips[index].Asset;
        }

        return PathBox.Text?.Trim() ?? "";
    }

    private VideoRenderPath SelectedRenderPath() => RenderPathSelector.SelectedIndex switch
    {
        1 => VideoRenderPath.GpuNoFallback,
        2 => VideoRenderPath.Cpu,
        _ => VideoRenderPath.GpuAuto,
    };

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

    private void Player_MediaOpened(object sender, EventArgs e)
    {
        ChapterSelector.Items.Clear();
        foreach (var chapter in Player.Chapters)
        {
            ChapterSelector.Items.Add(new ComboBoxItem { Content = $"{chapter.Start:mm\\:ss}  {chapter.Title}" });
        }

        // Only the chaptered Mode 2 clip carries chapters, so this list is usually empty - say so
        // rather than leaving an inexplicably dead control.
        ChapterSelector.IsEnabled = ChapterSelector.Items.Count > 0;
        StatusText.Text = ChapterSelector.Items.Count > 0
            ? $"Opened: {Player.Duration:mm\\:ss}, {Player.Chapters.Count} chapters"
            : $"Opened: {Player.Duration:mm\\:ss}, no chapters in this file";
    }

    private void ChapterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChapterSelector.SelectedIndex >= 0)
        {
            Player.SeekToChapter(ChapterSelector.SelectedIndex);
        }
    }

    private void Player_PlaybackEnded(object sender, EventArgs e) => StatusText.Text = "Playback ended";

    private void Player_MediaFailed(object sender, VideoPlayerFailedEventArgs e) =>
        StatusText.Text = $"Media failed: {e.Message}";

    // The readout above is bound; this handler is here to show that the moment the path settles -
    // or quietly falls back - is an event an application can react to.
    private void Player_RenderPathChanged(object sender, VideoPlayerRenderPathChangedEventArgs e) =>
        StatusText.Text = $"Render path settled: {e.ActiveRenderPath}, effects " +
                          (e.EffectsActive ? "active" : "not applied");

    #endregion

    #region | Smoke mode |

    /// <summary>
    /// What a scripted run asked for. Null when no scripted run was asked for at all.
    /// </summary>
    private sealed record SmokeOptions(
        string Source,
        string OutputFolder,
        VideoRenderPath RenderPath,
        string LutPath,
        string PausedLutPath)
    {
        public static SmokeOptions FromEnvironment()
        {
            var source = Environment.GetEnvironmentVariable("CODEBRIX_VIDEOPLAYER_SMOKE");
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var renderPath = Environment.GetEnvironmentVariable("CODEBRIX_VIDEOPLAYER_SMOKE_RENDERPATH");
            return new SmokeOptions(
                source.Trim(),
                Environment.GetEnvironmentVariable("CODEBRIX_VIDEOPLAYER_SMOKE_OUT") ?? Directory.GetCurrentDirectory(),
                Enum.TryParse(renderPath, ignoreCase: true, out VideoRenderPath parsed) ? parsed : VideoRenderPath.GpuAuto,
                Environment.GetEnvironmentVariable("CODEBRIX_VIDEOPLAYER_SMOKE_LUT") ?? "",
                Environment.GetEnvironmentVariable("CODEBRIX_VIDEOPLAYER_SMOKE_PAUSEDLUT") ?? "");
        }

        /// <summary>The clip to open: a corpus key, or anything the player itself accepts.</summary>
        public string ResolveSource()
        {
            var match = Clips.FirstOrDefault(c => string.Equals(c.Key, Source, StringComparison.OrdinalIgnoreCase));
            return match.Asset ?? Source;
        }

        /// <summary>A file name that says what this run was, so a folder of runs reads itself.</summary>
        public string PngName()
        {
            var stem = Path.GetFileNameWithoutExtension(ResolveSource());
            var lut = string.IsNullOrEmpty(LutPath) ? "" : "_lut-" + Path.GetFileNameWithoutExtension(LutPath);
            return $"{stem}_{RenderPath}{lut}.png";
        }
    }

    private static void Fact(string name, object value) =>
        Console.WriteLine($"VPD-SMOKE: {name}={value?.ToString() ?? "(null)"}");

    private async void RunSmoke(SmokeOptions options)
    {
        var failures = 0;

        void Check(string step, bool ok, string detail)
        {
            Console.WriteLine($"VPD-SMOKE: {(ok ? "PASS" : "FAIL")} {step} ({detail})");
            if (!ok)
            {
                failures++;
            }
        }

        try
        {
            var failureMessage = "";
            Player.MediaFailed += (_, e) => failureMessage = e.Message;

            var source = options.ResolveSource();
            Fact("source", source);
            Fact("requestedRenderPath", options.RenderPath);

            // Before the source, always: the render path is fixed once something is open.
            Player.RenderPath = options.RenderPath;

            if (!string.IsNullOrEmpty(options.LutPath))
            {
                // 100 % so the difference from an ungraded capture is unmistakable.
                Player.Effects.Add(LutEffect.FromCubeFile(options.LutPath, 100d));
                Fact("lut", Path.GetFileName(options.LutPath));
            }

            Player.Source = source;
            await Task.Delay(500);

            Check("opened", Player.DurationSeconds > 0 && failureMessage.Length == 0,
                failureMessage.Length == 0 ? $"DurationSeconds={Player.DurationSeconds:F2}" : failureMessage);

            if (failures > 0)
            {
                Finish(failures);
                return;
            }

            // Chapters are read at open (the bespoke container keeps them in its header), so they
            // are reportable before the first frame; the chaptered clip lists three, the rest none.
            Fact("chapters", Player.Chapters.Count);
            if (Player.Chapters.Count > 0)
            {
                Fact("chapterTitles", string.Join(" | ", Player.Chapters.Select(c => $"{c.Start:mm\\:ss\\.fff} {c.Title}")));
            }

            var startPosition = Player.PositionSeconds;
            Player.Play();
            await Task.Delay(3000);
            var endPosition = Player.PositionSeconds;

            Fact("positionAtStart", startPosition.ToString("F2", CultureInfo.InvariantCulture));
            Fact("positionAtEnd", endPosition.ToString("F2", CultureInfo.InvariantCulture));
            Check("position-advances", endPosition - startPosition > 1.0,
                $"advanced {endPosition - startPosition:F2} s in 3 s");

            Fact("activeRenderPath", Player.ActiveRenderPath);
            Fact("effectsActive", Player.EffectsActive);
            Fact("currentChapter", Player.CurrentChapter?.Title ?? "(none)");

            var statistics = Player.FrameStatistics;
            Fact("framesPosted", statistics.Posted);
            Fact("framesPresented", statistics.Presented);
            Fact("framesDropped", statistics.Dropped);

            using var capture = Player.CapturePresentedFrame();
            Check("captured-a-frame", capture is not null, capture is null ? "nothing presented" : "captured");

            if (capture is not null)
            {
                Fact("captureSize", $"{capture.Width}x{capture.Height}");

                Directory.CreateDirectory(options.OutputFolder);
                var pngPath = Path.Combine(options.OutputFolder, options.PngName());
                using (var png = capture.Encode(SKEncodedImageFormat.Png, 100))
                {
                    File.WriteAllBytes(pngPath, png.ToArray());
                }
                Fact("png", pngPath);

                Measure(capture, out var nonBlackPercent, out var meanLuminance);
                Fact("meanLuminance", meanLuminance.ToString("F2", CultureInfo.InvariantCulture));
                Fact("nonBlackPercent", nonBlackPercent.ToString("F1", CultureInfo.InvariantCulture));

                // A black rectangle is exactly what a broken present path produces, so this is the
                // check that says pixels really flowed.
                Check("picture-is-not-black", meanLuminance > 1.0, $"meanLuminance={meanLuminance:F2}");
            }

            if (!string.IsNullOrEmpty(options.PausedLutPath))
            {
                await RunPausedRecomposeCheck(options, Check);
            }

            Player.Stop();
            Finish(failures);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"VPD-SMOKE: RESULT FAIL (exception: {exception.Message})");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Proves that a grade dialled in while playback is PAUSED reaches the screen with no seek and
    /// nothing decoding - the picture is composed again from the frame the player is still holding.
    /// </summary>
    /// <param name="options">What the run asked for; its paused-LUT path is the grade to add.</param>
    /// <param name="check">The run's PASS/FAIL reporter.</param>
    private async Task RunPausedRecomposeCheck(SmokeOptions options, Action<string, bool, string> check)
    {
        Player.Pause();
        await Task.Delay(500);

        var positionBefore = Player.PositionSeconds;
        var stem = Path.GetFileNameWithoutExtension(options.ResolveSource()) + "_" + options.RenderPath;

        byte[] beforeBytes;
        double luminanceBefore;
        using (var before = Player.CapturePresentedFrame())
        {
            if (before is null)
            {
                check("paused-recompose", false, "nothing was on screen to recompose");
                return;
            }

            Measure(before, out _, out luminanceBefore);
            beforeBytes = WriteCapture(before, options.OutputFolder, stem + "_paused-before.png");
        }

        // The grade goes on while nothing is decoding. Nothing is sought, nothing is played.
        Player.Effects.Add(LutEffect.FromCubeFile(options.PausedLutPath, 100d));
        await Task.Delay(500);

        var positionAfter = Player.PositionSeconds;

        byte[] afterBytes;
        double luminanceAfter;
        using (var after = Player.CapturePresentedFrame())
        {
            if (after is null)
            {
                check("paused-recompose", false, "nothing was on screen after the grade");
                return;
            }

            Measure(after, out _, out luminanceAfter);
            afterBytes = WriteCapture(after, options.OutputFolder, stem + "_paused-after.png");
        }

        Fact("pausedLut", Path.GetFileName(options.PausedLutPath));
        Fact("pausedPositionBefore", positionBefore.ToString("F2", CultureInfo.InvariantCulture));
        Fact("pausedPositionAfter", positionAfter.ToString("F2", CultureInfo.InvariantCulture));
        Fact("pausedMeanLuminanceBefore", luminanceBefore.ToString("F2", CultureInfo.InvariantCulture));
        Fact("pausedMeanLuminanceAfter", luminanceAfter.ToString("F2", CultureInfo.InvariantCulture));
        Fact("pausedEffectsActive", Player.EffectsActive);
        Fact("pausedIsPlaying", Player.IsPlaying);

        check(
            "paused-position-did-not-move",
            Math.Abs(positionAfter - positionBefore) < 0.05 && !Player.IsPlaying,
            $"{positionBefore:F2} s -> {positionAfter:F2} s, playing={Player.IsPlaying}");

        // On the graphics path the grade is applied and the picture must change; on the processor
        // path with AllowEffectsOnCpu left false it is silently ignored, and the picture must NOT.
        var expectedToChange = Player.ActiveRenderPath == VideoRenderBackend.Gpu;
        var changed = !afterBytes.AsSpan().SequenceEqual(beforeBytes);

        check(
            expectedToChange ? "paused-recompose-changed-the-picture" : "paused-recompose-left-the-picture-alone",
            changed == expectedToChange,
            $"changed={changed}, meanLuminance {luminanceBefore:F2} -> {luminanceAfter:F2}");
    }

    /// <summary>Writes a capture to the run's folder and hands back the bytes that were written.</summary>
    /// <param name="image">The picture to write.</param>
    /// <param name="folder">The folder to write it into.</param>
    /// <param name="name">The file name to give it.</param>
    /// <returns>The encoded bytes, so two captures can be compared exactly.</returns>
    private static byte[] WriteCapture(SKImage image, string folder, string name)
    {
        Directory.CreateDirectory(folder);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, bytes);
        Fact("png", path);
        return bytes;
    }

    private static void Finish(int failures)
    {
        Console.WriteLine($"VPD-SMOKE: RESULT {(failures == 0 ? "PASS" : $"FAIL ({failures})")}");
        Console.Out.Flush();
        Environment.Exit(failures == 0 ? 0 : 1);
    }

    /// <summary>
    /// Measures how bright the captured picture is, and how much of it is not black.
    /// </summary>
    /// <remarks>
    /// Sampling every fourth pixel in each direction is plenty to tell a picture from a black
    /// rectangle, and keeps even a large capture to a few hundred thousand reads.
    /// </remarks>
    private static void Measure(SKImage image, out double nonBlackPercent, out double meanLuminance)
    {
        nonBlackPercent = 0;
        meanLuminance = 0;

        using var bitmap = SKBitmap.FromImage(image);
        if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0)
        {
            return;
        }

        const int step = 4;
        double total = 0;
        var counted = 0;
        var nonBlack = 0;

        for (var y = 0; y < bitmap.Height; y += step)
        {
            for (var x = 0; x < bitmap.Width; x += step)
            {
                var pixel = bitmap.GetPixel(x, y);
                var luminance = (0.2126 * pixel.Red) + (0.7152 * pixel.Green) + (0.0722 * pixel.Blue);

                total += luminance;
                counted++;

                if (pixel.Red > 8 || pixel.Green > 8 || pixel.Blue > 8)
                {
                    nonBlack++;
                }
            }
        }

        if (counted == 0)
        {
            return;
        }

        meanLuminance = total / counted;
        nonBlackPercent = nonBlack * 100.0 / counted;
    }

    #endregion
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
