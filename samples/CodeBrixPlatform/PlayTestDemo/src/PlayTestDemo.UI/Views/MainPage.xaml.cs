using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PlayTestDemo.ViewModels;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PlayTestDemo.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void OnCheckChanged(object sender, RoutedEventArgs e) => ViewModel.RecordCheck(((CheckBox)sender).IsChecked == true);
    private void OnToggleChanged(object sender, RoutedEventArgs e) => ViewModel.RecordToggle(((ToggleSwitch)sender).IsOn);
    private void OnScrollTarget(object sender, RoutedEventArgs e) => ViewModel.RecordScrollClick();

    private async void OnChooseFolder(object sender, RoutedEventArgs e) => await PickAsync(async () =>
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        return (folder == null ? Array.Empty<string>() : new[] { folder.Path }, "");
    });

    private async void OnOpenFile(object sender, RoutedEventArgs e) => await PickAsync(async () =>
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".txt");
        var file = await picker.PickSingleFileAsync();
        return file == null ? (Array.Empty<string>(), "") : (new[] { file.Path }, await FileIO.ReadTextAsync(file));
    });

    private async void OnOpenFiles(object sender, RoutedEventArgs e) => await PickAsync(async () =>
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        var files = await picker.PickMultipleFilesAsync();
        return (files.Select(file => file.Path).ToArray(), "");
    });

    private async void OnSaveFile(object sender, RoutedEventArgs e) => await PickAsync(async () =>
    {
        var picker = new FileSavePicker { SuggestedFileName = "Suggested.txt" };
        picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
        var file = await picker.PickSaveFileAsync();
        return (file == null ? Array.Empty<string>() : new[] { file.Path }, "");
    });

    private async Task PickAsync(Func<Task<(string[] Paths, string Text)>> pick)
    {
        try
        {
            var result = await pick();
            ViewModel.RecordPicker(result.Paths, result.Text, null);
        }
        catch (Exception error)
        {
            ViewModel.RecordPicker(null, null, error);
        }
    }
}
