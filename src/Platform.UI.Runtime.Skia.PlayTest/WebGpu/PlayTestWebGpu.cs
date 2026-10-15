using System;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Whether WebGPU is offered to the application under test (<see cref="PlayTestOptions.WebGpu"/>).</summary>
public enum PlayTestWebGpu
{
    /// <summary>The default: WebGPU elements create their devices as on a desktop head, once the registered
    /// provider (<c>CodeBrixPlayTestWebGpu.Register()</c> from CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever)
    /// has proved at launch that a WebGPU adapter exists. With no provider registered, the first WebGPU element the
    /// application initializes fails the test.</summary>
    Available,
    /// <summary>No WebGPU for this launch: WebGPU elements report that no device is available and the application's
    /// own fallback runs, whether or not a provider is registered.</summary>
    Unavailable,
}

/// <summary>What a WebGPU provider's launch-time probe found (<see cref="IPlayTestWebGpuProvider.Probe"/>): plain
/// strings and an opaque object, so the PlayTest head needs no WebGPU library of its own.</summary>
public sealed class PlayTestWebGpuProbeResult
{
    /// <summary>Creates a probe result.</summary>
    /// <param name="adapterName">The adapter's name (for example the GPU or "llvmpipe").</param>
    /// <param name="backendName">The native backend the adapter runs on (for example Vulkan, Metal, D3D12).</param>
    /// <param name="deviceDescription">Any further description of the device, or null.</param>
    /// <param name="isSoftware">True when the adapter is a CPU rasterizer.</param>
    /// <param name="state">An object the provider keeps for the WebGPU elements of this launch (for example the probe
    /// device), or null. The head hands it to the elements unchanged and never inspects it.</param>
    public PlayTestWebGpuProbeResult(string? adapterName, string? backendName, string? deviceDescription, bool isSoftware, object? state)
    {
        AdapterName = adapterName;
        BackendName = backendName;
        DeviceDescription = deviceDescription;
        IsSoftware = isSoftware;
        State = state;
    }

    /// <summary>The adapter's name, or null when the provider could not tell.</summary>
    public string? AdapterName { get; }
    /// <summary>The native backend, or null when the provider could not tell.</summary>
    public string? BackendName { get; }
    /// <summary>A further description of the device, or null.</summary>
    public string? DeviceDescription { get; }
    /// <summary>True when the adapter is a CPU rasterizer.</summary>
    public bool IsSoftware { get; }
    /// <summary>The provider's object for this launch, or null.</summary>
    public object? State { get; }
}

/// <summary>A source of WebGPU for the PlayTest head. One provider is registered per process with
/// <see cref="PlayTestWebGpuProviders.Register"/>; the head probes it once at launch and disposes it when the
/// application is disposed.</summary>
public interface IPlayTestWebGpuProvider : IDisposable
{
    /// <summary>A short description of the provider and its mechanism, reported by
    /// <see cref="PlayTestWebGpuInfo.Provider"/> and in failure messages.</summary>
    string Name { get; }

    /// <summary>Proves that a WebGPU adapter and device can be created on this machine. Called once, on the
    /// application's UI thread, before the application starts.</summary>
    /// <returns>What the probe found.</returns>
    /// <exception cref="Exception">No adapter or device can be created; the message states the concrete reason.</exception>
    PlayTestWebGpuProbeResult Probe();
}

/// <summary>The process-wide registry of the PlayTest head's WebGPU provider.</summary>
public static class PlayTestWebGpuProviders
{
    internal static PlayTestWebGpuProviderRegistry Registry { get; } = new();

    /// <summary>True once a provider has been registered in this process.</summary>
    public static bool IsRegistered => Registry.IsRegistered;

    /// <summary>Registers the provider that makes WebGPU available to the application. Registering a second
    /// provider of the same type does nothing; call it before <see cref="PlayTestApplication.LaunchAsync"/>.</summary>
    /// <param name="provider">The provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A provider of a different type is already registered.</exception>
    public static void Register(IPlayTestWebGpuProvider provider) => Registry.Register(provider);
}

// The registry behind PlayTestWebGpuProviders, as an instance so the head's tests can use an empty one.
internal sealed class PlayTestWebGpuProviderRegistry
{
    private readonly object _lock = new();
    private IPlayTestWebGpuProvider? _provider;

    internal bool IsRegistered { get { lock (_lock) return _provider != null; } }
    internal IPlayTestWebGpuProvider? Provider { get { lock (_lock) return _provider; } }

    internal void Register(IPlayTestWebGpuProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_lock)
        {
            if (_provider == null) _provider = provider;
            else if (_provider.GetType() != provider.GetType())
                throw new InvalidOperationException(
                    $"A WebGPU provider of type {_provider.GetType().FullName} is already registered; PlayTest uses one provider per process.");
        }
    }
}

/// <summary>What the launch found out about WebGPU (<see cref="PlayTestApplication.WebGpu"/>).</summary>
public sealed class PlayTestWebGpuInfo
{
    internal PlayTestWebGpuInfo(string? provider, string? adapterName, string? backendName, string? deviceDescription,
        bool isSoftware, string? unavailableReason, object? state = null)
    {
        Provider = provider;
        AdapterName = adapterName;
        BackendName = backendName;
        DeviceDescription = deviceDescription;
        IsSoftware = isSoftware;
        UnavailableReason = unavailableReason;
        IsAvailable = unavailableReason == null;
        State = state;
    }

    /// <summary>True when WebGPU elements get a device in this run.</summary>
    public bool IsAvailable { get; }
    /// <summary>The registered provider's name, or null when none is registered.</summary>
    public string? Provider { get; }
    /// <summary>The probed adapter's name, or null when WebGPU is unavailable.</summary>
    public string? AdapterName { get; }
    /// <summary>The probed adapter's native backend (Vulkan, Metal, D3D12 ...), or null when WebGPU is unavailable.</summary>
    public string? BackendName { get; }
    /// <summary>A further description of the probed device, or null.</summary>
    public string? DeviceDescription { get; }
    /// <summary>True when the adapter is a CPU rasterizer.</summary>
    public bool IsSoftware { get; }
    /// <summary>Why WebGPU is unavailable, or null when it is available.</summary>
    public string? UnavailableReason { get; }
    // The provider's opaque object for the WebGPU elements of this launch (IOwnedGpuDeviceGate.GetProviderState).
    internal object? State { get; }

    /// <summary>Describes the result for diagnostics.</summary>
    /// <returns>The provider, adapter and backend, or the reason WebGPU is unavailable.</returns>
    public override string ToString() => IsAvailable
        ? $"{Provider}: {AdapterName} ({BackendName})"
        : $"WebGPU unavailable: {UnavailableReason}";
}
