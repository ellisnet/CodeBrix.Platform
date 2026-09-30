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

/// <summary>Scripted responses to the application's real folder/open/save pickers. Null means cancel.
/// Enqueue a response before clicking the control that opens the picker.</summary>
public sealed class PlayTestFilePickers
{
    private readonly ConcurrentQueue<string> _folders = new();
    private readonly ConcurrentQueue<string> _saveFiles = new();
    private readonly ConcurrentQueue<string[]> _openFiles = new();
    public string LastSuggestedFileName { get; private set; }
    public int FolderRequestCount { get; private set; }
    public int SaveFileRequestCount { get; private set; }
    public int OpenFileRequestCount { get; private set; }

    public void EnqueueFolder(string path) => _folders.Enqueue(path == null ? null : Path.GetFullPath(path));
    public void EnqueueSaveFile(string path) => _saveFiles.Enqueue(path == null ? null : Path.GetFullPath(path));
    public void EnqueueOpenFile(string path) => EnqueueOpenFiles(path == null ? null : new[] { path });
    public void EnqueueOpenFiles(params string[] paths) =>
        _openFiles.Enqueue(paths?.Select(Path.GetFullPath).ToArray() ?? Array.Empty<string>());

    /// <summary>Clear unused responses between serialized tests.</summary>
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
            if (!owner._openFiles.TryDequeue(out var paths))
                throw new PlayTestException("FileOpenPicker opened without a queued response. Call Application.FilePickers.EnqueueOpenFile(path) or EnqueueOpenFiles(paths), or enqueue null to cancel.");
            return paths;
        }

        public async Task<StorageFile> PickSingleFileAsync(CancellationToken token)
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
        public async Task<StorageFolder> PickSingleFolderAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            owner.FolderRequestCount++;
            if (!owner._folders.TryDequeue(out var path))
                throw new PlayTestException("FolderPicker opened without a queued response. Call Application.FilePickers.EnqueueFolder(path), or enqueue null to cancel.");
            if (path == null) return null;
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("The scripted picker folder does not exist: " + path);
            return await StorageFolder.GetFolderFromPathAsync(path);
        }
    }

    private sealed class SaveResponse(PlayTestFilePickers owner) : IFileSavePickerExtension
    {
        public void Customize(FileSavePicker picker) => owner.LastSuggestedFileName = picker.SuggestedFileName;

        public async Task<StorageFile> PickSaveFileAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            owner.SaveFileRequestCount++;
            if (!owner._saveFiles.TryDequeue(out var path))
                throw new PlayTestException("FileSavePicker opened without a queued response. Call Application.FilePickers.EnqueueSaveFile(path), or enqueue null to cancel.");
            if (path == null) return null;
            // Match a native save picker: a new name may create an empty placeholder;
            // an existing file is returned intact, never truncated by the picker.
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("The scripted save-picker parent folder does not exist: " + directory);
            var folder = await StorageFolder.GetFolderFromPathAsync(directory);
            return await folder.CreateFileAsync(Path.GetFileName(path), CreationCollisionOption.OpenIfExists);
        }
    }
}
