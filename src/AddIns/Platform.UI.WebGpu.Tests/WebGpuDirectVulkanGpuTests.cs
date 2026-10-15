using System;
using System.Linq;
using System.Threading;
using CodeBrix.Platform.UI.WebGpu.Internal;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using SilverAssertions;
using SkiaSharp;
using Xunit;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The DirectVulkan render path on this machine's GPU, through a head's own Vulkan device: the shared
/// <c>VulkanContext</c> the X11 and Wayland heads render with (initialized headless) behind the provider those heads
/// register. Adopt -> render clear colour + triangle into the frame textures -> wrap the published texture as an
/// SKImage on the head's GRContext -> draw it the way a head draws a window frame (recorded into a picture, played
/// onto a GPU surface) -> read the pixels. Every forced failure ends on Readback. Each test skips with the reason
/// where this machine has no Vulkan device.
/// </summary>
public sealed unsafe class WebGpuDirectVulkanGpuTests : IDisposable
{
	private readonly RecordingEngineHost _host = new();
	private readonly WebGpuRenderEngine _engine;
	private readonly DirectGpuLatch _latch = DirectGpuLatch.On();
	private HeadlessVulkanHead? _head;
	private TrianglePipeline? _triangle;
	private Func<CodeBrix.Platform.Graphics.INativeGpuDeviceProvider?>? _probe;

	public WebGpuDirectVulkanGpuTests()
	{
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.None;
		_engine = new WebGpuRenderEngine(_host);
		_host.OnReady = () => _triangle = new TrianglePipeline(((IWebGpuDeviceSource)_engine).Device, _engine.Format);
		_host.OnLost = _ =>
		{
			_triangle?.Dispose();
			_triangle = null;
		};
	}

	public void Dispose()
	{
		_engine.Detach();
		_triangle?.Dispose();
		_head?.Dispose();
		_latch.Dispose();
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.None;
	}

	private HeadlessVulkanHead Head => _head ??= HeadlessVulkanHead.CreateOrSkip();

	private void AttachAt(int width, int height, CodeBrix.Platform.Graphics.INativeGpuDeviceProvider? provider = null)
	{
		var resolved = provider ?? Head.Provider;
		_probe = () => resolved;
		_engine.Attach(WebGpuDeviceGateDecision.Proceed, _probe);
		_engine.SetPixelSize(width, height);
	}

	private void RenderOne(bool drawTriangle)
	{
		_engine.OpenFrame(wait: true).Should().Be(WebGpuFrameOutcome.Rendered);
		if (drawTriangle)
		{
			_triangle!.Draw(_engine.Frame.Encoder, _engine.Frame.TextureView);
		}

		_engine.EndFrame();
	}

