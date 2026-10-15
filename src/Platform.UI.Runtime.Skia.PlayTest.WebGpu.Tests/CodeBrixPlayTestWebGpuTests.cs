using System;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.PlayTest.WebGpu.Providers;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.WebGpu.Tests;

// The provider package end to end, without launching an application: the provider is driven through the PlayTest
// head's own launch-time WebGPU setup (plan -> probe -> gate) with the REAL device route, and the gate the head would
// register is handed to the WebGpu view's own gate evaluation - the decision a WebGpuRenderView makes in Loaded.
// Registries of their own reach every mode (registered, opted out, not registered) in one process. The running
// application (real views, real pixels) is WebGpuDemo's PlayTests. The OpenGL twin of these paths is
// PlayTestOpenGLTests (fake providers) plus PlayTestDemo's OpenGL tests (the real provider).
public sealed class CodeBrixPlayTestWebGpuTests
{
    private const string PackageId = "CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever";

    [Fact]
    public void Register_registers_the_provider_with_the_head_once()
    {
        //Act
        CodeBrixPlayTestWebGpu.Register();
        CodeBrixPlayTestWebGpu.Register();

        //Assert
        CodeBrixPlayTestWebGpu.IsRegistered.Should().BeTrue();
        PlayTestWebGpuProviders.IsRegistered.Should().BeTrue();
        PlayTestWebGpuProviders.Registry.Provider.Should().BeOfType<WebGpuSkiaDeviceProvider>();
    }

    [Fact]
    public void Register_creates_no_device()
    {
        //Act
        CodeBrixPlayTestWebGpu.Register();

        //Assert
        ((WebGpuSkiaDeviceProvider)PlayTestWebGpuProviders.Registry.Provider!).ProbeCount.Should().Be(0);
    }

    [Fact]
    public void The_provider_names_this_package_and_the_backends_it_tries()
    {
        //Arrange
        using var provider = new WebGpuSkiaDeviceProvider();

        //Assert
        provider.Name.Should().StartWith("CodeBrix.Platform.PlayTest.WebGpu (");
        provider.Name.Should().Contain(OperatingSystem.IsWindows() ? "Direct3D 12" : OperatingSystem.IsMacOS() ? "Metal" : "Vulkan");
    }

    [Fact]
    public void The_head_message_names_this_package_and_its_Register_call()
    {
        // The head names the package and the call in its "not registered" message; they must be this package's own.
        PlayTestWebGpuSetup.PackageId.Should().Be(PackageId);
        PlayTestWebGpuSetup.NotRegisteredError().Message.Should().Contain(nameof(CodeBrixPlayTestWebGpu) + "." + nameof(CodeBrixPlayTestWebGpu.Register) + "()");
        typeof(CodeBrixPlayTestWebGpu).Namespace.Should().Be("CodeBrix.Platform.PlayTest.WebGpu");
    }

    [Fact]
    public void Probing_creates_a_device_that_renders_reports_it_and_keeps_nothing()
    {
        //Arrange
        TestGpu.RequireAdapter();
        using var provider = new WebGpuSkiaDeviceProvider();

        //Act
        var first = provider.Probe();
        var second = provider.Probe();

        //Assert
        first.AdapterName.Should().NotBeNullOrWhiteSpace();
        first.BackendName.Should().Be(OperatingSystem.IsWindows() ? first.BackendName : OperatingSystem.IsMacOS() ? "Metal" : "Vulkan");
        first.State.Should().BeNull("each view creates its own device; the probe device is released before Probe returns");
        second.AdapterName.Should().Be(first.AdapterName);
        provider.ProbeCount.Should().Be(2);
    }

    [Fact]
    public void A_registered_provider_makes_the_launch_report_WebGpu_available_with_its_adapter()
    {
        //Arrange
        TestGpu.RequireAdapter();
        var registry = new PlayTestWebGpuProviderRegistry();
        using var provider = new WebGpuSkiaDeviceProvider();
        registry.Register(provider);
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry);

        //Act
        var info = PlayTestWebGpuSetup.Probe(plan);

