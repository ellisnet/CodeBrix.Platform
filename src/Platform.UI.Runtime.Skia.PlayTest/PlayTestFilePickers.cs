using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.Storage.Pickers;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Scripted responses to the application's real folder/open/save pickers. Null means cancel; an
/// enqueued failure makes the picker throw. Enqueue a response before clicking the control that opens the picker.</summary>
public sealed class PlayTestFilePickers
{
    // One scripted answer: selected paths (empty = cancelled) or a failure the picker throws.
    private sealed record Answer(string[] Paths, Exception? Failure);

    private readonly ConcurrentQueue<Answer> _folders = new();
    private readonly ConcurrentQueue<Answer> _saveFiles = new();
    private readonly ConcurrentQueue<Answer> _openFiles = new();

    /// <summary>The <c>SuggestedFileName</c> of the most recent save picker, or null.</summary>
    public string? LastSuggestedFileName { get; private set; }

    /// <summary>Number of folder pickers the application has opened since the last <see cref="Clear"/>.</summary>
    public int FolderRequestCount { get; private set; }

    /// <summary>Number of save-file pickers the application has opened since the last <see cref="Clear"/>.</summary>
    public int SaveFileRequestCount { get; private set; }

    /// <summary>Number of open-file pickers (single or multiple) opened since the last <see cref="Clear"/>.</summary>
    public int OpenFileRequestCount { get; private set; }

    /// <summary>Queues the answer for the next folder picker. The folder must exist when the picker opens.</summary>
    /// <param name="path">The folder to select (made absolute now), or null to cancel.</param>
    public void EnqueueFolder(string? path) => _folders.Enqueue(Selection(path));

    /// <summary>Queues the answer for the next save-file picker. Its parent folder must exist; a new name
    /// creates an empty file, and an existing file is returned intact.</summary>
    /// <param name="path">The file to select (made absolute now), or null to cancel.</param>
    public void EnqueueSaveFile(string? path) => _saveFiles.Enqueue(Selection(path));

    /// <summary>Queues one file for the next open-file picker. The file must exist when the picker opens.</summary>
    /// <param name="path">The file to select (made absolute now), or null to cancel.</param>
    public void EnqueueOpenFile(string? path) => EnqueueOpenFiles(path == null ? null : new[] { path });

    /// <summary>Queues several files for the next open-file picker. A single-file picker rejects more than one.</summary>
    /// <param name="paths">The files to select (made absolute now), or null or empty to cancel.</param>
    public void EnqueueOpenFiles(params string[]? paths) =>
        _openFiles.Enqueue(new Answer(paths?.Select(Path.GetFullPath).ToArray() ?? Array.Empty<string>(), null));

    /// <summary>Makes the next folder picker throw <paramref name="error"/> instead of answering, so the
    /// application's "picker failed" or "pickers not supported" branch runs.</summary>
    /// <param name="error">The exception the picker throws, for example a <see cref="NotSupportedException"/>.</param>
    public void EnqueueFolderFailure(Exception error) => _folders.Enqueue(Failure(error));

    /// <summary>Makes the next open-file picker (single or multiple) throw <paramref name="error"/> instead of answering.</summary>
    /// <param name="error">The exception the picker throws.</param>
    public void EnqueueOpenFileFailure(Exception error) => _openFiles.Enqueue(Failure(error));

    /// <summary>Makes the next save-file picker throw <paramref name="error"/> instead of answering.</summary>
    /// <param name="error">The exception the picker throws.</param>
    public void EnqueueSaveFileFailure(Exception error) => _saveFiles.Enqueue(Failure(error));

    private static Answer Selection(string? path) => new(path == null ? Array.Empty<string>() : new[] { Path.GetFullPath(path) }, null);

    private static Answer Failure(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Answer(Array.Empty<string>(), error);
    }

    // The next answer for one picker kind: its paths (empty = cancelled), or its scripted failure thrown.
    private static string[] Take(ConcurrentQueue<Answer> queue, string missing)
    {
        if (!queue.TryDequeue(out var answer)) throw new PlayTestException(missing);
        if (answer.Failure != null) throw answer.Failure;
        return answer.Paths;
    }

    // Internal so the host-free unit tests can fence the queue rules.
    internal string[] TakeFolder() => Take(_folders, "FolderPicker opened without a queued response. Call Application.FilePickers.EnqueueFolder(path), or enqueue null to cancel.");
    internal string[] TakeSaveFile() => Take(_saveFiles, "FileSavePicker opened without a queued response. Call Application.FilePickers.EnqueueSaveFile(path), or enqueue null to cancel.");
    internal string[] TakeOpenFiles() => Take(_openFiles, "FileOpenPicker opened without a queued response. Call Application.FilePickers.EnqueueOpenFile(path) or EnqueueOpenFiles(paths), or enqueue null to cancel.");

    /// <summary>Clear unused responses (paths, cancellations and failures), the request counts and the last
    /// suggested file name, between serialized tests.</summary>
    public void Clear()
    {
        _folders.Clear();
        _saveFiles.Clear();
        _openFiles.Clear();
        LastSuggestedFileName = null;
        FolderRequestCount = SaveFileRequestCount = OpenFileRequestCount = 0;
    }

    internal void Register()
    {
        ApiExtensibility.Register(typeof(IFolderPickerExtension), _ => new FolderResponse(this));
        ApiExtensibility.Register(typeof(IFileSavePickerExtension), _ => new SaveResponse(this));
        ApiExtensibility.Register(typeof(IFileOpenPickerExtension), _ => new OpenResponse(this));
    }

    private sealed class OpenResponse(PlayTestFilePickers owner) : IFileOpenPickerExtension
    {
        private string[] Take(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            owner.OpenFileRequestCount++;
            return owner.TakeOpenFiles();
        }

        public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
        {
            var paths = Take(token);
            if (paths.Length > 1) throw new PlayTestException("A single-file picker cannot select multiple queued files.");
            return paths.Length == 0 ? null : await OpenExistingAsync(paths[0]);
        }

        public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
        {
            var files = new List<StorageFile>();
            foreach (var path in Take(token))
            {
                token.ThrowIfCancellationRequested();
                files.Add(await OpenExistingAsync(path));
            }
            return files;
        }

        private static async Task<StorageFile> OpenExistingAsync(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("The scripted open-picker file does not exist.", path);
            return await StorageFile.GetFileFromPathAsync(path);
        }
    }

    private sealed class FolderResponse(PlayTestFilePickers owner) : IFolderPickerExtension
    {
        public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            owner.FolderRequestCount++;
            var paths = owner.TakeFolder();
            if (paths.Length == 0) return null;
            var path = paths[0];
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("The scripted picker folder does not exist: " + path);
            return await StorageFolder.GetFolderFromPathAsync(path);
        }
    }

    private sealed class SaveResponse(PlayTestFilePickers owner) : IFileSavePickerExtension
    {
        public void Customize(FileSavePicker picker) => owner.LastSuggestedFileName = picker.SuggestedFileName;

        public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            owner.SaveFileRequestCount++;
            var paths = owner.TakeSaveFile();
            if (paths.Length == 0) return null;
            var path = paths[0];
            // Match a native save picker: a new name may create an empty placeholder;
            // an existing file is returned intact, never truncated by the picker.
            var directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory)) throw new DirectoryNotFoundException("The scripted save-picker parent folder does not exist: " + directory);
            var folder = await StorageFolder.GetFolderFromPathAsync(directory);
            return await folder.CreateFileAsync(Path.GetFileName(path), CreationCollisionOption.OpenIfExists);
        }
    }
}
