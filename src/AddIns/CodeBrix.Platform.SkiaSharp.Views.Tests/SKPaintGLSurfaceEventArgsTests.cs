using System;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The GL flavour of the paint args. No Skia head ever raises it - <c>SKSwapChainPanel</c> is
/// unsupported there - but the type is public API of the add-in and it names five SkiaSharp
/// graphics types (<see cref="GRBackendRenderTarget"/>, <see cref="GRGlFramebufferInfo"/>,
/// <see cref="GRSurfaceOrigin"/>, <see cref="SKColorType"/>, <see cref="SKImageInfo"/>), so a
/// SkiaSharp bump that changes any of their shapes has to fail somewhere. It fails here.
/// </summary>
/// <remarks>
/// A <see cref="GRBackendRenderTarget"/> built from a <see cref="GRGlFramebufferInfo"/> is a plain
/// descriptor: no GL context is created, nothing is drawn, and the tests run on any machine.
/// </remarks>
public class SKPaintGLSurfaceEventArgsTests
{
	private static GRBackendRenderTarget CreateRenderTarget(int width = 200, int height = 100)
	{
		//The framebuffer id and format describe a target that is never bound; only the descriptor's
		//own arithmetic (Width/Height) is read below.
		var glInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
		return new GRBackendRenderTarget(width, height, sampleCount: 0, stencilBits: 8, glInfo);
	}

	[Fact]
	public void ctor_with_surface_and_render_target_defaults_origin_and_colour_type()
	{
		//Arrange
		using var renderTarget = CreateRenderTarget();

		//Act
		var args = new SKPaintGLSurfaceEventArgs(surface: null!, renderTarget);

		//Assert
		args.BackendRenderTarget.Should().BeSameAs(renderTarget);
		args.Origin.Should().Be(GRSurfaceOrigin.BottomLeft);
		args.ColorType.Should().Be(SKColorType.Rgba8888);
		args.Info.Width.Should().Be(200);
		args.Info.Height.Should().Be(100);
		args.RawInfo.Should().Be(args.Info);
	}

	[Fact]
	public void ctor_with_origin_and_colour_type_builds_the_info_from_the_render_target()
	{
		//Arrange
		using var renderTarget = CreateRenderTarget(320, 240);

		//Act
		var args = new SKPaintGLSurfaceEventArgs(
			surface: null!, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);

		//Assert
		args.Origin.Should().Be(GRSurfaceOrigin.TopLeft);
		args.ColorType.Should().Be(SKColorType.Bgra8888);
		args.Info.Width.Should().Be(320);
		args.Info.Height.Should().Be(240);
		args.Info.ColorType.Should().Be(SKColorType.Bgra8888);
		args.RawInfo.Should().Be(args.Info);
	}

	[Fact]
	public void ctor_with_an_info_takes_its_colour_type_and_copies_it_to_rawinfo()
	{
		//Arrange
		using var renderTarget = CreateRenderTarget();
		var info = new SKImageInfo(200, 100, SKColorType.Rgb565, SKAlphaType.Opaque);

		//Act
		var args = new SKPaintGLSurfaceEventArgs(
			surface: null!, renderTarget, GRSurfaceOrigin.TopLeft, info);

		//Assert
		args.ColorType.Should().Be(SKColorType.Rgb565);
		args.Info.Should().Be(info);
		args.RawInfo.Should().Be(info);
	}

	[Fact]
	public void ctor_with_an_info_and_a_rawinfo_keeps_them_apart()
	{
		//Arrange
		using var renderTarget = CreateRenderTarget(400, 200);
		var rawInfo = new SKImageInfo(400, 200, SKColorType.Rgba8888, SKAlphaType.Premul);
		var info = rawInfo.WithSize(new SKSizeI(200, 100));

		//Act
		var args = new SKPaintGLSurfaceEventArgs(
			surface: null!, renderTarget, GRSurfaceOrigin.BottomLeft, info, rawInfo);

		//Assert
		args.Info.Width.Should().Be(200);
		args.RawInfo.Width.Should().Be(400);
		args.ColorType.Should().Be(SKColorType.Rgba8888);
		args.Origin.Should().Be(GRSurfaceOrigin.BottomLeft);
	}

	[Fact]
	public void the_args_are_an_EventArgs()
	{
		typeof(SKPaintGLSurfaceEventArgs).Should().BeAssignableTo<EventArgs>();
	}
}
