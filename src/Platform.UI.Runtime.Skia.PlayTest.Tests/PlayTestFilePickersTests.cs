using System;
using System.IO;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The scripted answer queues behind PlayTestFilePickers: paths, cancellations and failures, per picker kind.
public sealed class PlayTestFilePickersTests
{
    [Fact]
    public void A_queued_failure_is_thrown_by_its_picker_kind_only()
    {
        //Arrange
        var pickers = new PlayTestFilePickers();
        var folderError = new NotSupportedException("No folder dialogs.");
        var openError = new InvalidOperationException("Open failed.");
        var saveError = new UnauthorizedAccessException("Save denied.");
        pickers.EnqueueFolderFailure(folderError);
        pickers.EnqueueOpenFileFailure(openError);
        pickers.EnqueueSaveFileFailure(saveError);

        //Act
        Action folder = () => pickers.TakeFolder();
        Action open = () => pickers.TakeOpenFiles();
        Action save = () => pickers.TakeSaveFile();

        //Assert
        folder.Should().Throw<NotSupportedException>().Which.Should().BeSameAs(folderError);
        open.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(openError);
        save.Should().Throw<UnauthorizedAccessException>().Which.Should().BeSameAs(saveError);
    }

    [Fact]
    public void Failures_and_answers_are_consumed_in_queue_order()
    {
        //Arrange
        var pickers = new PlayTestFilePickers();
        pickers.EnqueueFolder("first");
        pickers.EnqueueFolderFailure(new IOException("second"));
        pickers.EnqueueFolder(null);

        //Act
        var first = pickers.TakeFolder();
        Action second = () => pickers.TakeFolder();

        //Assert
        first.Should().Equal(Path.GetFullPath("first"));
        second.Should().Throw<IOException>().WithMessage("second");
        pickers.TakeFolder().Should().BeEmpty();
    }

    [Fact]
    public void Clear_removes_queued_failures()
    {
        //Arrange
        var pickers = new PlayTestFilePickers();
        pickers.EnqueueSaveFileFailure(new IOException("unused"));

        //Act
        pickers.Clear();
        Action save = () => pickers.TakeSaveFile();

        //Assert
        save.Should().Throw<PlayTestException>().WithMessage("FileSavePicker opened without a queued response*");
    }

    [Fact]
    public void A_null_failure_is_rejected()
    {
        //Arrange
        var pickers = new PlayTestFilePickers();

        //Act
        Action enqueue = () => pickers.EnqueueOpenFileFailure(null!);

        //Assert
        enqueue.Should().Throw<ArgumentNullException>();
    }
}
