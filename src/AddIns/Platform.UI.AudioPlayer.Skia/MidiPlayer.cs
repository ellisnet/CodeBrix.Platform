using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Audio.Playback;
using CodeBrix.Audio.Synth;
using CodeBrix.Audio.Synth.Sfz;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// A non-visual, XAML-declarable player for MIDI music, rendered through a SoundFont (.sf2) or an
/// SFZ (.sfz) instrument. It is the synthesized counterpart of <see cref="AudioPlayer"/> and carries
/// the same transport: point <see cref="Source"/> at a MIDI file and <see cref="Instrument"/> at the
/// instrument to render it with, then control playback with <see cref="Play"/> / <see cref="Pause"/> /
/// <see cref="Stop"/> / <see cref="Seek"/>.
///
/// The bindable properties match <see cref="AudioPlayer"/> exactly - <see cref="Position"/> /
/// <see cref="PositionSeconds"/> follow playback and seek when written (debounced for
/// scrubbing), and <see cref="Duration"/> / <see cref="DurationSeconds"/> give a Slider its
/// Maximum - so the same scrubber markup drives either player. On top of those sit the things only
/// a sequence can offer: <see cref="Speed"/> (tempo with no pitch change), the per-channel mixing
/// calls, and <see cref="MidiMessageProcessed"/> for reacting to the notes as they play.
/// </summary>
/// <remarks>
/// <para>
/// LOADING IS ASYNCHRONOUS, which is the one way this differs from <see cref="AudioPlayer"/>.
/// Instruments are large - a sampled piano is hundreds of megabytes of decoded audio and takes
/// seconds to read - so setting <see cref="Source"/> or <see cref="Instrument"/> starts a background
/// load, raises <see cref="IsLoading"/>, and returns immediately. <see cref="Duration"/> is
/// available, and the transport works, once <see cref="MediaOpened"/> has been raised;
/// <see cref="MediaFailed"/> reports a load that did not get there. Both events are raised on the
/// UI thread.
/// </para>
/// <para>
/// Instruments are cached across every <see cref="MidiPlayer"/> in the application, so two players
/// sharing an instrument pay for it once.
/// </para>
/// <para>
/// SOURCE FORMS: <see cref="Source"/> and a <c>.sf2</c> <see cref="Instrument"/> accept everything
/// <see cref="AudioPlayer.Source"/> does - a file path, an ms-appx:/// asset URI or an
/// embedded://Assembly/Resource.Name URI. A <c>.sfz</c> instrument accepts only the forms that name
/// a real file on disk (a path or an ms-appx:/// URI), because an SFZ instrument is not one file:
/// it references its samples as separate files beside it.
/// </para>
/// </remarks>
[Bindable]
public sealed partial class MidiPlayer : FrameworkElement
{
	// Instruments are immutable once loaded and safe to share, and they are the expensive thing
	// here - so they are cached for the whole application rather than per player.
	private static readonly SfzInstrumentCache _sfzInstruments = new();
	private static readonly SoundFontCache _soundFonts = new();

	// A Slider drag writes the bound position on every tick of thumb travel; the seek runs only
	// after the value has been stable for this long, landing one seek per gesture.
	private static readonly TimeSpan SeekDebounceInterval = TimeSpan.FromMilliseconds(200);

	private const string SfzNeedsFileMessage =
		"An SFZ instrument must be given as a file path or an ms-appx:/// URI. An .sfz file is not " +
		"self-contained - it references its sample files as separate files beside it - so it cannot " +
		"be loaded from an embedded resource or a stream. A .sf2 SoundFont can.";

	private readonly MidiMusicPlayer _player = new();
	private readonly SemaphoreSlim _loadGate = new(1, 1);

	private DispatcherQueueTimer? _positionTimer;
	private DispatcherQueueTimer? _seekDebounceTimer;
	private bool _updatingFromPlayback; // set while playback progress writes the position DPs
	private bool _syncingPositionPair;  // set while Position and PositionSeconds mirror each other
	private TimeSpan _pendingSeek;
	private bool _isSourceLoaded;
	private bool _loadQueued;
	private int _loadGeneration;

