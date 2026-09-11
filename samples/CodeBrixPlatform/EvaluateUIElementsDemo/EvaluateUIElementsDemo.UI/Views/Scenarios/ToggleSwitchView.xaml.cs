using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views.Scenarios;

/// <summary>Scenario 7: ToggleSwitches reaching their off state by different routes.</summary>
public sealed partial class ToggleSwitchView : UserControl
{
    public ToggleSwitchView()
    {
        InitializeComponent();
        Loaded += (_, _) => Report();
    }

    private void AllOn_Click(object sender, RoutedEventArgs e)
    {
        SwitchA.IsOn = true;
        SwitchB.IsOn = true;
    }

    private void AllOff_Click(object sender, RoutedEventArgs e)
    {
        SwitchA.IsOn = false;
        SwitchB.IsOn = false;
    }

    private void Flip_Click(object sender, RoutedEventArgs e)
    {
        SwitchA.IsOn = !SwitchA.IsOn;
        SwitchB.IsOn = !SwitchB.IsOn;
    }

    private void Switch_Toggled(object sender, RoutedEventArgs e) => Report();

    private void Report()
    {
        if (State != null && SwitchA != null && SwitchB != null)
        {
            State.Text = $"A IsOn: {SwitchA.IsOn}   B IsOn: {SwitchB.IsOn}";
        }
    }
}
