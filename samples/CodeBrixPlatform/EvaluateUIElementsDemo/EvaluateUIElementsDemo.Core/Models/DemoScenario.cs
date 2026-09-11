namespace EvaluateUIElementsDemo.Models;

/// <summary>
/// One entry of the evaluation list: a numbered UI element or scenario, the short line the list
/// shows beside its button, the message the dialog shows before the scenario opens, and the fuller
/// instructions the scenario page shows above the element itself.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class DemoScenario
{
    /// <summary>The number on the button; the number the evaluation is reported by.</summary>
    public int Number { get; init; }

    /// <summary>The scenario's name.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>One or two lines for the list, beside the numbered button.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// The message the dialog shows before the scenario opens. Kept short: the dialog wraps at
    /// 74 characters and shows only a few lines.
    /// </summary>
    public string DialogText { get; init; } = string.Empty;

    /// <summary>What to do and what to look for, shown on the scenario page above the element.</summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>Which scenario view the UI builds for this entry.</summary>
    public string ViewKey { get; init; } = string.Empty;

    /// <summary>
    /// True once the scenario has been evaluated by hand and found to work the way it should.
    /// The list shows a verified scenario's button in green.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>The number and the title together, as the page and dialog head themselves.</summary>
    public string Label => $"{Number}. {Title}";
}
