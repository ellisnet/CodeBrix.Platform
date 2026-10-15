using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.UI.WebGpu;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace WebGpuDemo.PlayTests;

// The Triangle page: raw WGSL recorded in the Render event, over the view's clear colour.
public sealed partial class ApplicationTests
{
    private static readonly Color TriangleClear = Color.FromArgb(255, 16, 24, 48);

    private async Task WaitForTriangleAsync(long frames = 3)
    {
        var page = await Page.EvaluateAsync(() => fixture.View.Triangle);
        await WaitForFramesAsync(() => page.RenderView, () => page.FramesRendered, frames);
    }

    [Fact]
    public async Task Triangle_page_draws_the_triangle_over_its_clear_colour()
    {
        await WaitForTriangleAsync();
        var view = await Page.EvaluateAsync(() => fixture.View.Triangle.RenderView);
        await Expect(Page.GetByTestId("TriangleStatus")).ToContainTextAsync("render path Readback");
        await Expect(Page.GetByTestId("TrianglePlaceholder")).ToBeHiddenAsync();
        (await Page.EvaluateAsync(() => view.IsDeviceReady)).Should().BeTrue();

        var canvas = await CaptureAsync("TriangleViewport", "triangle");
        canvas.IsBlank.Should().BeFalse(canvas.ToString());
        canvas.IsUniform.Should().BeFalse(canvas.ToString());
        // The triangle's corners are (-0.55, -0.5), (0.55, -0.5), (0, 0.6) in clip space: 0.605 of the 4 square
        // units, so 15 % of the view. The rest is the clear colour.
        (1 - canvas.Coverage(TriangleClear)).Should().BeInRange(0.13, 0.17);
        var bounds = canvas.Bounds(TriangleClear);
        bounds.Should().NotBeNull();
        bounds.Value.X.Should().BeCloseTo((int)(canvas.Width * 0.225), (uint)(canvas.Width / 100 + 2));
        bounds.Value.Width.Should().BeCloseTo((int)(canvas.Width * 0.55), (uint)(canvas.Width / 100 + 2));
        bounds.Value.Y.Should().BeCloseTo((int)(canvas.Height * 0.2), (uint)(canvas.Height / 100 + 2));
        bounds.Value.Height.Should().BeCloseTo((int)(canvas.Height * 0.55), (uint)(canvas.Height / 100 + 2));
        canvas.MirrorSymmetry(PixelAxis.Vertical).Should().BeGreaterThan(0.98);
    }

    [Fact]
    public async Task The_triangles_colour_keeps_changing()
    {
        await WaitForTriangleAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Triangle);
        var before = await CaptureAsync("TriangleViewport");
        var colourBefore = await Page.EvaluateAsync(() => page.LastColor);
        var framesBefore = await Page.EvaluateAsync(() => page.FramesRendered);
        await fixture.Application.WaitForAsync(() => page.FramesRendered, count => count >= framesBefore + 30, description: "30 more frames");
        var after = await CaptureAsync("TriangleViewport");

        (await Page.EvaluateAsync(() => page.LastColor)).Should().NotBe(colourBefore);
        var difference = before.Difference(after, tolerance: 4);
        // Only the triangle changes colour: about its 15 % of the view, never the clear colour around it.
        difference.ChangedFraction.Should().BeInRange(0.10, 0.17, difference.ToString());
    }
}
