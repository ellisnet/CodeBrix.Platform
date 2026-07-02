using System;
using System.Linq;
using CodeBrix.Platform.MediaPlayerCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace CodeBrix.Platform.UI.MediaPlayer.Skia;

/// <summary>
/// The IMediaPlayerPresenterExtension implementation for all CodeBrix.Platform Skia heads except
/// macOS. Decoded frames arrive from the playback engine's <see cref="VideoFrameSink"/> on a
/// libvlc thread, are copied into an SKImage there (the sink's buffer is only valid during the
/// callback), and are then presented on the UI thread through <see cref="VideoFrameHostElement"/>,
/// which composites them into the Skia scene like any other XAML content.
/// </summary>
public class SkiaMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;
	private readonly VideoFrameHostElement _element;
	private SkiaMediaPlayerExtension? _playerExtension;

	/// <summary>
	/// Creates the presenter extension for the given <see cref="MediaPlayerPresenter"/>.
	/// Instantiated by the framework through the ApiExtension registration; not intended to be
	/// constructed directly by app code.
	/// </summary>
	public SkiaMediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		_presenter = presenter;
		_element = new VideoFrameHostElement(presenter.Visual.Compositor)
		{
			// Collapsed until the loaded media turns out to have a video track, mirroring the
			// behavior of the other presenter implementations.
			Visibility = Visibility.Collapsed,
		};
		_presenter.Child = _element;
		StretchChanged();
	}

	/// <inheritdoc />
	public void MediaPlayerChanged()
	{
		if (SkiaMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			if (_playerExtension is { })
			{
				_playerExtension.FrameSink.FrameReady -= OnFrameReady;
				_playerExtension.IsVideoChanged -= OnExtensionOnIsVideoChanged;
			}
			_playerExtension = extension;
			_playerExtension.FrameSink.FrameReady += OnFrameReady;
			_playerExtension.IsVideoChanged += OnExtensionOnIsVideoChanged;
			_element.ClearFrame();
		}
	}

	private void OnFrameReady(object? sender, VideoFrameReadyEventArgs args)
	{
		// Raised on a libvlc thread. The sink's pixel buffer is only valid until this handler
		// returns, so the copy into an SKImage must happen here; only the presentation of the
		// copied image is marshaled to the UI thread.
		var info = new SKImageInfo((int)args.Width, (int)args.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
		var image = SKImage.FromPixelCopy(info, args.Plane, (int)args.PitchBytes);
		if (image is null)
		{
			return;
		}
		if (!_presenter.DispatcherQueue.TryEnqueue(() => _element.PresentFrame(image)))
		{
			image.Dispose();
		}
	}

	private void OnExtensionOnIsVideoChanged(object? sender, bool? args)
	{
		_element.Visibility = args ?? false ? Visibility.Visible : Visibility.Collapsed;
		if (args ?? false)
		{
			StretchChanged();
		}
	}

	/// <inheritdoc />
	public void StretchChanged() => _element.Stretch = _presenter.Stretch;

	/// <inheritdoc />
	public void RequestFullScreen()
	{
		// TODO
	}

	/// <inheritdoc />
	public void ExitFullScreen()
	{
		// TODO
	}

	/// <inheritdoc />
	public void RequestCompactOverlay()
	{
		// TODO
	}

	/// <inheritdoc />
	public void ExitCompactOverlay()
	{
		// TODO
	}

	/// <inheritdoc />
	public uint NaturalVideoHeight
		=> _playerExtension?.VlcPlayer.Media?.Tracks
			.FirstOrDefault(t => t.TrackType == TrackType.Video)
			.Data.Video.Height ?? 0;

	/// <inheritdoc />
	public uint NaturalVideoWidth
		=> _playerExtension?.VlcPlayer.Media?.Tracks
			.FirstOrDefault(t => t.TrackType == TrackType.Video)
			.Data.Video.Width ?? 0;
}
