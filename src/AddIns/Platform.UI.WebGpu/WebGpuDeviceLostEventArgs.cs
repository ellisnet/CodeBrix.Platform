using System;

namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// Event args for <c>WebGpuRenderView.DeviceLost</c>: why the view has no usable device.
/// </summary>
public sealed class WebGpuDeviceLostEventArgs : EventArgs
{
	/// <summary>Creates the args.</summary>
	/// <param name="reason">Why the view has no usable device, in words a person can act on.</param>
	public WebGpuDeviceLostEventArgs(string reason)
	{
		Reason = reason ?? string.Empty;
	}

	/// <summary>
	/// Why the view has no usable device: no adapter on this machine (the engine's own reason), an error the device
	/// reported, or a launch that said there is no device. Never null.
	/// </summary>
	public string Reason { get; }
}
