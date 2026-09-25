#nullable enable

using System;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fence for WPE1-1 item C0f (decision: the Core picks the read-back format per context): GLCanvasElement reads its
/// picture back as BGRA wherever the context supports it (desktop OpenGL, or OpenGL ES with GL_EXT_read_format_bgra -
/// every Skia head, unchanged) and otherwise as RGBA with red and blue swapped in place, so an OpenGL ES context that
/// guarantees only RGBA needs no glReadPixels shim.
/// </summary>
public class GLReadBackFormatTests
{
	[Theory]
	[InlineData("4.6 (Core Profile) Mesa 25.0.7", "", true)]
	[InlineData("3.3.0 NVIDIA 580.95.05", "", true)]
	[InlineData(null, "", true)]
	[InlineData("OpenGL ES 3.2 Mesa 25.0.7", "GL_EXT_blend_minmax GL_EXT_read_format_bgra", true)]
	[InlineData("OpenGL ES 3.2 build 1.13@5776728", "GL_EXT_blend_minmax GL_OES_texture_npot", false)]
	[InlineData("OpenGL ES 3.0", "", false)]
	public void When_A_Context_Is_Described_Then_Bgra_Read_Back_Is_Used_Only_Where_Supported(string? version, string extensions, bool expected)
	{
		//Act
		var bgra = GLCanvasElement.SupportsBgraReadBack(version, extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries));

		//Assert
		bgra.Should().Be(expected);
	}

	[Fact]
	public void When_Rgba_Pixels_Are_Swapped_Then_They_Are_Bgra_And_Alpha_And_Green_Stay()
	{
		//Arrange: two RGBA pixels (red opaque, a half-transparent teal)
		var pixels = new byte[] { 0xFF, 0x00, 0x00, 0xFF, 0x10, 0x80, 0xC0, 0x7F };

		//Act
		GLCanvasElement.SwapRedAndBlue(pixels);

		//Assert
		pixels.Should().Equal(new byte[] { 0x00, 0x00, 0xFF, 0xFF, 0xC0, 0x80, 0x10, 0x7F });
	}
}
