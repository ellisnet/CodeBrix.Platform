using System.Threading.Tasks;
using EvaluateUIElementsDemo.Models;

namespace EvaluateUIElementsDemo.ViewModels;

/// <summary>
/// What the main page does for the view model when a scenario is chosen: the dialog that
/// introduces it, and the move to the page that shows it. The view model owns the sequence;
/// the page owns the platform plumbing (XamlRoot, dispatcher, Frame).
/// </summary>
public interface IScenarioPresenter
{
    /// <summary>Shows the scenario's introduction and completes when the person has dismissed it.</summary>
    /// <param name="scenario">The scenario about to open.</param>
    /// <returns>A task that completes when the introduction has been dismissed.</returns>
    Task IntroduceAsync(DemoScenario scenario);

    /// <summary>Replaces the main screen with the scenario's own view.</summary>
    /// <param name="scenario">The scenario to show.</param>
    void Show(DemoScenario scenario);
}
