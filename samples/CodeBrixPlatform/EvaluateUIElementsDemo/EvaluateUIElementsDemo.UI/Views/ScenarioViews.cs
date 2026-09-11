using EvaluateUIElementsDemo.Models;
using EvaluateUIElementsDemo.Views.Scenarios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views;

/// <summary>
/// Builds the view for a scenario from the key the catalogue gives it. The catalogue lives in
/// .Core and knows nothing about XAML; this is the one place that maps a key to a control.
/// </summary>
public static class ScenarioViews
{
    /// <summary>A fresh view for a scenario key.</summary>
    /// <param name="viewKey">The key from <see cref="DemoScenario.ViewKey"/>.</param>
    /// <returns>The view, or a TextBlock that says the key is unknown.</returns>
    public static UIElement Create(string viewKey) => viewKey switch
    {
        DemoScenarios.Views.TextBox => new TextBoxFocusView(),
        DemoScenarios.Views.PasswordBox => new PasswordBoxFocusView(),
        DemoScenarios.Views.ProgressRing => new ProgressRingView(),
        DemoScenarios.Views.NavigationView => new NavigationViewPaneView(),
        DemoScenarios.Views.SplitView => new SplitViewOverlayView(),
        DemoScenarios.Views.ImageUniformToFill => new ImageUniformToFillView(),
        DemoScenarios.Views.ToggleSwitch => new ToggleSwitchView(),
        DemoScenarios.Views.ToggleButton => new ToggleButtonView(),
        _ => new TextBlock { Text = $"No view is registered for \"{viewKey}\"." },
    };
}
