// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// Host-level policy for the in-application file open/save dialogs enabled with
/// <see cref="FramebufferHostBuilder.EnableFileOpenPicker"/> and
/// <see cref="FramebufferHostBuilder.EnableFileSavePicker"/>. Each of those methods
/// takes its own instance, so open and save may carry different policies. These are
/// build-time settings for the whole application; the WinUI picker properties
/// (file-type filters, suggested file name, commit button text) remain the
/// per-invocation levers.
/// </summary>
public class FilePickerOptions
{
	/// <summary>
	/// Whether the dialog offers a "New folder" action. Defaults to false.
	/// </summary>
	public bool AllowNewFolderCreate { get; set; }

	/// <summary>
	/// A fence around the file system: when non-blank, the dialog presents this
	/// folder as the ROOT of the file system — nothing outside it is visible,
	/// reachable, or expressible, and the dialog never displays its real path.
	/// Validated on every dialog launch: if missing or not a directory the picker
	/// call throws <see cref="System.IO.DirectoryNotFoundException"/> (with no path
	/// in the message). Null or empty means no restriction.
	/// </summary>
	public string? RestrictToFolder { get; set; }

	/// <summary>
	/// When non-blank, a hard host policy: files without this extension do not
	/// exist from this picker's point of view — they are never listed, never
	/// selectable, and never returnable, regardless of what the application's
	/// own file-type filter requests (that filter can only narrow further within
	/// it). The save dialog appends this extension to a typed name that lacks it,
	/// so a non-matching file cannot be created either. A leading dot is optional
	/// ("txt" and ".txt" are equivalent). Null or empty means no requirement.
	/// </summary>
	public string? RequiredExtension { get; set; }

	/// <summary>
	/// The folder the dialog starts in. When <see cref="RestrictToFolder"/> is set
	/// this must lie inside the fence, otherwise the picker call throws
	/// <see cref="System.InvalidOperationException"/>. Null or empty starts at the
	/// fence root when fenced, or at the user's home directory otherwise. When a
	/// fence is set, the WinUI SuggestedStartLocation is ignored.
	/// </summary>
	public string? StartFolder { get; set; }

	/// <summary>
	/// Whether the open dialog honors PickMultipleFilesAsync with multi-selection.
	/// Defaults to true. False is a host-level veto that forces single selection
	/// even then — the call still works, returning at most one file.
	/// PickSingleFileAsync is always single regardless. Ignored by the save dialog.
	/// </summary>
	public bool AllowMultipleFileSelect { get; set; } = true;

	/// <summary>
	/// Whether hidden files (dot-prefixed on Linux) are listed. Defaults to false.
	/// </summary>
	public bool ShowHiddenFiles { get; set; }

	/// <summary>
	/// Whether hidden folders (dot-prefixed on Linux) are listed. Defaults to false.
	/// </summary>
	public bool ShowHiddenFolders { get; set; }
}
