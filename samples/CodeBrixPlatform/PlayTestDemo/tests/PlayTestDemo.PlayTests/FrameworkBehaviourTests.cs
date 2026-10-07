using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Media.Playback;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.Simple;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Xunit;

namespace PlayTestDemo.PlayTests;

/// <summary>Framework behaviour seen through real UI: keyboard focus on click, dialog text, the media element's
/// start-up, accessible names, list roles and window events. Each test builds its own content.</summary>
public sealed partial class ApplicationTests
{
    private Task<bool> HasFocusAsync(UIElement element) =>
        Page.EvaluateAsync(() => element.XamlRoot != null && FocusManager.GetFocusedElement(element.XamlRoot) == element);

    private async Task ClickEmptyBackgroundAsync(FrameworkElement root)
    {
        var point = await Page.EvaluateAsync(() =>
            root.TransformToVisual(null).TransformPoint(new Point(root.ActualWidth - 40, root.ActualHeight - 40)));
        await Page.Mouse.ClickAsync((float)point.X, (float)point.Y);
    }

    private static Grid FocusRoot(params UIElement[] children)
    {
        var panel = new StackPanel { Spacing = 20, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var child in children) panel.Children.Add(child);
        return new Grid { Background = new SolidColorBrush(Colors.WhiteSmoke), Children = { panel } };
    }

    private static Canvas FocusSurface(string id)
    {
        var surface = new Canvas { Width = 240, Height = 120, IsTabStop = true, Background = new SolidColorBrush(Colors.SteelBlue) };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(surface, id);
        return surface;
    }

    [Fact]
    public async Task A_click_on_a_focused_surface_keeps_its_keyboard_focus()
    {
        Grid root = null;
        Canvas surface = null;
        await Page.SetContentAsync(() => root = FocusRoot(surface = FocusSurface("FocusSurface")));
        (await Page.EvaluateAsync(() => surface.Focus(FocusState.Programmatic))).Should().BeTrue();
        (await HasFocusAsync(surface)).Should().BeTrue();

        await Page.GetByTestId("FocusSurface").ClickAsync();
        (await HasFocusAsync(surface)).Should().BeTrue();

        await ClickEmptyBackgroundAsync(root);
        (await HasFocusAsync(surface)).Should().BeFalse();
    }

    [Fact]
    public async Task A_click_inside_a_focused_text_box_keeps_focus_and_the_background_clears_it()
    {
        Grid root = null;
        TextBox box = null;
        await Page.SetContentAsync(() =>
        {
            box = new TextBox { Width = 300, Text = "Some text" };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(box, "FocusTextBox");
            return root = FocusRoot(box);
        });

        await Page.GetByTestId("FocusTextBox").ClickAsync();
        (await HasFocusAsync(box)).Should().BeTrue();
        await Page.GetByTestId("FocusTextBox").ClickAsync();
        (await HasFocusAsync(box)).Should().BeTrue();

        await ClickEmptyBackgroundAsync(root);
        (await HasFocusAsync(box)).Should().BeFalse();
    }

    [Fact]
    public async Task A_focused_surface_removed_by_its_own_click_does_not_keep_the_focus()
    {
        Grid root = null;
        Canvas surface = null;
        await Page.SetContentAsync(() => root = FocusRoot(surface = FocusSurface("VanishingSurface")));
        await Page.EvaluateAsync(() =>
        {
            surface.PointerPressed += (_, _) => ((Panel)surface.Parent).Children.Remove(surface);
            surface.Focus(FocusState.Programmatic);
        });
        (await HasFocusAsync(surface)).Should().BeTrue();

        await Page.GetByTestId("VanishingSurface").ClickAsync();
        (await Page.EvaluateAsync(() => surface.Parent == null)).Should().BeTrue();
        (await Page.EvaluateAsync(() => FocusManager.GetFocusedElement(root.XamlRoot) == surface)).Should().BeFalse();
    }

