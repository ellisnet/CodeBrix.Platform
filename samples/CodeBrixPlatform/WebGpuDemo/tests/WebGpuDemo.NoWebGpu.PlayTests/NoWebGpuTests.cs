using System;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.WebGpu;
using CodeBrix.Platform.UI.WebGpu;
using SilverAssertions;
using WebGpuDemo.Views;
using Xunit;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace WebGpuDemo.NoWebGpu.PlayTests;

public sealed class NoWebGpuFixture : IAsyncLifetime
{
    public PlayTestApplication Application { get; private set; }
    public MainPage View { get; private set; }

    public async ValueTask InitializeAsync()
    {
        App.InitializeLogging();
        CodeBrixPlayTestWebGpu.Register();
        Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
        {
            ConfigurationAssembly = typeof(NoWebGpuFixture).Assembly,
            WebGpu = PlayTestWebGpu.Unavailable,
        });
    }

    public Task ResetAsync(ScreenOrientation? orientation) =>
        Application.Page.SetContentAsync(() => View = new MainPage(), orientation);

    public async ValueTask DisposeAsync()
    {
        if (Application != null) await Application.DisposeAsync();
    }
}

// Every page of the demo with no WebGPU at all: each view reports that it has no device (DeviceLost with the launch's
// reason, RenderPath None), the page shows its placeholder message, and nothing throws - a PlayTest call would fail
// with the recorded error if anything had.
public sealed class NoWebGpuTests(NoWebGpuFixture fixture) : PageTest(fixture.Application), IClassFixture<NoWebGpuFixture>, IAsyncLifetime
{
    private const string Expected = "WebGPU is not available on this machine. PlayTestOptions.WebGpu is Unavailable for this launch.";

    public async ValueTask InitializeAsync()
    {
        var test = (Xunit.v3.IXunitTest)TestContext.Current.Test;
        test.Traits.TryGetValue(PlayTestOrientationAttribute.CaseTraitName, out var orientations);
        await fixture.ResetAsync(PlayTestOrientationAttribute.Resolve(test.TestMethod.Method, orientations));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Locator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Fact]
    public void The_launch_reports_WebGpu_unavailable_and_why()
    {
        var info = fixture.Application.WebGpu;
        info.IsAvailable.Should().BeFalse();
        info.UnavailableReason.Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
        info.AdapterName.Should().BeNull();
        info.BackendName.Should().BeNull();
    }

    [Fact]
    public async Task The_Triangle_page_shows_its_placeholder()
    {
        await Expect(Page.GetByTestId("TrianglePlaceholder")).ToHaveTextAsync(Expected);
        await Expect(Page.GetByTestId("TriangleStatus")).ToContainTextAsync("No device");
        var page = await Page.EvaluateAsync(() => fixture.View.Triangle);
        (await Page.EvaluateAsync(() => page.RenderView.RenderPath)).Should().Be(WebGpuRenderPath.None);
        (await Page.EvaluateAsync(() => page.RenderView.IsDeviceReady)).Should().BeFalse();
        page.FramesRendered.Should().Be(0);
        page.DevicesReady.Should().Be(0);

        var folder = Path.Combine(AppContext.BaseDirectory, "TestResults", "WebGpuDemo");
        Directory.CreateDirectory(folder);
        await Page.ScreenshotAsync(new() { Path = Path.Combine(folder, "no-webgpu-triangle.png") });
    }

    [Fact]
    public async Task The_Models_page_shows_its_placeholder_and_loads_nothing()
    {
        await Button("Models").ClickAsync();
        await Expect(Page.GetByTestId("ModelsPlaceholder")).ToHaveTextAsync(Expected);
        var page = await Page.EvaluateAsync(() => fixture.View.Models);
        (await Page.EvaluateAsync(() => page.Scene)).Should().BeNull();
        page.FramesRendered.Should().Be(0);
    }

    [Fact]
    public async Task The_Renderer_class_page_shows_its_placeholder_and_the_renderer_gets_no_device()
    {
        await Button("Renderer class").ClickAsync();
        await Expect(Page.GetByTestId("RendererPlaceholder")).ToHaveTextAsync(Expected);
        var page = await Page.EvaluateAsync(() => fixture.View.Renderer);
        var renderer = await Page.EvaluateAsync(() => page.SquaresRenderer);
        renderer.DevicesReady.Should().Be(0);
        renderer.HasPipeline.Should().BeFalse();
        renderer.FramesRendered.Should().Be(0);
    }

    [Fact]
    public async Task The_Controls_page_shows_its_placeholder_and_a_manual_frame_is_refused_quietly()
    {
        await Button("Controls").ClickAsync();
        await Expect(Page.GetByTestId("ControlsPlaceholder")).ToHaveTextAsync(Expected);
        await Expect(Page.GetByTestId("RenderPathText")).ToHaveTextAsync("Render path: None");
        await Expect(Page.GetByTestId("AdapterText")).ToHaveTextAsync("Adapter: none");
        await Expect(Page.GetByTestId("CapabilitiesText")).ToHaveTextAsync("Features: none");

        await Page.GetByTestId("DriverSelector").SelectOptionAsync("Manual");
        await Button("Render one frame").ClickAsync();
        await Expect(Page.GetByTestId("ControlsStatus")).ToHaveTextAsync("The view cannot render now");
        var page = await Page.EvaluateAsync(() => fixture.View.Controls);
        page.FramesRendered.Should().Be(0);
    }
}
