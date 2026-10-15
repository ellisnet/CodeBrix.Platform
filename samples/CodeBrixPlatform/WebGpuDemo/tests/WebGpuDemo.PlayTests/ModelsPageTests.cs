using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using SilverAssertions;
using Xunit;

namespace WebGpuDemo.PlayTests;

// The Models page: CodeBrix.WebGpu.Graphics drawing glTF models into the view through its sharing contracts.
public sealed partial class ApplicationTests
{
    [Fact]
    public async Task Models_page_draws_the_lit_models_under_the_sky()
    {
        await Button("Models").ClickAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Models);
        await WaitForFramesAsync(() => page.RenderView, () => page.FramesRendered, 3);
        await Expect(Page.GetByTestId("ModelsStatus")).ToContainTextAsync("5 models loaded");
        await Expect(Page.GetByTestId("ModelsStatus")).ToContainTextAsync("render path Readback");
        await Expect(Page.GetByTestId("ModelsPlaceholder")).ToBeHiddenAsync();

        var canvas = await CaptureAsync("ModelsViewport", "models");
        canvas.IsBlank.Should().BeFalse(canvas.ToString());
        // Shaded, textured models, grass and a sky gradient: thousands of colours, nothing like a flat clear.
        canvas.DistinctColorCount.Should().BeGreaterThan(500, canvas.ToString());
        canvas.LuminanceVariance().Should().BeGreaterThan(50);
        // The sky is above the horizon and is bluer than it is red; the ground below is greener than it is blue.
        var sky = canvas.Region(0, 0, canvas.Width, canvas.Height / 10);
        var skyPixel = sky.GetPixel(sky.Width / 2, sky.Height / 2);
        ((int)skyPixel.B).Should().BeGreaterThan(skyPixel.R);
        var ground = canvas.Region(0, canvas.Height * 9 / 10, canvas.Width / 10, canvas.Height / 10);
        var groundPixel = ground.GetPixel(ground.Width / 2, ground.Height / 2);
        ((int)groundPixel.G).Should().BeGreaterThan(groundPixel.B);
        // The models stand in the middle: the centre has far more colours than an equal strip of sky.
        var centre = canvas.Region(canvas.Width / 3, canvas.Height / 3, canvas.Width / 3, canvas.Height / 3);
        centre.DistinctColorCount.Should().BeGreaterThan(sky.Region(0, 0, sky.Width / 3, sky.Height).DistinctColorCount);
    }

    [Fact]
    public async Task The_models_move_and_the_camera_turns()
    {
        await Button("Models").ClickAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Models);
        await WaitForFramesAsync(() => page.RenderView, () => page.FramesRendered, 3);
        var before = await CaptureAsync("ModelsViewport");
        var frames = await Page.EvaluateAsync(() => page.FramesRendered);
        await fixture.Application.WaitForAsync(() => page.FramesRendered, count => count >= frames + 20, description: "20 more frames");
        var after = await CaptureAsync("ModelsViewport");

        var difference = before.Difference(after, tolerance: 8);
        difference.ChangedFraction.Should().BeGreaterThan(0.02, difference.ToString());
    }
}
