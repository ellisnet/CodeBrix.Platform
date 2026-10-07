using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Media.Core;

namespace CodeBrix.Platform.UI.Tests.Windows_Media_Core;

[TestClass]
public class Given_MediaSource
{
	[TestMethod]
	public void When_Created_State_Is_Initial()
	{
		// Arrange
		var source = MediaSource.CreateFromUri(new Uri("file:///tmp/clip.mp4"));

		// Act
		var state = source.State;

		// Assert
		state.Should().Be(MediaSourceState.Initial);
	}

	[TestMethod]
	public void When_Disposed_State_Is_Closed()
	{
		// Arrange
		var source = MediaSource.CreateFromUri(new Uri("file:///tmp/clip.mp4"));

		// Act
		source.Dispose();

		// Assert
		source.State.Should().Be(MediaSourceState.Closed);
	}

	[TestMethod]
	public void When_Disposed_Twice_State_Stays_Closed()
	{
		// Arrange
		var source = MediaSource.CreateFromUri(new Uri("file:///tmp/clip.mp4"));
		source.Dispose();

		// Act
		source.Dispose();

		// Assert
		source.State.Should().Be(MediaSourceState.Closed);
		source.Uri.Should().Be(new Uri("file:///tmp/clip.mp4"));
	}
}
