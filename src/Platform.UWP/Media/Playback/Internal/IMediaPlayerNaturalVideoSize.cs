namespace CodeBrix.Platform.Media.Playback;

/// <summary>
/// What knows the natural size of the video a <see cref="Windows.Media.Playback.MediaPlayer"/> plays: the
/// MediaPlayerPresenter that shows the player registers itself with
/// <see cref="Windows.Media.Playback.MediaPlayer.SetNaturalVideoSizeSource"/>, answering from its presenter extension, so
/// <see cref="Windows.Media.Playback.MediaPlaybackSession.NaturalVideoWidth"/> and
/// <see cref="Windows.Media.Playback.MediaPlaybackSession.NaturalVideoHeight"/> report it.
/// </summary>
internal interface IMediaPlayerNaturalVideoSize
{
	/// <summary>Gets the natural width of the video, in pixels (0 when unknown).</summary>
	uint NaturalVideoWidth { get; }

	/// <summary>Gets the natural height of the video, in pixels (0 when unknown).</summary>
	uint NaturalVideoHeight { get; }
}
