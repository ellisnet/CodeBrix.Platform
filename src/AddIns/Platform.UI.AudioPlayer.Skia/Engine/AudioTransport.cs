using System;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Engine;

/// <summary>
/// The audio TRANSPORT engine (WPE1 C10): load, play, pause, stop, seek (clamped to the duration, and debounced for a
/// scrubbing slider), position polling while playing, looping and volume, the natural end, and the failure text - over
/// the platform's per-player output (<see cref="IAudioPlayerPlatform"/>). Names no XAML type: the host (the AudioPlayer
/// element on CodeBrix.Platform, a CodeBrix.Mobile view) supplies the timers (<see cref="ITickSource"/>), runs work on
/// its UI thread, and mirrors the state the engine reports (<see cref="IsPlayingChanged"/>,
/// <see cref="PositionUpdated"/>, <see cref="DurationChanged"/>) into its own properties.
/// </summary>
/// <remarks>
/// Not thread-safe: a host calls every member on its UI thread; the platform's natural end arrives on the audio thread
/// and is marshalled through the host's delegate.
/// </remarks>
internal sealed class AudioTransport
{
	// A Slider drag writes the bound position on every tick of thumb travel; the seek runs
	// only after the value has been stable for this long, landing one seek per gesture.
	internal static readonly TimeSpan SeekDebounceInterval = TimeSpan.FromMilliseconds(200);

	private readonly IAudioPlayerPlatform _player;
	private readonly Func<ITickSource> _createTickSource;
	private readonly Func<Action, bool> _runOnOwnerThread;
	private ITickSource? _positionTimer;
	private ITickSource? _seekDebounceTimer;
	private TimeSpan _pendingSeek;
	private TimeSpan _positionUpdateInterval = TimeSpan.FromMilliseconds(150);
	private bool _isSourceLoaded;
	private bool _updatingFromPlayback;

	/// <summary>Creates the engine over a platform player.</summary>
	/// <param name="player">The platform's output for this player.</param>
	/// <param name="createTickSource">Creates a UI-thread timer (called lazily, once for polling and once for the seek
	/// debounce).</param>
	/// <param name="runOnOwnerThread">Queues an action on the host's UI thread (false when it cannot).</param>
	internal AudioTransport(IAudioPlayerPlatform player, Func<ITickSource> createTickSource, Func<Action, bool> runOnOwnerThread)
	{
		_player = player ?? throw new ArgumentNullException(nameof(player));
		_createTickSource = createTickSource ?? throw new ArgumentNullException(nameof(createTickSource));
		_runOnOwnerThread = runOnOwnerThread ?? throw new ArgumentNullException(nameof(runOnOwnerThread));
	}

	/// <summary>Raised with the playing state each time the transport sets it (also when unchanged).</summary>
	internal event Action<bool>? IsPlayingChanged;

	/// <summary>Raised with the playback position each time it is refreshed (<see cref="IsUpdatingFromPlayback"/> is true meanwhile).</summary>
	internal event Action<TimeSpan>? PositionUpdated;

	/// <summary>Raised with the duration when a source loads (the duration) or unloads or fails (zero).</summary>
	internal event Action<TimeSpan>? DurationChanged;

	/// <summary>Raised on the UI thread when playback reaches the natural end of the audio.</summary>
	internal event Action? PlaybackEnded;

	/// <summary>Raised when loading or playing fails: the message for the application, and the error.</summary>
	internal event Action<string, Exception>? Failed;

	/// <summary>The platform's player.</summary>
	internal IAudioPlayerPlatform Player => _player;

	/// <summary>Whether a source is loaded.</summary>
	internal bool IsSourceLoaded => _isSourceLoaded;

	/// <summary>True while the transport reports a position (a host's position property must not seek back then).</summary>
	internal bool IsUpdatingFromPlayback => _updatingFromPlayback;

	/// <summary>The volume, 0.0 to 1.0 (clamped); applied to the platform player at once.</summary>
	internal double Volume
	{
		set => _player.Volume = (float)Math.Clamp(value, 0.0, 1.0);
	}

	/// <summary>Whether playback restarts at the end; applied to the platform player at once.</summary>
	internal bool IsLooping
	{
		set => _player.IsLooping = value;
	}

	/// <summary>How often the position refreshes while playing.</summary>
	internal TimeSpan PositionUpdateInterval
	{
		get => _positionUpdateInterval;
		set
		{
			_positionUpdateInterval = value;
			if (_positionTimer is not null)
			{
				_positionTimer.Interval = value;
			}
		}
	}

	/// <summary>Starts or resumes playback of the loaded source (nothing without one).</summary>
	internal void Play()
	{
		if (!_isSourceLoaded)
		{
			return;
		}

		try
		{
			_player.Play();
		}
		catch (Exception e)
		{
			Failed?.Invoke("Playback could not be started.", e);
			return;
		}
		SetIsPlaying(true);
		StartPositionTimer();
	}

	/// <summary>Pauses playback, keeping the position.</summary>
	internal void Pause()
	{
		_player.Pause();
		SetIsPlaying(false);
		StopPositionTimer();
		RefreshPosition();
	}

	/// <summary>Stops playback and rewinds.</summary>
	internal void Stop()
	{
		_player.Stop();
		SetIsPlaying(false);
		StopPositionTimer();
		RefreshPosition();
	}

	/// <summary>Seeks at once (no debounce), clamped to the duration (nothing without a source).</summary>
	/// <param name="position">The position.</param>
	internal void Seek(TimeSpan position)
	{
		if (!_isSourceLoaded)
		{
			return;
		}

		_seekDebounceTimer?.Stop();
		_player.Seek(ClampToDuration(position));
		RefreshPosition();
	}

