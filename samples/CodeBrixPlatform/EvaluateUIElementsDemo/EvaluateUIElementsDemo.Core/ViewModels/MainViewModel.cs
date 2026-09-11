using System.Collections.Generic;
using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using EvaluateUIElementsDemo.Models;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.ViewModels;

/// <summary>
/// The view model behind the main list: the scenarios, and the one command every numbered
/// button runs. Choosing a scenario introduces it through the presenter and then shows it.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class MainViewModel : SimpleViewModel
{
    private readonly IScenarioPresenter _presenter;

    /// <summary>Initializes the list and its command.</summary>
    /// <param name="presenter">The page's side of introducing and showing a scenario.</param>
    public MainViewModel(IScenarioPresenter presenter)
    {
        _presenter = presenter;

        if (IsDesignMode(true)) { return; } //Leave as the first line of constructor (after field assignment)

        OpenScenarioCommand = new SimpleCommand(async parameter =>
        {
            if (parameter is DemoScenario scenario)
            {
                await OpenAsync(scenario);
            }
        });
    }

    /// <summary>The scenarios, in the order the list shows them.</summary>
    public IReadOnlyList<DemoScenario> Scenarios { get; } = DemoScenarios.All;

    /// <summary>Opens the scenario passed as the command parameter.</summary>
    public SimpleCommand OpenScenarioCommand { get; }

    /// <summary>The scenario that was opened last, so the page can say where it has been.</summary>
    public DemoScenario LastOpened
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private async Task OpenAsync(DemoScenario scenario)
    {
        await _presenter.IntroduceAsync(scenario);
        LastOpened = scenario;
        _presenter.Show(scenario);
    }
}
