using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.UI.WebGpu;
using SilverAssertions;
using Windows.UI;
using WebGpuDemo.Rendering;
using WebGpuDemo.Views;
using Xunit;

namespace WebGpuDemo.PlayTests;

// The Controls page: the frame drivers, pausing and the SkiaSharp HUD the Overlay event draws.
public sealed partial class ApplicationTests
{
    private async Task<ControlsView> OpenControlsAsync()
    {
        await Button("Controls").ClickAsync();
        var page = await Page.EvaluateAsync(() => fixture.View.Controls);
        await WaitForFramesAsync(() => page.RenderView, () => page.FramesRendered, 3);
        return page;
    }

    private Task SelectDriverAsync(string driver) => Page.GetByTestId("DriverSelector").SelectOptionAsync(driver);

    [Fact]
    public async Task The_page_reports_the_Readback_path()
    {
        var page = await OpenControlsAsync();
        await Expect(Page.GetByTestId("RenderPathText")).ToHaveTextAsync("Render path: Readback");
        (await Page.EvaluateAsync(() => page.RenderView.RenderPath)).Should().Be(WebGpuRenderPath.Readback);
    }

    [Fact]
    public async Task The_page_shows_what_the_device_is_adapter_features_and_limits()
    {
        var page = await OpenControlsAsync();
        var info = await Page.EvaluateAsync(() => page.RenderView.DeviceInfo);
        info.Should().NotBeNull();
        info!.AdapterName.Should().NotBeNullOrWhiteSpace();
        (info.AdapterFeatures.Count + info.AdapterNativeFeatures.Count).Should().BeGreaterThan(0);
        info.Limits.MaxTextureDimension2D.Should().BeGreaterThanOrEqualTo(8192u);

        await Expect(Page.GetByTestId("AdapterText")).ToHaveTextAsync($"Adapter: {info.AdapterName} ({info.AdapterType}, {info.Backend})");
        await Expect(Page.GetByTestId("CapabilitiesText")).ToHaveTextAsync(
            $"Features: {info.Features.Count + info.NativeFeatures.Count} enabled of " +
            $"{info.AdapterFeatures.Count + info.AdapterNativeFeatures.Count} available, " +
            $"max texture {info.Limits.MaxTextureDimension2D}, max bind groups {info.Limits.MaxBindGroups}");
    }

    [Fact]
    public async Task The_HUD_overlay_shows_the_frame_rate_the_path_and_the_driver()
    {
        var page = await OpenControlsAsync();
        await fixture.Application.WaitForAsync(() => page.Hud.FramesPerSecond, fps => fps > 1, description: "a measured frame rate");
        page.Hud.Line1.Should().StartWith("fps ");
        page.Hud.Line2.Should().Be("path Readback   driver Continuous");

        var canvas = await CaptureAsync("ControlsViewport", "controls-hud");
        var panel = HudOverlay.PanelBounds;
        var hud = canvas.Region((int)panel.Left, (int)panel.Top, (int)panel.Width, (int)panel.Height);
        hud.Coverage(Color.FromArgb(255, HudOverlay.PanelColor.Red, HudOverlay.PanelColor.Green, HudOverlay.PanelColor.Blue)).Should().BeGreaterThan(0.5);
        hud.Coverage(Color.FromArgb(255, HudOverlay.EdgeColor.Red, HudOverlay.EdgeColor.Green, HudOverlay.EdgeColor.Blue), tolerance: 40).Should().BeGreaterThan(0.01);
        hud.Coverage(Color.FromArgb(255, 255, 255, 255), tolerance: 40).Should().BeGreaterThan(0.01, "the HUD's two lines of text");
    }

    [Fact]
    public async Task OnDemand_renders_exactly_one_frame_per_Invalidate()
    {
        var page = await OpenControlsAsync();
        await SelectDriverAsync("OnDemand");
        var settled = await SettledAsync(() => page.FramesRendered);

        await Button("Invalidate").ClickAsync();
        await fixture.Application.WaitForAsync(() => page.FramesRendered, count => count == settled + 1, description: "one more frame");
        (await SettledAsync(() => page.FramesRendered)).Should().Be(settled + 1);
        await Expect(Page.GetByTestId("FrameCount")).ToHaveTextAsync($"Frames: {settled + 1}");
        page.Hud.Line2.Should().Be("path Readback   driver OnDemand");
    }

    [Fact]
    public async Task Manual_renders_only_when_the_page_opens_a_frame()
    {
        var page = await OpenControlsAsync();
        await SelectDriverAsync("Manual");
        var settled = await SettledAsync(() => page.FramesRendered);

        // Invalidate is the OnDemand driver's call: under Manual it does nothing.
        await Button("Invalidate").ClickAsync();
        (await SettledAsync(() => page.FramesRendered)).Should().Be(settled);

        await Button("Render one frame").ClickAsync();
        await Expect(Page.GetByTestId("FrameCount")).ToHaveTextAsync($"Frames: {settled + 1}");
        (await SettledAsync(() => page.FramesRendered)).Should().Be(settled + 1);
        var canvas = await CaptureAsync("ControlsViewport");
        (1 - canvas.Coverage(Color.FromArgb(255, 11, 19, 43))).Should().BeGreaterThan(0.10, "the frame the button rendered shows the triangle");
    }

    [Fact]
    public async Task Pausing_stops_the_continuous_driver_and_resuming_starts_it_again()
    {
        var page = await OpenControlsAsync();
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Pause", Exact = true }).CheckAsync();
        var paused = await SettledAsync(() => page.FramesRendered);
        (await Page.EvaluateAsync(() => page.RenderView.IsPaused)).Should().BeTrue();

        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Pause", Exact = true }).UncheckAsync();
        await fixture.Application.WaitForAsync(() => page.FramesRendered, count => count >= paused + 10, description: "frames again");
    }

    [Fact]
    public async Task The_frame_rate_slider_sets_the_target_frame_rate()
    {
        var page = await OpenControlsAsync();
        // Arrow keys move a focused slider one step each.
        var slider = Page.GetByRole(AriaRole.Slider, new() { Name = "Target frame rate", Exact = true });
        await slider.PressAsync("ArrowLeft");
        await slider.PressAsync("ArrowLeft");
        await Expect(Page.GetByTestId("RateText")).ToHaveTextAsync("58");
        (await Page.EvaluateAsync(() => page.RenderView.TargetFrameRate)).Should().Be(58);
    }
}
