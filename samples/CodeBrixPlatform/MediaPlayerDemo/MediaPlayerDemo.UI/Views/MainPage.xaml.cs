using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using System;

namespace MediaPlayerDemo.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        LoadMedia();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e) => LoadMedia();

    private void LoadMedia()
    {
        try
        {
            var uri = new Uri(AddressBox.Text);
            Player.Source = MediaSource.CreateFromUri(uri);
            StatusText.Text = $"Loaded: {uri}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Cannot load '{AddressBox.Text}': {ex.Message}";
        }
    }

    private void StretchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Player is null)
        {
            return; // fired during InitializeComponent
        }

        Player.Stretch = StretchBox.SelectedIndex switch
        {
            1 => Stretch.UniformToFill,
            2 => Stretch.Fill,
            3 => Stretch.None,
            _ => Stretch.Uniform,
        };
    }
}
