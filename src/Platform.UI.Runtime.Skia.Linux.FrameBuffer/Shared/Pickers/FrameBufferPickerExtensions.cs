// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using CodeBrix.Platform.Extensions.Storage.Pickers;

namespace CodeBrix.Platform.UI.Runtime.Skia.Pickers;

/// <summary>
/// Implements the standard FileOpenPicker for this head with the in-application
/// dialog. Registered only when the host builder's EnableFileOpenPicker was
/// called; otherwise the picker keeps throwing NotSupportedException exactly as
/// before.
/// </summary>
internal sealed class FrameBufferFileOpenPickerExtension(IFilePicker picker, FilePickerOptions options) : IFileOpenPickerExtension
{
	public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
	{
		var paths = await PickAsync(multiple: false, token);
		return paths.Select(StorageFile.GetFileFromPath).FirstOrDefault();
	}

	public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
	{
		// AllowMultipleFileSelect false is a host-level veto: the call still works,
		// but the dialog is single-selection and returns at most one file.
		var paths = await PickAsync(multiple: options.AllowMultipleFileSelect, token);
		return paths.Select(StorageFile.GetFileFromPath).ToList();
	}

	private Task<IReadOnlyList<string>> PickAsync(bool multiple, CancellationToken token)
	{
		var navigator = new PickerNavigator(
			options.RestrictToFolder,
			PickerStartLocation.Resolve(options, picker.SuggestedStartLocationInternal),
			options.ShowHiddenFiles,
			options.ShowHiddenFolders,
			options.RequiredExtension,
			picker.FileTypeFilterInternal);
		var commitText = string.IsNullOrWhiteSpace(picker.CommitButtonTextInternal)
			? "Open"
			: picker.CommitButtonTextInternal;
		return PickerDialog.ShowAsync(PickerDialog.PickerMode.OpenFile, navigator,
			multiple, options.AllowNewFolderCreate, commitText, suggestedFileName: null, token);
	}
}

/// <summary>
/// Implements the standard FileSavePicker for this head with the in-application
/// dialog. Registered only when the host builder's EnableFileSavePicker was
/// called; otherwise the picker keeps throwing NotSupportedException exactly as
/// before.
/// </summary>
internal sealed class FrameBufferFileSavePickerExtension(FileSavePicker picker, FilePickerOptions options) : IFileSavePickerExtension
{
	public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		var navigator = new PickerNavigator(
			options.RestrictToFolder,
			PickerStartLocation.Resolve(options, picker.SuggestedStartLocation),
			options.ShowHiddenFiles,
			options.ShowHiddenFolders,
			options.RequiredExtension,
			picker.FileTypeChoices.Values.SelectMany(extensions => extensions));
		var commitText = string.IsNullOrWhiteSpace(picker.CommitButtonText)
			? "Save"
			: picker.CommitButtonText;

		var paths = await PickerDialog.ShowAsync(PickerDialog.PickerMode.SaveFile, navigator,
			allowMultiple: false, options.AllowNewFolderCreate, commitText,
			picker.SuggestedFileName, token);
		if (paths.Count == 0)
		{
			return null;
		}

		// The picker's contract is to hand back a real file: materialize an empty
		// one when the chosen name does not exist yet.
		var path = paths[0];
		if (!System.IO.File.Exists(path))
		{
			try
			{
				System.IO.File.WriteAllBytes(path, []);
			}
			catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}
		return StorageFile.GetFileFromPath(path);
	}
}

/// <summary>
/// Implements the standard FolderPicker for this head with the in-application
/// dialog. Registered only when the host builder's EnableFolderPicker was
/// called; otherwise the picker keeps throwing NotSupportedException exactly as
/// before.
/// </summary>
internal sealed class FrameBufferFolderPickerExtension(IFilePicker picker, FolderPickerOptions options) : IFolderPickerExtension
{
	public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
	{
		var navigator = new PickerNavigator(
			options.RestrictToFolder,
			PickerStartLocation.Resolve(options.StartFolder, options.RestrictToFolder, picker.SuggestedStartLocationInternal),
			showHiddenFiles: false,
			options.ShowHiddenFolders,
			requiredExtension: null,
			applicationExtensions: null);
		var commitText = string.IsNullOrWhiteSpace(picker.CommitButtonTextInternal)
			? "Select folder"
			: picker.CommitButtonTextInternal;

		var paths = await PickerDialog.ShowAsync(PickerDialog.PickerMode.PickFolder, navigator,
			allowMultiple: false, options.AllowNewFolderCreate, commitText, suggestedFileName: null, token);
		return paths.Count == 0 ? null : await StorageFolder.GetFolderFromPathAsync(paths[0]);
	}
}

/// <summary>
/// Resolves where a dialog starts. The host's StartFolder always wins; otherwise,
/// on an UNFENCED dialog only, the application's SuggestedStartLocation is mapped
/// to a real folder when one exists. A fenced dialog ignores SuggestedStartLocation
/// entirely and starts at the fence root.
/// </summary>
internal static class PickerStartLocation
{
	internal static string? Resolve(FilePickerOptions options, PickerLocationId suggested)
		=> Resolve(options.StartFolder, options.RestrictToFolder, suggested);

	internal static string? Resolve(string? startFolder, string? restrictToFolder, PickerLocationId suggested)
	{
		if (!string.IsNullOrWhiteSpace(startFolder))
		{
			return startFolder;
		}
		if (!string.IsNullOrWhiteSpace(restrictToFolder))
		{
			return null;
		}
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var mapped = suggested switch
		{
			PickerLocationId.Desktop => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
			PickerLocationId.DocumentsLibrary => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			PickerLocationId.MusicLibrary => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
			PickerLocationId.PicturesLibrary => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
			PickerLocationId.VideosLibrary => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
			PickerLocationId.Downloads => System.IO.Path.Combine(home, "Downloads"),
			PickerLocationId.ComputerFolder => System.IO.Path.DirectorySeparatorChar.ToString(),
			_ => home,
		};
		return string.IsNullOrEmpty(mapped) || !System.IO.Directory.Exists(mapped) ? home : mapped;
	}
}
