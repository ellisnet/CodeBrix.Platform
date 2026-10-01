using System;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.UI.AdvancedTextEdit;
using CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using PlayTestDemo.Views;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed partial class ApplicationTests
{
    [Fact]
    public async Task Element_tab_headers_expose_their_text_and_respect_explicit_names()
    {
        await OpenDesktopAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Score", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Tab, new() { Name = "Score preview", Exact = true })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Tab, new() { Name = "Score preview", Exact = true }).ClickAsync();
        (await Page.EvaluateAsync(() => ((ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(
            (Microsoft.UI.Xaml.Controls.TabViewItem)Desktop.Tabs.TabItems[1])
            .GetPattern(PatternInterface.SelectionItem)).IsSelected)).Should().BeTrue();
    }

    private DesktopControlsView Desktop => (DesktopControlsView)fixture.View.Content;
    private Locator SourceEditor => Page.GetByRole(AriaRole.Textbox, new() { Name = "Source editor", Exact = true });
    private Locator DesktopTool(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name + ", Editor tools", Exact = true });
    private Locator DesktopMenu(string name) => Page.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true });
    private Task OpenDesktopAsync() => Button("Try desktop controls").ClickAsync();

    [Fact]
    public async Task Composite_editor_typing_keeps_focus_and_handles_first_key_newlines_and_selection()
    {
        await OpenDesktopAsync();
        await SourceEditor.PressSequentiallyAsync("c");
        await SourceEditor.PressSequentiallyAsync("4\nd4");
        await Expect(SourceEditor).ToHaveValueAsync("c4\nd4");
        await SourceEditor.PressAsync("Control+Home");
        await SourceEditor.PressAsync("Shift+ArrowRight");
        await SourceEditor.PressSequentiallyAsync("g");
        await Expect(SourceEditor).ToHaveValueAsync("g4\nd4");
        (await Page.EvaluateAsync(() => FrameworkElementAutomationPeer.CreatePeerForElement(Desktop.Editor).HasKeyboardFocus())).Should().BeTrue();
    }

    [Fact]
    public async Task Value_provider_fill_preserves_undo_and_rejects_read_only_replacement()
    {
        await OpenDesktopAsync();
        await SourceEditor.FillAsync("first");
        await SourceEditor.FillAsync("second");
        await SourceEditor.PressAsync("Control+z");
        await Expect(SourceEditor).ToHaveValueAsync("first");
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Read only", Exact = true }).CheckAsync();
        var fill = () => SourceEditor.FillAsync("forbidden", new() { Timeout = 150 });
        await fill.Should().ThrowAsync<PlayTestException>().WithMessage("*editable element*");
        await SourceEditor.PressSequentiallyAsync("forbidden");
        await Expect(SourceEditor).ToHaveValueAsync("first");
        (await Page.EvaluateAsync(() => ((IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Desktop.Editor).GetPattern(PatternInterface.Value)).IsReadOnly)).Should().BeTrue();
    }

    [Fact]
    public async Task Value_provider_preserves_protected_sections_but_allows_typing_outside_them()
    {
        await OpenDesktopAsync();
        await SourceEditor.FillAsync("protected tail");
        await Page.EvaluateAsync(() =>
        {
            var protection = new TextSegmentReadOnlySectionProvider<TextSegment>(Desktop.Editor.Document);
            protection.Segments.Add(new TextSegment { StartOffset = 0, Length = 9 });
            Desktop.Editor.TextArea.ReadOnlySectionProvider = protection;
        });
        var replace = () => SourceEditor.FillAsync("erased", new() { Timeout = 150 });
        await replace.Should().ThrowAsync<PlayTestException>();
        await Expect(SourceEditor).ToHaveValueAsync("protected tail");
        await SourceEditor.PressAsync("Control+End");
        await SourceEditor.PressSequentiallyAsync("!");
        await Expect(SourceEditor).ToHaveValueAsync("protected tail!");
        await SourceEditor.PressAsync("Control+z");
        await Expect(SourceEditor).ToHaveValueAsync("protected tail");
    }

    [Fact]
    public async Task Empty_completion_popup_does_not_swallow_enter()
    {
        await OpenDesktopAsync();
        await SourceEditor.FillAsync("c4");
        await Page.EvaluateAsync(() => new CompletionWindow(Desktop.Editor.TextArea).Show());
        await SourceEditor.PressAsync("Enter");
        await SourceEditor.PressSequentiallyAsync("d4");
        await Expect(SourceEditor).ToHaveValueAsync("c4\nd4");
    }

    [Fact]
    public async Task Toolbar_commands_toggles_and_disabled_buttons_use_real_input()
    {
        await OpenDesktopAsync();
        await DesktopTool("Run").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
        await DesktopTool("Pin").CheckAsync();
        await DesktopTool("Pin").CheckAsync();
        await Expect(DesktopTool("Pin")).ToBeCheckedAsync();
        await DesktopTool("Pin").UncheckAsync();
        await Expect(DesktopTool("Pin")).Not.ToBeCheckedAsync();
        await Expect(DesktopTool("Unavailable")).ToBeDisabledAsync();
        var disabled = () => DesktopTool("Unavailable").ClickAsync(new() { Timeout = 150 });
        await disabled.Should().ThrowAsync<PlayTestException>();
        var disabledDrag = () => DesktopTool("Unavailable").DragByAsync(10, 0, new() { Timeout = 150 });
        await disabledDrag.Should().ThrowAsync<PlayTestException>();
        await DesktopTool("Run").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(2);
    }

    [Fact]
    public async Task Split_toolbar_arrow_opens_flyout_without_running_the_primary_command()
    {
        await OpenDesktopAsync();
        var actions = DesktopTool("Actions");
        await actions.ClickAsync(new() { Position = new() { X = 8, Y = 12 } });
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
        var bounds = await actions.BoundingBoxAsync();
        await actions.ClickAsync(new() { Position = new() { X = bounds.Width - 16, Y = bounds.Height / 2 } });
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
        await DesktopMenu("Choose action").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(2);
    }

    [Fact]
    public async Task Toolbar_overflow_preserves_toggle_state_and_command_bindings()
    {
        await OpenDesktopAsync();
        await DesktopTool("Pin").CheckAsync();
        await Page.GetByRole(AriaRole.Checkbox, new() { Name = "Narrow toolbar", Exact = true }).CheckAsync();
        await Page.GetByType<ToolBarOverflowButton>().ClickAsync();
        await Expect(DesktopTool("Pin")).ToBeCheckedAsync();
        await DesktopTool("Actions").ClickAsync(new() { Position = new() { X = 8, Y = 12 } });
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
    }

    [Fact]
    public async Task Toggle_menu_item_check_completes_after_its_flyout_dismisses()
    {
        await OpenDesktopAsync();
        await DesktopMenu("View").ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Show details", Exact = true }).CheckAsync();
        (await Page.EvaluateAsync(() => Desktop.MenuToggle.IsChecked)).Should().BeTrue();
        await DesktopMenu("View").ClickAsync();
        await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Show details", Exact = true }).UncheckAsync();
        (await Page.EvaluateAsync(() => Desktop.MenuToggle.IsChecked)).Should().BeFalse();
    }

    [Fact]
    public async Task Hover_opens_nested_menu_and_right_click_opens_editor_context_menu()
    {
        await OpenDesktopAsync();
        await DesktopMenu("View").ClickAsync();
        await DesktopMenu("More").HoverAsync();
        await DesktopMenu("Record action").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
        await SourceEditor.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 100, Y = 30 } });
        await DesktopMenu("Context action").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.ContextCount)).Should().Be(1);
    }

    [Fact]
    public async Task Double_click_uses_pointer_gestures_and_invalid_positions_are_rejected()
    {
        await OpenDesktopAsync();
        await SourceEditor.ClickAsync(new() { ClickCount = 2, Position = new() { X = 100, Y = 30 } });
        (await Page.EvaluateAsync(() => Desktop.DoubleClickCount)).Should().Be(1);
        var invalid = () => SourceEditor.DragByAsync(10, 0, new() { Position = new() { X = -1, Y = 0 } });
        await invalid.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await DesktopTool("Run").ClickAsync();
        (await Page.EvaluateAsync(() => Desktop.CommandCount)).Should().Be(1);
    }
}
