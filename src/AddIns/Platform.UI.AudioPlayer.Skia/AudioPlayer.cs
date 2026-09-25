using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Engine;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// A non-visual, XAML-declarable audio player for WAV, MP3, Ogg Vorbis and FLAC files. Declare it
/// on a page (it renders nothing and takes no space), point <see cref="Source"/> at a file path, an
/// ms-appx:/// asset URI, an embedded://Assembly/Resource.Name embedded resource, or load a
/// stream with <see cref="SetSourceStream"/>, and control playback with <see cref="Play"/> /
/// <see cref="Pause"/> / <see cref="Stop"/> / <see cref="Seek"/>.
///
/// Formats beyond the four built in - Opus, say - play here as soon as an add-on codec package is
/// registered with CodeBrix.Audio by the application. This class needs no change and takes no
/// dependency on one. For MIDI music rendered through a SoundFont or SFZ instrument, see
/// <see cref="MidiPlayer"/>.
///
/// While playing, <see cref="Position"/> and <see cref="PositionSeconds"/> update on the UI
/// thread every <see cref="PositionUpdateInterval"/>, so an indicator can one-way bind to them.
/// Both are also two-way bindable: writing them (for example from a Slider the user drags)
/// seeks the audio, debounced so that releasing the slider lands a single seek ("seek on
/// release"). <see cref="Duration"/> / <see cref="DurationSeconds"/> are available as soon as
/// a source is loaded, so a Slider's Maximum can bind with no converter.
/// </summary>
//WPE1 C10: the transport (load, play/pause/stop, clamped and debounced seeking, position polling, looping, volume,
//the natural end, the failure text) moved into the WinUI-free engine (Engine/AudioTransport, over the platform's
//IAudioPlayerPlatform and Engine/ITickSource timers); this element wraps it and keeps the XAML half (the dependency
//properties, the public events, the dispatcher, logging).
[Bindable]
public sealed partial class AudioPlayer : FrameworkElement
{
	private readonly IAudioPlayerPlatform _player;
	private readonly AudioTransport _transport;
	private bool _syncingPositionPair;  // set while Position and PositionSeconds mirror each other

	/// <summary>
	/// Initializes a new audio player control.
	/// </summary>
	/// <remarks>
	/// Playback is paused when the control leaves the visual tree, so a page that is navigated
	/// away from does not keep playing.
	/// </remarks>
	public AudioPlayer()
	{
		// The audio output is the platform's (decode and device playback); this element keeps the control API and
		// the playback state.
		_player = PlatformContract.Create<IAudioPlayerPlatform>(this);
		_transport = new AudioTransport(
			_player,
			() => new DispatcherQueueTickSource(DispatcherQueue.CreateTimer()),
			action => DispatcherQueue.TryEnqueue(() => action()));
		_transport.IsPlayingChanged += isPlaying => IsPlaying = isPlaying;
		_transport.PositionUpdated += position => Position = position;
		_transport.DurationChanged += duration =>
		{
			Duration = duration;
			DurationSeconds = duration.TotalSeconds;
		};
		_transport.PlaybackEnded += () => PlaybackEnded?.Invoke(this, EventArgs.Empty);
		_transport.Failed += ReportFailure;
		Unloaded += (_, _) => Pause();
	}

	/// <summary>
	/// Raised (on the UI thread) when playback reaches the natural end of the audio file.
	/// Not raised when <see cref="IsLooping"/> is true, when <see cref="Stop"/> is called, or
	/// when playback fails.
	/// </summary>
	public event EventHandler? PlaybackEnded;

	/// <summary>
	/// Raised (on the UI thread) when a source fails to load or play - for example a missing
	/// file, an unsupported format, or an unreadable stream.
	/// </summary>
	public event EventHandler<AudioPlayerFailedEventArgs>? MediaFailed;

	#region | Dependency properties |

