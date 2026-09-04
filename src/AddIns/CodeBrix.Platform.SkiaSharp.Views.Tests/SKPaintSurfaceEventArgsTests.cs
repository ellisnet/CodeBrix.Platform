using System;
using System.Runtime.InteropServices;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The event args every <c>SKXamlCanvas</c> paint handler receives. A plain data class, but the
/// two-argument constructor's "RawInfo defaults to Info" behaviour is what an application relies
/// on when it ignores pixel scaling, so it is pinned rather than assumed.
/// </summary>
public class SKPaintSurfaceEventArgsTests
{
	private static SKSurface CreateSurface(SKImageInfo info, out GCHandle handle)
	{
		var pixels = new byte[info.BytesSize];
		handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		return SKSurface.Create(info, handle.AddrOfPinnedObject(), info.RowBytes);
	}

	[Fact]
	public void ctor_with_one_info_sets_rawinfo_to_the_same_info()
	{
		//Arrange
		var info = new SKImageInfo(200, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = CreateSurface(info, out var handle);

		try
		{
			//Act
			var args = new SKPaintSurfaceEventArgs(surface, info);

			//Assert
			args.Surface.Should().BeSameAs(surface);
			args.Info.Should().Be(info);
			args.RawInfo.Should().Be(info);
		}
		finally
		{
			handle.Free();
		}
	}

	[Fact]
	public void ctor_with_two_infos_keeps_them_apart()
	{
		//Arrange
		//This is the scaled case: the handler is told it is drawing on a 200x100 logical surface
		//while the memory behind it is 400x200 device pixels.
		var rawInfo = new SKImageInfo(400, 200, SKColorType.Bgra8888, SKAlphaType.Premul);
		var info = rawInfo.WithSize(new SKSizeI(200, 100));
		using var surface = CreateSurface(rawInfo, out var handle);

		try
		{
			//Act
			var args = new SKPaintSurfaceEventArgs(surface, info, rawInfo);

			//Assert
			args.Surface.Should().BeSameAs(surface);
			args.Info.Width.Should().Be(200);
			args.Info.Height.Should().Be(100);
			args.RawInfo.Width.Should().Be(400);
			args.RawInfo.Height.Should().Be(200);
			args.Info.Should().NotBe(args.RawInfo);
		}
		finally
		{
			handle.Free();
		}
	}

	[Fact]
	public void the_args_are_an_EventArgs()
	{
		//A handler written as EventHandler<SKPaintSurfaceEventArgs> only compiles while this holds.
		typeof(SKPaintSurfaceEventArgs).Should().BeAssignableTo<EventArgs>();
	}

	[Fact]
	public void Surface_Info_and_RawInfo_are_read_only()
	{
		//Arrange
		//The control caches ONE args instance across frames (see SKXamlCanvas.Skia.cs); if any of
		//these gained a setter, a handler could quietly rewrite the next frame's description.
		var type = typeof(SKPaintSurfaceEventArgs);

		//Act
		//Assert
		type.GetProperty(nameof(SKPaintSurfaceEventArgs.Surface))!.SetMethod.Should().BeNull();
		type.GetProperty(nameof(SKPaintSurfaceEventArgs.Info))!.SetMethod.Should().BeNull();
		type.GetProperty(nameof(SKPaintSurfaceEventArgs.RawInfo))!.SetMethod.Should().BeNull();
	}
}
