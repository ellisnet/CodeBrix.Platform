using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SilverAssertions;
using SkiaSharp;
using Windows.Graphics.Imaging;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// Fence for the WPA1 FIXLIST entry on the WinRT surface's Skia imaging platform (CreateBitmapFromPixels): when
/// SKBitmap.InstallPixels refuses the pixels, the pinned array is released (Skia runs the release proc on failure) and
/// the empty bitmap is disposed; the platform returns null.
/// </summary>
public class GraphicsImagingInstallPixelsTests
{
	[Fact]
	public void When_InstallPixels_Refuses_The_Pixels_Then_Skia_Runs_The_Release_Proc_So_The_Pin_Is_Freed()
	{
		//Arrange
		var pixels = new byte[16];
		var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		var released = false;
		using var bitmap = new SKBitmap();

		//Act: an image info Skia refuses (negative width)
		var success = bitmap.InstallPixels(new SKImageInfo(-1, 1, SKColorType.Bgra8888, SKAlphaType.Premul),
			handle.AddrOfPinnedObject(), 4, (address, context) =>
			{
				released = true;
				((GCHandle)context).Free();
			}, handle);

		//Assert (the proc freed the handle; this local copy of the GCHandle struct still carries the old value)
		success.Should().BeFalse();
		released.Should().BeTrue();
	}

	[Fact]
	public void When_The_Imaging_Platform_Is_Given_Pixels_It_Cannot_Install_Then_It_Returns_No_Bitmap()
	{
		//Arrange: the Skia implementation is internal to CodeBrix.Platform (this suite does not compile against it)
		var type = Type.GetType("CodeBrix.Platform.Skia.GraphicsImagingSkiaPlatform, CodeBrix.Platform", throwOnError: true)!;
		var platform = Activator.CreateInstance(type, nonPublic: true)!;
		var create = type.GetMethod("CreateBitmapFromPixels", BindingFlags.Public | BindingFlags.Instance)!;

		//Act
		var result = create.Invoke(platform, [new byte[16], BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, -1, 1]);
		var valid = create.Invoke(platform, [new byte[16], BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, 2, 2]);

		//Assert
		result.Should().BeNull();
		valid.Should().BeOfType<SKBitmap>();
		((SKBitmap)valid!).Dispose();
	}
}
