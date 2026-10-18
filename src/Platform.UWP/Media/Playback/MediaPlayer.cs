using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.AccessControl;
using Windows.Foundation;

namespace Windows.Media.Playback
{
	public sealed partial class MediaPlayer
	{
		internal const bool ImplementedByExtensions =
#if false
			false;
#else
			true;
#endif

		#region Properties

		private IMediaPlaybackSource _source;
		public IMediaPlaybackSource Source
		{
			get => _source;
			set
			{
				Stop();

				_source = value;

				InitializeSource();
				SourceChanged?.Invoke(this, null);

				if (_source != null && AutoPlay)
				{
					Play();
				}
			}
		}

		public bool AutoPlay { get; set; }

		// The presenter showing this player (weakly held: a player must not keep a presenter alive), which answers the
		// session's NaturalVideoWidth / NaturalVideoHeight.
		private WeakReference<CodeBrix.Platform.Media.Playback.IMediaPlayerNaturalVideoSize> _naturalVideoSizeSource;

		/// <summary>
		/// Gets what reports the natural size of this player's video (the MediaPlayerPresenter showing it), or null when
		/// nothing does.
		/// </summary>
		internal CodeBrix.Platform.Media.Playback.IMediaPlayerNaturalVideoSize NaturalVideoSizeSource
			=> _naturalVideoSizeSource is not null && _naturalVideoSizeSource.TryGetTarget(out var source) ? source : null;

		/// <summary>
		/// Sets what reports the natural size of this player's video; the last presenter to show the player wins.
		/// </summary>
		/// <param name="source">The size source, or null to clear it.</param>
		internal void SetNaturalVideoSizeSource(CodeBrix.Platform.Media.Playback.IMediaPlayerNaturalVideoSize source)
			=> _naturalVideoSizeSource = source is null ? null : new WeakReference<CodeBrix.Platform.Media.Playback.IMediaPlayerNaturalVideoSize>(source);

		private bool _isMuted;
		public bool IsMuted
		{
			get => _isMuted;
			set
			{
				_isMuted = value;
				ToggleMute();
				IsMutedChanged?.Invoke(this, _isMuted);
			}
		}

		private double _volume = 1d;
		public double Volume
		{
			get => _volume;
			set
			{
				if (value >= 0.0 && value <= 1.0)
				{
					_volume = value;
					OnVolumeChanged();
					VolumeChanged?.Invoke(this, _volume);
				}
			}
		}

		public MediaPlaybackSession PlaybackSession { get; }

		#endregion

		#region Events

		public event TypedEventHandler<MediaPlayer, object> SourceChanged;

		public event TypedEventHandler<MediaPlayer, object> IsMutedChanged;

		public event TypedEventHandler<MediaPlayer, object> VolumeChanged;

		public event TypedEventHandler<MediaPlayer, object> MediaEnded;

		public event TypedEventHandler<MediaPlayer, MediaPlayerFailedEventArgs> MediaFailed;

		public event TypedEventHandler<MediaPlayer, object> MediaOpened;

		public event TypedEventHandler<MediaPlayer, object> SeekCompleted;

		public event TypedEventHandler<MediaPlayer, object> NaturalVideoDimensionChanged;

		#endregion

		public MediaPlayer()
		{
			PlaybackSession = new MediaPlaybackSession(this);
			Initialize();
		}

		internal void SetOption(string name, object value)
		{
			OnOptionChanged(name, value);
		}

		partial void OnOptionChanged(string name, object value);

		internal void SetTransportControlBounds(Rect bounds)
		{
			OnTransportControlBoundsChanged(bounds);
		}

		partial void OnTransportControlBoundsChanged(Rect bounds);
	}
}
