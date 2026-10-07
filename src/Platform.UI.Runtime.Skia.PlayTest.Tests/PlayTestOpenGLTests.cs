using System;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The launch-time OpenGL decisions, driven with registries of their own: an empty one reaches the
// "no provider registered" path with no second process and no skip. Fake providers have no GL at all.
public sealed class PlayTestOpenGLTests
{
    [Fact]
    public void OpenGL_option_defaults_to_Available()
        => new PlayTestOptions().OpenGL.Should().Be(PlayTestOpenGL.Available);

    [Fact]
    public void Registry_starts_empty()
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();

        //Assert
        registry.IsRegistered.Should().BeFalse();
        registry.Provider.Should().BeNull();
    }

    [Fact]
    public void Registering_the_same_provider_type_again_keeps_the_first()
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();
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
        var registry = new PlayTestOpenGLProviderRegistry();
        registry.Register(new FakeProvider());

        //Act
        var register = () => registry.Register(new OtherFakeProvider());

        //Assert
        register.Should().Throw<InvalidOperationException>().WithMessage("*one provider per process*");
    }

    [Fact]
    public void Registering_null_throws()
    {
        var register = () => PlayTestOpenGLProviders.Register(null!);
        register.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Without_a_provider_the_wrapper_factory_throws_naming_the_package_and_Register()
    {
        //Arrange
        var plan = PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Available, new PlayTestOpenGLProviderRegistry());
        PlayTestException recorded = null!;

        //Act
        var factory = PlayTestOpenGLSetup.WrapperFactory(plan, error => recorded = error);
        var create = () => factory!(null!);

        //Assert
        plan.Mode.Should().Be(PlayTestOpenGLMode.NotRegistered);
        factory.Should().NotBeNull();
        var thrown = create.Should().Throw<PlayTestException>().Which;
        thrown.Message.Should().Contain("CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever");
        thrown.Message.Should().Contain("CodeBrixPlayTestOpenGL.Register()");
        thrown.Message.Should().Contain("PlayTestOpenGL.Unavailable");
        recorded.Should().BeSameAs(thrown);
    }

    [Fact]
    public void Without_a_provider_the_launch_reports_OpenGL_unavailable_and_why()
    {
        //Act
        var info = PlayTestOpenGLSetup.Probe(PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Available, new PlayTestOpenGLProviderRegistry()));

        //Assert
        info.IsAvailable.Should().BeFalse();
        info.Provider.Should().BeNull();
        info.UnavailableReason.Should().Contain("CodeBrixPlayTestOpenGL.Register()");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Opting_out_registers_nothing_and_never_touches_the_provider(bool providerRegistered)
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();
        var provider = new FakeProvider();
        if (providerRegistered) registry.Register(provider);

        //Act
        var plan = PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Unavailable, registry);
        var factory = PlayTestOpenGLSetup.WrapperFactory(plan, _ => throw new InvalidOperationException("not expected"));
        var info = PlayTestOpenGLSetup.Probe(plan);

        //Assert
        plan.Mode.Should().Be(PlayTestOpenGLMode.OptedOut);
        factory.Should().BeNull();
        info.IsAvailable.Should().BeFalse();
        info.UnavailableReason.Should().Contain("Unavailable");
        provider.Created.Should().Be(0);
        provider.Disposed.Should().BeFalse();
    }

    [Fact]
    public void An_undefined_option_is_rejected()
    {
        var plan = () => PlayTestOpenGLSetup.Plan((PlayTestOpenGL)42, new PlayTestOpenGLProviderRegistry());
        plan.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Probing_a_working_provider_reports_it_and_releases_the_probe_context()
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();
        var provider = new FakeProvider();
        registry.Register(provider);

        //Act
        var info = PlayTestOpenGLSetup.Probe(PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Available, registry));

        //Assert
        info.IsAvailable.Should().BeTrue();
        info.Provider.Should().Be("Fake provider");
        info.IsGles.Should().BeTrue();
        info.UnavailableReason.Should().BeNull();
        // The fake has no glGetString, so there are no strings to report.
        info.Renderer.Should().BeNull();
        info.Version.Should().BeNull();
        provider.Created.Should().Be(1);
        provider.Contexts[0].Disposed.Should().BeTrue();
        provider.Contexts[0].CurrentScopes.Should().Be(0, "the probe restores the previous context");
    }

    [Fact]
    public void Probing_a_provider_that_cannot_create_a_context_throws_its_reason()
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();
        registry.Register(new FakeProvider { Failure = new InvalidOperationException("libEGL.so.1 could not be loaded") });

        //Act
        var probe = () => PlayTestOpenGLSetup.Probe(PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Available, registry));

        //Assert
        var thrown = probe.Should().Throw<PlayTestException>().Which;
        thrown.Message.Should().Contain("\"Fake provider\"");
        thrown.Message.Should().Contain("libEGL.so.1 could not be loaded");
        thrown.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void With_a_provider_the_wrapper_factory_creates_one_context_per_wrapper()
    {
        //Arrange
        var registry = new PlayTestOpenGLProviderRegistry();
        var provider = new FakeProvider();
        registry.Register(provider);
        var factory = PlayTestOpenGLSetup.WrapperFactory(PlayTestOpenGLSetup.Plan(PlayTestOpenGL.Available, registry), _ => { });

        //Act
        var first = (PlayTestNativeOpenGLWrapper)factory!(null!);
        var second = (PlayTestNativeOpenGLWrapper)factory(null!);

        //Assert
        provider.Created.Should().Be(2);
        first.Should().NotBeSameAs(second);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Wrapper_declares_the_context_flavour_and_forwards_to_it(bool gles)
    {
        //Arrange
        var context = new FakeContext(gles);
        var wrapper = new PlayTestNativeOpenGLWrapper(context);

        //Act
        var found = wrapper.TryGetProcAddress("glFake", out var address);
        var missing = () => wrapper.GetProcAddress("glFake");
        var scope = wrapper.MakeCurrent();
        var current = context.CurrentScopes;
        scope.Dispose();
        wrapper.Dispose();

        //Assert
        wrapper.UsesGles.Should().Be(gles);
        found.Should().BeFalse();
        address.Should().Be(0);
        missing.Should().Throw<InvalidOperationException>().WithMessage("*glFake*");
        current.Should().Be(1);
        context.CurrentScopes.Should().Be(0);
        context.Disposed.Should().BeTrue();
    }

    [Theory]
    [InlineData("llvmpipe (LLVM 19.1.7, 256 bits)", true)]
    [InlineData("softpipe", true)]
    [InlineData("ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device (Subzero)), SwiftShader driver)", true)]
    [InlineData("GDI Generic", true)]
    [InlineData("Mesa Intel(R) UHD Graphics (CML GT2)", false)]
    [InlineData("ANGLE (Apple, ANGLE Metal Renderer: Intel(R) UHD Graphics 630, Version 15.7)", false)]
    public void Info_marks_software_renderers(string renderer, bool software)
        => new PlayTestOpenGLInfo("p", renderer, "v", true, null).IsSoftware.Should().Be(software);

    private sealed class FakeContext(bool gles) : IPlayTestOpenGLContext
    {
        public bool IsGles => gles;
        public int CurrentScopes { get; private set; }
        public bool Disposed { get; private set; }
        public nint GetProcAddress(string name) => 0;

        public IDisposable MakeCurrent()
        {
            CurrentScopes++;
            return new Scope(() => CurrentScopes--);
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class Scope(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private class FakeProvider : IPlayTestOpenGLProvider
    {
        public string Name => "Fake provider";
        public Exception? Failure { get; set; }
        public System.Collections.Generic.List<FakeContext> Contexts { get; } = new();
        public int Created => Contexts.Count;
        public bool Disposed { get; private set; }

        public IPlayTestOpenGLContext CreateContext()
        {
            if (Failure != null) throw Failure;
            var context = new FakeContext(gles: true);
            Contexts.Add(context);
            return context;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class OtherFakeProvider : FakeProvider { }
}
