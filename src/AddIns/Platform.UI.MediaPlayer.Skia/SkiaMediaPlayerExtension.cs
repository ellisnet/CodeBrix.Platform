using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using CodeBrix.Platform.MediaPlayerCore;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.Media.Playback;
using CodeBrix.Platform.UI.Dispatching;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;
using Windows.Web.Http.Headers;
using CodeBrix.Platform.Extensions.Logging;
using System.Timers;

namespace CodeBrix.Platform.UI.MediaPlayer.Skia; //Adapted from the (legacy) Platform.UI.MediaPlayer.Skia.X11 SharedMediaPlayerExtension; was originally: Uno.UI.MediaPlayer.Skia.X11

/// <summary>
/// The IMediaPlayerExtension implementation for all CodeBrix.Platform Skia heads except macOS.
/// Playback is driven by LibVLC via CodeBrix.Platform.MediaPlayerCore; decoded video frames are
/// delivered through a <see cref="VideoFrameSink"/> (libvlc's windowing-system-agnostic memory
/// output), so this engine works identically on Win32, Skia-on-WPF, X11, Wayland, and FrameBuffer
/// hosts — no native child windows, and no XWayland.
/// </summary>
public class SkiaMediaPlayerExtension : IMediaPlayerExtension
{
	private static int _vlcInitialized;
	private static LibVLC _vlc = null!;

	private const string MsAppXScheme = "ms-appx";
	private static readonly ConditionalWeakTable<Windows.Media.Playback.MediaPlayer, SkiaMediaPlayerExtension> _mediaPlayerToExtension = new();

	private readonly IDisposable _timerDisposable;
	private int _playlistIndex = -1; // -1 if no playlist or empty playlist, otherwise the 0-based index of the current track in the playlist
	private MediaPlaybackList? _playlist; // only set and used if the current _player.Source is a playlist

	private int _vlcPlayerVolume = 100;

	// the current effective url (e.g. current video in playlist) that is set natively
	// DO NOT READ OR WRITE THIS. It's only used to RaiseSourceChanged.
	private Uri? _uri;

	private Uri? Uri
	{
		set
		{
			if (_uri != value)
			{
				IsVideo = null;
				_uri = value;
				Events?.RaiseSourceChanged();
				// We don't return here since setting the uri to itself should reload (e.g. looping playlist of a single element)
			}

			VlcPlayer.Media?.Dispose();

			if (value is null)
			{
				VlcPlayer.Media = null;
				return;
			}

			Player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

			var uri = value;
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + value.OriginalString.TrimStart('/'));
			}

