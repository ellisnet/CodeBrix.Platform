using System;
using CodeBrix.Platform.UI.FlexPanel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelDemo.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        DirectionCombo.ItemsSource = Enum.GetNames(typeof(FlexDirection));
        WrapCombo.ItemsSource = Enum.GetNames(typeof(FlexWrap));
        JustifyCombo.ItemsSource = Enum.GetNames(typeof(FlexJustify));
        AlignItemsCombo.ItemsSource = Enum.GetNames(typeof(FlexAlignItems));
        AlignContentCombo.ItemsSource = Enum.GetNames(typeof(FlexAlignContent));

        // Match the playground's XAML-declared starting values.
        DirectionCombo.SelectedItem = nameof(FlexDirection.Row);
        WrapCombo.SelectedItem = nameof(FlexWrap.Wrap);
        JustifyCombo.SelectedItem = nameof(FlexJustify.Start);
        AlignItemsCombo.SelectedItem = nameof(FlexAlignItems.Stretch);
        AlignContentCombo.SelectedItem = nameof(FlexAlignContent.Stretch);
    }

    private void OnPanelPropertyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Playground == null)
        {
            return;
        }

        if (DirectionCombo.SelectedItem is string direction)
        {
            Playground.Direction = Enum.Parse<FlexDirection>(direction);
        }

        if (WrapCombo.SelectedItem is string wrap)
        {
            Playground.Wrap = Enum.Parse<FlexWrap>(wrap);
        }

        if (JustifyCombo.SelectedItem is string justify)
        {
            Playground.JustifyContent = Enum.Parse<FlexJustify>(justify);
        }

        if (AlignItemsCombo.SelectedItem is string alignItems)
        {
            Playground.AlignItems = Enum.Parse<FlexAlignItems>(alignItems);
        }

        if (AlignContentCombo.SelectedItem is string alignContent)
        {
            Playground.AlignContent = Enum.Parse<FlexAlignContent>(alignContent);
        }
    }

    private void OnShowChild5Changed(object sender, RoutedEventArgs e)
    {
        if (Child5 == null)
        {
            return;
        }

        Child5.Visibility = ShowChild5Check.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
