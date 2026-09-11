using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views.Scenarios;

/// <summary>Scenario 5: a SplitView whose pane is opened and shut in each display mode.</summary>
public sealed partial class SplitViewOverlayView : UserControl
{
    public SplitViewOverlayView()
    {
        InitializeComponent();
        Loaded += (_, _) => ReportPane();
    }

    private void OpenPane_Click(object sender, RoutedEventArgs e) => Shell.IsPaneOpen = true;

    private void ClosePane_Click(object sender, RoutedEventArgs e) => Shell.IsPaneOpen = false;

    private void DisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Shell != null && SelectedText(DisplayModeBox) is { } text
            && Enum.TryParse<SplitViewDisplayMode>(text, out var mode))
        {
            Shell.DisplayMode = mode;
            ReportPane();
        }
    }

    private void DismissMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Shell != null && SelectedText(DismissModeBox) is { } text
            && Enum.TryParse<LightDismissOverlayMode>(text, out var mode))
        {
            Shell.LightDismissOverlayMode = mode;
            ReportPane();
        }
    }

    private void Shell_PaneOpened(SplitView sender, object args) => ReportPane();

    private void Shell_PaneClosed(SplitView sender, object args) => ReportPane();

    private static string SelectedText(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content as string;

    private void ReportPane()
    {
        if (Shell != null && PaneState != null)
        {
            PaneState.Text = $"IsPaneOpen: {Shell.IsPaneOpen}   DisplayMode: {Shell.DisplayMode}   LightDismissOverlayMode: {Shell.LightDismissOverlayMode}";
        }
    }
}
