// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

namespace CodeBrix.Platform.UI.Runtime.Skia;

public partial class FramebufferHostBuilder
{
	/// <summary>
	/// Enables the in-application file OPEN dialog for the standard
	/// Windows.Storage.Pickers.FileOpenPicker API. Without this call the picker
	/// keeps its default behavior on this head and throws
	/// <see cref="System.NotSupportedException"/>. The dialog is modal, always on
	/// top of all application content, and always within the application frame.
	/// </summary>
	/// <param name="options">Host-level policy for the open dialog; null applies
	/// the documented <see cref="FilePickerOptions"/> defaults.</param>
	public FramebufferHostBuilder EnableFileOpenPicker(FilePickerOptions? options = null)
	{
		FileOpenPickerEnabled = true;
		FileOpenPickerOptions = options ?? new FilePickerOptions();
		return this;
	}

	/// <summary>
	/// Enables the in-application file SAVE dialog for the standard
	/// Windows.Storage.Pickers.FileSavePicker API. Without this call the picker
	/// keeps its default behavior on this head and throws
	/// <see cref="System.NotSupportedException"/>. The dialog is modal, always on
	/// top of all application content, and always within the application frame.
	/// </summary>
	/// <param name="options">Host-level policy for the save dialog; null applies
	/// the documented <see cref="FilePickerOptions"/> defaults.</param>
	public FramebufferHostBuilder EnableFileSavePicker(FilePickerOptions? options = null)
	{
		FileSavePickerEnabled = true;
		FileSavePickerOptions = options ?? new FilePickerOptions();
		return this;
	}

	/// <summary>
	/// Enables the in-application folder dialog for the standard
	/// Windows.Storage.Pickers.FolderPicker API. Without this call the picker
	/// keeps its default behavior on this head and throws
	/// <see cref="System.NotSupportedException"/>. The dialog is modal, always on
	/// top of all application content, and always within the application frame.
	/// </summary>
	/// <param name="options">Host-level policy for the folder dialog; null applies
	/// the documented <see cref="FolderPickerOptions"/> defaults.</param>
	public FramebufferHostBuilder EnableFolderPicker(FolderPickerOptions? options = null)
	{
		FolderPickerEnabled = true;
		FolderPickerOptions = options ?? new FolderPickerOptions();
		return this;
	}

	/// <summary>
	/// Enables the on-screen software keyboard. It shows automatically when a
	/// TextBox or PasswordBox gains focus and hides when focus leaves, and it also
	/// honors manual Windows.UI.ViewManagement.InputPane.TryShow()/TryHide() calls.
	/// While visible, the application's layout height is reduced by the keyboard's
	/// height so the focused field can never be covered. Without this call, no
	/// software keyboard exists and the head behaves exactly as before.
	/// </summary>
	/// <param name="options">Layout settings; null resolves the layout from the
	/// system as documented on <see cref="SoftwareKeyboardOptions"/>.</param>
	public FramebufferHostBuilder EnableSoftwareKeyboard(SoftwareKeyboardOptions? options = null)
	{
		SoftwareKeyboardEnabled = true;
		SoftwareKeyboardOptions = options ?? new SoftwareKeyboardOptions();
		return this;
	}

	/// <summary>
	/// Enables a simple Last-In-Only-Out, TEXT-ONLY clipboard that exists in the
	/// application process alone: copy and paste work within the application, but
	/// nothing reaches a system clipboard and nothing crosses applications.
	/// Copying content without a text representation logs an error and keeps the
	/// previous clipboard text. Without this call, the head has no clipboard at
	/// all and clipboard use logs "Clipboard is not implemented on this platform."
	/// </summary>
	public FramebufferHostBuilder EnableSimpleTextClipboard()
	{
		SimpleTextClipboardEnabled = true;
		return this;
	}

	/// <summary>
	/// Follows the device's orientation SENSOR: on a device running
	/// iio-sensor-proxy (Debian: <c>apt install iio-sensor-proxy</c>) with an
	/// accelerometer the kernel supports, physically turning the device rotates
	/// the application — gated, like every rotation source, by
	/// <see cref="AutoRotationEnabled(bool)"/>. Without this call no sensor is
	/// ever consulted. The launcher can override the source with the
	/// CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE environment variable:
	/// "develop" (what CodeBrix.Develop sets) listens for orientation
	/// instructions from the IDE INSTEAD of the sensor, "sensor" forces the
	/// sensor, "none" disables both, unset honors this declaration. Under the
	/// emulator this is a no-op — the Emulator View drives rotation itself.
	/// </summary>
	public FramebufferHostBuilder UseOrientationSensor()
	{
		OrientationSensorEnabled = true;
		return this;
	}

	/// <summary>
	/// Allows more than one instance of this application to run at the same time.
	/// By DEFAULT a second instance of the same application refuses to start with
	/// an informative error: both instances would share the one framebuffer (the
	/// screen "flashes" as each blits its own frames) and each would receive every
	/// touch event, which is virtually always an accident rather than an intent.
	/// Call this only when concurrent instances are genuinely wanted. Under the
	/// emulator this is a no-op — CodeBrix.Develop hosts a single emulated view,
	/// so there is no second instance to guard against.
	/// </summary>
	public FramebufferHostBuilder AllowMultipleApplicationInstances()
	{
		AllowMultipleInstances = true;
		return this;
	}

	internal bool FileOpenPickerEnabled { get; private set; }

	internal FilePickerOptions FileOpenPickerOptions { get; private set; } = new();

	internal bool FileSavePickerEnabled { get; private set; }

	internal FilePickerOptions FileSavePickerOptions { get; private set; } = new();

	internal bool FolderPickerEnabled { get; private set; }

	internal FolderPickerOptions FolderPickerOptions { get; private set; } = new();

	internal bool SoftwareKeyboardEnabled { get; private set; }

	internal SoftwareKeyboardOptions SoftwareKeyboardOptions { get; private set; } = new();

	internal bool SimpleTextClipboardEnabled { get; private set; }

	internal bool AllowMultipleInstances { get; private set; }

	internal bool OrientationSensorEnabled { get; private set; }
}
