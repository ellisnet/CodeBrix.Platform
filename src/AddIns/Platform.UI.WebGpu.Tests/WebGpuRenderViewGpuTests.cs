using CodeBrix.Platform.UI.WebGpu.Internal;
using Microsoft.UI;
using SilverAssertions;
using SkiaSharp;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The element over this machine's real WebGPU device (Readback): the frame the Continuous driver leaves in flight is
/// presented when continuous ticking stops - the driver changes, the view is paused, the view leaves the tree. The view
/// is constructed host-free (never loaded), so its engine is attached and its frames are ended here exactly as its
/// built-in driver ends them. Every test skips with the engine's reason where there is no adapter.
/// </summary>
public sealed class WebGpuRenderViewGpuTests
{
	private static readonly Color[] Colours = { Colors.Red, Colors.Lime, Colors.Blue };

	// A view with a device and two frames ended as the Continuous driver ends them: the first presented, the second in
	// flight.
	private static WebGpuRenderView ViewWithAFrameInFlight()
	{
		TestGpu.RequireAdapter();
		var view = new WebGpuRenderView();
		view.Engine.Attach(WebGpuDeviceGateDecision.Proceed, null);
		view.Engine.SetPixelSize(16, 16);
		for (var i = 0; i < 2; i++)
		{
			view.ClearColor = Colours[i];
			view.Engine.OpenFrame(wait: true).Should().Be(WebGpuFrameOutcome.Rendered);
			view.Engine.EndFrame(WebGpuRenderEngine.MayDeferPresent(view.Scheduler));
		}

		view.Surface.FramesPresented.Should().Be(1);
		((WebGpuReadbackPresenter)view.Engine.Presenter!).HasPendingFrame.Should().BeTrue();
		return view;
	}

	private static SKColor PresentedPixel(WebGpuRenderView view)
	{
		using var captured = view.Surface.CaptureFrame();
		captured.Should().NotBeNull();
		using var bitmap = new SKBitmap(new SKImageInfo(captured!.Width, captured.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
		captured.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0).Should().BeTrue();
		return bitmap.GetPixel(8, 8);
	}

	[Fact]
	public void leaving_the_continuous_driver_presents_the_frame_in_flight()
	{
		//Arrange
		var view = ViewWithAFrameInFlight();

		//Act
		view.FrameDriver = WebGpuFrameDriver.OnDemand;

		//Assert
		view.Surface.FramesPresented.Should().Be(2);
		PresentedPixel(view).Should().Be(SKColors.Lime);
		((WebGpuReadbackPresenter)view.Engine.Presenter!).HasPendingFrame.Should().BeFalse();
		view.LeaveTree();
	}

	[Fact]
	public void pausing_presents_the_frame_in_flight()
	{
		//Arrange
		var view = ViewWithAFrameInFlight();

		//Act
		view.IsPaused = true;

		//Assert
		view.Surface.FramesPresented.Should().Be(2);
		PresentedPixel(view).Should().Be(SKColors.Lime);
		view.LeaveTree();
	}

	[Fact]
	public void leaving_the_tree_presents_the_frame_in_flight_before_the_device_is_torn_down()
	{
		//Arrange
		var view = ViewWithAFrameInFlight();
		long presentedAtDeviceLost = -1;
		SKColor? pixelAtDeviceLost = null;
		view.DeviceLost += (_, _) =>
		{
			presentedAtDeviceLost = view.Surface.FramesPresented;
			pixelAtDeviceLost = PresentedPixel(view);
		};

		//Act
		view.LeaveTree();

		//Assert: the flush came first - when DeviceLost was raised the in-flight frame was already on screen.
		presentedAtDeviceLost.Should().Be(2);
		pixelAtDeviceLost.Should().Be(SKColors.Lime);
		view.IsDeviceReady.Should().BeFalse();
		view.Surface.HasFrame.Should().BeFalse();
	}

	[Fact]
	public void frames_of_the_other_drivers_are_presented_inside_their_own_end_frame()
	{
		//Arrange
		TestGpu.RequireAdapter();
		var view = new WebGpuRenderView { FrameDriver = WebGpuFrameDriver.OnDemand };
		view.Engine.Attach(WebGpuDeviceGateDecision.Proceed, null);
		view.Engine.SetPixelSize(16, 16);

		//Act: the OnDemand built-in frame, then a Manual frame through the public protocol.
		view.ClearColor = Colors.Red;
		view.Engine.OpenFrame(wait: true).Should().Be(WebGpuFrameOutcome.Rendered);
		view.Engine.EndFrame(WebGpuRenderEngine.MayDeferPresent(view.Scheduler));
		var afterOnDemand = view.Surface.FramesPresented;
		view.FrameDriver = WebGpuFrameDriver.Manual;
		view.ClearColor = Colors.Blue;
		view.BeginFrame().Should().NotBeNull();
		view.EndFrame();

		//Assert
		afterOnDemand.Should().Be(1);
		view.Surface.FramesPresented.Should().Be(2);
		PresentedPixel(view).Should().Be(SKColors.Blue);
		view.LeaveTree();
	}
}
