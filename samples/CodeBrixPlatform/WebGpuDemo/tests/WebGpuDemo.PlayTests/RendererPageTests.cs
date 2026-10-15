using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using SilverAssertions;
using Windows.UI;
using WebGpuDemo.Rendering;
using Xunit;

namespace WebGpuDemo.PlayTests;

// The Renderer class page: an IWebGpuRenderer declared as a XAML resource and set on the view's Renderer property.
public sealed partial class ApplicationTests
{
    private static readonly Color RendererClear = Color.FromArgb(255, 24, 24, 24);

    [Fact]
    public async Task The_renderer_from_XAML_draws_its_four_squares()
    {
        await Button("Renderer class").ClickAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Renderer);
        var renderer = await Page.EvaluateAsync(() => page.SquaresRenderer);
        renderer.Should().BeOfType<QuadrantSquaresRenderer>();
        await WaitForFramesAsync(() => page.RenderView, () => renderer.FramesRendered, 3);
        await Expect(Page.GetByTestId("RendererStatus")).ToContainTextAsync("render path Readback");
        renderer.DevicesReady.Should().Be(1);
        renderer.HasPipeline.Should().BeTrue();

        var canvas = await CaptureAsync("RendererViewport", "renderer-class");
        canvas.IsBlank.Should().BeFalse(canvas.ToString());
        // Four squares of 0.4 x 0.4 clip units, made square by the view's aspect: about 2 % of the view each, the
        // label panel aside.
        foreach (var colour in new[]
                 {
                     Color.FromArgb(255, 230, 57, 70), Color.FromArgb(255, 42, 157, 143),
                     Color.FromArgb(255, 69, 123, 222), Color.FromArgb(255, 233, 196, 106),
                 })
        {
            canvas.Coverage(colour, tolerance: 6).Should().BeInRange(0.010, 0.030, colour.ToString());
        }

        canvas.Coverage(RendererClear).Should().BeGreaterThan(0.85);
        // The overlay label (the renderer's OnOverlay): a dark panel with white text in the top-left corner.
        var label = canvas.Region(14, 14, 240, 28);
        label.Coverage(Color.FromArgb(255, 255, 255, 255), tolerance: 40).Should().BeGreaterThan(0.01);
        label.MeanLuminance().Should().BeLessThan(80);
    }
}
