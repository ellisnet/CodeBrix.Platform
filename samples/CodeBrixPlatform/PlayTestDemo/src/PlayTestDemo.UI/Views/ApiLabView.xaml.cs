using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace PlayTestDemo.Views;

/// <summary>Interactive examples for held keys, hidden and duplicate elements, the mouse wheel, option
/// selection, dialogs, launching, window events and screenshots. Shared by the desktop heads and the PlayTests.</summary>
public sealed partial class ApiLabView : UserControl
{
    public KeySampler KeySampler => Sampler;
    public int RowChanges { get; private set; }
    public int ColorChanges { get; private set; }
    public string ReportPath { get; } = Path.Combine(Path.GetTempPath(), "PlayTestDemo report.txt");

    public ApiLabView()
    {
        InitializeComponent();
        for (var i = 1; i <= 15; i++)
            WheelRows.Children.Add(new Button { Content = $"Wheel row {i}", Height = 40, HorizontalAlignment = HorizontalAlignment.Stretch });
        for (var i = 1; i <= 24; i++) RowCombo.Items.Add($"Row {i}");
        foreach (var color in new[] { "Red", "Green", "Blue", "Yellow" }) ColorList.Items.Add(color);
        WindowLifecycle.KeepOpen = false;
        Sampler.Sampled += (_, _) => KeyStatus.Text =
            $"Right arrow {(Sampler.IsDown(VirtualKey.Right) ? "down" : "up")} · {Sampler.SamplesDown} samples down";
        Loaded += (_, _) =>
        {
            WindowLifecycle.Changed += OnWindowChanged;
            OnWindowChanged(null, EventArgs.Empty);
        };
        Unloaded += (_, _) => WindowLifecycle.Changed -= OnWindowChanged;
    }

    private void OnWindowChanged(object sender, EventArgs e) => WindowStatus.Text = WindowLifecycle.Summary;
    private void OnKeepOpenChanged(object sender, RoutedEventArgs e) => WindowLifecycle.KeepOpen = KeepOpen.IsChecked == true;

    // The new status appears before the old one goes away, as when one dialog replaces another.
    private async void OnReplaceStatus(object sender, RoutedEventArgs e)
    {
        var replacement = new TextBlock { Text = "Status ready" };
        ReplacementHost.Children.Add(replacement);
        await Task.Delay(400);
        ReplacementHost.Children.Remove(ReplacementHost.Children.OfType<TextBlock>().First());
    }

    private void OnRowSelected(object sender, SelectionChangedEventArgs e)
    {
        RowChanges++;
        RowStatus.Text = $"{RowCombo.SelectedItem} · {RowChanges} changes";
    }

    private void OnColorsSelected(object sender, SelectionChangedEventArgs e)
    {
        ColorChanges++;
        var selected = ColorList.SelectedItems.Cast<string>().ToArray();
        ColorStatus.Text = selected.Length == 0 ? "No colors selected" : string.Join(", ", selected);
    }

    private async void OnDeleteFile(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete file?",
            Content = "This cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot,
        };
        DialogStatus.Text = await dialog.ShowAsync() == ContentDialogResult.Primary ? "Deleted" : "Kept";
    }

    private async void OnOpenReport(object sender, RoutedEventArgs e)
    {
        var launched = await Launcher.LaunchUriAsync(new Uri(ReportPath));
        LaunchStatus.Text = $"Launch result: {launched}";
    }

    private void OnRecolor(object sender, RoutedEventArgs e)
    {
        var animation = new ColorAnimation { To = Colors.SteelBlue, Duration = TimeSpan.FromMilliseconds(600), EnableDependentAnimation = true };
        Storyboard.SetTarget(animation, SwatchBrush);
        Storyboard.SetTargetProperty(animation, "Color");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}