			if (uri.IsLocalResource())
			{
				var filePath = uri.PathAndQuery;

				if (uri.Host is { Length: > 0 } host)
				{
					filePath = host + "/" + filePath.TrimStart('/');
				}

				VlcPlayer.Media = new CodeBrix.Platform.MediaPlayerCore.Media(_vlc, new Uri(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, filePath.TrimStart('/'))));
			}
			else if (uri.IsAppData())
			{
				VlcPlayer.Media = new CodeBrix.Platform.MediaPlayerCore.Media(_vlc, new Uri(AppDataUriEvaluator.ToPath(uri)));
			}
			else if (uri.IsFile)
			{
				VlcPlayer.Media = new CodeBrix.Platform.MediaPlayerCore.Media(_vlc, uri);
			}
			else
			{
				VlcPlayer.Media = new CodeBrix.Platform.MediaPlayerCore.Media(_vlc, uri);
			}

			if (VlcPlayer.Media is { } m)
			{
				var weakRef = new WeakReference<SkiaMediaPlayerExtension>(this);
				m.ParsedChanged += (_, a) => weakRef.GetTarget()?.OnLoadedMetadata(a.ParsedStatus);
			}

			// This doesn't start the playback. It just force-loads the media. This is the behaviour only when --start-paused
			VlcPlayer.Play();
		}
	}

	private bool? _isVideo;
	private bool _updatingPositionFromNative;
	internal event EventHandler<bool?>? IsVideoChanged;

	/// <summary>
	/// Whether the currently loaded media has a video track (null until known).
	/// </summary>
	public bool? IsVideo
	{
		get => _isVideo;
		private set
		{
			if (_isVideo != value)
			{
				IsVideoChanged?.Invoke(this, value);
			}
			_isVideo = value;
		}
	}

	internal Windows.Media.Playback.MediaPlayer Player { get; }

	internal CodeBrix.Platform.MediaPlayerCore.MediaPlayer VlcPlayer { get; }

	/// <summary>
	/// The memory frame sink that delivers this player's decoded video frames. Attached in the
	/// constructor, before any playback, as VideoFrameSink requires.
	/// </summary>
	internal VideoFrameSink FrameSink { get; }

	/// <summary>
	/// Optionally call this early in app startup (e.g. before the first MediaPlayerElement is
	/// used) to warm up the LibVLC runtime on a background thread, reducing first-playback latency.
	/// </summary>
	public static void PreloadVlc()
	{
		Task.Run(() =>
		{
			if (Volatile.Read(ref _vlcInitialized) == 0)
			{
				var vlc = CreateLibVlc();
				try
				{
					var mediaPlayer = new CodeBrix.Platform.MediaPlayerCore.MediaPlayer(vlc);
					var stream = typeof(SkiaMediaPlayerExtension).Assembly.GetManifestResourceStream($"{typeof(SkiaMediaPlayerExtension).Assembly.GetName().Name}.Assets.libvlc_init_sample.mp4");
					var media = new CodeBrix.Platform.MediaPlayerCore.Media(vlc, new StreamMediaInput(stream!));
					EventHandler<MediaParsedChangedEventArgs>? mediaOnParsedChanged = default;
					mediaOnParsedChanged = (_, a) =>
					{
						if (Interlocked.CompareExchange(ref _vlcInitialized, 1, 0) == 0)
						{
							_vlc = vlc;
						}
						else
						{
							vlc.Dispose();
						}
						media.ParsedChanged -= mediaOnParsedChanged;
					};
					media.ParsedChanged += mediaOnParsedChanged;
					mediaPlayer.Media = media;
					mediaPlayer.Play();
				}
				catch (Exception e)
				{
					typeof(SkiaMediaPlayerExtension).Log().Error(e.Message);
					_vlc = vlc;
				}
			}
		});
	}

	private static LibVLC CreateLibVlc()
	{
		try
		{
			return new LibVLC("--start-paused");
		}
		catch (VLCException e)
		{
			var hint = OperatingSystem.IsLinux()
				? "The native libvlc runtime was not found. Install it via the system package manager: sudo apt install libvlc5 vlc-plugin-base (Debian/Ubuntu)."
				: "The native libvlc runtime was not found. Add the VideoLAN.LibVLC.Windows package to your Windows head project(s).";
			throw new PlatformNotSupportedException(hint, e);
		}
	}

	/// <summary>
	/// Creates the playback engine for the given <see cref="Windows.Media.Playback.MediaPlayer"/>.
	/// Instantiated by the framework through the ApiExtension registration; not intended to be
	/// constructed directly by app code.
	/// </summary>
	public SkiaMediaPlayerExtension(Windows.Media.Playback.MediaPlayer player)
	{
		if (Interlocked.CompareExchange(ref _vlcInitialized, 1, 0) == 0)
		{
			_vlc = CreateLibVlc();
		}

		VlcPlayer = new CodeBrix.Platform.MediaPlayerCore.MediaPlayer(_vlc) { EnableMouseInput = false, EnableKeyInput = false };
		// The frame sink must be attached before any playback starts; it permanently switches
		// this VLC player to memory ("vmem") rendering. Audio-only media is unaffected (no
		// video track means the sink's callbacks simply never fire).
		FrameSink = new VideoFrameSink(VlcPlayer);
		Player = player;
		_mediaPlayerToExtension.TryAdd(player, this);

		// It's important not to let libVLC's media player grab a strong reference to this object,
		// otherwise neither will ever get collected. It seems like libVLC's media player is never
		// collected until explicitly disposed. The lifetime of this extension is similar
		// to that of its owning MediaPlayer, which is part of the public API and has an indefinite
		// lifetime. We rely on the GC to determine when it's time to end this object's lifetime,
		// and in turn, dispose of libVLC's media player handle as well.
		var weakRef = new WeakReference<SkiaMediaPlayerExtension>(this);

		VlcPlayer.LengthChanged += (o, a) => weakRef.GetTarget()?.OnLengthChange(o, a);
		VlcPlayer.EndReached += (o, a) => weakRef.GetTarget()?.OnEndReached(o, a);
		VlcPlayer.EncounteredError += (o, a) => weakRef.GetTarget()?.OnEncounteredError(o, a);
		VlcPlayer.Playing += (o, a) => weakRef.GetTarget()?.OnPlaying(o, a);
		VlcPlayer.Buffering += (o, a) => weakRef.GetTarget()?.OnBuffering(o, a);
		VlcPlayer.Paused += (o, a) => weakRef.GetTarget()?.OnPaused(o, a);
		VlcPlayer.TimeChanged += (o, a) => weakRef.GetTarget()?.OnTimeChanged(o, a); // PositionChanged fires way too frequently (probably every frame). We use TimeChanged instead.

		// We need to start a timer to update the playback state, since libVLC doesn't
		// provide a way to get the end of buffering without polling.
		// Also, attaching to VolumeChanged with debugger attached causes crashes on some systems,
		// so we poll the volume on the timer as well.
		var timer = new System.Timers.Timer(16);
		ElapsedEventHandler timerOnTick = (_, _) => weakRef.GetTarget()?.OnTick();
		timer.Elapsed += timerOnTick;
		_timerDisposable = timer;
		timer.Start();
	}

	~SkiaMediaPlayerExtension()
	{
		Dispose();
	}

	internal static SkiaMediaPlayerExtension? GetByMediaPlayer(Windows.Media.Playback.MediaPlayer player) => _mediaPlayerToExtension.TryGetValue(player, out var ext) ? ext : null;

	/// <inheritdoc />
	public IMediaPlayerEventsExtension? Events { get; set; }

	/// <inheritdoc />
	public double PlaybackRate
	{
		get => VlcPlayer.Rate;
		set => VlcPlayer.SetRate((float)value);
	}

	/// <inheritdoc />
	public bool IsLoopingEnabled { get; set; }

	/// <inheritdoc />
	public bool IsLoopingAllEnabled { get; set; }

	// Deprecated.
	/// <inheritdoc />
	public MediaPlayerState CurrentState => MediaPlayerState.Closed;

	/// <inheritdoc />
	public TimeSpan NaturalDuration => VlcPlayer.Media?.Duration is { } d ? TimeSpan.FromMilliseconds(d) : TimeSpan.Zero;

	/// <inheritdoc />
	public bool IsProtected => false;

	/// <inheritdoc />
	public double BufferingProgress => 0.0;

	/// <inheritdoc />
	public bool CanPause => VlcPlayer.CanPause;

	/// <inheritdoc />
	public bool CanSeek => VlcPlayer.IsSeekable;

	/// <inheritdoc />
	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	/// <inheritdoc />
	public MediaPlayerAudioCategory AudioCategory { get; set; }

	/// <inheritdoc />
	public TimeSpan TimelineControllerPositionOffset
	{
		get => Position;
		set => Position = value;
	}

	/// <inheritdoc />
	public bool RealTimePlayback { get; set; }

	// TODO
	/// <inheritdoc />
	public double AudioBalance { get; set; }

	/// <inheritdoc />
	public TimeSpan Position
	{
		get => TimeSpan.FromMilliseconds(Math.Max(0, VlcPlayer.Position * NaturalDuration.TotalMilliseconds));
		set
		{
			if (!_updatingPositionFromNative && NaturalDuration.TotalMilliseconds > 0)
			{
				VlcPlayer.Position = (float)(value.TotalMilliseconds / (float)NaturalDuration.TotalMilliseconds);
			}
		}
	}

	// not applicable, we use the managed MTC
	/// <inheritdoc />
	public void SetTransportControlsBounds(Rect bounds) { }

	/// <inheritdoc />
	public void Initialize() { }

	/// <inheritdoc />
	public void InitializeSource()
	{
		_playlistIndex = -1;
		_playlist = null;
		switch (Player.Source)
		{
			case MediaPlaybackItem item:
				Uri = item.Source.Uri;
				break;
			case MediaSource source:
				Uri = source.Uri;
				break;
			case MediaPlaybackList playlist:
				_playlist = playlist;
				_playlistIndex = _playlist.Items.Count > 0 ? 0 : -1;
				Uri = _playlist.Items.FirstOrDefault()?.Source.Uri;
				break;
			default:
				Uri = null;
				break;
		}
	}

	// Deprecated. Use MediaPlayer.Source instead
	/// <inheritdoc />
	public void SetUriSource(Uri uri) => throw new NotImplementedException();
	/// <inheritdoc />
	public void SetFileSource(IStorageFile file) => throw new NotImplementedException();
	/// <inheritdoc />
	public void SetStreamSource(IRandomAccessStream stream) => throw new NotImplementedException();
	/// <inheritdoc />
	public void SetMediaSource(IMediaSource source) => throw new NotImplementedException();

	/// <inheritdoc />
	public void StepForwardOneFrame() => VlcPlayer.NextFrame();
	// VLC only supports forward frame stepping.
	/// <inheritdoc />
	public void StepBackwardOneFrame() => throw new NotImplementedException();

	/// <inheritdoc />
	public void SetSurfaceSize(Size size) => throw new NotImplementedException();

	/// <inheritdoc />
	public void Play() => VlcPlayer.Play();

	/// <inheritdoc />
	public void Pause() => VlcPlayer.Pause();

	/// <inheritdoc />
	public void Stop()
	{
		if (OperatingSystem.IsWindows())
		{
			// On Win32, Stop() deadlocks for some reason. The best guess is that Stop does something like SendMessage
			// and needs the window message queue to continue pumping before returning, so calling it on the UI
			// thread (which also pumps the queue) deadlocks. This is not a problem on X11/Wayland/FrameBuffer
			// because those hosts run their event queues on a separate thread.
			Task.Run(() =>
			{
				VlcPlayer.Stop();
			});
		}
		else
		{
			VlcPlayer.Stop();
		}
	}

	/// <inheritdoc />
	public void ToggleMute() => VlcPlayer.Mute = Player.IsMuted;

	/// <inheritdoc />
	public void OnVolumeChanged()
	{
		var volume = (int)Math.Round(Player.Volume * 100);
		if (volume != _vlcPlayerVolume)
		{
			_vlcPlayerVolume = volume;
			VlcPlayer.Volume = volume;
		}
	}

	/// <inheritdoc />
	public void OnOptionChanged(string name, object value) { }

	/// <inheritdoc />
	public void PreviousTrack()
	{
		if (_playlist != null && _playlistIndex > 0)
		{
			Uri = _playlist.Items[--_playlistIndex].Source.Uri;
			Play();
		}
	}

	/// <inheritdoc />
	public void NextTrack()
	{
		if (_playlist != null && _playlist.Items.Count > 0 && _playlistIndex + 1 < _playlist.Items.Count)
		{
			Uri = _playlist.Items[++_playlistIndex].Source.Uri;
			Play();
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		try
		{
			_timerDisposable?.Dispose();
			_mediaPlayerToExtension.Remove(Player);
			VlcPlayer.Dispose();
			// The sink's buffers must outlive the VLC player (libvlc's cleanup callback fires
			// during the player's disposal), so the sink is disposed last.
			FrameSink.Dispose();
		}
		catch (Exception)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
			{
				this.Log().Warn("Unable to dispose MediaPlayerExtension");
			}
		}
	}

	private void OnPlaying(object? _, EventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing);

	private void OnLoadedMetadata(MediaParsedStatus status)
	{
		if (status == MediaParsedStatus.Done)
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Events?.RaiseNaturalVideoDimensionChanged();
				Events?.NaturalDurationChanged();
				Events?.RaiseMediaOpened();
				IsVideo = VlcPlayer.Media?.Tracks.Any(track => track.TrackType is TrackType.Video);
				VlcPlayer.Time = 1; // this shows the first frame of the video after loading instead of a black frame
			});
		}
	}

	private void OnTimeChanged(object? _, MediaPlayerTimeChangedEventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			var oldValue = _updatingPositionFromNative;
			_updatingPositionFromNative = true; // RaisePositionChanged will set Position, so we need a way to flag this so we can ignore it
			Events?.RaisePositionChanged();
			_updatingPositionFromNative = oldValue;
		});
	}

	private void OnBuffering(object? _, MediaPlayerBufferingEventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering);

	private void OnLengthChange(object? _, MediaPlayerLengthChangedEventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() =>
		{
			if (Player.PlaybackSession.NaturalDuration != NaturalDuration)
			{
				Events?.NaturalDurationChanged();
			}
		});

	private void OnEndReached(object? _, EventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (VlcPlayer.Media is { Mrl: { } url } media)
			{
				// without recreating the media object, any attempt at
				// rewinding and replaying the video fails.
				// cf. https://github.com/unoplatform/uno-private/issues/1230
				VlcPlayer.Media.Dispose();
				url = url.TrimStart("file:///").Replace('/', '\\');
				VlcPlayer.Media = new CodeBrix.Platform.MediaPlayerCore.Media(_vlc, url);
				// This doesn't start the playback. It just force-loads the media. This is the behaviour only when --start-paused
				VlcPlayer.Play();
			}
			Events?.RaiseMediaEnded();
			Player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
			if (this is { IsLoopingEnabled: false, IsLoopingAllEnabled: false })
			{
				NextTrack();
			}
			if (this is { IsLoopingEnabled: true, IsLoopingAllEnabled: false })
			{
				Stop();
				Play();
			}
			else // IsLoopingAllEnabled
			{
				if (_playlist is not null && _playlist.Items.Count > 0)
				{
					_playlistIndex = (_playlistIndex + 1) % _playlist.Items.Count;
					Uri = _playlist.Items[_playlistIndex]?.Source.Uri;
					Play();
				}
			}
		});
	}

	private void OnEncounteredError(object? _, EventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			Events?.RaiseMediaFailed(MediaPlayerError.Unknown, null, null);
			Player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		});
	}

	private void OnPaused(object? _, EventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused);

	private void OnTick()
	{
		// This is primarily to update the Buffering status, since libVLC doesn't
		// expose a BufferingEnded event.
		MediaPlaybackState? state = null;
		switch (VlcPlayer.State)
		{
			case VLCState.Opening:
				state = MediaPlaybackState.Opening;
				break;
			case VLCState.Buffering:
				state = MediaPlaybackState.Buffering;
				break;
			case VLCState.Playing:
				state = MediaPlaybackState.Playing;
				break;
			case VLCState.Paused:
				state = MediaPlaybackState.Paused;
				break;
			case VLCState.Stopped:
			case VLCState.Ended:
			case VLCState.Error:
			case VLCState.NothingSpecial:
				break;
		}

		if (state != null && state != Player.PlaybackSession.PlaybackState)
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Player.PlaybackSession.PlaybackState = state.Value;
			});
		}

		// We also update the volume, in case it has been changed externally.
		var volume = VlcPlayer.Volume;
		if (_vlcPlayerVolume != volume && volume != -1)
		{
			_vlcPlayerVolume = volume;
			NativeDispatcher.Main.Enqueue(() =>
			{
				double newVolume = volume / 100.0; // VlcPlayer.Volume is in [0, 100]
				Events?.RaiseVolumeChanged(newVolume);
			});
		}
	}
}
