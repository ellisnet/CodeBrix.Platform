using System;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

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
    public void Preview_mode_reaches_the_running_application()
    {
        var arguments = Environment.GetCommandLineArgs();
        var headed = Array.IndexOf(arguments, "--headed") >= 0 || Array.IndexOf(arguments, "--nonheadless") >= 0;
        var headless = Array.IndexOf(arguments, "--headless") >= 0;
        var expected = headed ? false : headless || Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED") != "1";
        fixture.Application.Headless.Should().Be(expected);
    }

    [Fact]
    [PlayTestOrientation(ScreenOrientation.Landscape)]
    public async Task Landscape_preview_displays_the_demo() => await VerifyScreenAsync(ScreenOrientation.Landscape);

    [Fact]
    [PlayTestOrientation(ScreenOrientation.Portrait)]
    public async Task Portrait_preview_displays_the_demo() => await VerifyScreenAsync(ScreenOrientation.Portrait);

    [Theory]
    [PlayTestOrientation(ScreenOrientation.Landscape)]
    [InlineData(ScreenOrientation.Landscape, Traits = new[] { "PlayTestOrientation", "Landscape" })]
    [InlineData(ScreenOrientation.Portrait, Traits = new[] { "PlayTestOrientation", "Portrait" })]
    public async Task Case_orientation_overrides_the_method_preference(ScreenOrientation orientation) => await VerifyScreenAsync(orientation);

    private async Task VerifyScreenAsync(ScreenOrientation orientation)
    {
        fixture.Application.Orientation.Should().Be(orientation);
        await Expect(Page.GetByText("PlayTestDemo", new() { Exact = true })).ToBeVisibleAsync();
        var size = await Page.EvaluateAsync(() => (fixture.View.XamlRoot.Size.Width, fixture.View.XamlRoot.Size.Height));
        size.Should().Be(orientation == ScreenOrientation.Landscape ? (1920d, 1080d) : (1080d, 1920d));
        await Page.GetByTestId("DemoCheckBox").CheckAsync();
        await Expect(Page.GetByTestId("CheckStatus")).ToHaveTextAsync("Checked · 1 changes");
        await Page.ScreenshotAsync(new() { Path = Path.Combine("TestResults", "PlayTest", $"PlayTestDemo-{orientation}.png") });
    }
}
