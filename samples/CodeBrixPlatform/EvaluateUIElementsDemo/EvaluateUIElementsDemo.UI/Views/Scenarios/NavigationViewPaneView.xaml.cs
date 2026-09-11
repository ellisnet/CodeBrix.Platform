using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views.Scenarios;

/// <summary>Scenario 4: two NavigationViews, their panes opened and shut, their pages following the selection.</summary>
public sealed partial class NavigationViewPaneView : UserControl
{
    public NavigationViewPaneView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Start on Alpha in both, the way the survey's scenarios did.
        NavA.SelectedItem = NavA.MenuItems[0];
        NavB.SelectedItem = NavB.MenuItems[0];

        NavA.PaneOpened += (_, _) => ReportPane();
        NavA.PaneClosed += (_, _) => ReportPane();
        ReportPane();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        var page = ReferenceEquals(sender, NavA) ? PageA : PageB;
        page.Background = new SolidColorBrush(item.Tag as string == "Blue" ? Microsoft.UI.Colors.Blue : Microsoft.UI.Colors.Red);
    }

    private void OpenPane_Click(object sender, RoutedEventArgs e)
    {
        NavA.IsPaneOpen = true;
        NavB.IsPaneOpen = true;
        ReportPane();
    }

    private void ClosePane_Click(object sender, RoutedEventArgs e)
    {
        NavA.IsPaneOpen = false;
        NavB.IsPaneOpen = false;
        ReportPane();
    }

    private void ReportPane() =>
        PaneState.Text = $"IsPaneOpen (A): {NavA.IsPaneOpen}   DisplayMode (A): {NavA.DisplayMode}";
}
