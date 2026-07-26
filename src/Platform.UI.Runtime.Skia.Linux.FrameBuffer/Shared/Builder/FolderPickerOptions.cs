// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// Host-level policy for the in-application folder dialog enabled with
/// <see cref="FramebufferHostBuilder.EnableFolderPicker"/>. These are build-time
/// settings for the whole application.
/// </summary>
public class FolderPickerOptions
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
	/// The folder the dialog starts in. When <see cref="RestrictToFolder"/> is set
	/// this must lie inside the fence, otherwise the picker call throws
	/// <see cref="System.InvalidOperationException"/>. Null or empty starts at the
	/// fence root when fenced, or at the user's home directory otherwise. When a
	/// fence is set, the WinUI SuggestedStartLocation is ignored.
	/// </summary>
	public string? StartFolder { get; set; }

	/// <summary>
	/// Whether hidden folders (dot-prefixed on Linux) are listed. Defaults to false.
	/// </summary>
	public bool ShowHiddenFolders { get; set; }
}
