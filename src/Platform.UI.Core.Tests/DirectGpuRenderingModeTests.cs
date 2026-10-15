#nullable enable

using System.Reflection;
using CodeBrix.Platform.UI.Hosting;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The opt-in direct GPU rendering latch: off unless the application asks, one-way, and set only by the
/// host-builder call.
/// </summary>
/// <remarks>
/// The latch is process-wide; this suite runs one test at a time (AssemblyInfo.cs) and every test that turns
/// it on restores it in a finally (<see cref="DirectGpuRenderingModeSwitch"/>).
/// </remarks>
public class DirectGpuRenderingModeTests
{
	[Fact]
	public void the_mode_is_off_unless_an_application_turns_it_on()
	{
		//Assert
		DirectGpuRenderingMode.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void the_shipped_latch_is_still_one_way_and_internal()
	{
		//Arrange
		//The suite reaches around the latch by reflection, so it owes a check that the SHIPPED shape has not
		//quietly changed: no public setter, and Enable() not public.
		var type = typeof(DirectGpuRenderingMode);

		//Act
		var isEnabled = type.GetProperty(nameof(DirectGpuRenderingMode.IsEnabled));
		var enable = type.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);

		//Assert
		isEnabled.Should().NotBeNull();
		isEnabled!.SetMethod.Should().BeNull();
		enable.Should().NotBeNull();
		enable!.IsPublic.Should().BeFalse();
		type.GetMethod("Disable").Should().BeNull();
	}

	[Fact]
	public void the_host_builder_call_turns_the_mode_on_and_returns_the_same_builder()
	{
		DirectGpuRenderingModeSwitch.Restoring(() =>
		{
			//Arrange
			var builder = CodeBrixPlatformHostBuilder.Create();

			//Act
			var returned = builder.UseDirectGpuRenderingMode();

			//Assert
			returned.Should().BeSameAs(builder);
			DirectGpuRenderingMode.IsEnabled.Should().BeTrue();
		});
	}

	[Fact]
	public void calling_the_host_builder_twice_keeps_the_mode_on()
	{
		DirectGpuRenderingModeSwitch.Restoring(() =>
		{
			//Act
			CodeBrixPlatformHostBuilder.Create().UseDirectGpuRenderingMode().UseDirectGpuRenderingMode();

			//Assert
			DirectGpuRenderingMode.IsEnabled.Should().BeTrue();
		});
	}

	[Fact]
	public void the_direct_gpu_latch_is_independent_of_the_direct_skia_canvas_latch()
	{
		DirectGpuRenderingModeSwitch.Enabled(() =>
		{
			//Assert
			//Two separate opt-ins: turning one on must not turn the other on.
			DirectSkiaCanvasMode.IsEnabled.Should().BeFalse();
		});
	}

	[Fact]
	public void the_mode_is_off_again_after_a_test_that_turned_it_on()
	{
		//Arrange
		DirectGpuRenderingModeSwitch.Enabled(() => { });

		//Assert
		//The guarantee the rest of the suite rests on.
		DirectGpuRenderingMode.IsEnabled.Should().BeFalse();
	}
}
