using CodeBrix.Platform.Simple;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace FileFolderDialogDemo.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDialogDemoAsync("Select File", async () =>
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            return file == null
                ? "You selected cancel."
                : $"You selected path: {file.Path}";
        });
    }

    private async void SelectFilteredFileButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDialogDemoAsync("Select File (.txt / .md only)", async () =>
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".md");

            var file = await picker.PickSingleFileAsync();
            return file == null
                ? "You selected cancel."
                : $"You selected path: {file.Path}";
        });
    }

    private async void SelectMultipleFilesButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDialogDemoAsync("Select Multiple Files", async () =>
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var files = await picker.PickMultipleFilesAsync();
            return (files == null || files.Count == 0)
                ? "You selected cancel."
                : "You selected path(s):\n" + string.Join("\n", files.Select(f => f.Path));
        });
    }

    private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDialogDemoAsync("Save File", async () =>
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "FileFolderDialogDemo",
                DefaultFileExtension = ".txt"
            };
            picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });
            picker.FileTypeChoices.Add("Markdown file", new List<string> { ".md" });

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return "You selected cancel.";
            }

            await File.WriteAllTextAsync(file.Path,
                $"Hello from FileFolderDialogDemo!{Environment.NewLine}" +
                $"This file was written by the Save File demo at {DateTime.Now}.{Environment.NewLine}");

            return $"You selected path: {file.Path}\n(a small demo text file was written there)";
        });
    }

    private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
    {
        await RunDialogDemoAsync("Select Folder", async () =>
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            return folder == null
                ? "You selected cancel."
                : $"You selected path: {folder.Path}";
        });
    }

    // Runs one picker demo: disables the buttons while the (modal) native dialog and the
    // result popup are up, and reports the outcome via SimpleDialog + the status line.
    private async Task RunDialogDemoAsync(string title, Func<Task<string>> operation)
    {
        SetButtonsEnabled(false);
        try
        {
            StatusText.Text = $"{title}: waiting on the dialog...";
            var message = await operation();
            StatusText.Text = $"{title}: {message.Replace("\n", " ")}";
            await ShowResultAsync(title, message);
        }
        catch (NotSupportedException)
        {
            // The Linux FrameBuffer head registers no picker extensions (there is no windowing
            // system to host a native dialog), so the pickers throw NotSupportedException there.
            StatusText.Text = $"{title}: not supported on this head";
            await ShowResultAsync(title, "File/folder dialogs are not supported on this head.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{title}: failed - {ex.Message}";
            await ShowResultAsync(title, $"The operation failed: {ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    // StackPanel is a Panel (not a Control), so it has no IsEnabled of its own
    private void SetButtonsEnabled(bool enabled)
    {
        foreach (var child in ButtonsPanel.Children)
        {
            if (child is Control control)
            {
                control.IsEnabled = enabled;
            }
        }
    }

    private async Task ShowResultAsync(string title, string message)
    {
        using var dialog = SimpleDialog.Create(() => XamlRoot, DispatcherQueue, message, title);
        await dialog.ShowAsync();
    }
}
