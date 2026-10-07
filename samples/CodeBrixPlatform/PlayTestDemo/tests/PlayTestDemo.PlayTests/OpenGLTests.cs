using System;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using PlayTestDemo.Views;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace PlayTestDemo.PlayTests;

// OpenGL content has no visual tree to assert on: these tests measure the canvases' pixels, captured in the
// running test, with PixelStats - the structure of the picture, never a comparison with a saved image.
public sealed partial class ApplicationTests
{
    // The lit square's canvas is cleared to 0.1 grey; the circle's canvas to white.
    private static readonly Color GLBackground = Color.FromArgb(255, 26, 26, 26);
    private static readonly Color SkiaBackground = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color CircleBlue = Color.FromArgb(255, 0, 0x50, 0xC8);

    private async Task<OpenGLView> OpenOpenGLAsync()
    {
        await Button("Try OpenGL").ClickAsync();
        await Expect(Page.GetByTestId("GLStatus")).ToHaveTextAsync("OpenGL: initialized");
        await Expect(Page.GetByTestId("SkiaGLStatus")).ToHaveTextAsync("GPU Skia: initialized");
        return await Page.EvaluateAsync(() => (OpenGLView)fixture.View.Content);
    }

    private async Task<PixelStats> CanvasAsync(string testId) =>
        PixelStats.FromPng(await Page.GetByTestId(testId).ScreenshotAsync(new() { Stable = true }));

    [Fact]
    public void OpenGL_reports_its_provider_renderer_and_version()
    {
        var info = fixture.Application.OpenGL;
        info.IsAvailable.Should().BeTrue(info.ToString());
        info.Provider.Should().Contain("CodeBrix.Platform.PlayTest.OpenGL");
        info.Renderer.Should().NotBeNullOrEmpty();
        info.Version.Should().NotBeNullOrEmpty();
        info.UnavailableReason.Should().BeNull();
        // Mesa EGL and ANGLE give OpenGL ES contexts; WGL gives desktop OpenGL.
        info.IsGles.Should().Be(!OperatingSystem.IsWindows());
    }

    [Fact]
    public async Task GL_canvas_draws_the_lit_square()
    {
        var view = await OpenOpenGLAsync();
        (await Page.EvaluateAsync(() => view.Square.GetGLInitializationState().Status)).Should().Be(GLInitializationStatus.Initialized);
        await Expect(Page.GetByTestId("GLFallback")).ToBeHiddenAsync();

        var canvas = await CanvasAsync("GLCanvas");
        canvas.IsBlank.Should().BeFalse(canvas.ToString());
        canvas.IsUniform.Should().BeFalse(canvas.ToString());
        // A 180-pixel square in the middle of the 360-pixel canvas: a quarter of it.
        (1 - canvas.Coverage(GLBackground)).Should().BeInRange(0.23, 0.27);
        var bounds = canvas.Bounds(GLBackground);
        bounds.Should().NotBeNull();
        bounds.Value.X.Should().BeCloseTo(90, 3u);
        bounds.Value.Y.Should().BeCloseTo(90, 3u);
        bounds.Value.Width.Should().BeCloseTo(180, 3u);
        bounds.Value.Height.Should().BeCloseTo(180, 3u);
        var centre = canvas.Centroid(GLBackground);
        centre.Value.X.Should().BeApproximately(180, 3);
        centre.Value.Y.Should().BeApproximately(180, 3);
        // Lit from the left: the square's left half is clearly brighter than its right half, and the
        // shading makes it far from flat.
        var left = canvas.Half(PixelHalf.Left).MeanLuminance(GLBackground);
        var right = canvas.Half(PixelHalf.Right).MeanLuminance(GLBackground);
        left.Should().BeGreaterThan(right + 30, $"left {left:0.0}, right {right:0.0}");
        canvas.Region(90, 90, 180, 180).LuminanceVariance().Should().BeGreaterThan(50);
    }

    [Fact]
    public async Task Rotating_the_square_changes_the_GL_picture()
    {
        var view = await OpenOpenGLAsync();
        var before = await CanvasAsync("GLCanvas");
        await Button("Rotate the square").ClickAsync();
        await fixture.Application.WaitForAsync(() => view.Square.Angle, angle => angle == 30, description: "the square's new angle");
        var after = await CanvasAsync("GLCanvas");

        var difference = before.Difference(after, tolerance: 8);
        difference.ChangedFraction.Should().BeGreaterThan(0.05, difference.ToString());
        difference.MaxChannelDelta.Should().BeGreaterThan(100);
        // Turned by 30 degrees, the square's extent grows from 180 to about 246 pixels; it stays centred.
        after.Bounds(GLBackground).Value.Width.Should().BeCloseTo(246, 4u);
        var centre = after.Centroid(GLBackground);
        centre.Value.X.Should().BeApproximately(180, 3);
        centre.Value.Y.Should().BeApproximately(180, 3);
    }

    [Fact]
    public async Task GPU_Skia_canvas_draws_the_circle()
    {
        var view = await OpenOpenGLAsync();
        (await Page.EvaluateAsync(() => view.Circle.IsGpuInitialized)).Should().Be(true);

        var canvas = await CanvasAsync("SkiaGLCanvas");
        canvas.IsBlank.Should().BeFalse(canvas.ToString());
        canvas.DistinctColorCount.Should().BeGreaterThan(2, "a blue circle, anti-aliased, on white");
        // A circle of radius 90 on a 360-pixel square: pi/16 of it.
        canvas.Coverage(CircleBlue).Should().BeInRange(0.18, 0.21);
        var bounds = canvas.Bounds(SkiaBackground);
        bounds.Value.X.Should().BeCloseTo(90, 2u);
        bounds.Value.Y.Should().BeCloseTo(90, 2u);
        bounds.Value.Width.Should().BeCloseTo(180, 2u);
        bounds.Value.Height.Should().BeCloseTo(180, 2u);
        var centre = canvas.Centroid(SkiaBackground);
        centre.Value.X.Should().BeApproximately(180, 2);
        centre.Value.Y.Should().BeApproximately(180, 2);
        canvas.MirrorSymmetry(PixelAxis.Vertical).Should().BeGreaterThan(0.99);
        canvas.MirrorSymmetry(PixelAxis.Horizontal).Should().BeGreaterThan(0.99);
        canvas.LuminanceVariance().Should().BeGreaterThan(0);
    }
}
