using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlayTestDemo.Views;
using SilverAssertions;
using SkiaSharp;
using Windows.System;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed partial class ApplicationTests
{
    private ApiLabView Lab => (ApiLabView)fixture.View.Content;
    private Task OpenLabAsync() => Button("Try the API lab").ClickAsync();

    private static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    [Fact]
    public async Task Held_key_stays_down_across_samples_until_released()
    {
        await OpenLabAsync();
        await Page.GetByTestId("KeySampler").ClickAsync();
        await Page.Keyboard.DownAsync("ArrowRight");
        (await Page.EvaluateAsync(() => Lab.KeySampler.IsDown(VirtualKey.Right))).Should().BeTrue();
        await fixture.Application.WaitForAsync(() => Lab.KeySampler.SamplesDown, samples => samples >= 3, description: "samples with the key down");
        await Expect(Page.GetByTestId("KeyStatus")).ToContainTextAsync("Right arrow down");
        await Page.Keyboard.UpAsync("ArrowRight");
        await Expect(Page.GetByTestId("KeyStatus")).ToContainTextAsync("Right arrow up");
        var released = await Page.EvaluateAsync(() => Lab.KeySampler.SamplesDown);
        await Task.Delay(150, TestContext.Current.CancellationToken);
        (await Page.EvaluateAsync(() => Lab.KeySampler.SamplesDown)).Should().Be(released);
    }

    [Fact]
    public async Task Press_with_a_delay_holds_the_key_long_enough_to_be_sampled()
    {
        await OpenLabAsync();
        var sampler = Page.GetByTestId("KeySampler");
        await sampler.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight", new() { Delay = 200 });
        var keyboardSamples = await Page.EvaluateAsync(() => Lab.KeySampler.SamplesDown);
        keyboardSamples.Should().BeGreaterThanOrEqualTo(3);
        await sampler.PressAsync("ArrowRight", new() { Delay = 200 });
        (await Page.EvaluateAsync(() => Lab.KeySampler.SamplesDown)).Should().BeGreaterThanOrEqualTo(keyboardSamples + 3);
        (await Page.EvaluateAsync(() => Lab.KeySampler.IsDown(VirtualKey.Right))).Should().BeFalse();
        var chord = () => Page.Keyboard.DownAsync("Shift+ArrowRight");
        await chord.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Keys_still_held_are_released_when_the_page_is_reset()
    {
        await OpenLabAsync();
        await Page.GetByTestId("KeySampler").ClickAsync();
        await Page.Keyboard.DownAsync("ArrowRight");
        var sampler = await Page.EvaluateAsync(() => Lab.KeySampler);
        await fixture.ResetAsync(null);
        (await Page.EvaluateAsync(() => sampler.KeyUps)).Should().Be(1);
        await fixture.Application.WaitForAsync(() => sampler.IsDown(VirtualKey.Right), down => !down, description: "the released key");
    }

    [Fact]
    public async Task Hidden_elements_match_only_when_included()
    {
        await OpenLabAsync();
        var twins = Page.GetByText("Twin label", new() { Exact = true });
        await Expect(twins).ToHaveCountAsync(1);
        await Expect(twins).ToHaveTextAsync("Twin label");
        await Expect(Page.GetByText("Twin label", new() { Exact = true, IncludeHidden = true })).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId("HiddenTwin")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("HiddenTwin")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("HiddenTwin", new() { IncludeHidden = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByLabel("Hidden action")).ToHaveCountAsync(0);
        await Expect(Page.GetByLabel("Hidden action", new() { IncludeHidden = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Hidden action", IncludeHidden = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Cells_an_items_repeater_keeps_for_reuse_do_not_match()
    {
        ScrollViewer viewer = null;
        await Page.SetContentAsync(() =>
        {
            var repeater = new ItemsRepeater
            {
                ItemsSource = Enumerable.Range(1, 300).Select(i => $"Cell {i}").ToList(),
                ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Height='40' Text='{Binding}' /></DataTemplate>"),
            };
            viewer = new ScrollViewer { Height = 400, VerticalAlignment = VerticalAlignment.Top, Content = repeater };
            return new Grid { Children = { viewer } };
        });
        await Expect(Page.GetByText("Cell 1", new() { Exact = true })).ToBeVisibleAsync();
        // A much smaller viewport clears most realized cells; the repeater parks them for reuse.
        await Page.EvaluateAsync(() => viewer.Height = 40);
        await Expect(Page.GetByText("Cell 1", new() { Exact = true })).ToBeVisibleAsync();
        var parked = await Page.EvaluateAsync(() => Descendants(viewer).OfType<TextBlock>()
            .Count(text => text.TransformToVisual(null).TransformPoint(default).Y < -5000));
        parked.Should().BeGreaterThan(0);
        var cells = Page.GetByText(new Regex(@"^Cell \d+$"));
        var count = await cells.CountAsync();
        count.Should().BeGreaterThan(0);
        for (var i = 0; i < count; i++)
            (await cells.Nth(i).BoundingBoxAsync()).Y.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Strict_mode_waits_for_a_brief_duplicate_to_resolve()
    {
        await OpenLabAsync();
        await Button("Replace the status").ClickAsync();
        (await Page.GetByText("Status ready", new() { Exact = true, IncludeHidden = true }).CountAsync()).Should().Be(2);
        await Expect(Page.GetByText("Status ready", new() { Exact = true })).ToHaveTextAsync("Status ready");
        await Expect(Page.GetByText("Status ready", new() { Exact = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Strict_mode_violation_is_reported_after_the_timeout_and_immediately_outside_retries()
    {
        await OpenLabAsync();
        var duplicate = Page.GetByText("Duplicate label", new() { Exact = true });
        var visible = () => Expect(duplicate).ToBeVisibleAsync(new() { Timeout = 300 });
        await visible.Should().ThrowAsync<PlayTestException>().WithMessage("Strict mode violation*2 elements*still ambiguous*300ms*");
        var single = () => duplicate.IsVisibleAsync();
        await single.Should().ThrowAsync<PlayTestException>().WithMessage("Strict mode violation*2 elements*");
    }

    [Fact]
    public async Task A_disabled_ancestor_control_disables_its_descendants()
    {
        await OpenLabAsync();
        await Expect(Page.GetByTestId("InsideDisabledControl")).ToBeDisabledAsync();
        await Expect(Button("Delete file")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Wheel_over_a_button_scrolls_its_scroll_viewer()
    {
        await OpenLabAsync();
        var row = await Button("Wheel row 2").BoundingBoxAsync();
        await Page.Mouse.MoveAsync(row.X + row.Width / 2, row.Y + row.Height / 2);
        await Page.Mouse.WheelAsync(0, 120);
        await fixture.Application.WaitForAsync(() => ((ScrollViewer)Lab.FindName("WheelScroll")).VerticalOffset, offset => offset > 0,
            description: "the scrolled offset");
    }

    [Fact]
    public async Task Select_option_chooses_an_unrealized_combo_box_row_with_its_normal_event()
    {
        await OpenLabAsync();
        var combo = Page.GetByRole(AriaRole.Combobox, new() { Name = "Row", Exact = true });
        (await combo.SelectOptionAsync("Row 24")).Should().Equal("Row 24");
        await Expect(Page.GetByTestId("RowStatus")).ToHaveTextAsync("Row 24 · 1 changes");
        await Expect(combo).ToHaveValueAsync("Row 24");
        (await combo.SelectOptionAsync(new SelectOptionValue { Index = 2 })).Should().Equal("Row 3");
        await Expect(Page.GetByTestId("RowStatus")).ToHaveTextAsync("Row 3 · 2 changes");
        (await combo.SelectOptionAsync(new SelectOptionValue { Label = "Row 3" })).Should().Equal("Row 3");
        (await Page.EvaluateAsync(() => Lab.RowChanges)).Should().Be(2);
        var missing = () => combo.SelectOptionAsync("Row 99", new() { Timeout = 200 });
        await missing.Should().ThrowAsync<PlayTestException>().WithMessage("*Row 99*");
        var notSelector = () => Page.GetByTestId("RowStatus").SelectOptionAsync("Row 1");
        await notSelector.Should().ThrowAsync<PlayTestException>().WithMessage("*requires a ComboBox*");
    }

    [Fact]
    public async Task Select_option_sets_exactly_the_requested_rows_of_a_multiple_selection_list()
    {
        await OpenLabAsync();
        var colors = Page.GetByTestId("ColorList");
        (await colors.SelectOptionAsync(new[] { "Yellow", "Green" })).Should().Equal("Green", "Yellow");
        await Expect(Page.GetByTestId("ColorStatus")).ToHaveTextAsync("Green, Yellow");
        (await colors.SelectOptionAsync(new[] { "Blue" })).Should().Equal("Blue");
        await Expect(Page.GetByTestId("ColorStatus")).ToHaveTextAsync("Blue");
        (await colors.SelectOptionAsync(Array.Empty<string>())).Should().BeEmpty();
        await Expect(Page.GetByTestId("ColorStatus")).ToHaveTextAsync("No colors selected");
    }

    [Fact]
    public async Task Dialog_text_filter_matches_the_title_of_a_string_content_dialog()
    {
        await OpenLabAsync();
        await Button("Delete file").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog).Filter(new() { HasText = "Delete file?" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToContainTextAsync("This cannot be undone.");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("DialogStatus")).ToHaveTextAsync("Deleted");
    }

    [Fact]
    public async Task Launches_are_recorded_and_never_start_a_process()
    {
        await OpenLabAsync();
        var launcher = fixture.Application.Launcher;
        await Page.GetByRole(AriaRole.Link, new() { Name = "Project website", Exact = true }).ClickAsync();
        await fixture.Application.WaitForAsync(() => launcher.LaunchedUris.Count, count => count == 1, description: "the link launch");
        launcher.LaunchedUris[0].Should().Be(new Uri("https://example.com/playtest-demo"));
        await Button("Open report").ClickAsync();
        await Expect(Page.GetByTestId("LaunchStatus")).ToHaveTextAsync("Launch result: True");
        launcher.LaunchedUris.Should().HaveCount(2);
        launcher.LaunchedUris[1].Should().Be(new Uri(Lab.ReportPath));
        launcher.LaunchResult = false;
        await Button("Open report").ClickAsync();
        await Expect(Page.GetByTestId("LaunchStatus")).ToHaveTextAsync("Launch result: False");
    }

    [Fact]
    public async Task A_cancelled_close_keeps_the_window_and_an_accepted_close_hides_it_until_the_next_reset()
    {
        await OpenLabAsync();
        var app = fixture.Application;
        var cancelled = await Page.EvaluateAsync(() => WindowLifecycle.CancelledCloses);
        var closed = await Page.EvaluateAsync(() => WindowLifecycle.Closed);
        var hidden = await Page.EvaluateAsync(() => WindowLifecycle.Hidden);
        var shown = await Page.EvaluateAsync(() => WindowLifecycle.Shown);
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Unsaved changes (refuse to close)", Exact = true }).CheckAsync();
        (await app.RequestCloseAsync()).Should().BeFalse();
        (await Page.EvaluateAsync(() => WindowLifecycle.CancelledCloses)).Should().Be(cancelled + 1);
        (await Page.EvaluateAsync(() => WindowLifecycle.Closed)).Should().Be(closed);
        await Expect(Page.GetByTestId("WindowStatus")).ToContainTextAsync($"cancelled {cancelled + 1}");
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Unsaved changes (refuse to close)", Exact = true }).UncheckAsync();
        (await app.RequestCloseAsync()).Should().BeTrue();
        (await Page.EvaluateAsync(() => WindowLifecycle.Closed)).Should().Be(closed + 1);
        (await Page.EvaluateAsync(() => WindowLifecycle.Hidden)).Should().BeGreaterThan(hidden);
        await fixture.ResetAsync(null);
        (await Page.EvaluateAsync(() => WindowLifecycle.Shown)).Should().Be(shown + 1);
        await Expect(Page.GetByText("PlayTestDemo", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Minimize_and_restore_raise_visibility_and_activation_events()
    {
        await OpenLabAsync();
        var app = fixture.Application;
        var hidden = await Page.EvaluateAsync(() => WindowLifecycle.Hidden);
        var shown = await Page.EvaluateAsync(() => WindowLifecycle.Shown);
        var deactivated = await Page.EvaluateAsync(() => WindowLifecycle.Deactivated);
        await app.MinimizeAsync();
        (await Page.EvaluateAsync(() => WindowLifecycle.Hidden)).Should().Be(hidden + 1);
        (await Page.EvaluateAsync(() => WindowLifecycle.Deactivated)).Should().Be(deactivated + 1);
        await app.MinimizeAsync();
        (await Page.EvaluateAsync(() => WindowLifecycle.Hidden)).Should().Be(hidden + 1);
        await app.RestoreAsync();
        (await Page.EvaluateAsync(() => WindowLifecycle.Shown)).Should().Be(shown + 1);
        await app.MinimizeAsync();
        await fixture.ResetAsync(null);
        (await Page.EvaluateAsync(() => WindowLifecycle.Shown)).Should().Be(shown + 2);
    }

    [Fact]
    public async Task The_window_the_application_created_is_reachable()
    {
        var window = fixture.Application.Window;
        (await Page.EvaluateAsync(() => window.Content)).Should().BeSameAs(fixture.View);
        (await Page.EvaluateAsync(() => window.Title)).Should().Be("PlayTestDemo");
    }

    [Fact]
    public async Task Element_screenshot_is_cropped_to_the_element()
    {
        await OpenLabAsync();
        var png = await Page.GetByTestId("Swatch").ScreenshotAsync();
        using var image = SKBitmap.Decode(png);
        (image.Width, image.Height).Should().Be((160, 80));
        image.GetPixel(5, 5).Should().Be(new SKColor(0x2E, 0x7D, 0x32));
        image.GetPixel(80, 40).Should().Be(SKColors.White);
    }

    [Fact]
    public async Task Stable_screenshot_waits_for_an_animation_to_finish()
    {
        await OpenLabAsync();
        await Button("Recolor the swatch").ClickAsync();
        var box = await Page.GetByTestId("Swatch").BoundingBoxAsync();
        var png = await Page.ScreenshotAsync(new() { Stable = true });
        using var image = SKBitmap.Decode(png);
        image.GetPixel((int)box.X + 5, (int)box.Y + 5).Should().Be(new SKColor(0x46, 0x82, 0xB4));
    }
}
