using System;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// What the head's owned-GPU-device gate said about this view's WebGPU device: create it, or report it unavailable.
/// </summary>
/// <param name="MayCreate">True when the view may create its own device.</param>
/// <param name="Reason">Why it may not (null when it may).</param>
/// <param name="ProviderState">The gate provider's opaque object (accepted, not used yet), or null.</param>
/// <param name="Failed">True when the gate threw: the launch cannot serve WebGPU at all and its owner has recorded
/// the failure (a test head missing its provider package).</param>
internal readonly record struct WebGpuDeviceGateDecision(bool MayCreate, string? Reason, object? ProviderState, bool Failed)
{
	/// <summary>No gate, or a gate that lets the view create its device.</summary>
	internal static WebGpuDeviceGateDecision Proceed { get; } = new(true, null, null, false);
}

/// <summary>
/// Asks the head's <see cref="IOwnedGpuDeviceGate"/> (resolved from the view's XamlRoot) whether the view may create
/// its own WebGPU device - BEFORE it creates one. Every desktop head registers no gate, so the view proceeds; a test
/// head registers one per launch (the PlayTest head's <c>PlayTestOptions.WebGpu</c>).
/// </summary>
/// <remarks>
/// The OpenGL elements' equivalent is their per-context <c>INativeOpenGLWrapper</c> resolution, which the PlayTest
/// head answers; like them, a gate that THROWS is an initialization failure the head has already recorded as the
/// test's failure, so it is swallowed here and the view simply reports that it has no device.
/// </remarks>
internal static class WebGpuDeviceGate
{
	/// <summary>Resolves the gate registered for <paramref name="xamlRoot"/> (if any) and asks it.</summary>
	/// <param name="xamlRoot">The view's XamlRoot.</param>
	/// <returns>The decision.</returns>
	internal static WebGpuDeviceGateDecision Resolve(object xamlRoot)
	{
		IOwnedGpuDeviceGate? gate;
		try
		{
			ApiExtensibility.CreateInstance(xamlRoot, out gate);
		}
		catch (Exception e)
		{
			return new(false, e.Message, null, true);
		}

		return Evaluate(gate);
	}

	/// <summary>Asks <paramref name="gate"/> about WebGPU.</summary>
	/// <param name="gate">The gate, or null when the head registered none.</param>
	/// <returns>The decision.</returns>
	internal static WebGpuDeviceGateDecision Evaluate(IOwnedGpuDeviceGate? gate)
	{
		if (gate is null)
		{
			return WebGpuDeviceGateDecision.Proceed;
		}

		try
		{
			var reason = gate.GetUnavailableReason(OwnedGpuDeviceApis.WebGpu);
			if (reason is not null)
			{
				return new(false, reason, null, false);
			}

			// WP4's provider decides what this object is; until then it is accepted and ignored.
			return new(true, null, gate.GetProviderState(OwnedGpuDeviceApis.WebGpu), false);
		}
		catch (Exception e)
		{
			return new(false, e.Message, null, true);
		}
	}
}
