using System;
using CodeBrix.Platform.UI.WebGpu.Internal;
using CodeBrix.WebGpu.Native;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The element itself, constructed host-free (no window, so never loaded): its defaults, its property rules, and that
/// nothing touches the GPU until it is loaded.
/// </summary>
public class WebGpuRenderViewTests
{
	private sealed class Renderer : IWebGpuRenderer
	{
		public void OnRender(WebGpuFrame frame)
		{
		}
	}

	[Fact]
	public void a_new_view_has_the_documented_defaults()
	{
		//Act
		var view = new WebGpuRenderView();

		//Assert
		view.FrameDriver.Should().Be(WebGpuFrameDriver.Continuous);
		view.TargetFrameRate.Should().Be(60.0);
		view.IsPaused.Should().BeFalse();
		view.Renderer.Should().BeNull();
		view.Stretch.Should().Be(Stretch.Fill);
		view.ClearColor.Should().Be(Colors.Black);
		view.RenderPath.Should().Be(WebGpuRenderPath.None);
		view.IsDeviceReady.Should().BeFalse();
		view.PreferredFormat.Should().Be(TextureFormat.BGRA8Unorm);
		view.Format.Should().Be(TextureFormat.BGRA8Unorm);
		view.PixelScale.Should().Be(1.0);
		view.TextureWidth.Should().Be(0);
		view.Device.IsNull.Should().BeTrue();
		view.Texture.IsNull.Should().BeTrue();
		view.DeviceOptions.Should().BeNull();
		view.RequireOwnedDevice.Should().BeFalse();
		view.DeviceInfo.Should().BeNull();
	}

	[Fact]
	public void a_view_that_was_never_loaded_renders_nothing_and_creates_no_device()
	{
		//Arrange
		var view = new WebGpuRenderView { FrameDriver = WebGpuFrameDriver.Manual };

		//Act
		var frame = view.BeginFrame();

		//Assert
		frame.Should().BeNull();
		view.IsDeviceReady.Should().BeFalse();
		view.Scheduler.IsAttached.Should().BeFalse();
	}

	[Fact]
	public void an_invalid_target_frame_rate_is_refused_and_the_old_value_kept()
	{
		//Arrange
		var view = new WebGpuRenderView { TargetFrameRate = 30 };

		//Act
		Action act = () => view.TargetFrameRate = -1;

		//Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
		view.TargetFrameRate.Should().Be(30.0);
		view.Scheduler.TargetFrameRate.Should().Be(30.0);
	}

	[Fact]
	public void the_properties_reach_the_scheduler_the_engine_and_the_surface()
	{
		//Arrange
		var view = new WebGpuRenderView();

		//Act
		view.FrameDriver = WebGpuFrameDriver.OnDemand;
		view.TargetFrameRate = 0;
		view.IsPaused = true;
		view.Stretch = Stretch.Uniform;
		view.ClearColor = Colors.Red;
		var options = new CodeBrix.WebGpu.Skia.WebGpuSkiaDeviceOptions { ForceFallbackAdapter = true };
		view.DeviceOptions = options;
		view.RequireOwnedDevice = true;

		//Assert
		view.Engine.DeviceOptions.Should().BeSameAs(options);
		view.Engine.RequireOwnedDevice.Should().BeTrue();
		view.Scheduler.Driver.Should().Be(WebGpuFrameDriver.OnDemand);
		view.Scheduler.TargetFrameRate.Should().Be(0.0);
		view.Scheduler.IsPaused.Should().BeTrue();
		view.Surface.Stretch.Should().Be(Stretch.Uniform);
		view.Engine.ClearColor.Should().Be(SKColors.Red);
	}

	[Fact]
	public void the_renderer_can_be_set_and_cleared()
	{
		//Arrange
		var view = new WebGpuRenderView();
		var renderer = new Renderer();

		//Act
		view.Renderer = renderer;
		var set = view.Renderer;
		view.Renderer = null;

		//Assert
		set.Should().BeSameAs(renderer);
		view.Renderer.Should().BeNull();
	}

	[Fact]
	public void the_renderer_interface_needs_only_on_render()
	{
		//Arrange
		IWebGpuRenderer renderer = new Renderer();

		//Act
		Action defaults = () =>
		{
			renderer.OnDeviceReady(new WebGpuRenderView());
			renderer.OnOverlay(null!, SKSize.Empty);
			renderer.OnDeviceLost("gone");
		};

		//Assert
		defaults.Should().NotThrow();
	}

	[Fact]
	public unsafe void each_view_has_its_own_hand_off_at_a_stable_address()
	{
		//Arrange
		var first = new WebGpuRenderView();
		var second = new WebGpuRenderView();

		//Act
		var a1 = first.GetViewHandoff();
		var a2 = first.GetViewHandoff();
		var b = second.GetViewHandoff();

		//Assert
		(a1 == a2).Should().BeTrue();
		(a1 == b).Should().BeFalse();
		(first.GetDeviceHandoff() == a1->Device).Should().BeTrue();
		(second.GetDeviceHandoff() == b->Device).Should().BeTrue();
		a1->Size.Should().Be(40u);
		(a1->BeginFrame(a1->Context) == null).Should().BeTrue(); // never loaded
	}

	[Fact]
	public void the_view_is_both_sharing_contracts()
	{
		//Arrange
		var view = new WebGpuRenderView();

		//Act
		IWebGpuDeviceSource source = view;
		IWebGpuTextureTarget target = view;

		//Assert
		source.Device.IsNull.Should().BeTrue();
		source.Backend.Should().Be(BackendType.Undefined);
		target.BeginWebGpuPass().IsNull.Should().BeTrue();
		target.SampleCount.Should().Be(1);
	}

	[Fact]
	public void the_frame_object_is_the_one_the_render_event_args_carry()
	{
		//Arrange
		var view = new WebGpuRenderView();

		//Act
		var frame = view.Engine.Frame;

		//Assert
		frame.IsOpen.Should().BeFalse();
		frame.Encoder.IsNull.Should().BeTrue();
		new WebGpuRenderEventArgs(frame).Frame.Should().BeSameAs(frame);
	}
}
