#nullable enable

using System;
using CodeBrix.Platform.Foundation.Extensibility;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Media.Core;
using Xunit;
using MediaPlayerEngine = Windows.Media.Playback.MediaPlayer;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for MediaPlaybackSession.NaturalVideoWidth / NaturalVideoHeight: the session asks the MediaPlayerPresenter
/// that shows its player, which answers from its presenter extension; 0 with no source, no presenter, or no size yet.
/// </summary>
public class MediaPlaybackSessionNaturalVideoSizeTests
{
	private static FakePresenterExtension? _extension;
	private static bool _registered;

	[Fact]
	public void When_A_Presenter_Knows_The_Video_Size_Then_The_Session_Reports_It_Once_A_Source_Is_Set()
	{
		//Arrange
		using var player = new MediaPlayerEngine();
		var fake = new FakePresenterExtension { NaturalVideoWidth = 1280, NaturalVideoHeight = 720 };
		var presenter = CreatePresenter(fake);
		presenter.MediaPlayer = player;

		//Act
		var beforeSource = (player.PlaybackSession.NaturalVideoWidth, player.PlaybackSession.NaturalVideoHeight);
		player.Source = MediaSource.CreateFromUri(new Uri("file:///memory/clip.mp4"));
		var withSource = (player.PlaybackSession.NaturalVideoWidth, player.PlaybackSession.NaturalVideoHeight);

		//Assert
		beforeSource.Should().Be((0u, 0u), "a player with no source has no video");
		withSource.Should().Be((1280u, 720u));
	}

	[Fact]
	public void When_The_Presenter_Extension_Learns_A_New_Size_Then_The_Session_Follows()
	{
		//Arrange
		using var player = new MediaPlayerEngine { Source = MediaSource.CreateFromUri(new Uri("file:///memory/clip.mp4")) };
		var fake = new FakePresenterExtension();
		var presenter = CreatePresenter(fake);
		presenter.MediaPlayer = player;
		var unknown = player.PlaybackSession.NaturalVideoWidth;

		//Act
		fake.NaturalVideoWidth = 640;
		fake.NaturalVideoHeight = 360;

		//Assert
		unknown.Should().Be(0u, "the size is not known before the first frame is decoded");
		player.PlaybackSession.NaturalVideoWidth.Should().Be(640u);
		player.PlaybackSession.NaturalVideoHeight.Should().Be(360u);
	}

	[Fact]
	public void When_No_Presenter_Shows_The_Player_Then_The_Session_Reports_Zero()
	{
		//Arrange
		using var player = new MediaPlayerEngine { Source = MediaSource.CreateFromUri(new Uri("file:///memory/clip.mp4")) };
		var presenter = CreatePresenter(new FakePresenterExtension { NaturalVideoWidth = 1920, NaturalVideoHeight = 1080 });
		presenter.MediaPlayer = player;
		var shown = player.PlaybackSession.NaturalVideoWidth;

		//Act
		presenter.MediaPlayer = null!;

		//Assert
		shown.Should().Be(1920u);
		player.PlaybackSession.NaturalVideoWidth.Should().Be(0u);
		player.PlaybackSession.NaturalVideoHeight.Should().Be(0u);
	}

	private static MediaPlayerPresenter CreatePresenter(FakePresenterExtension extension)
	{
		if (!_registered)
		{
			// A builder that returns null means "not registered" to the registry, so every other presenter in this
			// process is created exactly as before (no extension).
			ApiExtensibility.Register(typeof(IMediaPlayerPresenterExtension), _ => _extension!);
			_registered = true;
		}

		_extension = extension;
		try
		{
			return new MediaPlayerPresenter();
		}
		finally
		{
			_extension = null;
		}
	}

	private sealed class FakePresenterExtension : IMediaPlayerPresenterExtension
	{
		public uint NaturalVideoWidth { get; set; }

		public uint NaturalVideoHeight { get; set; }

		public void MediaPlayerChanged() { }

		public void StretchChanged() { }

		public void RequestFullScreen() { }

		public void ExitFullScreen() { }

		public void RequestCompactOverlay() { }

		public void ExitCompactOverlay() { }
	}
}