	public MidiPlayer()
	{
		Unloaded += (_, _) => Pause();
	}

	/// <summary>
	/// Raised (on the UI thread) when an instrument and sequence have finished loading and the
	/// transport is ready. <see cref="Duration"/> is set by the time this is raised.
	/// </summary>
	public event EventHandler? MediaOpened;

	/// <summary>
	/// Raised (on the UI thread) when the sequence reaches its end. Not raised when
	/// <see cref="IsLooping"/> is true, when <see cref="Stop"/> is called, or when playback fails.
	/// </summary>
	public event EventHandler? PlaybackEnded;

	/// <summary>
	/// Raised (on the UI thread) when a source or instrument fails to load or play - a missing
	/// file, an unreadable MIDI file, or an SFZ instrument given in a form that cannot name one.
	/// </summary>
	public event EventHandler<AudioPlayerFailedEventArgs>? MediaFailed;

	#region | Dependency properties |

	/// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
	public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
		nameof(Source), typeof(string), typeof(MidiPlayer),
		new PropertyMetadata("", (o, e) => ((MidiPlayer)o).QueueLoad()));

	/// <summary>
	/// The MIDI file to play: a file path, an ms-appx:/// asset URI, or an
	/// embedded://Assembly/Resource.Name embedded-resource URI. Playback needs an
	/// <see cref="Instrument"/> as well; loading starts once both are set. Set an empty string to
	/// unload.
	/// </summary>
	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>Identifies the <see cref="Instrument"/> dependency property.</summary>
	public static readonly DependencyProperty InstrumentProperty = DependencyProperty.Register(
		nameof(Instrument), typeof(string), typeof(MidiPlayer),
		new PropertyMetadata("", (o, e) => ((MidiPlayer)o).QueueLoad()));

	/// <summary>
	/// The instrument the sequence is rendered through: a <c>.sf2</c> SoundFont or a <c>.sfz</c>
	/// instrument. The extension decides which synthesizer runs.
	/// </summary>
	/// <remarks>
	/// A <c>.sf2</c> accepts every form <see cref="Source"/> does. A <c>.sfz</c> accepts a file
	/// path or an ms-appx:/// URI only - see the note on the class - and any other form fails with
	/// <see cref="MediaFailed"/> saying so.
	/// </remarks>
	public string Instrument
	{
		get => (string)GetValue(InstrumentProperty);
		set => SetValue(InstrumentProperty, value);
	}

	/// <summary>Identifies the <see cref="AutoPlay"/> dependency property.</summary>
	public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
		nameof(AutoPlay), typeof(bool), typeof(MidiPlayer), new PropertyMetadata(false));

	/// <summary>When true, playback starts as soon as a load completes. Defaults to false.</summary>
	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	/// <summary>Identifies the <see cref="IsLoading"/> dependency property.</summary>
	public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
		nameof(IsLoading), typeof(bool), typeof(MidiPlayer), new PropertyMetadata(false));

	/// <summary>
	/// True while an instrument and sequence are being loaded in the background (read-only). Bind a
	/// ProgressRing or a status line to it: a large sampled instrument takes seconds.
	/// </summary>
	public bool IsLoading
	{
		get => (bool)GetValue(IsLoadingProperty);
		private set => SetValue(IsLoadingProperty, value);
	}

	/// <summary>Identifies the <see cref="Position"/> dependency property.</summary>
	public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
		nameof(Position), typeof(TimeSpan), typeof(MidiPlayer),
		new PropertyMetadata(TimeSpan.Zero, (o, e) => ((MidiPlayer)o).OnPositionChanged((TimeSpan)e.NewValue)));

	/// <summary>
	/// The current playback timecode. Updated on the UI thread while playing (bind an indicator
	/// one-way to follow playback); writing it seeks the sequence, debounced for seek-on-release
	/// scrubbing. <see cref="PositionSeconds"/> is the same value in seconds.
	/// </summary>
	public TimeSpan Position
	{
		get => (TimeSpan)GetValue(PositionProperty);
		set => SetValue(PositionProperty, value);
	}

	/// <summary>Identifies the <see cref="PositionSeconds"/> dependency property.</summary>
	public static readonly DependencyProperty PositionSecondsProperty = DependencyProperty.Register(
		nameof(PositionSeconds), typeof(double), typeof(MidiPlayer),
		new PropertyMetadata(0.0, (o, e) => ((MidiPlayer)o).OnPositionSecondsChanged((double)e.NewValue)));

	/// <summary>
	/// <see cref="Position"/> expressed in seconds, so a Slider's Value can two-way bind with no
	/// converter: the slider follows playback, and dragging it seeks (on release).
	/// </summary>
	public double PositionSeconds
	{
		get => (double)GetValue(PositionSecondsProperty);
		set => SetValue(PositionSecondsProperty, value);
	}

	/// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
	public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
		nameof(Duration), typeof(TimeSpan), typeof(MidiPlayer), new PropertyMetadata(TimeSpan.Zero));

	/// <summary>
	/// The total length of the loaded sequence (read-only; <see cref="TimeSpan.Zero"/> until
	/// <see cref="MediaOpened"/> has been raised).
	/// </summary>
	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		private set => SetValue(DurationProperty, value);
	}

	/// <summary>Identifies the <see cref="DurationSeconds"/> dependency property.</summary>
	public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
		nameof(DurationSeconds), typeof(double), typeof(MidiPlayer), new PropertyMetadata(0.0));

	/// <summary>
	/// <see cref="Duration"/> expressed in seconds (read-only) - bind a Slider's Maximum to it with
	/// no converter.
	/// </summary>
	public double DurationSeconds
	{
		get => (double)GetValue(DurationSecondsProperty);
		private set => SetValue(DurationSecondsProperty, value);
	}

	/// <summary>Identifies the <see cref="IsPlaying"/> dependency property.</summary>
	public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
		nameof(IsPlaying), typeof(bool), typeof(MidiPlayer), new PropertyMetadata(false));

	/// <summary>True while the sequence is playing (read-only).</summary>
	public bool IsPlaying
	{
		get => (bool)GetValue(IsPlayingProperty);
		private set => SetValue(IsPlayingProperty, value);
	}

	/// <summary>Identifies the <see cref="Volume"/> dependency property.</summary>
	public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
		nameof(Volume), typeof(double), typeof(MidiPlayer),
		new PropertyMetadata(1.0, (o, e) => ((MidiPlayer)o)._player.Volume = (float)Math.Clamp((double)e.NewValue, 0.0, 1.0)));

	/// <summary>Playback volume from 0.0 (silent) to 1.0 (unity gain, the default).</summary>
	public double Volume
	{
		get => (double)GetValue(VolumeProperty);
		set => SetValue(VolumeProperty, value);
	}

	/// <summary>Identifies the <see cref="IsLooping"/> dependency property.</summary>
	public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(
		nameof(IsLooping), typeof(bool), typeof(MidiPlayer),
		new PropertyMetadata(false, (o, e) => ((MidiPlayer)o)._player.IsLooping = (bool)e.NewValue));

	/// <summary>
	/// When true, the sequence repeats. It repeats from its own loop point when it carries one, and
	/// from the beginning when it does not.
	/// </summary>
	public bool IsLooping
	{
		get => (bool)GetValue(IsLoopingProperty);
		set => SetValue(IsLoopingProperty, value);
	}

	/// <summary>Identifies the <see cref="Speed"/> dependency property.</summary>
	public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
		nameof(Speed), typeof(double), typeof(MidiPlayer),
		new PropertyMetadata(1.0, (o, e) => ((MidiPlayer)o).OnSpeedChanged((double)e.NewValue)));

	/// <summary>
	/// The playback speed multiplier: 1.0 is the sequence's written tempo, 0.5 half speed, 2.0
	/// double speed. Negative values are clamped to 0, which freezes the transport while sounding
	/// notes ring out.
	/// </summary>
	/// <remarks>
	/// This is a property of a sequence rather than of a recording: the tempo changes and the pitch
	/// does not, because every note is still synthesized at its written frequency.
	/// </remarks>
	public double Speed
	{
		get => (double)GetValue(SpeedProperty);
		set => SetValue(SpeedProperty, value);
	}

	/// <summary>Identifies the <see cref="ActiveVoiceCount"/> dependency property.</summary>
	public static readonly DependencyProperty ActiveVoiceCountProperty = DependencyProperty.Register(
		nameof(ActiveVoiceCount), typeof(int), typeof(MidiPlayer), new PropertyMetadata(0));

	/// <summary>
	/// The number of synthesizer voices currently sounding (read-only), refreshed with
	/// <see cref="Position"/> while playing. Useful on screen for diagnostics, and for seeing what
	/// an arrangement actually costs.
	/// </summary>
	public int ActiveVoiceCount
	{
		get => (int)GetValue(ActiveVoiceCountProperty);
		private set => SetValue(ActiveVoiceCountProperty, value);
	}

	/// <summary>Identifies the <see cref="PositionUpdateInterval"/> dependency property.</summary>
	public static readonly DependencyProperty PositionUpdateIntervalProperty = DependencyProperty.Register(
		nameof(PositionUpdateInterval), typeof(TimeSpan), typeof(MidiPlayer),
		new PropertyMetadata(TimeSpan.FromMilliseconds(150), (o, e) => ((MidiPlayer)o).OnPositionUpdateIntervalChanged((TimeSpan)e.NewValue)));

	/// <summary>
	/// How often <see cref="Position"/>, <see cref="PositionSeconds"/> and
	/// <see cref="ActiveVoiceCount"/> refresh while playing. Defaults to 150 milliseconds.
	/// </summary>
	public TimeSpan PositionUpdateInterval
	{
		get => (TimeSpan)GetValue(PositionUpdateIntervalProperty);
		set => SetValue(PositionUpdateIntervalProperty, value);
	}

	#endregion

	#region | What the loaded instrument reported |

	/// <summary>
	/// Anything the loaded instrument could not make sense of - a sample file it references that is
	/// missing, for example. Empty when the instrument loaded cleanly, which is the normal case.
	/// </summary>
	/// <remarks>
	/// An instrument loads even when this is non-empty: the regions it could not build are silent
	/// and the rest plays. Surface it when an instrument sounds wrong, rather than guessing.
	/// </remarks>
	public IReadOnlyList<string> InstrumentProblems { get; private set; } = Array.Empty<string>();

	/// <summary>
	/// Opcodes the loaded SFZ instrument uses that the synthesizer does not implement. Empty for a
	/// SoundFont, and empty for an SFZ instrument that is fully supported.
	/// </summary>
	public IReadOnlyCollection<string> UnsupportedInstrumentOpcodes { get; private set; } = Array.Empty<string>();

	#endregion

	#region | Transport |

	/// <summary>Starts or resumes playback. Does nothing until a load has completed.</summary>
	public void Play()
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
			ReportFailure("Playback could not be started.", e);
			return;
		}
		IsPlaying = true;
		StartPositionTimer();
	}

	/// <summary>Pauses playback, keeping the current position.</summary>
	public void Pause()
	{
		_player.Pause();
		IsPlaying = false;
		StopPositionTimer();
		RefreshPositionFromPlayback();
	}

	/// <summary>
	/// Stops playback and rewinds to the beginning, silencing every sounding voice and clearing
	/// the controller state the sequence had set.
	/// </summary>
	public void Stop()
	{
		_player.Stop();
		IsPlaying = false;
		StopPositionTimer();
		RefreshPositionFromPlayback();
	}

	/// <summary>
	/// Jumps playback to <paramref name="position"/> immediately (no debounce).
	/// </summary>
	/// <remarks>
	/// Controller state up to that point is replayed so the instruments sound right, but notes that
	/// were already sounding there do not resume - a seek into the middle of a held chord starts
	/// from silence.
	/// </remarks>
	public void Seek(TimeSpan position)
	{
		if (!_isSourceLoaded)
		{
			return;
		}

		_seekDebounceTimer?.Stop();
		_player.Seek(ClampToDuration(position));
		RefreshPositionFromPlayback();
	}

	#endregion

	#region | Playing alongside the sequence |

	/// <summary>
	/// An observe-only callback raised after each MIDI message reaches the synthesizer - the hook
	/// for making something outside the audio react to the music.
	/// </summary>
	/// <remarks>
	/// It runs on the real-time AUDIO THREAD, so it must be fast and allocation-free, must not
	/// touch the UI directly, and must not call back into this player. Hand what it sees to the UI
	/// thread through the element's DispatcherQueue.
	/// </remarks>
	public MidiMessageObserver? MidiMessageProcessed
	{
		get => _player.MidiMessageProcessed;
		set => _player.MidiMessageProcessed = value;
	}

	/// <summary>
	/// Sends a MIDI message to the synthesizer alongside the sequence that is playing - the general
	/// form of the three calls below. Safe from any thread; does nothing until a load completes.
	/// </summary>
	/// <param name="channel">The channel to send to, 0-15.</param>
	/// <param name="command">The command nibble: 0x80 note-off, 0x90 note-on, 0xB0 control change, 0xC0 program change, 0xE0 pitch bend.</param>
	/// <param name="data1">The first data byte, 0-127.</param>
	/// <param name="data2">The second data byte, 0-127. Ignored by commands that take one byte.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is outside 0-15.</exception>
	public void SendMidiMessage(int channel, int command, int data1, int data2)
		=> _player.SendMidiMessage(channel, command, data1, data2);

	/// <summary>
	/// Sets one channel's volume, as MIDI control change 7 - how a layered arrangement is mixed
	/// live while the rest of the sequence plays on unchanged.
	/// </summary>
	/// <param name="channel">The channel to set, 0-15.</param>
	/// <param name="volume">The volume, 0.0 (silent) to 1.0 (full). Clamped.</param>
	/// <remarks>
	/// The sequence's own volume automation still applies: a track that writes control change 7
	/// will overwrite this the next time it does so.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is outside 0-15.</exception>
	public void SetChannelVolume(int channel, double volume)
		=> _player.SetChannelVolume(channel, (float)volume);

	/// <summary>Sets one channel's stereo position, as MIDI control change 10.</summary>
	/// <param name="channel">The channel to set, 0-15.</param>
	/// <param name="pan">The position, -1.0 (full left) through 0.0 (centre) to 1.0 (full right). Clamped.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is outside 0-15.</exception>
	public void SetChannelPan(int channel, double pan)
		=> _player.SetChannelPan(channel, (float)pan);

	/// <summary>Changes the instrument one channel plays, as a MIDI program change.</summary>
	/// <param name="channel">The channel to set, 0-15.</param>
	/// <param name="program">The program (patch) number, 0-127.</param>
	/// <remarks>
	/// Which sound a program number selects is the loaded instrument's business, not this player's.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is outside 0-15, or <paramref name="program"/> is outside 0-127.</exception>
	public void SetChannelProgram(int channel, int program)
		=> _player.SetChannelProgram(channel, program);

	#endregion

	#region | Source loading |

	// Source and Instrument are almost always set together (both from markup, or both in one
	// handler), so a change queues the load rather than starting it: by the time the queued call
	// runs, both values are in place and one load covers them.
	private void QueueLoad()
	{
		// Raise IsLoading here rather than in the queued call, so that a caller which sets both
		// properties and then looks at IsLoading sees the load it just started. Only once both are
		// set: a half-configured player has nothing to load yet, and would otherwise flicker.
		if (!string.IsNullOrEmpty(Source) && !string.IsNullOrEmpty(Instrument))
		{
			IsLoading = true;
		}

		if (_loadQueued)
		{
			return;
		}

		_loadQueued = true;

		var queue = DispatcherQueue;
		if (queue is null || !queue.TryEnqueue(BeginLoad))
		{
			// Nothing to queue on (an element built outside the UI thread's dispatcher): load
			// directly, so a source set that way still plays.
			BeginLoad();
		}
	}

	private async void BeginLoad()
	{
		_loadQueued = false;

		var source = Source;
		var instrument = Instrument;

		StopPositionTimer();
		IsPlaying = false;
		_isSourceLoaded = false;

		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(instrument))
		{
			// A MIDI player needs both halves; until it has them there is nothing to load.
			UnloadSource();
			return;
		}

		var generation = Interlocked.Increment(ref _loadGeneration);
		IsLoading = true;

		LoadResult result;
		await _loadGate.WaitAsync();
		try
		{
			if (generation != Volatile.Read(ref _loadGeneration))
			{
				// Superseded while waiting for an earlier load to finish; that newer load owns the
				// outcome, including clearing IsLoading.
				return;
			}

			result = await Task.Run(() => LoadOffThread(source, instrument));
		}
		finally
		{
			_loadGate.Release();
		}

		if (generation != Volatile.Read(ref _loadGeneration))
		{
			return;
		}

		IsLoading = false;

		if (result.Error is not null)
		{
			Duration = TimeSpan.Zero;
			DurationSeconds = 0.0;
			InstrumentProblems = Array.Empty<string>();
			UnsupportedInstrumentOpcodes = Array.Empty<string>();
			ReportFailure($"The MIDI source '{source}' could not be loaded with the instrument '{instrument}'.", result.Error);
			return;
		}

		_isSourceLoaded = true;
		_player.Volume = (float)Math.Clamp(Volume, 0.0, 1.0);
		_player.IsLooping = IsLooping;
		_player.PlaybackEnded -= OnPlayerPlaybackEnded;
		_player.PlaybackEnded += OnPlayerPlaybackEnded;

		Duration = result.Duration;
		DurationSeconds = result.Duration.TotalSeconds;
		InstrumentProblems = result.Problems;
		UnsupportedInstrumentOpcodes = result.UnsupportedOpcodes;
		RefreshPositionFromPlayback();

		MediaOpened?.Invoke(this, EventArgs.Empty);

		if (AutoPlay)
		{
			Play();
		}
	}

	// Runs on a thread-pool thread: reading an instrument is seconds of work and hundreds of
	// megabytes of decoding, which must never happen on the UI thread.
	private LoadResult LoadOffThread(string source, string instrument)
	{
		try
		{
			var sequence = OpenSequence(source);

			if (IsSfz(instrument))
			{
				var instrumentPath = AudioSourceResolver.ResolveFilePathOrNull(instrument)
					?? throw new NotSupportedException(SfzNeedsFileMessage);

				var sfz = _sfzInstruments.Get(instrumentPath);
				_player.Load(sfz, sequence);
				return new LoadResult(_player.Duration, sfz.Problems, sfz.UnsupportedOpcodes, null);
			}

			var soundFont = OpenSoundFont(instrument);
			_player.Load(soundFont, sequence);
			return new LoadResult(_player.Duration, Array.Empty<string>(), Array.Empty<string>(), null);
		}
		catch (Exception e)
		{
			return new LoadResult(TimeSpan.Zero, Array.Empty<string>(), Array.Empty<string>(), e);
		}
	}

	private static MidiSequence OpenSequence(string source)
	{
		var (filePath, stream) = AudioSourceResolver.Resolve(source);
		if (filePath is not null)
		{
			return new MidiSequence(filePath);
		}

		// The sequence is parsed in full by the constructor, so the stream is finished with here.
		using (stream)
		{
			return new MidiSequence(stream!);
		}
	}

	private static SoundFont OpenSoundFont(string instrument)
	{
		var (filePath, stream) = AudioSourceResolver.Resolve(instrument);
		if (filePath is not null)
		{
			// Cached by path: a SoundFont shared by two players is read once.
			return _soundFonts.Get(filePath);
		}

		using (stream)
		{
			return new SoundFont(stream!);
		}
	}

	// The instrument's extension decides the synthesizer, matching CodeBrix.Audio's own rule.
	private static bool IsSfz(string instrument)
		=> Path.GetExtension(instrument).Equals(".sfz", StringComparison.OrdinalIgnoreCase);

	private void UnloadSource()
	{
		StopPositionTimer();
		IsPlaying = false;
		IsLoading = false;
		_isSourceLoaded = false;
		_player.Stop();
		Duration = TimeSpan.Zero;
		DurationSeconds = 0.0;
		ActiveVoiceCount = 0;
		InstrumentProblems = Array.Empty<string>();
		UnsupportedInstrumentOpcodes = Array.Empty<string>();
		RefreshPositionFromPlayback();
	}

	private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
	{
		// Raised off the UI thread (the load that captured the context ran on the thread pool), so
		// everything here has to be marshalled.
		DispatcherQueue.TryEnqueue(() =>
		{
			IsPlaying = false;
			StopPositionTimer();
			RefreshPositionFromPlayback();
			PlaybackEnded?.Invoke(this, EventArgs.Empty);
		});
	}

	private void ReportFailure(string message, Exception error)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error(message, error);
		}
		MediaFailed?.Invoke(this, new AudioPlayerFailedEventArgs(message, error));
	}

	private readonly record struct LoadResult(
		TimeSpan Duration,
		IReadOnlyList<string> Problems,
		IReadOnlyCollection<string> UnsupportedOpcodes,
		Exception? Error);

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

		if (!_updatingFromPlayback)
		{
			QueueSeek(newPosition);
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

		if (!_updatingFromPlayback)
		{
			QueueSeek(TimeSpan.FromSeconds(newSeconds));
		}
	}

	private void QueueSeek(TimeSpan position)
	{
		if (!_isSourceLoaded)
		{
			return;
		}

		_pendingSeek = position;
		if (_seekDebounceTimer is null)
		{
			_seekDebounceTimer = DispatcherQueue.CreateTimer();
			_seekDebounceTimer.Interval = SeekDebounceInterval;
			_seekDebounceTimer.IsRepeating = false;
			_seekDebounceTimer.Tick += (_, _) =>
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

	private void OnSpeedChanged(double newSpeed)
		=> _player.Speed = (float)Math.Max(0.0, newSpeed);

	private void StartPositionTimer()
	{
		if (_positionTimer is null)
		{
			_positionTimer = DispatcherQueue.CreateTimer();
			_positionTimer.Interval = PositionUpdateInterval;
			_positionTimer.Tick += (_, _) => RefreshPositionFromPlayback();
		}
		_positionTimer.Start();
	}

	private void StopPositionTimer() => _positionTimer?.Stop();

	private void OnPositionUpdateIntervalChanged(TimeSpan newInterval)
	{
		if (_positionTimer is not null)
		{
			_positionTimer.Interval = newInterval;
		}
	}

	private void RefreshPositionFromPlayback()
	{
		_updatingFromPlayback = true;
		Position = _isSourceLoaded ? _player.Position : TimeSpan.Zero;
		_updatingFromPlayback = false;
		ActiveVoiceCount = _isSourceLoaded ? _player.ActiveVoiceCount : 0;
	}

	private TimeSpan ClampToDuration(TimeSpan position)
	{
		var duration = _player.Duration;
		if (position < TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}
		return duration > TimeSpan.Zero && position > duration ? duration : position;
	}

	#endregion
}