    [Fact]
    public async Task A_simple_dialog_shows_a_long_path_unbroken_and_wraps_it()
    {
        const string path = "/home/someone/Documents/Projects/an-unusually-long-folder-name-for-testing/and-another-level/output-file-name.pdf";
        Grid root = null;
        await Page.SetContentAsync(() => root = FocusRoot(new TextBlock { Text = "Dialog host" }));
        await Page.EvaluateAsync(() =>
        {
            var dialog = SimpleDialog.Create(() => root.XamlRoot, DispatcherQueue.GetForCurrentThread(), $"Saved to {path}", "Saved");
            _ = dialog.ShowAsync();
        });

        var shown = Page.GetByRole(AriaRole.Dialog).Filter(new() { HasText = "Saved" });
        await Expect(shown).ToBeVisibleAsync();
        await Expect(shown).ToContainTextAsync(path);
        var layout = await Page.EvaluateAsync(() =>
        {
            var text = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                .SelectMany(popup => Descendants(popup.Child).Prepend(popup.Child))
                .OfType<TextBlock>()
                .First(t => t.Text != null && t.Text.Contains("Saved to"));
            var dialog = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                .SelectMany(popup => Descendants(popup.Child).Prepend(popup.Child))
                .OfType<ContentDialog>()
                .First();
            return (text.TextWrapping, text.ActualWidth, text.ActualHeight, text.FontSize, DialogWidth: dialog.ActualWidth);
        });
        layout.TextWrapping.Should().Be(TextWrapping.Wrap);
        layout.ActualWidth.Should().BeLessThanOrEqualTo(layout.DialogWidth);
        layout.ActualHeight.Should().BeGreaterThan(layout.FontSize * 1.5);
        await shown.GetByRole(AriaRole.Button).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task The_media_engine_sees_auto_play_for_the_start_up_source_and_starts_it_once()
    {
        RecordingMediaEngine.EnsureRegistered();
        var uri = new Uri($"file:///tmp/playtest-demo-{Guid.NewGuid():N}.mp4");
        MediaPlayerElement element = null;
        await Page.SetContentAsync(() => element = new MediaPlayerElement
        {
            Width = 320,
            Height = 180,
            Source = MediaSource.CreateFromUri(uri),
        });

        await fixture.Application.WaitForAsync(() => RecordingMediaEngine.Sources(uri).Count, count => count > 0, description: "the start-up source");
        await fixture.Application.WaitForAsync(() => element.IsLoaded, loaded => loaded, description: "the loaded element");
        await Task.Delay(200, TestContext.Current.CancellationToken);
        RecordingMediaEngine.Sources(uri).Should().AllSatisfy(autoPlay => autoPlay.Should().BeTrue());
        (await Page.EvaluateAsync(() => RecordingMediaEngine.Plays(uri))).Should().Be(1);
    }

    [Fact]
    public async Task A_media_element_loaded_again_starts_its_open_source_again()
    {
        RecordingMediaEngine.EnsureRegistered();
        var uri = new Uri($"file:///tmp/playtest-demo-{Guid.NewGuid():N}.mp4");
        MediaPlayerElement element = null;
        Grid host = null;
        await Page.SetContentAsync(() => host = new Grid
        {
            Children = { (element = new MediaPlayerElement { Width = 320, Height = 180, Source = MediaSource.CreateFromUri(uri) }) },
        });
        await fixture.Application.WaitForAsync(() => element.IsLoaded, loaded => loaded, description: "the loaded element");
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var first = await Page.EvaluateAsync(() => RecordingMediaEngine.Plays(uri));

        // Nothing starts the open source on this load except the element itself (OnLoaded -> Play).
        await Page.EvaluateAsync(() => host.Children.Remove(element));
        await fixture.Application.WaitForAsync(() => element.IsLoaded, loaded => !loaded, description: "the unloaded element");
        await Page.EvaluateAsync(() => host.Children.Add(element));
        await fixture.Application.WaitForAsync(() => RecordingMediaEngine.Plays(uri), plays => plays > first, description: "the play on the second load");
        (await Page.EvaluateAsync(() => RecordingMediaEngine.Plays(uri))).Should().Be(first + 1);
    }

    [Fact]
    public async Task An_icon_only_button_is_named_by_its_tooltip()
    {
        await Page.SetContentAsync(() =>
        {
            var save = new Button { Content = new SymbolIcon(Symbol.Save) };
            ToolTipService.SetToolTip(save, "Save the document");
            var print = new Button { Content = new SymbolIcon(Symbol.Print) };
            ToolTipService.SetToolTip(print, new ToolTip { Content = new TextBlock { Text = "Print the document" } });
            var named = new Button { Content = new SymbolIcon(Symbol.Delete) };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(named, "Delete");
            ToolTipService.SetToolTip(named, "Remove the selected document");
            var labelled = new Button { Content = "Open" };
            ToolTipService.SetToolTip(labelled, "Open a document");
            return FocusRoot(save, print, named, labelled);
        });

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save the document", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Print the document", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Remove the selected document", Exact = true })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Open", Exact = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task List_and_grid_views_report_the_list_role_and_list_rows_stay_options()
    {
        await Page.SetContentAsync(() =>
        {
            var list = new ListView { Height = 160, Width = 200 };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(list, "Colors");
            foreach (var color in new[] { "Red", "Green" }) list.Items.Add(color);
            var grid = new GridView { Height = 160, Width = 300 };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(grid, "Shapes");
            foreach (var shape in new[] { "Circle", "Square" }) grid.Items.Add(shape);
            return FocusRoot(list, grid);
        });

        await Expect(Page.GetByRole(AriaRole.Listbox, new() { Name = "Colors", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Colors", Exact = true })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Listbox, new() { Name = "Shapes", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Red", Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Shapes", Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task An_accepted_close_reports_the_window_hidden_once()
    {
        await OpenLabAsync();
        WindowLifecycle.KeepOpen = false;
        var hidden = await Page.EvaluateAsync(() => WindowLifecycle.Hidden);
        (await fixture.Application.RequestCloseAsync()).Should().BeTrue();
        (await Page.EvaluateAsync(() => WindowLifecycle.Hidden)).Should().Be(hidden + 1);
    }

    /// <summary>A media engine that plays nothing and records what the player hands it.</summary>
    private sealed class RecordingMediaEngine(MediaPlayer player) : IMediaPlayerExtension
    {
        private static readonly object Gate = new();
        private static readonly List<(Uri Uri, bool AutoPlay)> Initialized = new();
        private static readonly List<Uri> Played = new();

        public static void EnsureRegistered()
        {
            if (!ApiExtensibility.IsRegistered<IMediaPlayerExtension>())
                ApiExtensibility.Register<MediaPlayer>(typeof(IMediaPlayerExtension), owner => new RecordingMediaEngine(owner));
        }

        public static List<bool> Sources(Uri uri)
        {
            lock (Gate) return Initialized.Where(s => s.Uri == uri).Select(s => s.AutoPlay).ToList();
        }

        public static int Plays(Uri uri)
        {
            lock (Gate) return Played.Count(p => p == uri);
        }

        private Uri Current => (player.Source as MediaSource)?.Uri;

        public IMediaPlayerEventsExtension Events { get; set; }
        public double PlaybackRate { get; set; } = 1;
        public bool IsLoopingEnabled { get; set; }
        public bool IsLoopingAllEnabled { get; set; }
        public MediaPlayerState CurrentState => MediaPlayerState.Closed;
        public TimeSpan NaturalDuration => TimeSpan.Zero;
        public bool IsProtected => false;
        public double BufferingProgress => 0;
        public bool CanPause => false;
        public bool CanSeek => false;
        public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }
        public MediaPlayerAudioCategory AudioCategory { get; set; }
        public TimeSpan TimelineControllerPositionOffset { get; set; }
        public bool RealTimePlayback { get; set; }
        public double AudioBalance { get; set; }
        public TimeSpan Position { get; set; }
        public bool? IsVideo => null;

        // Like the real engine: a source being opened is Opening, a started one is Playing.
        public void InitializeSource()
        {
            if (Current is { } uri)
            {
                lock (Gate) Initialized.Add((uri, player.AutoPlay));
                player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
            }
        }

        public void Play()
        {
            if (Current is { } uri)
            {
                lock (Gate) Played.Add(uri);
                player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
            }
        }

        public void SetTransportControlsBounds(Rect bounds) { }
        public void SetUriSource(Uri value) { }
        public void SetFileSource(IStorageFile file) { }
        public void SetStreamSource(IRandomAccessStream stream) { }
        public void SetMediaSource(IMediaSource source) { }
        public void StepForwardOneFrame() { }
        public void StepBackwardOneFrame() { }
        public void SetSurfaceSize(Size size) { }
        public void Pause() { }
        public void Stop() { }
        public void ToggleMute() { }
        public void OnVolumeChanged() { }
        public void Initialize() { }
        public void OnOptionChanged(string name, object value) { }
        public void PreviousTrack() { }
        public void NextTrack() { }
        public void Dispose() { }
    }
}
