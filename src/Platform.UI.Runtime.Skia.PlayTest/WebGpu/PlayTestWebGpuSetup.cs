using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Graphics;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest;

internal enum PlayTestWebGpuMode
{
    // PlayTestOptions.WebGpu is Unavailable: WebGPU elements are told that no device is available.
    OptedOut,
    // A provider is registered and the launch offers WebGPU.
    Provider,
    // No provider is registered: the first WebGPU element the application initializes fails the test.
    NotRegistered,
}

internal sealed class PlayTestWebGpuPlan
{
    internal PlayTestWebGpuPlan(PlayTestWebGpuMode mode, IPlayTestWebGpuProvider? provider)
    {
        Mode = mode;
        Provider = provider;
    }

    internal PlayTestWebGpuMode Mode { get; }
    internal IPlayTestWebGpuProvider? Provider { get; }
}

// The launch-time WebGPU decisions, kept apart from the host so the head's tests can drive them with their own
// registry: no second process and no skip is needed to reach the "not registered" path. The OpenGL twin is
// OpenGL/PlayTestOpenGLSetup.cs.
internal static class PlayTestWebGpuSetup
{
    internal const string PackageId = "CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever";
    internal const string OptedOutReason = "PlayTestOptions.WebGpu is Unavailable for this launch.";

    internal static PlayTestWebGpuPlan Plan(PlayTestWebGpu option, PlayTestWebGpuProviderRegistry registry)
    {
        if (!Enum.IsDefined(option)) throw new ArgumentOutOfRangeException(nameof(option));
        if (option == PlayTestWebGpu.Unavailable) return new(PlayTestWebGpuMode.OptedOut, null);
        return registry.Provider is { } provider
            ? new(PlayTestWebGpuMode.Provider, provider)
            : new(PlayTestWebGpuMode.NotRegistered, null);
    }

    internal static PlayTestException NotRegisteredError() => new(
        "The application initialized a WebGPU element, but no WebGPU provider is registered - reference "
        + PackageId + " and call CodeBrixPlayTestWebGpu.Register() before PlayTestApplication.LaunchAsync, "
        + "or launch with PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable to test the application without WebGPU.");

    // Runs on the UI thread before the application starts. A registered provider must prove a working adapter and
    // device now, so a machine without WebGPU fails the launch with the provider's own reason.
    internal static PlayTestWebGpuInfo Probe(PlayTestWebGpuPlan plan)
    {
        switch (plan.Mode)
        {
            case PlayTestWebGpuMode.OptedOut:
                return new(null, null, null, null, false, OptedOutReason);
            case PlayTestWebGpuMode.NotRegistered:
                return new(null, null, null, null, false,
                    "No WebGPU provider is registered - reference " + PackageId + " and call CodeBrixPlayTestWebGpu.Register().");
        }
        var provider = plan.Provider!;
        PlayTestWebGpuProbeResult result;
        try { result = provider.Probe() ?? throw new InvalidOperationException("The provider's probe returned no result."); }
        catch (Exception e) { throw ProviderError(provider, e); }
        return new(provider.Name, result.AdapterName, result.BackendName, result.DeviceDescription, result.IsSoftware, null, result.State);
    }

    // The IOwnedGpuDeviceGate builder the host registers for every XamlRoot. Unlike OpenGL (whose opt-out registers
    // nothing, because GL elements need the head's wrapper for every context), WebGPU elements create their own
    // device unless a gate says otherwise, so every mode registers one.
    internal static Func<XamlRoot, object> GateFactory(PlayTestWebGpuPlan plan, PlayTestWebGpuInfo info, Action<PlayTestException> notRegistered)
    {
        var gate = new PlayTestOwnedGpuDeviceGate(plan.Mode, info, notRegistered);
        return _ => gate;
    }

    private static PlayTestException ProviderError(IPlayTestWebGpuProvider provider, Exception e) => new(
        $"The WebGPU provider \"{provider.Name}\" could not create a device on {RuntimeInformation.OSDescription}: {e.Message}", e);
}

// The head's IOwnedGpuDeviceGate: answers the WebGPU elements of this launch; no opinion about any other API.
internal sealed class PlayTestOwnedGpuDeviceGate : IOwnedGpuDeviceGate
{
    private readonly PlayTestWebGpuMode _mode;
    private readonly PlayTestWebGpuInfo _info;
    private readonly Action<PlayTestException> _notRegistered;

    internal PlayTestOwnedGpuDeviceGate(PlayTestWebGpuMode mode, PlayTestWebGpuInfo info, Action<PlayTestException> notRegistered)
    {
        _mode = mode;
        _info = info;
        _notRegistered = notRegistered;
    }

    public string? GetUnavailableReason(string api)
    {
        if (api != OwnedGpuDeviceApis.WebGpu) return null;
        switch (_mode)
        {
            case PlayTestWebGpuMode.Provider:
                return null;
            case PlayTestWebGpuMode.OptedOut:
                return PlayTestWebGpuSetup.OptedOutReason;
            default:
                // A WebGPU element catches every failure and reports that no device is available, so the exception
                // thrown here never reaches the test on its own. _notRegistered records it as the run's failure,
                // which every following PlayTest call reports.
                var error = PlayTestWebGpuSetup.NotRegisteredError();
                _notRegistered(error);
                throw error;
        }
    }

    public object? GetProviderState(string api)
        => api == OwnedGpuDeviceApis.WebGpu && _mode == PlayTestWebGpuMode.Provider ? _info.State : null;
}
