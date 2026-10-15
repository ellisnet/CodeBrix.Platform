using System;
using CodeBrix.WebGpu.Skia;
using Xunit;

namespace CodeBrix.Platform.PlayTest.WebGpu.Tests;

/// <summary>
/// Whether this machine has a WebGPU adapter, asked once per process. Every GPU test calls <see cref="RequireAdapter"/>
/// first, so a machine without one reports the tests as skipped WITH the engine's own reason instead of failing.
/// </summary>
internal static class TestGpu
{
	private static readonly Lazy<string?> MissingReason = new(Probe);

	/// <summary>Skips the calling test when no WebGPU device can be created here.</summary>
	internal static void RequireAdapter()
	{
		var reason = MissingReason.Value;
		Assert.SkipWhen(reason is not null, "No WebGPU adapter on this machine: " + reason);
	}

	/// <summary>A device factory that always fails: a backend this platform's native library does not have.</summary>
	/// <returns>An unavailable device, with the engine's reason.</returns>
	internal static WebGpuSkiaDevice CreateAbsentDevice() =>
		WebGpuSkiaDevice.Create(new WebGpuSkiaDeviceOptions
		{
			PreferredBackend = OperatingSystem.IsMacOS() ? WebGpuSkiaBackend.Direct3D12 : WebGpuSkiaBackend.Metal,
			AllowFallbackBackend = false,
		});

	private static string? Probe()
	{
		using var device = WebGpuSkiaDevice.Create();
		return device.IsAvailable ? null : device.Reason ?? "no reason given";
	}
}