	// The head's part: what the surface's paint would draw, recorded and played back onto the head's GPU surface.
	private SKBitmap Paint(int width, int height) =>
		Head.ReadThroughHeadSurface(canvas =>
		{
			var image = _host.DirectSource?.AcquirePaintImage();
			if (image is not null)
			{
				canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Nearest));
			}
		}, width, height);

	private WebGpuDirectPresenter Direct => (WebGpuDirectPresenter)_engine.Presenter!;

	[Fact]
	public void the_heads_vulkan_device_is_adopted_and_the_path_is_direct_vulkan()
	{
		//Arrange
		AttachAt(32, 32);

		//Act
		RenderOne(drawTriangle: false);
		IWebGpuDeviceSource source = _engine;

		//Assert
		_host.Events.Should().Equal("Path:None->DirectVulkan", "DeviceReady");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
		source.Backend.Should().Be(BackendType.Vulkan);
		source.HasUncapturedErrorHandler.Should().BeFalse();
		(_engine.DeviceHandoff->Flags & CodebrixDeviceHandoff.FlagUncapturedErrorHandler).Should().Be(0u);
		_engine.Device!.Route.Should().Be(WebGpuSkiaDeviceRoute.Adopted);
		Direct.SharesQueueWithSkia.Should().BeTrue();
		_host.DirectSource.Should().BeSameAs(Direct);
	}

	[Fact]
	public void clear_colour_and_triangle_reach_the_heads_surface_with_no_readback()
	{
		//Arrange
		AttachAt(80, 80);
		_engine.ClearColor = new SKColor(200, 30, 40);

		//Act
		RenderOne(drawTriangle: true);
		using var pixels = Paint(80, 80);

		//Assert
		pixels.GetPixel(2, 2).Should().Be(new SKColor(200, 30, 40));
		pixels.GetPixel(77, 77).Should().Be(new SKColor(200, 30, 40));
		pixels.GetPixel(40, 45).Should().Be(new SKColor(0, 255, 0));
		Direct.ReadbackCount.Should().Be(0L);
		Direct.FrameCount.Should().Be(1L);
	}

	[Fact]
	public void the_wrapped_image_lives_on_the_heads_context_and_is_cached_per_texture()
	{
		//Arrange
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);

		//Act
		var first = _host.DirectSource!.AcquirePaintImage();
		var again = _host.DirectSource!.AcquirePaintImage();

		//Assert
		first.Should().NotBeNull();
		first!.IsTextureBacked.Should().BeTrue();
		again.Should().BeSameAs(first);
		(first.Width, first.Height).Should().Be((16, 16));
	}

	[Fact]
	public void every_frame_shows_the_newest_picture_through_the_head()
	{
		//Arrange
		AttachAt(24, 24);
		var colours = new[] { SKColors.Red, SKColors.Blue, new SKColor(255, 255, 0), SKColors.White, new SKColor(0, 128, 255) };

		//Act + Assert
		foreach (var colour in colours)
		{
			_engine.ClearColor = colour;
			RenderOne(drawTriangle: false);
			using var pixels = Paint(24, 24);
			pixels.GetPixel(12, 12).Should().Be(colour);
		}

		Direct.ReadbackCount.Should().Be(0L);
	}

	[Fact]
	public void a_frame_never_renders_into_the_published_texture_nor_the_one_painted_last()
	{
		//Arrange
		AttachAt(8, 8);
		var violations = 0;
		var slotsUsed = new System.Collections.Generic.HashSet<int>();

		//Act: paint after some frames and not others, so "published" and "painted" diverge.
		for (var i = 0; i < 24; i++)
		{
			_engine.OpenFrame(wait: true).Should().Be(WebGpuFrameOutcome.Rendered);
			var write = Direct.WriteSlot;
			slotsUsed.Add(write);
			if (write == Direct.PublishedSlot || write == Direct.PaintedSlot)
			{
				violations++;
			}

			_engine.EndFrame();
			if (i % 3 == 0)
			{
				_host.DirectSource!.AcquirePaintImage();
			}
		}

		//Assert
		violations.Should().Be(0);
		slotsUsed.Count.Should().Be(WebGpuDirectPresenter.SlotCount);
	}

	[Fact]
	public void a_resize_renders_new_textures_and_the_last_frame_stays_paintable_until_then()
	{
		//Arrange
		AttachAt(40, 20);
		_engine.ClearColor = SKColors.Blue;
		RenderOne(drawTriangle: false);
		_host.DirectSource!.AcquirePaintImage();

		//Act
		_engine.SetPixelSize(20, 40);
		var beforeNextFrame = _host.DirectSource!.AcquirePaintImage()!;
		var beforeSize = (beforeNextFrame.Width, beforeNextFrame.Height);
		_engine.ClearColor = SKColors.Red;
		RenderOne(drawTriangle: false);
		var after = _host.DirectSource!.AcquirePaintImage()!;
		using var pixels = Paint(20, 40);

		//Assert
		beforeSize.Should().Be((40, 20));
		(after.Width, after.Height).Should().Be((20, 40));
		pixels.GetPixel(10, 30).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_picture_recorded_before_teardown_still_plays_after_it()
	{
		//Arrange
		AttachAt(16, 16);
		_engine.ClearColor = SKColors.Blue;
		RenderOne(drawTriangle: false);
		using var recorder = new SKPictureRecorder();
		var canvas = recorder.BeginRecording(new SKRect(0, 0, 16, 16));
		canvas.DrawImage(_host.DirectSource!.AcquirePaintImage()!, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), null);
		using var picture = recorder.EndRecording();

		//Act: the view leaves the tree (adopted device and frame textures released) while the picture still holds
		// the wrapper - Skia's release callback keeps the texture alive until the picture is gone.
		_engine.Detach();
		using var pixels = Head.ReadThroughHeadSurface(c => c.DrawPicture(picture), 16, 16);

		//Assert
		pixels.GetPixel(8, 8).Should().Be(SKColors.Blue);
		_engine.RenderPath.Should().Be(WebGpuRenderPath.None);
	}

	[Fact]
	public void detach_raises_device_lost_and_reattach_adopts_again()
	{
		//Arrange
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);

		//Act
		_engine.Detach();
		var afterDetach = (_engine.RenderPath, Source: _host.DirectSource);
		_engine.Attach(WebGpuDeviceGateDecision.Proceed, _probe);
		RenderOne(drawTriangle: true);
		using var pixels = Paint(16, 16);

		//Assert
		afterDetach.RenderPath.Should().Be(WebGpuRenderPath.None);
		afterDetach.Source.Should().BeNull();
		_host.Events.Should().Equal(
			"Path:None->DirectVulkan", "DeviceReady",
			"DeviceLost:The view was unloaded.", "ClearPresented", "Path:DirectVulkan->None",
			"Path:None->DirectVulkan", "DeviceReady");
		pixels.GetPixel(8, 9).Should().Be(new SKColor(0, 255, 0));
	}

	[Fact]
	public void the_manual_driver_renders_direct_frames_from_a_worker_thread_while_the_paint_reads_them()
	{
		//Arrange
		AttachAt(32, 32);
		_engine.ClearColor = SKColors.Blue;
		var rendered = 0;
		var stop = 0;
		Exception? failure = null;
		var worker = new Thread(() =>
		{
			try
			{
				while (Volatile.Read(ref stop) == 0)
				{
					if (_engine.OpenFrame(wait: true) == WebGpuFrameOutcome.Rendered)
					{
						_triangle?.Draw(_engine.Frame.Encoder, _engine.Frame.TextureView);
						_engine.EndFrame();
						Interlocked.Increment(ref rendered);
					}
				}
			}
			catch (Exception e)
			{
				failure = e;
			}
		}) { Name = "direct manual render thread" };

		//Act: the test thread is the head (paints and plays back) while the worker renders.
		worker.Start();
		var paints = 0;
		var deadline = DateTime.UtcNow.AddSeconds(1.5);
		SKColor centre = default;
		while (DateTime.UtcNow < deadline)
		{
			if (_host.DirectSource is not null)
			{
				using var pixels = Paint(32, 32);
				centre = pixels.GetPixel(16, 18);
				paints++;
			}
		}

		Volatile.Write(ref stop, 1);
		worker.Join();

		//Assert
		failure.Should().BeNull();
		rendered.Should().BeGreaterThan(10);
		paints.Should().BeGreaterThan(5);
		centre.Should().Be(new SKColor(0, 255, 0));
		Direct.ReadbackCount.Should().Be(0L);
	}

	[Fact]
	public void an_error_a_renderer_causes_on_the_adopted_device_is_caught_and_replaces_the_device()
	{
		//Arrange
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);
		var device = ((IWebGpuDeviceSource)_engine).Device;

		//Act: an invalid buffer on a device with no uncaptured-error handler would end the process without the
		// frame's error scope.
		_engine.OpenFrame(wait: true).Should().Be(WebGpuFrameOutcome.Rendered);
		var invalid = new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.Storage, Size = 64 };
		var buffer = Wgpu.DeviceCreateBuffer(device, &invalid);
		if (!buffer.IsNull)
		{
			Wgpu.BufferRelease(buffer);
		}

		_engine.EndFrame();
		var afterError = (_engine.IsDeviceReady, _host.LastLostReason);
		RenderOne(drawTriangle: false);

		//Assert
		afterError.IsDeviceReady.Should().BeFalse();
		afterError.LastLostReason.Should().StartWith("The WebGPU device reported an error and was replaced:");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
		_host.Events.Count(e => e == "DeviceReady").Should().Be(2);
	}

	[Fact]
	public void an_error_in_device_ready_on_the_adopted_device_is_caught()
	{
		//Arrange
		var failOnce = true;
		_host.OnReady = () =>
		{
			if (failOnce)
			{
				failOnce = false;
				var invalid = new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.Storage, Size = 64 };
				var buffer = Wgpu.DeviceCreateBuffer(((IWebGpuDeviceSource)_engine).Device, &invalid);
				if (!buffer.IsNull)
				{
					Wgpu.BufferRelease(buffer);
				}
			}
		};
		AttachAt(16, 16);

		//Act
		var first = _engine.OpenFrame(wait: true);
		RenderOne(drawTriangle: false);

		//Assert
		first.Should().Be(WebGpuFrameOutcome.NotReady);
		_host.LastLostReason.Should().StartWith("The WebGPU device reported an error and was replaced:");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
	}

	// ------------------------------------------------------------------ fallback to Readback

	[Fact]
	public void with_the_latch_off_the_view_runs_readback_even_with_a_vulkan_provider()
	{
		//Arrange
		using var off = DirectGpuLatch.Off();
		AttachAt(16, 16);

		//Act
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_engine.Device!.Route.Should().Be(WebGpuSkiaDeviceRoute.Owned);
		_host.DirectSource.Should().BeNull();
	}

	[Fact]
	public void the_adopted_device_reports_its_adapter_features_and_limits_the_same_way()
	{
		//Arrange
		AttachAt(16, 16);
		WebGpuDeviceInfo? atReady = null;
		_host.OnReady += () => atReady = _engine.DeviceInfo;

		//Act
		RenderOne(drawTriangle: false);
		var info = _engine.DeviceInfo;

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
		atReady.Should().BeSameAs(info);
		info!.Backend.Should().Be(BackendType.Vulkan);
		info.AdapterName.Should().NotBeNullOrEmpty();
		foreach (var feature in info.Features)
		{
			Wgpu.DeviceHasFeature(((IWebGpuDeviceSource)_engine).Device, feature).Should().Be(1u);
		}

		info.Limits.MaxTextureDimension2D.Should().BeGreaterThanOrEqualTo(8192u);
		info.AdapterFeatures.Should().NotBeEmpty();
		info.Features.Should().BeSubsetOf(info.AdapterFeatures);
	}

	[Fact]
	public void require_owned_device_runs_readback_on_an_owned_device_although_a_vulkan_provider_is_offered()
	{
		//Arrange
		_engine.RequireOwnedDevice = true;
		AttachAt(32, 32);
		_engine.ClearColor = new SKColor(200, 30, 40);

		//Act
		RenderOne(drawTriangle: true);

		//Assert
		Head.Provider.IsAvailable.Should().BeTrue();
		_host.Events.Should().Equal("Path:None->Readback", "DeviceReady");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_engine.Device!.Route.Should().Be(WebGpuSkiaDeviceRoute.Owned);
		((IWebGpuDeviceSource)_engine).HasUncapturedErrorHandler.Should().BeTrue();
		_engine.DeviceInfo.Should().NotBeNull();
		_host.DirectSource.Should().BeNull();
		_host.PixelAt(2, 2).Should().Be(new SKColor(200, 30, 40));
		_host.PixelAt(16, 18).Should().Be(new SKColor(0, 255, 0));
	}

	[Fact]
	public void switching_require_owned_device_replaces_the_device_at_the_next_frame_both_ways()
	{
		//Arrange
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);
		var adopted = _engine.RenderPath;

		//Act
		_engine.RequireOwnedDevice = true;
		RenderOne(drawTriangle: false);
		var owned = (_engine.RenderPath, _engine.Device!.Route);
		_engine.RequireOwnedDevice = false;
		RenderOne(drawTriangle: false);

		//Assert
		adopted.Should().Be(WebGpuRenderPath.DirectVulkan);
		owned.Should().Be((WebGpuRenderPath.Readback, WebGpuSkiaDeviceRoute.Owned));
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
		_host.Events.Should().Equal(
			"Path:None->DirectVulkan", "DeviceReady",
			"DeviceLost:Device options changed.", "ClearPresented", "Path:DirectVulkan->None", "Path:None->Readback", "DeviceReady",
			"DeviceLost:Device options changed.", "ClearPresented", "Path:Readback->None", "Path:None->DirectVulkan", "DeviceReady");
	}

	[Fact]
	public void changing_the_device_options_leaves_an_adopted_device_alone()
	{
		//Arrange
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);

		//Act
		_engine.DeviceOptions = new WebGpuSkiaDeviceOptions { PowerPreference = PowerPreference.LowPower };
		RenderOne(drawTriangle: false);

		//Assert
		_host.Events.Should().Equal("Path:None->DirectVulkan", "DeviceReady");
	}

	[Fact]
	public void no_provider_falls_back_to_readback()
	{
		//Arrange
		TestGpu.RequireAdapter();
		_engine.Attach(WebGpuDeviceGateDecision.Proceed, () => null);
		_engine.SetPixelSize(16, 16);
		_engine.ClearColor = SKColors.Red;

		//Act
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void an_adoption_the_library_refuses_falls_back_to_readback()
	{
		//Arrange: no loader entry point -> WebGpuSkiaDevice.AdoptVulkan reports unavailable with its reason.
		TestGpu.RequireAdapter();
		var provider = new TestNativeGpuDeviceProvider(Head.Provider) { HideProcAddr = true };
		AttachAt(16, 16, provider);
		_engine.ClearColor = SKColors.Red;

		//Act
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_engine.Device!.Route.Should().Be(WebGpuSkiaDeviceRoute.Owned);
		_host.Events.Should().Equal("Path:None->Readback", "DeviceReady");
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_provider_without_a_gpu_context_falls_back_to_readback()
	{
		//Arrange
		TestGpu.RequireAdapter();
		AttachAt(16, 16, new TestNativeGpuDeviceProvider(Head.Provider) { HideGrContext = true });

		//Act
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
	}

	[Fact]
	public void an_injected_adoption_refusal_falls_back_to_readback()
	{
		//Arrange
		TestGpu.RequireAdapter();
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.Adopt;
		AttachAt(16, 16);
		_engine.ClearColor = SKColors.Red;

		//Act
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_frame_texture_the_adopted_device_refuses_falls_back_to_readback_in_the_same_frame()
	{
		//Arrange
		TestGpu.RequireAdapter();
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.Target;
		AttachAt(16, 16);
		_engine.ClearColor = SKColors.Red;

		//Act
		var first = _engine.OpenFrame(wait: true);
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.None;
		RenderOne(drawTriangle: false);

		//Assert
		first.Should().Be(WebGpuFrameOutcome.NotReady);
		_host.Events.Take(5).Should().Equal(
			"Path:None->DirectVulkan", "DeviceReady",
			"DeviceLost:The DirectVulkan render path could not be used (the frame texture was refused (injected failure)) - Readback is used.",
			"ClearPresented", "Path:DirectVulkan->Readback");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_frame_the_paint_cannot_wrap_falls_back_to_readback_at_the_next_frame()
	{
		//Arrange
		TestGpu.RequireAdapter();
		AttachAt(16, 16);
		_engine.ClearColor = SKColors.Red;
		RenderOne(drawTriangle: false);

		//Act
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.Wrap;
		var wrapped = _host.DirectSource!.AcquirePaintImage();
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.None;
		RenderOne(drawTriangle: false);

		//Assert
		wrapped.Should().BeNull();
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_host.LastLostReason.Should().Contain("could not be wrapped");
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_fallback_holds_until_the_view_is_loaded_again()
	{
		//Arrange
		TestGpu.RequireAdapter();
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.Adopt;
		AttachAt(16, 16);
		RenderOne(drawTriangle: false);
		WebGpuDirectPresenter.InjectedFailure = WebGpuDirectFailurePoint.None;

		//Act: a lost device inside the same attach stays on Readback; a new attach tries the direct path again.
		_engine.Detach();
		_engine.Attach(WebGpuDeviceGateDecision.Proceed, _probe);
		RenderOne(drawTriangle: false);

		//Assert
		_host.Events.First().Should().Be("Path:None->Readback");
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
	}

	[Fact]
	public void a_provider_whose_device_is_not_created_yet_is_waited_for()
	{
		//Arrange: a Wayland window before its first frame.
		var provider = new TestNativeGpuDeviceProvider(Head.Provider) { Available = false };
		AttachAt(16, 16, provider);

		//Act
		var whileWaiting = _engine.OpenFrame(wait: true);
		provider.Available = true;
		RenderOne(drawTriangle: false);

		//Assert
		whileWaiting.Should().Be(WebGpuFrameOutcome.NotReady);
		_engine.IsDeviceReady.Should().BeTrue();
		_engine.RenderPath.Should().Be(WebGpuRenderPath.DirectVulkan);
	}

	[Fact]
	public void a_provider_that_never_gets_a_device_is_given_up_on_for_readback()
	{
		//Arrange
		TestGpu.RequireAdapter();
		var provider = new TestNativeGpuDeviceProvider(Head.Provider) { Available = false };
		AttachAt(16, 16, provider);

		//Act
		var whileWaiting = _engine.OpenFrame(wait: true);
		Thread.Sleep(TimeSpan.FromMilliseconds(WebGpuRenderViewRules.DirectProviderWaitMilliseconds + 100));
		RenderOne(drawTriangle: false);

		//Assert
		whileWaiting.Should().Be(WebGpuFrameOutcome.NotReady);
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
	}

	[Fact]
	public void a_head_device_that_goes_away_falls_back_to_readback_without_touching_it()
	{
		//Arrange
		TestGpu.RequireAdapter();
		var provider = new TestNativeGpuDeviceProvider(Head.Provider);
		AttachAt(16, 16, provider);
		RenderOne(drawTriangle: false);
		_engine.ClearColor = SKColors.Red;

		//Act: the window closes (the provider reports unavailable): the adopted objects are left alone.
		provider.Available = false;
		RenderOne(drawTriangle: false);

		//Assert
		_engine.RenderPath.Should().Be(WebGpuRenderPath.Readback);
		_host.LastLostReason.Should().Contain("DirectVulkan");
		_host.PixelAt(8, 8).Should().Be(SKColors.Red);
	}

	[Fact]
	public void the_vulkan_usage_flags_match_the_engines_image_usages()
	{
		//Arrange
		var usage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopySrc | TextureUsage.CopyDst;

		//Act
		var flags = WebGpuVulkanDirectPresenter.VulkanUsage(usage);

		//Assert
		flags.Should().Be(0x1u | 0x2u | 0x4u | 0x10u);
		WebGpuVulkanDirectPresenter.VulkanUsage(TextureUsage.StorageBinding).Should().Be(0x8u);
		WebGpuVulkanDirectPresenter.VulkanUsage(TextureUsage.None).Should().Be(0u);
	}
}
