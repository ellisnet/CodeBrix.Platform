using System;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Support;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using CodeBrix.Platform.UI.WebGpu;
using Reqnroll;
using SilverAssertions;
using SkiaSharp;
using UIColors = CodeBrix.Platform.UI.Core.UIReqs.Support.Colors;

namespace CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Steps;

/// <summary>
/// The direct render path's vocabulary: the application-wide direct GPU rendering latch, a window that offers a head's
/// Vulkan device (see <see cref="HeadVulkanDevice"/> for why that device is the headless twin of the X11 / Wayland
/// heads' own), and the view's picture read through its own capture.
/// </summary>
[Binding]
public sealed class WebGpuDirectSteps
{
	/// <summary>The prerequisite name of <c>@needs-head-vulkan</c>: a Vulkan device a head could render with.</summary>
	public const string HeadVulkanPrerequisite = "head-vulkan";

	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

	/// <summary>Registers the window-provider builder once, and finds out whether a head Vulkan device can be made.</summary>
	[BeforeTestRun(Order = 20)]
	public static void Register_the_window_provider()
	{
		HeadVulkanDevice.EnsureRegistered();
		HeadVulkanDevice.SetLatch(true);
		try
		{
			if (!HeadVulkanDevice.TryOffer(out var reason))
			{
				Prerequisite.Missing(HeadVulkanPrerequisite, "this machine has no Vulkan device for a head: " + reason);
			}
		}
		finally
		{
			HeadVulkanDevice.Withdraw();
			HeadVulkanDevice.SetLatch(false);
		}
	}

	/// <summary>Every scenario ends with the latch off and no device offered (the panel was emptied first).</summary>
	[AfterScenario(Order = 200)]
	public static void Turn_direct_rendering_off_again()
	{
		HeadVulkanDevice.Withdraw();
		HeadVulkanDevice.SetLatch(false);
	}

	/// <summary>Turns the application-wide direct GPU rendering latch on (UseDirectGpuRenderingMode()).</summary>
	[Given("the application uses direct GPU rendering")]
	public static void Given_the_application_uses_direct_GPU_rendering() => HeadVulkanDevice.SetLatch(true);

	/// <summary>Makes every window offer the head's Vulkan device from now on.</summary>
	[Given("the window offers the head's Vulkan device")]
	public static void Given_the_window_offers_the_heads_Vulkan_device()
	{
		HeadVulkanDevice.TryOffer(out var reason).Should().BeTrue("a head Vulkan device is required here: {0}", reason);
	}

	/// <summary>
	/// Asserts, within a budget, that the view's own capture of its picture (the frame image the paint draws, sampled
	/// on the head's device for a direct path) shows at least a share of a colour.
	/// </summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="percent">The share, 0-100.</param>
	/// <param name="color">The colour.</param>
	/// <param name="milliseconds">The budget.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the picture of {string} shows at least {int} percent {string} within {int} milliseconds")]
	public async Task Then_the_picture_of_shows_at_least_percent_within(string name, int percent, string color, int milliseconds)
	{
		var view = ElementRegistry.Resolve(name) as WebGpuRenderView
			?? throw new NotSupportedException($"\"{name}\" is not a WebGpuRenderView.");
		var expected = UIColors.Parse(GherkinValue.Unquote(color));
		var expectedSk = new SKColor(expected.R, expected.G, expected.B, expected.A);
		double share = 0;
		var reached = await Poll.UntilAsync(() =>
		{
			share = ShareOf(view, expectedSk);
			return Task.FromResult(share * 100.0 >= percent);
		}, TimeSpan.FromMilliseconds(milliseconds), PollInterval).ConfigureAwait(false);

		reached.Should().BeTrue("the picture of \"{0}\" must show at least {1} percent {2}; it showed {3:0.0} percent",
			name, percent, color, share * 100.0);
	}

	private static double ShareOf(WebGpuRenderView view, SKColor expected)
	{
		using var picture = view.Surface.CaptureFrame();
		if (picture is null)
		{
			return 0;
		}

		using var bitmap = new SKBitmap(new SKImageInfo(picture.Width, picture.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
		if (!picture.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
		{
			return 0;
		}

		var matches = 0;
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (ColorMatch.Matches(bitmap.GetPixel(x, y), expected))
				{
					matches++;
				}
			}
		}

		return matches / (double)(bitmap.Width * bitmap.Height);
	}
}
