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

	internal bool FileOpenPickerEnabled { get; private set; }

	internal FilePickerOptions FileOpenPickerOptions { get; private set; } = new();

	internal bool FileSavePickerEnabled { get; private set; }

	internal FilePickerOptions FileSavePickerOptions { get; private set; } = new();

	internal bool FolderPickerEnabled { get; private set; }

	internal FolderPickerOptions FolderPickerOptions { get; private set; } = new();

	internal bool SoftwareKeyboardEnabled { get; private set; }

	internal SoftwareKeyboardOptions SoftwareKeyboardOptions { get; private set; } = new();
}
