using System;
using System.Threading;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.PlayTest.WebGpu.Providers;

// Every operating system: the device route the WebGpuRenderView itself takes on its Readback path -
// WebGpuSkiaDevice.Create(), the engine's own backend order (Vulkan on Linux, Direct3D 12 then Vulkan on Windows,
// Metal on macOS) - so the launch proves exactly what the views of the run will do. The probe device is checked
// (one clear read back), described and DISPOSED again before the probe returns: each view creates its own device
// when it renders its first frame, as on a desktop head, and nothing of the probe is kept. The engine has no
// problem with several owned devices in one process; keeping the probe's alive would only hold GPU memory for the
// whole run.
internal sealed class WebGpuSkiaDeviceProvider : IPlayTestWebGpuProvider
{
    private readonly Func<WebGpuSkiaDevice> _createDevice;
    private int _probeCount;
    private bool _disposed;

    internal WebGpuSkiaDeviceProvider()
        : this(WebGpuSkiaDevice.Create)
    {
    }

    // The tests inject a factory that returns an unavailable device, to reach the "no adapter" path on a machine
    // that has one.
    internal WebGpuSkiaDeviceProvider(Func<WebGpuSkiaDevice> createDevice)
    {
        _createDevice = createDevice;
    }

    public string Name => "CodeBrix.Platform.PlayTest.WebGpu (the WebGpu add-in's own device, " + DefaultBackends + ")";

    // How many times the head asked this provider to probe (tests: an opted-out launch never probes).
    internal int ProbeCount => Volatile.Read(ref _probeCount);

    internal bool IsDisposed => Volatile.Read(ref _disposed);

    private static string DefaultBackends =>
        OperatingSystem.IsWindows() ? "Direct3D 12, then Vulkan"
        : OperatingSystem.IsMacOS() ? "Metal"
        : "Vulkan";

    private static string Remedy =>
        OperatingSystem.IsWindows()
            ? "PlayTests with WebGPU on Windows need a GPU driver with Direct3D 12 or Vulkan support."
        : OperatingSystem.IsMacOS()
            ? "PlayTests with WebGPU on macOS need a Mac with a Metal device."
            : "PlayTests with WebGPU on Linux need a Vulkan driver; on Debian: apt install mesa-vulkan-drivers";

    public PlayTestWebGpuProbeResult Probe()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        Interlocked.Increment(ref _probeCount);
        using var device = _createDevice();
        if (!device.IsAvailable)
            throw new InvalidOperationException((device.Reason ?? "No WebGPU device could be created.") + " " + Remedy);

        DeviceCheck.Verify(device);
        var (adapterName, description, isSoftware) = Describe(device);
        return new PlayTestWebGpuProbeResult(adapterName, BackendName(device.Backend), description, isSoftware, state: null);
    }

    public void Dispose() => Volatile.Write(ref _disposed, true);

    internal static string BackendName(WebGpuSkiaBackend backend) => backend switch
    {
        WebGpuSkiaBackend.Direct3D12 => "Direct3D 12",
        _ => backend.ToString(),
    };

    // The adapter as the engine describes it: the device name, the driver description, and whether it is a CPU
    // rasterizer (llvmpipe, WARP, SwiftShader ...). AdapterGetInfo, never DeviceGetAdapterInfo: the engine does
    // not implement the latter and ends the process when it is called.
    private static unsafe (string? Name, string? Description, bool IsSoftware) Describe(WebGpuSkiaDevice device)
    {
        AdapterInfo info = default;
        if (device.Adapter.IsNull || Wgpu.AdapterGetInfo(device.Adapter, &info) != Status.Success)
            return (device.AdapterName, null, false);
        try
        {
            var name = WgpuString.ToManaged(info.Device);
            var description = WgpuString.ToManaged(info.Description);
            return (string.IsNullOrEmpty(name) ? device.AdapterName : name,
                string.IsNullOrEmpty(description) ? null : description,
                info.AdapterType == AdapterType.CPU);
        }
        finally
        {
            Wgpu.AdapterInfoFreeMembers(info);
        }
    }
}
