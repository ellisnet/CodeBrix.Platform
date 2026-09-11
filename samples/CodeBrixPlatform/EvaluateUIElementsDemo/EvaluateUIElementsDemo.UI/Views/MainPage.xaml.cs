using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using EvaluateUIElementsDemo.Models;
using EvaluateUIElementsDemo.ViewModels;
using Microsoft.UI.Xaml.Controls;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views;

/// <summary>
/// The main screen: the numbered list of scenarios. The page is the view model's presenter - it
/// shows the introduction dialog and moves to the scenario page - and nothing else.
/// </summary>
public sealed partial class MainPage : Page, IScenarioPresenter
{
    private readonly MainViewModel _model;

    public MainPage()
    {
        InitializeComponent();
        _model = new MainViewModel(this);
        DataContext = _model;
    }

    /// <inheritdoc />
    public async Task IntroduceAsync(DemoScenario scenario)
    {
        using var dialog = SimpleDialog.Create(() => XamlRoot, DispatcherQueue, scenario.DialogText, scenario.Label);
        await dialog.ShowAsync();
    }

    /// <inheritdoc />
    public void Show(DemoScenario scenario) => Frame.Navigate(typeof(ScenarioPage), scenario.Number);
}