	/// <summary>
	/// Seeks once the requested position has been stable for <see cref="SeekDebounceInterval"/> (each request restarts
	/// the wait, so a whole slider drag lands one seek on release). Nothing without a source.
	/// </summary>
	/// <param name="position">The requested position.</param>
	internal void QueueSeek(TimeSpan position)
	{
		if (!_isSourceLoaded)
		{
			return;
		}

		_pendingSeek = position;
		if (_seekDebounceTimer is null)
		{
			_seekDebounceTimer = _createTickSource();
			_seekDebounceTimer.Interval = SeekDebounceInterval;
			_seekDebounceTimer.IsRepeating = false;
			_seekDebounceTimer.Tick += () =>
			{
				if (_isSourceLoaded)
				{
					_player.Seek(ClampToDuration(_pendingSeek));
				}
			};
		}

		// Restarting on every write coalesces a whole slider drag into one seek on release.
		_seekDebounceTimer.Stop();
		_seekDebounceTimer.Start();
	}

	/// <summary>
	/// Loads a source through <paramref name="load"/> (run with no synchronization context in scope), then applies the
	/// volume and looping, reports the duration and position, and plays when <paramref name="autoPlay"/>. A failure is
	/// reported through <see cref="Failed"/> with the platform's explanation, and leaves no source loaded.
	/// </summary>
	/// <param name="load">Loads the source into the platform player.</param>
	/// <param name="sourceDescription">The source, for the failure message.</param>
	/// <param name="volume">The volume to apply.</param>
	/// <param name="isLooping">The looping to apply.</param>
	/// <param name="autoPlay">Whether to play once loaded.</param>
	internal void Load(Action<IAudioPlayerPlatform> load, string sourceDescription, double volume, bool isLooping, bool autoPlay)
	{
		StopPositionTimer();
		SetIsPlaying(false);

		try
		{
			RunOffSynchronizationContext(() => load(_player));
		}
		catch (Exception e)
		{
			_isSourceLoaded = false;
			DurationChanged?.Invoke(TimeSpan.Zero);

			// The engine's own message for an unregistered codec names the CONTAINER ("format
			// 'ogg'"), which for an .opus file says neither what it is nor what to do; Amend adds
			// that where it applies and leaves every other failure untouched.
			Failed?.Invoke(
				AudioPlatform.Output.ExplainFailure($"The audio source '{sourceDescription}' could not be loaded.", sourceDescription),
				e);
			return;
		}

		_isSourceLoaded = true;
		_player.Volume = (float)Math.Clamp(volume, 0.0, 1.0);
		_player.IsLooping = isLooping;
		_player.PlaybackEnded -= OnPlayerPlaybackEnded;
		_player.PlaybackEnded += OnPlayerPlaybackEnded;

		DurationChanged?.Invoke(_player.Duration);
		RefreshPosition();

		if (autoPlay)
		{
			Play();
		}
	}

	/// <summary>Unloads the source: stops, rewinds, zero duration.</summary>
	internal void Unload()
	{
		StopPositionTimer();
		SetIsPlaying(false);
		_isSourceLoaded = false;
		_player.Stop();
		DurationChanged?.Invoke(TimeSpan.Zero);
		RefreshPosition();
	}

	/// <summary>Reports the current position (zero without a source) through <see cref="PositionUpdated"/>.</summary>
	internal void RefreshPosition()
	{
		_updatingFromPlayback = true;
		PositionUpdated?.Invoke(_isSourceLoaded ? _player.Position : TimeSpan.Zero);
		_updatingFromPlayback = false;
	}

	/// <summary>Clamps a position into 0..duration (no upper clamp while the duration is unknown).</summary>
	/// <param name="position">The position.</param>
	/// <returns>The clamped position.</returns>
	internal TimeSpan ClampToDuration(TimeSpan position)
	{
		var duration = _player.Duration;
		if (position < TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}
		return duration > TimeSpan.Zero && position > duration ? duration : position;
	}

	/// <summary>
	/// Runs a source load with no <see cref="SynchronizationContext"/> in scope, then waits for it.
	/// </summary>
	/// <remarks>
	/// The audio metadata layer this control loads through reads its headers asynchronously and then
	/// blocks on that read from its own synchronous entry point.
	///
	/// Loading is cheap and does not depend on file size - the player streams the file in chunks
	/// rather than reading it into memory, so even a very large WAV opens in a few milliseconds and
	/// waiting here is not perceptible.
	/// </remarks>
	private static void RunOffSynchronizationContext(Action load)
	{
		if (SynchronizationContext.Current is null)
		{
			load();
			return;
		}

		// GetAwaiter().GetResult() rethrows the original exception rather than an AggregateException,
		// so Load's catch block still sees the real load failure.
		Task.Run(load).GetAwaiter().GetResult();
	}

	private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
	{
		// The platform raises PlaybackEnded on its audio thread; everything here must run on
		// the UI thread.
		_runOnOwnerThread(() =>
		{
			SetIsPlaying(false);
			StopPositionTimer();
			RefreshPosition();
			PlaybackEnded?.Invoke();
		});
	}

	private void SetIsPlaying(bool isPlaying) => IsPlayingChanged?.Invoke(isPlaying);

	private void StartPositionTimer()
	{
		if (_positionTimer is null)
		{
			_positionTimer = _createTickSource();
			_positionTimer.Interval = _positionUpdateInterval;
			_positionTimer.Tick += RefreshPosition;
		}
		_positionTimer.Start();
	}

	private void StopPositionTimer() => _positionTimer?.Stop();
}
