using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.X11;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the X11 head's "try Vulkan first?" decision: explicit choices always decide, the
/// DirectGpuRenderingMode latch prefers Vulkan only when the application chose nothing, and with the latch off
/// the selection is exactly what it was before (OpenGL by default).
/// </summary>
public class X11RendererSelectionTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData(true, false)]
	[InlineData(false, false)]
	[InlineData(null, true)]
	public void with_the_latch_off_and_no_vulkan_choice_vulkan_is_never_tried(bool? useOpenGL, bool preferGles)
	{
		//Act
		var tryVulkan = X11RendererSelection.ShouldTryVulkan(null, useOpenGL, preferGles, directGpuRenderingEnabled: false);

		//Assert
		tryVulkan.Should().BeFalse();
	}

	[Fact]
	public void with_the_latch_on_and_no_renderer_choice_vulkan_is_tried_first()
	{
		//Act
		var tryVulkan = X11RendererSelection.ShouldTryVulkan(null, null, false, directGpuRenderingEnabled: true);

		//Assert
		tryVulkan.Should().BeTrue();
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, false)]
	[InlineData(null, true)]
	public void with_the_latch_on_an_explicit_other_renderer_choice_wins(bool? useOpenGL, bool preferGles)
	{
		//Act
		var tryVulkan = X11RendererSelection.ShouldTryVulkan(null, useOpenGL, preferGles, directGpuRenderingEnabled: true);

		//Assert
		tryVulkan.Should().BeFalse();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void an_explicit_vulkan_choice_decides_whatever_the_latch_says(bool latch)
	{
		//Assert
		X11RendererSelection.ShouldTryVulkan(true, null, false, latch).Should().BeTrue();
		X11RendererSelection.ShouldTryVulkan(true, false, true, latch).Should().BeTrue();
		X11RendererSelection.ShouldTryVulkan(false, null, false, latch).Should().BeFalse();
	}

	[Fact]
	public void the_backend_enum_values_are_pinned()
	{
		//Assert
		((int)X11RenderingBackend.Default).Should().Be(0);
		((int)X11RenderingBackend.Vulkan).Should().Be(1);
		((int)X11RenderingBackend.OpenGL).Should().Be(2);
		((int)X11RenderingBackend.OpenGLES).Should().Be(3);
		((int)X11RenderingBackend.Software).Should().Be(4);
	}

	[Theory]
	[InlineData(X11RenderingBackend.Vulkan, true, null, false)]
	[InlineData(X11RenderingBackend.OpenGL, false, true, false)]
	[InlineData(X11RenderingBackend.OpenGLES, false, true, true)]
	[InlineData(X11RenderingBackend.Software, false, false, false)]
	[InlineData(X11RenderingBackend.Default, null, null, false)]
	public void the_builder_backend_maps_to_the_feature_flags(X11RenderingBackend backend, bool? useVulkan, bool? useOpenGL, bool preferGles)
	{
		var saved = (FeatureConfiguration.Rendering.UseVulkanOnX11, FeatureConfiguration.Rendering.UseOpenGLOnX11, FeatureConfiguration.Rendering.PreferGLESOverGLOnX11);
		try
		{
			//Arrange
			FeatureConfiguration.Rendering.UseVulkanOnX11 = null;
			FeatureConfiguration.Rendering.UseOpenGLOnX11 = null;
			FeatureConfiguration.Rendering.PreferGLESOverGLOnX11 = false;

			//Act
			X11HostBuilder.ApplyRenderingBackend(backend);

			//Assert
			FeatureConfiguration.Rendering.UseVulkanOnX11.Should().Be(useVulkan);
			FeatureConfiguration.Rendering.UseOpenGLOnX11.Should().Be(useOpenGL);
			FeatureConfiguration.Rendering.PreferGLESOverGLOnX11.Should().Be(preferGles);
		}
		finally
		{
			(FeatureConfiguration.Rendering.UseVulkanOnX11, FeatureConfiguration.Rendering.UseOpenGLOnX11, FeatureConfiguration.Rendering.PreferGLESOverGLOnX11) = saved;
		}
	}

	[Fact]
	public void the_vulkan_feature_flag_defaults_to_no_choice()
	{
		//Assert
		//Null is what lets the DirectGpuRenderingMode latch decide; a bool default would make every app "choose".
		FeatureConfiguration.Rendering.UseVulkanOnX11.Should().BeNull();
	}
}
