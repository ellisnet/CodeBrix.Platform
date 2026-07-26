// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeBrix.Platform.UI.Runtime.Skia.Pickers;

/// <summary>
/// The file-system engine behind the in-application picker dialogs: navigation,
/// the fence (a restricted folder presented as the ROOT of the file system),
/// the required-extension hard policy, and hidden-entry filtering. All policy
/// enforcement lives here, in the enumeration layer — an entry this class does
/// not surface does not exist from the dialog's point of view.
/// </summary>
internal sealed class PickerNavigator
{
	// Real, canonical fence root ending WITHOUT a separator; null when unfenced.
	private readonly string? _fenceRoot;
	private readonly bool _showHiddenFiles;
	private readonly bool _showHiddenFolders;
	// Normalized with a leading dot, or null when no requirement.
	private readonly string? _requiredExtension;
	// The application's own filter, normalized with leading dots; empty means "all".
	private readonly List<string> _applicationExtensions = new();

	internal string CurrentPath { get; private set; }

	internal readonly record struct Entry(string Name, string FullPath, bool IsFolder);

	/// <exception cref="DirectoryNotFoundException">The restricted folder is missing
	/// or not a directory. The message never contains a path.</exception>
	/// <exception cref="InvalidOperationException">The start folder lies outside the
	/// restricted folder. The message never contains a path.</exception>
	internal PickerNavigator(string? restrictToFolder, string? startFolder,
		bool showHiddenFiles, bool showHiddenFolders,
		string? requiredExtension, IEnumerable<string>? applicationExtensions)
	{
		_showHiddenFiles = showHiddenFiles;
		_showHiddenFolders = showHiddenFolders;
		_requiredExtension = NormalizeExtension(requiredExtension);

		foreach (var extension in applicationExtensions ?? [])
		{
			if (extension == "*")
			{
				_applicationExtensions.Clear();
				break;
			}
			if (NormalizeExtension(extension) is { } normalized)
			{
				_applicationExtensions.Add(normalized);
			}
		}

		if (!string.IsNullOrWhiteSpace(restrictToFolder))
		{
			var fence = TrimSeparator(Path.GetFullPath(restrictToFolder));
			if (!Directory.Exists(fence))
			{
				throw new DirectoryNotFoundException("The restricted folder is not valid.");
			}
			_fenceRoot = fence;
		}

		string? start = null;
		if (!string.IsNullOrWhiteSpace(startFolder))
		{
			start = TrimSeparator(Path.GetFullPath(startFolder));
			if (_fenceRoot is not null && !IsWithinFence(start))
			{
				throw new InvalidOperationException("The specified folder is outside of the restricted folder.");
			}
			if (!Directory.Exists(start))
			{
				start = null;
			}
		}

		CurrentPath = start
			?? _fenceRoot
			?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!Directory.Exists(CurrentPath))
		{
			CurrentPath = _fenceRoot ?? Path.GetFullPath(Path.DirectorySeparatorChar.ToString());
		}
	}

	/// <summary>
	/// The path shown in the dialog. Fenced dialogs never display the fence's real
	/// path or anything above it: the fence root is presented as "/" and everything
	/// below it as fence-relative. Unfenced dialogs show the real path.
	/// </summary>
	internal string DisplayPath
	{
		get
		{
			if (_fenceRoot is null)
			{
				return CurrentPath;
			}
			var relative = Path.GetRelativePath(_fenceRoot, CurrentPath);
			return relative == "." ? "/" : "/" + relative;
		}
	}

	internal bool CanNavigateUp
		=> _fenceRoot is not null
			? !string.Equals(CurrentPath, _fenceRoot, StringComparison.Ordinal)
			: Path.GetDirectoryName(CurrentPath) is not null;

	internal void NavigateUp()
	{
		if (CanNavigateUp && Path.GetDirectoryName(CurrentPath) is { } parent)
		{
			CurrentPath = TrimSeparator(parent);
		}
	}

	internal bool NavigateInto(Entry entry)
	{
		if (entry.IsFolder && Directory.Exists(entry.FullPath) && IsAdmissibleTarget(entry.FullPath, isFolder: true))
		{
			CurrentPath = TrimSeparator(Path.GetFullPath(entry.FullPath));
			return true;
		}
		return false;
	}

	/// <summary>
	/// The visible entries of the current folder: admissible folders first, then
	/// admissible files (omitted entirely in folder-only mode), each group ordered
	/// ordinally without regard to case.
	/// </summary>
	internal IReadOnlyList<Entry> GetEntries(bool foldersOnly)
	{
		var entries = new List<Entry>();
		try
		{
			var current = new DirectoryInfo(CurrentPath);

			entries.AddRange(current.EnumerateDirectories()
				.Where(directory => (_showHiddenFolders || !directory.Name.StartsWith('.'))
					&& IsAdmissibleTarget(directory.FullName, isFolder: true))
				.OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
				.Select(directory => new Entry(directory.Name, directory.FullName, IsFolder: true)));

			if (!foldersOnly)
			{
				entries.AddRange(current.EnumerateFiles()
					.Where(file => (_showHiddenFiles || !file.Name.StartsWith('.'))
						&& MatchesExtensionPolicy(file.Name)
						&& IsAdmissibleTarget(file.FullName, isFolder: false))
					.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
					.Select(file => new Entry(file.Name, file.FullName, IsFolder: false)));
			}
		}
		catch (UnauthorizedAccessException)
		{
			// An unreadable folder simply lists as empty.
		}
		catch (IOException)
		{
		}
		return entries;
	}

	/// <summary>
	/// Whether <paramref name="name"/> is usable as a new file or folder name:
	/// non-blank, no path separators, and not a navigation token — so a typed name
	/// can never express a location outside the current folder.
	/// </summary>
	internal static bool IsValidEntryName(string? name)
		=> !string.IsNullOrWhiteSpace(name)
			&& !name.Contains(Path.DirectorySeparatorChar)
			&& !name.Contains(Path.AltDirectorySeparatorChar)
			&& name.Trim() is not ("." or "..");

	/// <returns>The created folder's entry, or null when creation failed.</returns>
	internal Entry? CreateFolder(string name)
	{
		if (!IsValidEntryName(name))
		{
			return null;
		}
		try
		{
			var info = Directory.CreateDirectory(Path.Combine(CurrentPath, name.Trim()));
			return new Entry(info.Name, info.FullName, IsFolder: true);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
		{
			return null;
		}
	}

	/// <summary>
	/// Turns the save dialog's typed name into the full path to return, enforcing
	/// the required extension by appending it when missing (a non-matching file
	/// cannot be created). When no extension is required and the typed name has
	/// none, the application's first offered extension is appended instead, if any.
	/// Null when the name is not usable.
	/// </summary>
	internal string? ResolveSaveTarget(string? typedName)
	{
		if (!IsValidEntryName(typedName))
		{
			return null;
		}
		var name = typedName!.Trim();
		if (_requiredExtension is not null)
		{
			if (!name.EndsWith(_requiredExtension, StringComparison.OrdinalIgnoreCase))
			{
				name += _requiredExtension;
			}
		}
		else if (!Path.HasExtension(name) && _applicationExtensions.Count > 0)
		{
			name += _applicationExtensions[0];
		}
		return Path.Combine(CurrentPath, name);
	}

	private bool MatchesExtensionPolicy(string fileName)
	{
		// The host's required extension is the hard outer bound; the application's
		// own filter can only narrow further within it.
		if (_requiredExtension is not null
			&& !fileName.EndsWith(_requiredExtension, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return _applicationExtensions.Count == 0
			|| _applicationExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
	}

	// A symbolic link inside the fence could otherwise smuggle the dialog outside
	// it, so fenced dialogs admit an entry only when its FINAL link target still
	// lies within the fence. Unfenced dialogs admit everything.
	private bool IsAdmissibleTarget(string fullPath, bool isFolder)
	{
		if (_fenceRoot is null)
		{
			return true;
		}
		try
		{
			FileSystemInfo info = isFolder ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);
			if (info.LinkTarget is not null)
			{
				var resolved = info.ResolveLinkTarget(returnFinalTarget: true);
				if (resolved is null || !IsWithinFence(TrimSeparator(Path.GetFullPath(resolved.FullName))))
				{
					return false;
				}
			}
			return IsWithinFence(TrimSeparator(Path.GetFullPath(fullPath)));
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	private bool IsWithinFence(string canonicalPath)
		=> _fenceRoot is not null
			&& (string.Equals(canonicalPath, _fenceRoot, StringComparison.Ordinal)
				|| canonicalPath.StartsWith(_fenceRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal));

	private static string? NormalizeExtension(string? extension)
	{
		if (string.IsNullOrWhiteSpace(extension))
		{
			return null;
		}
		var trimmed = extension.Trim();
		return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
	}

	private static string TrimSeparator(string path)
		=> path.Length > 1 ? path.TrimEnd(Path.DirectorySeparatorChar) : path;
}
