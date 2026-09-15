using System;
using CodeBrix.Platform.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Graphics;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the other half of the window-sizing rule: whether a number means the FRAMED window or the
/// CLIENT area inside it, and the arithmetic every head shares for converting one into the other.
/// </summary>
public class NativeWindowFrameConversionTests
{
	[Fact]
	public void a_window_the_manager_has_not_framed_yet_has_no_frame_extents()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1024, Height = 640 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var hasFrame = NativeWindowWrapperBase.HasNonClientFrame(framed, client);

		//Assert
		hasFrame.Should().BeFalse();
	}

	[Fact]
	public void a_decorated_window_has_frame_extents()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var hasFrame = NativeWindowWrapperBase.HasNonClientFrame(framed, client);

		//Assert
		hasFrame.Should().BeTrue();
	}

	[Fact]
	public void a_framed_size_loses_the_decorations_on_the_way_to_the_client_area()
	{
		//Arrange - the frame this laptop's window manager draws: 20 wide, 50 tall.
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };
		var requested = new SizeInt32 { Width = 1300, Height = 890 };

		//Act
		var clientRequest = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		clientRequest.Width.Should().Be(1280);
		clientRequest.Height.Should().Be(840);
	}

	[Fact]
	public void an_undecorated_window_asks_its_client_area_for_the_framed_size_unchanged()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1024, Height = 640 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };
		var requested = new SizeInt32 { Width = 1300, Height = 890 };

		//Act
		var clientRequest = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		clientRequest.Width.Should().Be(1300);
		clientRequest.Height.Should().Be(890);
	}

	[Fact]
	public void a_framed_size_smaller_than_its_own_decorations_still_leaves_a_client_area()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };
		var requested = new SizeInt32 { Width = 10, Height = 10 };

		//Act
		var clientRequest = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		clientRequest.Width.Should().Be(1);
		clientRequest.Height.Should().Be(1);
	}
}