	/// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
	public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
		nameof(Source), typeof(string), typeof(AudioPlayer),
		new PropertyMetadata("", (o, e) => ((AudioPlayer)o).OnSourceChanged((string)e.NewValue)));

	/// <summary>
	/// The audio source: a WAV, MP3, Ogg Vorbis or FLAC file path, an ms-appx:/// asset URI, or an
	/// embedded://Assembly/Resource.Name embedded-resource URI. Setting it loads the file
	/// (making <see cref="Duration"/> available immediately) and, when <see cref="AutoPlay"/>
	/// is true, starts playback. Set an empty string to unload.
	/// </summary>
	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>Identifies the <see cref="AutoPlay"/> dependency property.</summary>
	public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
		nameof(AutoPlay), typeof(bool), typeof(AudioPlayer), new PropertyMetadata(false));

	/// <summary>When true, playback starts as soon as a source is loaded. Defaults to false.</summary>
	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	/// <summary>Identifies the <see cref="Position"/> dependency property.</summary>
	public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
		nameof(Position), typeof(TimeSpan), typeof(AudioPlayer),
		new PropertyMetadata(TimeSpan.Zero, (o, e) => ((AudioPlayer)o).OnPositionChanged((TimeSpan)e.NewValue)));

	/// <summary>
	/// The current playback timecode. Updated on the UI thread while playing (bind an indicator
	/// one-way to follow playback); writing it seeks the audio, debounced for seek-on-release
	/// scrubbing. <see cref="PositionSeconds"/> is the same value in seconds.
	/// </summary>
	public TimeSpan Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
		set => SetValue(PositionProperty, value);
	}

	/// <summary>Identifies the <see cref="PositionSeconds"/> dependency property.</summary>
	public static readonly DependencyProperty PositionSecondsProperty = DependencyProperty.Register(
		nameof(PositionSeconds), typeof(double), typeof(AudioPlayer),
		new PropertyMetadata(0.0, (o, e) => ((AudioPlayer)o).OnPositionSecondsChanged((double)e.NewValue)));

	/// <summary>
	/// <see cref="Position"/> expressed in seconds, so a Slider's Value can two-way bind with
	/// no converter: the slider follows playback, and dragging it seeks (on release).
	/// </summary>
	public double PositionSeconds
	{
		get => (double)GetValue(PositionSecondsProperty);
		set => SetValue(PositionSecondsProperty, value);
	}

	/// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
	public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
		nameof(Duration), typeof(TimeSpan), typeof(AudioPlayer), new PropertyMetadata(TimeSpan.Zero));

	/// <summary>
	/// The total duration of the loaded audio file (read-only; <see cref="TimeSpan.Zero"/>
	/// while no source is loaded).
	/// </summary>
	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		private set => SetValue(DurationProperty, value);
	}

	/// <summary>Identifies the <see cref="DurationSeconds"/> dependency property.</summary>
	public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
		nameof(DurationSeconds), typeof(double), typeof(AudioPlayer), new PropertyMetadata(0.0));

	/// <summary>
	/// <see cref="Duration"/> expressed in seconds (read-only) - bind a Slider's Maximum to it
	/// with no converter.
	/// </summary>
	public double DurationSeconds
	{
		get => (double)GetValue(DurationSecondsProperty);
		private set => SetValue(DurationSecondsProperty, value);
	}

	/// <summary>Identifies the <see cref="IsPlaying"/> dependency property.</summary>
	public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
		nameof(IsPlaying), typeof(bool), typeof(AudioPlayer), new PropertyMetadata(false));

	/// <summary>True while audio is playing (read-only).</summary>
	public bool IsPlaying
	{
		get => (bool)GetValue(IsPlayingProperty);
		private set => SetValue(IsPlayingProperty, value);
	}

	/// <summary>Identifies the <see cref="Volume"/> dependency property.</summary>
	public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
		nameof(Volume), typeof(double), typeof(AudioPlayer),
		new PropertyMetadata(1.0, (o, e) => ((AudioPlayer)o)._transport.Volume = (double)e.NewValue));

	/// <summary>Playback volume from 0.0 (silent) to 1.0 (unity gain, the default).</summary>
	public double Volume
	{
		get => (double)GetValue(VolumeProperty);
		set => SetValue(VolumeProperty, value);
	}

	/// <summary>Identifies the <see cref="IsLooping"/> dependency property.</summary>
	public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(
		nameof(IsLooping), typeof(bool), typeof(AudioPlayer),
		new PropertyMetadata(false, (o, e) => ((AudioPlayer)o)._transport.IsLooping = (bool)e.NewValue));

	/// <summary>When true, playback restarts from the beginning at the end of the file.</summary>
	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	/// <summary>Identifies the <see cref="PositionUpdateInterval"/> dependency property.</summary>
	public static readonly DependencyProperty PositionUpdateIntervalProperty = DependencyProperty.Register(
		nameof(PositionUpdateInterval), typeof(TimeSpan), typeof(AudioPlayer),
		new PropertyMetadata(TimeSpan.FromMilliseconds(150), (o, e) => ((AudioPlayer)o).OnPositionUpdateIntervalChanged((TimeSpan)e.NewValue)));

	/// <summary>
	/// How often <see cref="Position"/> / <see cref="PositionSeconds"/> refresh while playing.
	/// Defaults to 150 milliseconds.
	/// </summary>
	public TimeSpan PositionUpdateInterval
	{
		get => (TimeSpan)GetValue(PositionUpdateIntervalProperty);
		set => SetValue(PositionUpdateIntervalProperty, value);
	}

	#endregion

	#region | Transport |

	/// <summary>Starts or resumes playback of the loaded source.</summary>
	public void Play() => _transport.Play();

	/// <summary>Pauses playback, keeping the current position.</summary>
	public void Pause() => _transport.Pause();

	/// <summary>Stops playback and rewinds to the beginning.</summary>
	public void Stop() => _transport.Stop();

	/// <summary>
	/// Jumps playback to <paramref name="position"/> immediately (no debounce); playback
	/// continues from there when playing, or the position is remembered when paused.
	/// </summary>
	public void Seek(TimeSpan position) => _transport.Seek(position);

	/// <summary>
	/// Loads a source from a stream, in any format this player reads (for sources that are neither
	/// files nor embedded resources). The stream should be seekable; the player takes ownership
	/// and disposes it when another source is loaded. Clears <see cref="Source"/>.
	/// </summary>
	public void SetSourceStream(Stream stream)
	{
		if (stream is null)
		{
			throw new ArgumentNullException(nameof(stream));
		}

		Source = "";
		LoadCore(player => player.Load(stream), "stream");
	}

	#endregion

	#region | Source loading |

	private void OnSourceChanged(string newSource)
	{
		if (string.IsNullOrEmpty(newSource))
		{
			UnloadSource();
			return;
		}

		LoadCore(
			player =>
			{
				var (filePath, stream) = AudioSourceResolver.Resolve(newSource);
				if (filePath is not null)
				{
					player.Load(filePath);
				}
				else
				{
					player.Load(stream!);
				}
			},
			newSource);
	}

	//The transport loads (off the synchronization context), applies the volume and looping, and plays when AutoPlay.
	private void LoadCore(Action<IAudioPlayerPlatform> load, string sourceDescription) =>
		_transport.Load(load, sourceDescription, Volume, IsLooping, AutoPlay);

	private void UnloadSource() => _transport.Unload();

	private void ReportFailure(string message, Exception error)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error(message, error);
		}
		MediaFailed?.Invoke(this, new AudioPlayerFailedEventArgs(message, error));
	}

	#endregion

	#region | Position updates and debounced seeking |

	private void OnPositionChanged(TimeSpan newPosition)
	{
		if (_syncingPositionPair)
		{
			return;
		}

		_syncingPositionPair = true;
		PositionSeconds = newPosition.TotalSeconds;
		_syncingPositionPair = false;

		if (!_transport.IsUpdatingFromPlayback)
		{
			_transport.QueueSeek(newPosition);
		}
	}

	private void OnPositionSecondsChanged(double newSeconds)
	{
		if (_syncingPositionPair)
		{
			return;
		}

		_syncingPositionPair = true;
		Position = TimeSpan.FromSeconds(newSeconds);
		_syncingPositionPair = false;

		if (!_transport.IsUpdatingFromPlayback)
		{
			_transport.QueueSeek(TimeSpan.FromSeconds(newSeconds));
		}
	}

	private void OnPositionUpdateIntervalChanged(TimeSpan newInterval) => _transport.PositionUpdateInterval = newInterval;

	#endregion
}

/// <summary>
/// Event args for <see cref="AudioPlayer.MediaFailed"/>.
/// </summary>
public sealed class AudioPlayerFailedEventArgs : EventArgs
{
	internal AudioPlayerFailedEventArgs(string message, Exception error)
	{
		Message = message;
		Error = error;
	}

	/// <summary>A description of what failed.</summary>
	public string Message { get; }

	/// <summary>The underlying exception.</summary>
	public Exception Error { get; }
}
