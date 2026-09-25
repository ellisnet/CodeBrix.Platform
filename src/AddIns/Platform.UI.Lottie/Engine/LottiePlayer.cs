#nullable enable

#if HAS_SKOTTIE

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SkiaSharp;
using SkiaSharp.SceneGraph;

namespace CodeBrix.Platform.UI.Lottie.Engine;

/// <summary>How an animation is scaled into its area (the values of the WinUI Stretch enum).</summary>
internal enum LottieStretch
{
	/// <summary>Natural size.</summary>
	None = 0,

	/// <summary>Stretched to fill, ignoring the aspect ratio.</summary>
	Fill = 1,

	/// <summary>Scaled to fit, keeping the aspect ratio.</summary>
	Uniform = 2,

	/// <summary>Scaled to fill, keeping the aspect ratio (cropped).</summary>
	UniformToFill = 3,
}

/// <summary>A segment being played: from/to progress (0..1 of the duration) and whether it loops.</summary>
/// <param name="FromProgress">Where the segment starts.</param>
/// <param name="ToProgress">Where it ends.</param>
/// <param name="Looped">Whether it restarts at the end.</param>
internal sealed record LottiePlayState(double FromProgress, double ToProgress, bool Looped)
{
	/// <summary>The segment start as a time.</summary>
	/// <param name="duration">The animation's duration.</param>
	/// <returns>The time.</returns>
	public TimeSpan GetFromProgressUsingDuration(TimeSpan duration)
		=> TimeSpan.FromSeconds(duration.TotalSeconds * FromProgress);

	/// <summary>The segment end as a time.</summary>
	/// <param name="duration">The animation's duration.</param>
	/// <returns>The time.</returns>
	public TimeSpan GetToProgressUsingDuration(TimeSpan duration)
		=> TimeSpan.FromSeconds(duration.TotalSeconds * ToProgress);
}

/// <summary>
/// The Lottie ENGINE (WPE1 C9; decision D-M7: our own player over Skottie): holds a Skottie animation and its play
/// state (segment, loop, pause, progress), advances its frame time from a stopwatch, and renders the current frame onto
/// an <see cref="SKCanvas"/>. The frames are driven by an <see cref="ITickSource"/> the host supplies (one per Play, as
/// the source classes always created a new dispatcher timer there). Names no XAML type: the host (LottieVisualSourceBase
/// on CodeBrix.Platform, a CodeBrix.Mobile view) supplies the surface, repaints on <see cref="InvalidateRequested"/>,
/// mirrors <see cref="IsPlayingChanged"/> and runs <see cref="Stop"/>'s work on its UI thread.
/// </summary>
internal sealed class LottiePlayer
{
	private readonly Func<ITickSource> _createTickSource;
	private readonly Action<Action> _runOnOwnerThread;
	private readonly Stopwatch _stopwatch = new Stopwatch();
	private readonly object _gate = new();

	private SkiaSharp.Skottie.Animation? _animation;
	private ITickSource? _timer;
	private LottiePlayState? _playState;
	private TimeSpan? _progress;
	private InvalidationController? _invalidationController;

	/// <summary>Creates the engine.</summary>
	/// <param name="createTickSource">Creates the frame timer; called on each <see cref="Play"/>, on the caller's
	/// thread (the host's UI thread).</param>
	/// <param name="runOnOwnerThread">Runs an action on the host's UI thread: at once when already there, else
	/// queued.</param>
	internal LottiePlayer(Func<ITickSource> createTickSource, Action<Action> runOnOwnerThread)
	{
		_createTickSource = createTickSource ?? throw new ArgumentNullException(nameof(createTickSource));
		_runOnOwnerThread = runOnOwnerThread ?? throw new ArgumentNullException(nameof(runOnOwnerThread));
	}

	/// <summary>Raised when the current frame must be repainted (each timer tick, a progress change).</summary>
	internal event Action? InvalidateRequested;

