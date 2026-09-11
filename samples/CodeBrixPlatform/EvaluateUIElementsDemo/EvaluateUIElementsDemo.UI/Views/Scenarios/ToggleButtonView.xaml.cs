using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// ReSharper disable CheckNamespace

namespace EvaluateUIElementsDemo.Views.Scenarios;

/// <summary>Scenario 8: a ToggleButton checked and cleared, beside a plain Button.</summary>
public sealed partial class ToggleButtonView : UserControl
{
    private int _checkedCount;
    private int _uncheckedCount;

    public ToggleButtonView()
    {
        InitializeComponent();
    }

    private void Bold_Changed(object sender, RoutedEventArgs e)
    {
        if (Bold.IsChecked == true)
        {
            _checkedCount++;
        }
        else
        {
            _uncheckedCount++;
        }

        State.Text = $"Bold IsChecked: {Bold.IsChecked == true}   (Checked raised {_checkedCount} times, Unchecked raised {_uncheckedCount} times)";
    }
}
