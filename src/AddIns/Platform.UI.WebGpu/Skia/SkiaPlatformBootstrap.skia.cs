using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.UI.WebGpu.Internal;
using CodeBrix.Platform.UI.WebGpu.Contracts;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.WebGpu.Skia;

/// <summary>
/// Registers this assembly's implementations of the WebGpu add-in's platform contracts with
/// <see cref="ApiExtensibility"/> (the frame timer the Core's scheduler runs on). Runs as a module initializer, so the
/// registrations exist before any code of this assembly runs; the Core assembly's <see cref="PlatformContract"/> runs
/// it when it first needs a contract. Also registers the WebGPU engine's adopt-requirements source for the heads'
/// Vulkan devices (<see cref="VulkanAdoptionProfile"/>), which loads this type by its assembly-qualified name - a
/// rename here must follow there (fenced by a test).
/// </summary>
internal static class SkiaPlatformBootstrap
{
	private static readonly object _gate = new();
	private static bool _registered;

	//A module initializer is the one place that runs before the first use of any type in this assembly,
	//which is exactly when the contracts must be in place (CA2255 is aimed at application-level code).
#pragma warning disable CA2255
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Initialize() => EnsureRegistered();

	/// <summary>
	/// Registers every Skia contract implementation of this assembly. Safe to call more than once.
	/// </summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			//The frame timer, one per view (the owner is the view).
			if (!ApiExtensibility.IsRegistered<IWebGpuFrameTimer>())
			{
				ApiExtensibility.Register<FrameworkElement>(typeof(IWebGpuFrameTimer), owner => new WebGpuFrameTimerSkiaPlatform(owner));
			}

			//The WebGPU engine's adopt-requirements source: what the heads' Vulkan devices enable while direct GPU
			//rendering is on. The X11 head usually creates its device before this assembly has loaded; it then loads
			//this assembly by the name VulkanAdoptionProfile.AddInBootstrapTypeName gives and runs this initializer.
			VulkanAdoptionProfile.RequirementsSource ??= new WebGpuVulkanAdoptRequirementsSource();

			_registered = true;
		}
	}
}
