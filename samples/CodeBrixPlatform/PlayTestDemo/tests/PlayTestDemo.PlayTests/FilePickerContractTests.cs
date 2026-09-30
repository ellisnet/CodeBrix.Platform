using System;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed partial class ApplicationTests
{
    // Every picker is invoked through the demo's real button and normal picker API.
    private async Task PickThroughButtonAsync(string name)
    {
        var completed = await Page.EvaluateAsync(() => fixture.Model.PickerOperations);
        await Button(name).ScrollIntoViewIfNeededAsync();
        await Button(name).ClickAsync();
        await fixture.Application.WaitForAsync(() => fixture.Model.PickerOperations, count => count == completed + 1);
    }

    private string PickerDirectory()
    {
        var path = Path.Combine(fixture.DataDirectory, "Picker Data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private async Task VerifySelectionAsync(bool cancel, params string[] paths)
    {
        (await Page.EvaluateAsync(() => fixture.Model.LastPickerError)).Should().BeNull();
        (await Page.EvaluateAsync(() => fixture.Model.SelectedPaths)).Should().Equal(cancel ? Array.Empty<string>() : paths);
        await Expect(Page.GetByTestId("PickerOutcome")).ToHaveTextAsync(cancel ? "Cancelled" : "Selected");
        await Expect(Page.GetByTestId("PickerStatus")).ToHaveTextAsync(cancel ? "Selection cancelled." : string.Join(Environment.NewLine, paths));
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Folder_picker_returns_the_queued_path_or_cancel(bool cancel)
    {
        var folder = PickerDirectory();
        fixture.Application.FilePickers.EnqueueFolder(cancel ? null : folder);
        await PickThroughButtonAsync("Choose folder");
        await VerifySelectionAsync(cancel, folder);
        fixture.Application.FilePickers.FolderRequestCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_picker_returns_a_new_path_or_cancel(bool cancel)
    {
        var path = Path.Combine(PickerDirectory(), "New File.txt");
        fixture.Application.FilePickers.EnqueueSaveFile(cancel ? null : path);
        await PickThroughButtonAsync("Choose save path");
        await VerifySelectionAsync(cancel, path);
        File.Exists(path).Should().Be(!cancel);
        fixture.Application.FilePickers.LastSuggestedFileName.Should().Be("Suggested.txt");
        fixture.Application.FilePickers.SaveFileRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Save_picker_does_not_truncate_an_existing_file()
    {
        var path = Path.Combine(PickerDirectory(), "Keep.txt");
        File.WriteAllText(path, "Existing content");
        fixture.Application.FilePickers.EnqueueSaveFile(path);
        await PickThroughButtonAsync("Choose save path");
        await VerifySelectionAsync(false, path);
        File.ReadAllText(path).Should().Be("Existing content");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Open_picker_returns_the_queued_file_or_cancel(bool cancel)
    {
        var path = Path.Combine(PickerDirectory(), "Open File.txt");
        File.WriteAllText(path, "Read this");
        fixture.Application.FilePickers.EnqueueOpenFile(cancel ? null : path);
        await PickThroughButtonAsync("Open text file");
        await VerifySelectionAsync(cancel, path);
        await Expect(Page.GetByTestId("SelectedText")).ToHaveTextAsync(cancel ? "" : "Read this");
        fixture.Application.FilePickers.OpenFileRequestCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multiple_file_picker_preserves_order_and_cancel_is_empty(bool cancel)
    {
        var folder = PickerDirectory();
        var paths = new[] { Path.Combine(folder, "Second.txt"), Path.Combine(folder, "First.txt") };
        foreach (var path in paths) File.WriteAllText(path, path);
        fixture.Application.FilePickers.EnqueueOpenFiles(cancel ? null : paths);
        await PickThroughButtonAsync("Open multiple files");
        await VerifySelectionAsync(cancel, paths);
    }

    [Fact]
    public async Task An_unscripted_picker_fails_with_an_actionable_message()
    {
        await PickThroughButtonAsync("Choose folder");
        (await Page.EvaluateAsync(() => fixture.Model.LastPickerError)).Should().BeOfType<PlayTestException>()
            .Which.Message.Should().Contain("EnqueueFolder");
        await Expect(Page.GetByTestId("PickerOutcome")).ToHaveTextAsync("Error");
        await Expect(Page.GetByTestId("PickerStatus")).ToContainTextAsync("EnqueueFolder");
    }

    [Fact]
    public async Task File_picker_rejects_a_nonexistent_selected_file()
    {
        fixture.Application.FilePickers.EnqueueOpenFile(Path.Combine(PickerDirectory(), "missing.txt"));
        await PickThroughButtonAsync("Open text file");
        (await Page.EvaluateAsync(() => fixture.Model.LastPickerError)).Should().BeOfType<FileNotFoundException>();
        await Expect(Page.GetByTestId("PickerOutcome")).ToHaveTextAsync("Error");
    }

    [Fact]
    public async Task Picker_responses_are_consumed_in_order_and_clear_resets_them()
    {
        fixture.Application.FilePickers.EnqueueFolder(PickerDirectory());
        fixture.Application.FilePickers.Clear();
        fixture.Application.FilePickers.FolderRequestCount.Should().Be(0);
        var folder = PickerDirectory();
        fixture.Application.FilePickers.EnqueueFolder(null);
        fixture.Application.FilePickers.EnqueueFolder(folder);
        await PickThroughButtonAsync("Choose folder");
        await VerifySelectionAsync(true);
        await PickThroughButtonAsync("Choose folder");
        await VerifySelectionAsync(false, folder);
        fixture.Application.FilePickers.FolderRequestCount.Should().Be(2);
    }
}
