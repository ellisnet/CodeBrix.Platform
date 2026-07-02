#nullable enable

// Ported from the CodeBrix.Platform (Uno-based) Lottie package:
// src/AddIns/Platform.UI.Lottie/LottieVisualSource.Skottie.cs
// Same SkiaSharp.Skottie playback engine, frame timing, stretch/centering math and
// render calls, adapted for native WinUI 3: the render surface is a software
// SKXamlCanvas (the same canvas the CodeBrix.Platform WinUI head uses for SVG), and
// dispatching uses Microsoft.UI.Dispatching.

using CodeBrix.Platform.WinUI.Skia;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.SceneGraph;
using SkiaSharp.Views.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CodeBrix.Platform.WinUI.Lottie;

partial class LottieVisualSourceBase
{
    private SkiaSharp.Skottie.Animation? _animation;

    private bool _wasPlaying;
    private DispatcherQueueTimer? _timer;
    private readonly object _gate = new();

    private SKXamlCanvas? _softwareCanvas;

    private Uri? _lastSource;
    private PlayState? _playState;

    private record PlayState(double FromProgress, double ToProgress, bool Looped)
    {
        public TimeSpan GetFromProgressUsingDuration(TimeSpan duration)
            => TimeSpan.FromSeconds(duration.TotalSeconds * FromProgress);

        public TimeSpan GetToProgressUsingDuration(TimeSpan duration)
            => TimeSpan.FromSeconds(duration.TotalSeconds * ToProgress);
    }

    private readonly Stopwatch _stopwatch = new Stopwatch();
    private TimeSpan? _progress;

    private InvalidationController? _invalidationController;
    private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

    private async Task InnerUpdate(CancellationToken ct)
    {
        var player = _player;

        if (player == null)
        {
            return;
        }

        await SetProperties();

        async Task SetProperties()
        {
            try
            {
                var sourceUri = UriSource;
                if (sourceUri is null)
                {
                    return;
                }

                if (_lastSource == null || !_lastSource.Equals(sourceUri))
                {
                    _lastSource = sourceUri;
                    if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
                    {
                        var cacheKey = sourceUri.OriginalString;
                        _animationDataSubscription.Disposable = null;
                        _animationDataSubscription.Disposable =
                            LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

                        void OnJsonChanged(string updatedJson, string updatedCacheKey)
                        {
                            // The render surface is a UI element, so apply on the UI thread.
                            if (DispatcherQueue.HasThreadAccess)
                            {
                                ApplyJson(updatedJson);
                            }
                            else
                            {
                                DispatcherQueue.TryEnqueue(() => ApplyJson(updatedJson));
                            }
                        }

                        void ApplyJson(string updatedJson)
                        {
                            try
                            {
                                var stream = new MemoryStream(Encoding.UTF8.GetBytes(updatedJson));

                                if (SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation))
                                {
                                    animation.Seek(0);

                                    Debug.WriteLine(
                                        $"[LottieVisualSource] Version: {animation.Version} Duration: {animation.Duration} Fps: {animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Failed to load animation.");
                                }

                                SetAnimation(animation);
                                UpdatePlayerProperties();

                                if (_playState != null)
                                {
                                    var (fromProgress, toProgress, looped) = _playState;
                                    Play(fromProgress, toProgress, looped);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException("Failed load the animation", ex);
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Failed to load animation: {sourceUri}");
                    }

                    // Force layout to recalculate
                    player.InvalidateMeasure();
                    player.InvalidateArrange();

                    if (_playState != null)
                    {
                        var (fromProgress, toProgress, looped) = _playState;
                        Play(fromProgress, toProgress, looped);
                    }
                    else if (player.AutoPlay)
                    {
                        Play(0, 1, true);
                    }
                }

                if (_animation == null)
                {
                    return;
                }

                UpdatePlayerProperties();
                Invalidate();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[LottieVisualSource] Failed to update lottie player for [{UriSource}]: {e}");
            }
        }
    }

    /// <summary>
    /// Pushes the loaded animation's Duration / IsAnimatedVisualLoaded onto the player
    /// and refreshes layout. (The Uno-based original sets these inline in InnerUpdate;
    /// here the JSON can arrive after InnerUpdate returns, so the same updates also run
    /// when the animation is applied.)
    /// </summary>
    private void UpdatePlayerProperties()
    {
        if (_player is not { } player || _animation is not { } animation)
        {
            return;
        }

        var duration = animation.Duration;
        player.SetValue(AnimatedVisualPlayer.DurationProperty, duration);

        var isLoaded = duration > TimeSpan.Zero;
        player.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, isLoaded);

        player.InvalidateMeasure();
        player.InvalidateArrange();
    }

    private void SetAnimation(SkiaSharp.Skottie.Animation animation)
    {
        lock (_gate)
        {
            _animation = animation;
        }

        if (_softwareCanvas is null)
        {
            _softwareCanvas = new SKXamlCanvas();
            _softwareCanvas.PaintSurface += OnSoftwareCanvas_PaintSurface;
        }

        AttachRenderSurface();
    }

    /// <summary>
    /// Hosts this source's render surface in the current player (no-op until the
    /// animation has been loaded and the surface created).
    /// </summary>
    private void AttachRenderSurface()
    {
        if (_softwareCanvas != null)
        {
            _player?.SetRenderSurface(_softwareCanvas);
        }
    }

    private void OnSoftwareCanvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        Render(e.Surface.Canvas, e.Surface.Canvas.LocalClipBounds.Size, saveRestoreAndCleanCanvas: true);
    }

