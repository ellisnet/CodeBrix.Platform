using System;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using Microsoft.UI.Xaml;
using PlayTestDemo.PlayTests;
using Xunit;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace RecordingProbe.SendButton.Clicking;

// This standalone harness intentionally fails one test. Run via playtest-recording.py.
public sealed class ClickTests(AppFixture fixture) : PageTest(fixture.Application), IClassFixture<AppFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(null);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData("first row")]
    [InlineData("second row")]
    public async Task Clicking_changes_pixels(string label)
    {
        Assert.Contains("row", label);
        await Page.GetByTestId("DemoCheckBox").CheckAsync();
        await Expect(Page.GetByTestId("CheckStatus")).ToHaveTextAsync("Checked · 1 changes");
        await Page.EvaluateAsync(() => fixture.View.RequestedTheme = ElementTheme.Light);
        await fixture.Application.SetOrientationAsync(ScreenOrientation.Landscape);
        await Page.GetByTestId("DemoCheckBox").UncheckAsync();
    }

    [Fact]
    public async Task Failure_keeps_final_screenshot()
    {
        await Page.GetByTestId("DemoCheckBox").CheckAsync();
        throw new InvalidOperationException("Intentional recording-harness failure");
    }

    [Fact(Skip = "A skipped body produces no screenshots")]
    public void Skipped_test() { }
}
