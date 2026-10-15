using System;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.UI.WebGpu;
using SilverAssertions;
using Xunit;

namespace WebGpuDemo.PlayTests;

// WebGPU pixels have no visual tree to assert on: these tests measure the views' pixels, captured in the running test,
// with PixelStats - the structure of the picture, never a comparison with a saved image. The views animate, so no
// capture here asks for Stable. Under PlayTest every view renders through its Readback path (its own device, one
// read-back per frame); a direct path is a live desktop matter (see the sample README).
public sealed partial class ApplicationTests(AppFixture fixture) : PageTest(fixture.Application), IClassFixture<AppFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var test = (Xunit.v3.IXunitTest)TestContext.Current.Test;
        test.Traits.TryGetValue(PlayTestOrientationAttribute.CaseTraitName, out var orientations);
        await fixture.ResetAsync(PlayTestOrientationAttribute.Resolve(test.TestMethod.Method, orientations));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Locator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Fact]
    public void WebGpu_reports_its_provider_adapter_and_backend()
    {
        var info = fixture.Application.WebGpu;
        info.IsAvailable.Should().BeTrue(info.ToString());
        info.Provider.Should().StartWith("CodeBrix.Platform.PlayTest.WebGpu");
        info.AdapterName.Should().NotBeNullOrWhiteSpace();
        info.UnavailableReason.Should().BeNull();
        if (OperatingSystem.IsLinux()) info.BackendName.Should().Be("Vulkan");
        if (OperatingSystem.IsMacOS()) info.BackendName.Should().Be("Metal");
    }

    // Waits until the view has a device on the Readback path and has drawn at least `frames` frames.
    private async Task WaitForFramesAsync(Func<WebGpuRenderView> view, Func<long> drawn, long frames)
    {
        await fixture.Application.WaitForAsync(() => view().RenderPath, path => path == WebGpuRenderPath.Readback,
            description: "the view's Readback path");
        await fixture.Application.WaitForAsync(drawn, count => count >= frames, description: $"{frames} frames");
    }

    // The frame count once it stops changing (OnDemand, Manual, paused): read twice, 400 ms apart, until equal.
    private async Task<long> SettledAsync(Func<long> drawn)
    {
        var last = await Page.EvaluateAsync(drawn);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(400, TestContext.Current.CancellationToken);
            var now = await Page.EvaluateAsync(drawn);
            if (now == last) return now;
            last = now;
        }

        throw new InvalidOperationException("The frame count never settled.");
    }

    private async Task<PixelStats> CaptureAsync(string testId, string saveAs = null)
    {
        var options = new LocatorScreenshotOptions();
        if (saveAs != null)
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "TestResults", "WebGpuDemo");
            Directory.CreateDirectory(folder);
            options.Path = Path.Combine(folder, saveAs + ".png");
        }

        return PixelStats.FromPng(await Page.GetByTestId(testId).ScreenshotAsync(options));
    }
}
