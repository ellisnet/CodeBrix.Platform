using System;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>The element's pure rules: frame timing, target size, direct-path eligibility.</summary>
public class WebGpuRenderViewRulesTests
{
	private const long Frequency = 1_000_000; // a microsecond clock keeps the arithmetic readable

	[Theory]
	[InlineData(60.0, 16_667)]
	[InlineData(120.0, 8_333)]
	[InlineData(30.0, 33_333)]
	[InlineData(1.0, 1_000_000)]
	public void the_frame_period_is_one_second_divided_by_the_target_rate(double rate, long expected)
	{
		//Act
		var period = WebGpuRenderViewRules.FramePeriodTicks(rate, Frequency);

		//Assert
		period.Should().Be(expected);
	}

	[Fact]
	public void a_target_rate_of_zero_means_no_period_at_all()
	{
		//Act
		var period = WebGpuRenderViewRules.FramePeriodTicks(0.0, Frequency);

		//Assert
		period.Should().Be(0L);
	}

	[Theory]
	[InlineData(-1.0)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public void an_invalid_target_rate_is_refused(double rate)
	{
		//Act
		Action act = () => WebGpuRenderViewRules.FramePeriodTicks(rate, Frequency);

		//Assert
		WebGpuRenderViewRules.IsValidTargetFrameRate(rate).Should().BeFalse();
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void the_next_deadline_is_one_period_after_the_previous_deadline_when_on_time()
	{
		//Arrange
		//The frame that was due at 1000 finished at 1005: the next is due at 1000 + 16667, not 1005 + 16667.

		//Act
		var next = WebGpuRenderViewRules.NextDeadline(1_000, 1_005, 16_667);

		//Assert
		next.Should().Be(17_667);
	}

	[Fact]
	public void a_late_frame_makes_the_next_one_due_now_and_drops_the_missed_ones()
	{
		//Arrange
		//Due at 1000, finished at 100000: five periods were missed. Latest wins - ONE frame, now.

		//Act
		var next = WebGpuRenderViewRules.NextDeadline(1_000, 100_000, 16_667);

		//Assert
		next.Should().Be(100_000);
	}

	[Fact]
	public void with_no_period_the_next_frame_is_always_due_now()
	{
		//Act
		var next = WebGpuRenderViewRules.NextDeadline(1_000, 2_000, 0);

		//Assert
		next.Should().Be(2_000);
	}

	[Theory]
	[InlineData(200.0, 100.0, 1.0, 200, 100)]
	[InlineData(200.0, 100.0, 1.5, 300, 150)]
	[InlineData(100.4, 50.6, 1.0, 100, 51)]
	[InlineData(0.0, 100.0, 1.0, 0, 0)]
	[InlineData(100.0, 0.2, 1.0, 0, 0)]
	[InlineData(10_000.0, 100.0, 1.0, 8192, 100)]
	[InlineData(200.0, 100.0, double.NaN, 200, 100)]
	public void the_target_size_is_the_element_size_times_the_scale_in_whole_pixels(double width, double height, double scale, int expectedWidth, int expectedHeight)
	{
		//Act
		var (w, h) = WebGpuRenderViewRules.TargetPixelSize(width, height, scale);

		//Assert
		w.Should().Be(expectedWidth);
		h.Should().Be(expectedHeight);
	}

	[Theory]
	[InlineData(false, NativeGpuDeviceKind.Vulkan, true, false)]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, true, true)]
	[InlineData(true, NativeGpuDeviceKind.Metal, true, true)]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, false, false)]
	public void a_direct_path_is_eligible_only_with_the_latch_on_and_an_available_provider(bool latch, NativeGpuDeviceKind kind, bool available, bool expected)
	{
		//Act
		var eligible = WebGpuRenderViewRules.IsDirectPathEligible(latch, kind, available);

		//Assert
		eligible.Should().Be(expected);
	}

	[Fact]
	public void no_provider_is_never_eligible()
	{
		//Act
		var eligible = WebGpuRenderViewRules.IsDirectPathEligible(true, null, true);

		//Assert
		eligible.Should().BeFalse();
	}

	[Fact]
	public void an_eligible_provider_without_a_direct_adapter_runs_readback()
	{
		//Act
		var path = WebGpuRenderViewRules.ChooseRenderPath(true, NativeGpuDeviceKind.Vulkan, directAdapterExists: false);

		//Assert
		path.Should().Be(WebGpuRenderPath.Readback);
	}

	[Theory]
	[InlineData(NativeGpuDeviceKind.Vulkan, WebGpuRenderPath.DirectVulkan)]
	[InlineData(NativeGpuDeviceKind.Metal, WebGpuRenderPath.DirectMetal)]
	public void an_eligible_provider_with_a_direct_adapter_runs_its_direct_path(NativeGpuDeviceKind kind, WebGpuRenderPath expected)
	{
		//Act
		var path = WebGpuRenderViewRules.ChooseRenderPath(true, kind, directAdapterExists: true);

		//Assert
		path.Should().Be(expected);
	}

	[Fact]
	public void readback_is_the_fallback_whenever_the_provider_is_not_eligible()
	{
		//Act
		var path = WebGpuRenderViewRules.ChooseRenderPath(false, NativeGpuDeviceKind.Vulkan, directAdapterExists: true);

		//Assert
		path.Should().Be(WebGpuRenderPath.Readback);
	}

	[Fact]
	public void the_no_direct_path_notice_says_readback_is_used()
	{
		//Act
		var message = WebGpuRenderViewRules.DirectPathNotImplementedMessage(NativeGpuDeviceKind.Metal);

		//Assert
		message.Should().Contain("no direct path for it on this platform - Readback is used.");
		message.Should().Contain("Metal");
	}

	[Fact]
	public void the_fallback_line_names_the_path_and_the_reason()
	{
		//Act
		var message = WebGpuRenderViewRules.DirectPathFallbackMessage(WebGpuRenderPath.DirectVulkan, "adoption refused");

		//Assert
		message.Should().Be("The DirectVulkan render path could not be used (adoption refused) - Readback is used.");
	}

	[Theory]
	[InlineData(NativeGpuDeviceKind.Vulkan, false, true)]
	[InlineData(NativeGpuDeviceKind.Vulkan, true, true)]
	[InlineData(NativeGpuDeviceKind.Metal, true, true)]
	[InlineData(NativeGpuDeviceKind.Metal, false, false)]
	public void a_direct_adapter_exists_for_vulkan_everywhere_and_for_metal_on_macos_only(NativeGpuDeviceKind kind, bool isMacOS, bool expected)
	{
		//Act
		var exists = WebGpuRenderViewRules.DirectAdapterExists(kind, isMacOS);

		//Assert
		exists.Should().Be(expected);
	}

	[Theory]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, false, 0.0, true)]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, false, 1999.0, true)]
	[InlineData(true, NativeGpuDeviceKind.Metal, false, 10.0, true)]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, false, 2000.0, false)]
	[InlineData(true, NativeGpuDeviceKind.Vulkan, true, 10.0, false)]
	[InlineData(false, NativeGpuDeviceKind.Vulkan, false, 10.0, false)]
	[InlineData(true, null, false, 10.0, false)]
	public void the_first_device_waits_briefly_only_for_an_announced_device_with_the_latch_on(bool latch, NativeGpuDeviceKind? kind, bool available, double elapsed, bool expected)
	{
		//Act
		var wait = WebGpuRenderViewRules.ShouldWaitForProvider(latch, kind, available, elapsed);

		//Assert
		wait.Should().Be(expected);
	}

	[Fact]
	public void the_provider_wait_is_two_seconds()
	{
		//Assert
		WebGpuRenderViewRules.DirectProviderWaitMilliseconds.Should().Be(2000.0);
	}

	[Fact]
	public void the_defaults_are_sixty_frames_a_second_and_a_half_second_settle()
	{
		//Assert
		WebGpuRenderViewRules.DefaultTargetFrameRate.Should().Be(60.0);
		WebGpuRenderViewRules.ResizeSettleMilliseconds.Should().Be(500.0);
	}

	[Theory]
	[InlineData(0, 0, false)]
	[InlineData(0, 480, false)]
	[InlineData(640, 0, false)]
	[InlineData(640, 480, true)]
	[InlineData(1, 1, true)]
	public void only_a_change_from_an_existing_size_is_a_resize(double previousWidth, double previousHeight, bool resize)
	{
		// The first layout after loading comes from no size; it must not hide the first half second of frames.
		WebGpuRenderViewRules.IsResize(previousWidth, previousHeight).Should().Be(resize);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, true, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	public void a_head_device_may_be_adopted_only_with_the_latch_and_without_require_owned_device(bool latch, bool requireOwned, bool expected)
	{
		//Assert
		WebGpuRenderViewRules.MayAdoptHeadDevice(latch, requireOwned).Should().Be(expected);
	}

	[Theory]
	[InlineData(WebGpuRenderPath.None, false)]
	[InlineData(WebGpuRenderPath.Readback, true)]
	[InlineData(WebGpuRenderPath.DirectVulkan, false)]
	[InlineData(WebGpuRenderPath.DirectMetal, false)]
	[InlineData(WebGpuRenderPath.DirectSurface, false)]
	public void a_device_options_change_replaces_only_an_owned_device(WebGpuRenderPath path, bool expected)
	{
		//Assert
		WebGpuRenderViewRules.DeviceOptionsChangeRecreatesDevice(path).Should().Be(expected);
	}

	[Theory]
	[InlineData(false, WebGpuRenderPath.Readback, false)]
	[InlineData(false, WebGpuRenderPath.None, false)]
	[InlineData(true, WebGpuRenderPath.None, false)]
	[InlineData(true, WebGpuRenderPath.Readback, true)]
	[InlineData(true, WebGpuRenderPath.DirectVulkan, true)]
	public void a_require_owned_device_change_replaces_the_device_only_with_the_latch_on(bool latch, WebGpuRenderPath path, bool expected)
	{
		//Assert
		WebGpuRenderViewRules.RequireOwnedDeviceChangeRecreatesDevice(latch, path).Should().Be(expected);
	}

	[Fact]
	public void the_device_options_reason_and_the_owned_device_line_are_fixed()
	{
		//Assert
		WebGpuRenderViewRules.DeviceOptionsChangedReason.Should().Be("Device options changed.");
		WebGpuRenderViewRules.RequireOwnedDeviceMessage.Should().Contain("RequireOwnedDevice").And.EndWith("Readback is used.");
	}
}
