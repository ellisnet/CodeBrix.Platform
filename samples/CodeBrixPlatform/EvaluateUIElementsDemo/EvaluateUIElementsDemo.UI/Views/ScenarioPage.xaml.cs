using EvaluateUIElementsDemo.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views;

/// <summary>
/// The page that replaces the main list while one scenario is being evaluated: a Back button,
/// the scenario's title and instructions, and the scenario's own view underneath.
/// </summary>
public sealed partial class ScenarioPage : Page
{
    public ScenarioPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var scenario = e.Parameter is int number ? DemoScenarios.Find(number) : null;
        if (scenario == null)
        {
            TitleText.Text = "Unknown scenario";
            InstructionsText.Text = "The list did not say which scenario to show.";
            ScenarioHost.Content = null;
            return;
        }

        TitleText.Text = scenario.Label;
        InstructionsText.Text = scenario.Instructions;
        ScenarioHost.Content = ScenarioViews.Create(scenario.ViewKey);
    }

    /// <inheritdoc />
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Let the scenario's controls go when the page does, so the next visit starts fresh.
        ScenarioHost.Content = null;
        base.OnNavigatedFrom(e);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
