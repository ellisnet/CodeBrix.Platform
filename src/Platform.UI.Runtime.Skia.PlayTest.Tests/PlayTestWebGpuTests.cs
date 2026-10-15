using System;
using CodeBrix.Platform.Graphics;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The launch-time WebGPU decisions, driven with registries of their own: an empty one reaches the "no provider
// registered" path with no second process and no skip. Fake providers have no WebGPU at all. The OpenGL twin is
// PlayTestOpenGLTests.
public sealed class PlayTestWebGpuTests
{
    [Fact]
    public void WebGpu_option_defaults_to_Available()
        => new PlayTestOptions().WebGpu.Should().Be(PlayTestWebGpu.Available);

    [Fact]
    public void WebGpu_and_OpenGL_options_share_the_same_default()
        => ((int)new PlayTestOptions().WebGpu).Should().Be((int)new PlayTestOptions().OpenGL);

    [Fact]
    public void Registry_starts_empty()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();

        //Assert
        registry.IsRegistered.Should().BeFalse();
        registry.Provider.Should().BeNull();
    }

    [Fact]
    public void Registering_the_same_provider_type_again_keeps_the_first()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        var first = new FakeProvider();

        //Act
        registry.Register(first);
        registry.Register(new FakeProvider());
        registry.Register(first);

        //Assert
        registry.IsRegistered.Should().BeTrue();
        registry.Provider.Should().BeSameAs(first);
    }

    [Fact]
    public void Registering_a_provider_of_another_type_throws()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        registry.Register(new FakeProvider());

        //Act
        var register = () => registry.Register(new OtherFakeProvider());

        //Assert
        register.Should().Throw<InvalidOperationException>().WithMessage("*one provider per process*");
    }

    [Fact]
    public void Registering_null_throws()
    {
        var register = () => PlayTestWebGpuProviders.Register(null!);
        register.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void An_undefined_option_is_rejected()
    {
        var plan = () => PlayTestWebGpuSetup.Plan((PlayTestWebGpu)42, new PlayTestWebGpuProviderRegistry());
        plan.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Without_a_provider_the_first_WebGpu_element_fails_naming_the_package_and_Register()
    {
        //Arrange
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, new PlayTestWebGpuProviderRegistry());
        var info = PlayTestWebGpuSetup.Probe(plan);
        PlayTestException recorded = null!;
        var gate = (IOwnedGpuDeviceGate)PlayTestWebGpuSetup.GateFactory(plan, info, error => recorded = error)(null!);

        //Act
        var ask = () => gate.GetUnavailableReason(OwnedGpuDeviceApis.WebGpu);

        //Assert
        plan.Mode.Should().Be(PlayTestWebGpuMode.NotRegistered);
        var thrown = ask.Should().Throw<PlayTestException>().Which;
        thrown.Message.Should().Contain("CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever");
        thrown.Message.Should().Contain("CodeBrixPlayTestWebGpu.Register()");
        thrown.Message.Should().Contain("PlayTestWebGpu.Unavailable");
        recorded.Should().BeSameAs(thrown);
    }

    [Fact]
    public void Without_a_provider_the_launch_reports_WebGpu_unavailable_and_why()
    {
        //Act
        var info = PlayTestWebGpuSetup.Probe(PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, new PlayTestWebGpuProviderRegistry()));

        //Assert
        info.IsAvailable.Should().BeFalse();
        info.Provider.Should().BeNull();
        info.UnavailableReason.Should().Contain("CodeBrixPlayTestWebGpu.Register()");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Opting_out_tells_elements_why_and_never_touches_the_provider(bool providerRegistered)
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        var provider = new FakeProvider();
        if (providerRegistered) registry.Register(provider);

        //Act
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Unavailable, registry);
        var info = PlayTestWebGpuSetup.Probe(plan);
        var gate = (IOwnedGpuDeviceGate)PlayTestWebGpuSetup.GateFactory(plan, info, _ => throw new InvalidOperationException("not expected"))(null!);

        //Assert
        plan.Mode.Should().Be(PlayTestWebGpuMode.OptedOut);
        info.IsAvailable.Should().BeFalse();
        info.UnavailableReason.Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
        gate.GetUnavailableReason(OwnedGpuDeviceApis.WebGpu).Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
        gate.GetProviderState(OwnedGpuDeviceApis.WebGpu).Should().BeNull();
        provider.Probes.Should().Be(0);
        provider.Disposed.Should().BeFalse();
    }

    [Fact]
    public void Probing_a_working_provider_reports_it_and_the_gate_lets_elements_create_devices()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        var state = new object();
        var provider = new FakeProvider { State = state };
        registry.Register(provider);
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry);

        //Act
        var info = PlayTestWebGpuSetup.Probe(plan);
        var gate = (IOwnedGpuDeviceGate)PlayTestWebGpuSetup.GateFactory(plan, info, _ => throw new InvalidOperationException("not expected"))(null!);

        //Assert
        plan.Mode.Should().Be(PlayTestWebGpuMode.Provider);
        info.IsAvailable.Should().BeTrue();
        info.Provider.Should().Be("Fake provider");
        info.AdapterName.Should().Be("Fake adapter");
        info.BackendName.Should().Be("Vulkan");
        info.DeviceDescription.Should().Be("fake device");
        info.IsSoftware.Should().BeTrue();
        info.UnavailableReason.Should().BeNull();
        info.ToString().Should().Be("Fake provider: Fake adapter (Vulkan)");
        provider.Probes.Should().Be(1);
        gate.GetUnavailableReason(OwnedGpuDeviceApis.WebGpu).Should().BeNull();
        gate.GetProviderState(OwnedGpuDeviceApis.WebGpu).Should().BeSameAs(state);
    }

    [Fact]
    public void Probing_a_provider_that_cannot_create_a_device_throws_its_reason()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        registry.Register(new FakeProvider { Failure = new InvalidOperationException("no WebGPU adapter was found") });

        //Act
        var probe = () => PlayTestWebGpuSetup.Probe(PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry));

        //Assert
        var thrown = probe.Should().Throw<PlayTestException>().Which;
        thrown.Message.Should().Contain("\"Fake provider\"");
        thrown.Message.Should().Contain("no WebGPU adapter was found");
        thrown.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void A_probe_with_no_result_is_a_provider_failure()
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        registry.Register(new FakeProvider { ReturnsNull = true });

        //Act
        var probe = () => PlayTestWebGpuSetup.Probe(PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Available, registry));

        //Assert
        probe.Should().Throw<PlayTestException>().WithMessage("*no result*");
    }

    [Theory]
    [InlineData(PlayTestWebGpu.Available, false)]
    [InlineData(PlayTestWebGpu.Available, true)]
    [InlineData(PlayTestWebGpu.Unavailable, true)]
    public void The_gate_has_no_opinion_about_other_apis(PlayTestWebGpu option, bool providerRegistered)
    {
        //Arrange
        var registry = new PlayTestWebGpuProviderRegistry();
        if (providerRegistered) registry.Register(new FakeProvider());
        var plan = PlayTestWebGpuSetup.Plan(option, registry);
        var gate = (IOwnedGpuDeviceGate)PlayTestWebGpuSetup.GateFactory(plan, PlayTestWebGpuSetup.Probe(plan), _ => throw new InvalidOperationException("not expected"))(null!);

        //Assert
        gate.GetUnavailableReason("SomeOtherApi").Should().BeNull();
        gate.GetProviderState("SomeOtherApi").Should().BeNull();
    }

    [Fact]
    public void The_gate_factory_answers_every_XamlRoot_with_the_same_gate()
    {
        //Arrange
        var plan = PlayTestWebGpuSetup.Plan(PlayTestWebGpu.Unavailable, new PlayTestWebGpuProviderRegistry());
        var factory = PlayTestWebGpuSetup.GateFactory(plan, PlayTestWebGpuSetup.Probe(plan), _ => { });

        //Assert
        factory(null!).Should().BeSameAs(factory(null!));
    }

    private class FakeProvider : IPlayTestWebGpuProvider
    {
        public string Name => "Fake provider";
        public Exception? Failure { get; set; }
        public bool ReturnsNull { get; set; }
        public object? State { get; set; }
        public int Probes { get; private set; }
        public bool Disposed { get; private set; }

        public PlayTestWebGpuProbeResult Probe()
        {
            Probes++;
            if (Failure != null) throw Failure;
            return ReturnsNull ? null! : new PlayTestWebGpuProbeResult("Fake adapter", "Vulkan", "fake device", isSoftware: true, State);
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class OtherFakeProvider : FakeProvider { }
}
