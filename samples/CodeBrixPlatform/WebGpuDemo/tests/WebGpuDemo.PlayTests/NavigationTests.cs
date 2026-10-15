using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.UI.WebGpu;
using SilverAssertions;
using Xunit;

namespace WebGpuDemo.PlayTests;

// The pages are kept by MainPage, so leaving one unloads its WebGpuRenderView (the view releases its device) and
// coming back loads the SAME view again (it creates a new device and renders on).
public sealed partial class ApplicationTests
{
    [Fact]
    public async Task A_page_that_is_left_and_shown_again_renders_on_a_new_device()
    {
        await WaitForTriangleAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Triangle);
        page.DevicesReady.Should().Be(1);

        await Button("Controls").ClickAsync();
        await fixture.Application.WaitForAsync(() => page.RenderView.RenderPath, path => path == WebGpuRenderPath.None,
            description: "the hidden page's view without a device");
        (await Page.EvaluateAsync(() => page.RenderView.IsDeviceReady)).Should().BeFalse();
        var framesWhileAway = await SettledAsync(() => page.FramesRendered);

        await Button("Triangle").ClickAsync();
        await fixture.Application.WaitForAsync(() => page.DevicesReady, count => count == 2, description: "a second device");
        await WaitForFramesAsync(() => page.RenderView, () => page.FramesRendered, framesWhileAway + 3);
        await Expect(Page.GetByTestId("TrianglePlaceholder")).ToBeHiddenAsync();
        var canvas = await CaptureAsync("TriangleViewport", "triangle-after-navigation");
        (1 - canvas.Coverage(TriangleClear)).Should().BeInRange(0.13, 0.17);
    }
}
