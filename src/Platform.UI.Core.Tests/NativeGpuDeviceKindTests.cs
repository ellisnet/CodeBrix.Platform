#nullable enable

using System;
using CodeBrix.Platform.Graphics;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The native GPU device provider's kind enum is platform-neutral: it lives in the Core assembly (no SkiaSharp), so a
/// Core-only consumer - an add-in's Core rules, CodeBrix.Android - can name it, while the provider interface itself
/// (which carries a GRContext) stays in the Skia assembly. Same split as SkiaGpuBackend.
/// </summary>
public class NativeGpuDeviceKindTests
{
	[Fact]
	public void the_kind_enum_lives_in_the_core_assembly_beside_skia_gpu_backend()
	{
		//Assert
		typeof(NativeGpuDeviceKind).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		typeof(NativeGpuDeviceKind).Assembly.Should().BeSameAs(typeof(SkiaGpuBackend).Assembly);
		typeof(NativeGpuDeviceKind).IsPublic.Should().BeTrue();
	}

	[Fact]
	public void the_kind_enum_has_exactly_vulkan_and_metal()
	{
		//Assert
		Enum.GetNames<NativeGpuDeviceKind>().Should().Equal("Vulkan", "Metal");
	}

	[Fact]
	public void the_provider_interface_is_not_in_the_core_assembly()
	{
		//Assert
		//It names SkiaSharp's GRContext, so it must stay in the Skia build (a .skia.cs file).
		typeof(NativeGpuDeviceKind).Assembly.GetType("CodeBrix.Platform.Graphics.INativeGpuDeviceProvider").Should().BeNull();
	}
}