    private void Render(SKCanvas canvas, SKSize localSize, bool saveRestoreAndCleanCanvas)
    {
        lock (_gate)
        {
            var animation = _animation;
            if (animation is null || _player is null)
            {
                return;
            }

            if (_invalidationController is null)
            {
                _invalidationController = new InvalidationController();
                _invalidationController.Begin();
            }

            var frameTime = GetFrameTime();

            var animationSize = new Size(animation.Size.Width, animation.Size.Height);
            var scale = ImageSizeHelper.BuildScale(_player.Stretch, new Size(localSize.Width, localSize.Height), animationSize);
            var scaledSize = new Size(animation.Size.Width * scale.x, animation.Size.Height * scale.y);

            var x = (localSize.Width - scaledSize.Width) / 2;
            var y = (localSize.Height - scaledSize.Height) / 2;

            animation.SeekFrameTime(frameTime, _invalidationController);

            if (saveRestoreAndCleanCanvas)
            {
                canvas.Save();
                canvas.Clear(GetBackgroundColor());
            }

            canvas.Translate((float)x, (float)y);
            canvas.Scale((float)(scaledSize.Width / animation.Size.Width), (float)(scaledSize.Height / animation.Size.Height));

            animation.Render(canvas, new SKRect(0, 0, animation.Size.Width, animation.Size.Height));

            if (saveRestoreAndCleanCanvas)
            {
                canvas.Restore();
            }

            _invalidationController.Reset();
        }
    }

    private SKColor GetBackgroundColor()
    {
        if (_player?.Background is SolidColorBrush sb)
        {
            var alpha = (byte)Math.Round(sb.Color.A * sb.Opacity);
            return new SKColor(alpha: alpha, red: sb.Color.R, green: sb.Color.G, blue: sb.Color.B);
        }

        return SKColors.Transparent;
    }

    private TimeSpan GetFrameTime()
    {
        if (_animation is null || _timer is null || !(_playState is { } playState) || _player is null)
        {
            return _progress ?? TimeSpan.Zero;
        }

        var frameTime = TimeSpan.FromSeconds((_stopwatch.Elapsed + playState.GetFromProgressUsingDuration(_animation.Duration)).TotalSeconds * _player.PlaybackRate);

        if (frameTime > playState.GetToProgressUsingDuration(_animation.Duration))
        {
            if (playState.Looped)
            {
                _stopwatch.Restart();
                _invalidationController?.End();
                _invalidationController?.Begin();
            }
            else
            {
                // Freeze the animation at the "to" progress value
                _progress = frameTime;

                Stop();
            }
        }

        return frameTime;
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Play"/>
    public void Play(double fromProgress, double toProgress, bool looped)
    {
        if (_animation != null)
        {
            if (_stopwatch.IsRunning)
            {
                Stop();
            }

            _playState = new(fromProgress, toProgress, looped);

            _progress = null;

            _timer = DispatcherQueue.CreateTimer();
            _timer.Tick += (s, e) => Invalidate();

            _timer.Interval = TimeSpan.FromSeconds(Math.Max(1 / 120d, 1 / _animation.Fps));
            _timer.Start();
            _stopwatch.Restart();

            SetIsPlaying(true);
        }
        else
        {
            _playState = new(fromProgress, toProgress, looped);
        }
    }

    private void Invalidate()
    {
        _softwareCanvas?.Invalidate();
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Stop"/>
    public void Stop()
    {
        void DoStop()
        {
            _playState = null;
            SetIsPlaying(false);
            _timer?.Stop();
            _stopwatch.Stop();
            _invalidationController?.End();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            DoStop();
        }
        else
        {
            DispatcherQueue.TryEnqueue(DoStop);
        }
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Pause"/>
    public void Pause()
    {
        _timer?.Stop();
        _stopwatch.Stop();

        SetIsPlaying(false);
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Resume"/>
    public void Resume()
    {
        _stopwatch.Start();
        _timer?.Start();

        SetIsPlaying(true);
    }

    /// <inheritdoc cref="IAnimatedVisualSource.SetProgress"/>
    public void SetProgress(double progress)
    {
        var clampedProgress = Math.Max(0, Math.Min(1, progress));

        if (_animation != null)
        {
            Stop();
            _progress = TimeSpan.FromSeconds(_animation.Duration.TotalSeconds * clampedProgress);
            Invalidate();
        }
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Load"/>
    public void Load()
    {
        if (_wasPlaying)
        {
            _wasPlaying = false;
            Resume();
        }
    }

    /// <inheritdoc cref="IAnimatedVisualSource.Unload"/>
    public void Unload()
    {
        if (_player?.IsPlaying ?? false)
        {
            _wasPlaying = true;
            Pause();
        }
    }

    private Size CompositionSize
        => _animation?.Size is { } size
            ? new Size(size.Width, size.Height)
            : default;
}
