#nullable enable

using System.IO;
using SilverAssertions;
using Xunit;
using AudioPlayerControl = CodeBrix.Platform.UI.AudioPlayer.Skia.AudioPlayer;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for AudioPlayer.AutoPlay: XAML sets attributes in the order they are written, so AutoPlay before Source and
/// AutoPlay after Source must both start playback; AutoPlay after Source starts it through the same transport call a
/// load makes, and only when a source is loaded and not already playing.
/// </summary>
public class AudioPlayerAutoPlayTests
{
	[Fact]
	public void When_AutoPlay_Is_Set_Before_Source_Then_The_Player_Plays()
	{
		//Arrange
		var player = new AudioPlayerControl();
		var output = AddInTestPlatform.LastAudioPlayer!;

		//Act
		player.AutoPlay = true;
		player.Source = "/memory/autoplay-before.wav";

		//Assert
		player.IsPlaying.Should().BeTrue();
		output.Calls.Should().Equal("Load(file)", "Play");
	}

	[Fact]
	public void When_AutoPlay_Is_Set_After_Source_Then_The_Player_Plays()
	{
		//Arrange
		var player = new AudioPlayerControl();
		var output = AddInTestPlatform.LastAudioPlayer!;

		//Act
		player.Source = "/memory/autoplay-after.wav";
		var playingBeforeAutoPlay = player.IsPlaying;
		player.AutoPlay = true;

		//Assert
		playingBeforeAutoPlay.Should().BeFalse();
		player.IsPlaying.Should().BeTrue();
		output.Calls.Should().Equal("Load(file)", "Play");
	}

	[Fact]
	public void When_AutoPlay_Is_Set_After_A_Stream_Was_Loaded_Then_The_Player_Plays()
	{
		//Arrange
		var player = new AudioPlayerControl();
		var output = AddInTestPlatform.LastAudioPlayer!;
		player.SetSourceStream(new MemoryStream(new byte[] { 1, 2, 3 }));

		//Act
		player.AutoPlay = true;

		//Assert
		player.IsPlaying.Should().BeTrue();
		output.Calls.Should().Equal("Load(stream)", "Play");
	}

	[Fact]
	public void When_AutoPlay_Is_Set_With_No_Source_Then_Nothing_Plays_Until_A_Source_Is_Set()
	{
		//Arrange
		var player = new AudioPlayerControl();
		var output = AddInTestPlatform.LastAudioPlayer!;

		//Act
		player.AutoPlay = true;
		var callsBeforeSource = output.Calls.Count;
		player.Source = "/memory/autoplay-later.wav";

		//Assert
		callsBeforeSource.Should().Be(0);
		player.IsPlaying.Should().BeTrue();
		output.Calls.Should().Equal("Load(file)", "Play");
	}

	[Fact]
	public void When_AutoPlay_Is_Set_On_A_Player_That_Is_Already_Playing_Then_Play_Is_Not_Called_Again()
	{
		//Arrange
		var player = new AudioPlayerControl { Source = "/memory/already-playing.wav" };
		var output = AddInTestPlatform.LastAudioPlayer!;
		player.Play();

		//Act
		player.AutoPlay = true;

		//Assert
		player.IsPlaying.Should().BeTrue();
		output.Calls.Should().Equal("Load(file)", "Play");
	}
}
