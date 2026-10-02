using System.Threading.Tasks;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed partial class ApplicationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Checked_actions_use_input_and_are_idempotent(bool useSwitch)
    {
        var locator = Page.GetByTestId(useSwitch ? "DemoToggleSwitch" : "DemoCheckBox");
        await Expect(locator).Not.ToBeCheckedAsync();
        await locator.CheckAsync();
        await Expect(locator).ToBeCheckedAsync();
        await locator.CheckAsync();
        (await Page.EvaluateAsync(() => useSwitch ? fixture.Model.ToggleChanges : fixture.Model.CheckChanges)).Should().Be(1);
        await locator.UncheckAsync();
        await locator.UncheckAsync();
        (await locator.IsCheckedAsync()).Should().BeFalse();
        (await Page.EvaluateAsync(() => useSwitch ? fixture.Model.ToggleChanges : fixture.Model.CheckChanges)).Should().Be(2);
        await Expect(Page.GetByTestId(useSwitch ? "ToggleStatus" : "CheckStatus"))
            .ToHaveTextAsync(useSwitch ? "Off · 2 changes" : "Unchecked · 2 changes");
    }

    [Fact]
    public async Task Scroll_into_view_makes_a_clipped_control_clickable()
    {
        var locator = Button("Below the fold");
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync();
        (await Page.EvaluateAsync(() => fixture.Model.ScrollClicks)).Should().Be(1);
        await Expect(Page.GetByTestId("ScrollStatus")).ToHaveTextAsync("Target clicked 1 time(s).");
    }
}
