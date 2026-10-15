using System;
using CodeBrix.WebGpu.Native;

namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>Event args for <see cref="WebGpuRenderView.DeviceReady"/>.</summary>
public sealed class WebGpuDeviceReadyEventArgs : EventArgs
{
	internal WebGpuDeviceReadyEventArgs(IWebGpuDeviceSource device, WebGpuDeviceInfo deviceInfo)
	{
		Device = device;
		DeviceInfo = deviceInfo;
	}

	/// <summary>
	/// The view's device (the view itself, as an <see cref="IWebGpuDeviceSource"/>): pass it to a library that accepts
	/// a device source, or read its handles directly. The handles stay valid until <see cref="WebGpuRenderView.DeviceLost"/>.
	/// </summary>
	public IWebGpuDeviceSource Device { get; }

	/// <summary>
	/// What the device is: adapter, backend, and the features and limits it was created with. Check a renderer's or a
	/// module's requirements against it before initialising (also <see cref="WebGpuRenderView.DeviceInfo"/>).
	/// </summary>
	public WebGpuDeviceInfo DeviceInfo { get; }
}