	/// <summary>Raised when playing starts or stops (the host mirrors it, e.g. on AnimatedVisualPlayer.IsPlaying).</summary>
	internal event Action<bool>? IsPlayingChanged;

	/// <summary>The animation, or null before one is set.</summary>
	internal SkiaSharp.Skottie.Animation? Animation
	{
		get => _animation;
		set => _animation = value;
	}

	/// <summary>The segment being played, or null when stopped (a Play before the animation arrives is kept here).</summary>
	internal LottiePlayState? PlayState => _playState;

	/// <summary>Whether the frame clock is running (playing and not paused).</summary>
	internal bool IsRunning => _stopwatch.IsRunning;

	/// <summary>
	/// Decodes an animation from its JSON and seeks it to its start.
	/// </summary>
	/// <param name="json">The animation JSON.</param>
	/// <returns>The animation.</returns>
	/// <exception cref="InvalidOperationException">The JSON is not a Lottie animation Skottie can load.</exception>
	internal static SkiaSharp.Skottie.Animation CreateAnimation(string json)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

		if (SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation))
		{
			animation.Seek(0);
			return animation;
		}

		throw new InvalidOperationException("Failed to load animation.");
	}

	/// <summary>
	/// Plays a segment. Without an animation yet, the segment is only remembered (the host plays it when the animation
	/// arrives). A segment already playing is stopped first; a new frame timer is started.
	/// </summary>
	/// <param name="fromProgress">Where the segment starts (0..1).</param>
	/// <param name="toProgress">Where it ends (0..1).</param>
	/// <param name="looped">Whether it restarts at the end.</param>
	internal void Play(double fromProgress, double toProgress, bool looped)
	{
		if (_animation != null)
		{
			if (_stopwatch.IsRunning)
			{
				Stop();
			}

			_playState = new(fromProgress, toProgress, looped);

			_progress = null;

			_timer = _createTickSource();
			_timer.Tick += Invalidate;

			_timer.Interval = TimeSpan.FromSeconds(Math.Max(1 / 120d, 1 / _animation.Fps));
			_timer.Start();
			_stopwatch.Restart();

			IsPlayingChanged?.Invoke(true);
		}
		else
		{
			_playState = new(fromProgress, toProgress, looped);
		}
	}

	/// <summary>Stops playing (on the host's UI thread): forgets the segment and stops the timer and the clock.</summary>
	internal void Stop()
	{
		void DoStop()
		{
			_playState = null;
			IsPlayingChanged?.Invoke(false);
			_timer?.Stop();
			_stopwatch.Stop();
			_invalidationController?.End();
		}

		_runOnOwnerThread(DoStop);
	}

	/// <summary>Pauses the timer and the clock.</summary>
	internal void Pause()
	{
		_timer?.Stop();
		_stopwatch.Stop();

		IsPlayingChanged?.Invoke(false);
	}

	/// <summary>Resumes the clock and the timer.</summary>
	internal void Resume()
	{
		_stopwatch.Start();
		_timer?.Start();

		IsPlayingChanged?.Invoke(true);
	}

	/// <summary>Stops and shows the frame at a progress (clamped to 0..1). Nothing without an animation.</summary>
	/// <param name="progress">The progress.</param>
	internal void SetProgress(double progress)
	{
		var clampedProgress = Math.Max(0, Math.Min(1, progress));

		if (_animation != null)
		{
			Stop();
			_progress = TimeSpan.FromSeconds(_animation.Duration.TotalSeconds * clampedProgress);
			Invalidate();
		}
	}

	/// <summary>
	/// Renders the current frame into an area, scaled by <paramref name="stretch"/> and centred. Nothing without an
	/// animation.
	/// </summary>
	/// <param name="canvas">The canvas (one unit = one DIP).</param>
	/// <param name="localSize">The area.</param>
	/// <param name="stretch">How the animation is scaled into the area.</param>
	/// <param name="playbackRate">The playback speed factor.</param>
	/// <param name="clearColor">When set, the canvas is saved, cleared to this colour, and restored afterwards.</param>
	internal void Render(SKCanvas canvas, SKSize localSize, LottieStretch stretch, double playbackRate, SKColor? clearColor)
	{
		lock (_gate)
		{
			var animation = _animation;
			if (animation is null)
			{
				return;
			}

			if (_invalidationController is null)
			{
				_invalidationController = new SkiaSharp.SceneGraph.InvalidationController();
				_invalidationController.Begin();
			}

			var frameTime = GetFrameTime(playbackRate);

			var scale = BuildScale(stretch, localSize.Width, localSize.Height, animation.Size.Width, animation.Size.Height);
			//(float) rounding: the scaled size was a Windows.Foundation.Size, which stores float values
			var scaledWidth = (double)(float)(animation.Size.Width * scale.x);
			var scaledHeight = (double)(float)(animation.Size.Height * scale.y);

			var x = (localSize.Width - scaledWidth) / 2;
			var y = (localSize.Height - scaledHeight) / 2;

			animation.SeekFrameTime(frameTime, _invalidationController);

			if (clearColor is { } color)
			{
				canvas.Save();
				canvas.Clear(color);
			}

			canvas.Translate((float)x, (float)y);
			canvas.Scale((float)(scaledWidth / animation.Size.Width), (float)(scaledHeight / animation.Size.Height));

			animation.Render(canvas, new SKRect(0, 0, animation.Size.Width, animation.Size.Height));

			if (clearColor is not null)
			{
				canvas.Restore();
			}

			_invalidationController.Reset();
		}
	}

	private void Invalidate() => InvalidateRequested?.Invoke();

	private TimeSpan GetFrameTime(double playbackRate)
	{
		if (_animation is null || _timer is null || !(_playState is { } playState))
		{
			return _progress ?? TimeSpan.Zero;
		}

		var frameTime = TimeSpan.FromSeconds((_stopwatch.Elapsed + playState.GetFromProgressUsingDuration(_animation.Duration)).TotalSeconds * playbackRate);

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
				// Free the animation at the "to" progress value - at the END OF THE SEGMENT,
				// not at the overshoot of whichever tick happened to cross it. The two are
				// not the same: a tick lands when it lands, so keeping the overshoot left
				// the stopped animation resting on a frame that depended on how busy the
				// thread had been, and this frame is the one that then stays on screen.
				var segmentEnd = playState.GetToProgressUsingDuration(_animation.Duration);
				_progress = segmentEnd;
				frameTime = segmentEnd;

				Stop();
			}
		}

		return frameTime;
	}

	/// <summary>
	/// The scale of a source into a destination for a stretch: the framework's ImageSizeHelper.BuildScale
	/// (src/Platform.UI/UI/Xaml/Controls/Image/ImageSizeHelper.cs) over the same double values - keep the two in step.
	/// </summary>
	internal static (double x, double y) BuildScale(LottieStretch stretch, double destinationWidth, double destinationHeight, double sourceWidth, double sourceHeight)
	{
		if (stretch != LottieStretch.None)
		{
			var scale = (
				x: destinationWidth / sourceWidth,
				y: destinationHeight / sourceHeight
			);

			if (double.IsInfinity(scale.x))
			{
				if (double.IsInfinity(scale.y))
				{
					return (1.0d, 1.0d);
				}

				scale.x = scale.y;
			}
			else if (double.IsInfinity(scale.y))
			{
				scale.y = scale.x;
			}

			switch (stretch)
			{
				case LottieStretch.UniformToFill:
					var max = Math.Max(scale.x, scale.y);
					scale = (max, max);
					break;

				case LottieStretch.Uniform:
					var min = Math.Min(scale.x, scale.y);
					scale = (min, min);
					break;
			}

			var scaleX = double.IsNaN(scale.x) ? 1.0d : scale.x;
			var scaleY = double.IsNaN(scale.y) ? 1.0d : scale.y;

			return (scaleX, scaleY);
		}

		return (1.0d, 1.0d);
	}
}

#endif