        //Assert
        plan.Mode.Should().Be(PlayTestWebGpuMode.Provider);
        info.IsAvailable.Should().BeTrue(info.ToString());
        info.Provider.Should().Be(provider.Name);
        info.AdapterName.Should().NotBeNullOrWhiteSpace();
        info.BackendName.Should().NotBeNullOrWhiteSpace();
        info.UnavailableReason.Should().BeNull();
        info.ToString().Should().Contain(info.AdapterName!);
        provider.ProbeCount.Should().Be(1);
    }

    [Fact]
    public void With_the_provider_the_view_may_create_its_own_device()
    {
        //Arrange
        TestGpu.RequireAdapter();
        var registry = new PlayTestWebGpuProviderRegistry();
        using var provider = new WebGpuSkiaDeviceProvider();
        registry.Register(provider);
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry);
        var info = PlayTestWebGpuSetup.Probe(plan);
        var gate = Gate(plan, info, _ => throw new InvalidOperationException("nothing may be recorded"));

        //Act
        var decision = WebGpuDeviceGate.Evaluate(gate);

        //Assert
        decision.MayCreate.Should().BeTrue();
        decision.Failed.Should().BeFalse();
        decision.Reason.Should().BeNull();
        decision.ProviderState.Should().BeNull();
    }

    [Fact]
    public void Unavailable_tells_the_view_why_and_never_probes_the_provider()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        using var provider = new WebGpuSkiaDeviceProvider();
        registry.Register(provider);
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Unavailable, registry);
        var info = PlayTestWebGpuSetup.Probe(plan);
        var gate = Gate(plan, info, _ => throw new InvalidOperationException("nothing may be recorded"));

        //Act
        var decision = WebGpuDeviceGate.Evaluate(gate);

        //Assert
        info.IsAvailable.Should().BeFalse();
        info.UnavailableReason.Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
        decision.MayCreate.Should().BeFalse();
        decision.Failed.Should().BeFalse();
        decision.Reason.Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
        provider.ProbeCount.Should().Be(0);
    }

    [Fact]
    public void A_machine_without_a_device_fails_the_launch_with_the_engines_reason()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        using var provider = new WebGpuSkiaDeviceProvider(TestGpu.CreateAbsentDevice);
        registry.Register(provider);
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry);

        //Act
        var launch = () => PlayTestWebGpuSetup.Probe(plan);

        //Assert
        var thrown = launch.Should().Throw<PlayTestException>().Which;
        thrown.Message.Should().Contain("The WebGPU provider \"" + provider.Name + "\" could not create a device");
        thrown.Message.Should().Contain("No WebGPU device could be created");
        thrown.Message.Should().Contain("not available in the native library for this platform");
        thrown.Message.Should().Contain(OperatingSystem.IsWindows() ? "Direct3D 12 or Vulkan" : OperatingSystem.IsMacOS() ? "Metal device" : "apt install mesa-vulkan-drivers");
        thrown.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Without_the_provider_the_views_initialization_fails_and_the_run_records_why()
    {
        //Arrange: Available, but nothing registered.
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, new PlayTestWebGpuProviderRegistry());
        var info = PlayTestWebGpuSetup.Probe(plan);
        PlayTestException? recorded = null;
        var gate = Gate(plan, info, error => recorded = error);

        //Act: what the view does in Loaded, before it would create a device.
        var decision = WebGpuDeviceGate.Evaluate(gate);

        //Assert: the view creates nothing and reports a failed initialization; the head recorded the failure that
        // every later PlayTest call reports, with the message naming this package and its Register() call.
        decision.MayCreate.Should().BeFalse();
        decision.Failed.Should().BeTrue();
        recorded.Should().NotBeNull();
        decision.Reason.Should().Be(recorded!.Message);
        recorded.Message.Should().Be(
            "The application initialized a WebGPU element, but no WebGPU provider is registered - reference "
            + PackageId + " and call CodeBrixPlayTestWebGpu.Register() before PlayTestApplication.LaunchAsync, "
            + "or launch with PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable to test the application without WebGPU.");
    }

    [Fact]
    public void A_disposed_provider_refuses_to_probe()
    {
        //Arrange
        var provider = new WebGpuSkiaDeviceProvider();
        provider.Dispose();

        //Act
        var probe = () => provider.Probe();

        //Assert
        provider.IsDisposed.Should().BeTrue();
        probe.Should().Throw<ObjectDisposedException>();
        provider.ProbeCount.Should().Be(0);
    }

    private static IOwnedGpuDeviceGate Gate(PlayTestWebGpuPlan plan, PlayTestWebGpuInfo info, Action<PlayTestException> notRegistered)
        => (IOwnedGpuDeviceGate)PlayTestWebGpuSetup.GateFactory(plan, info, notRegistered)(null!);
}
