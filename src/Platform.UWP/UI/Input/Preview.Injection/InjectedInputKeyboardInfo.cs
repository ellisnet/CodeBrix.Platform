#nullable enable

namespace Windows.UI.Input.Preview.Injection;

/// <summary>
/// Represents programmatically generated keyboard input, such as a Tab or Shift+Tab (Reverse Tabbing).
/// </summary>
public partial class InjectedInputKeyboardInfo
{
	/// <summary>
	/// Gets or sets the virtual key code of the key that was pressed or released. Ignored when <see cref="KeyOptions"/>
	/// has <see cref="InjectedInputKeyOptions.Unicode"/> or <see cref="InjectedInputKeyOptions.ScanCode"/>.
	/// </summary>
	public ushort VirtualKey { get; set; }

	/// <summary>
	/// Gets or sets a hardware-dependent scan code for the key. With <see cref="InjectedInputKeyOptions.Unicode"/>
	/// it is the UTF-16 code unit to type; with <see cref="InjectedInputKeyOptions.ScanCode"/> it identifies the key.
	/// </summary>
	public ushort ScanCode { get; set; }

	/// <summary>
	/// Gets or sets the options that describe the key (key up, extended key, scan code, Unicode character).
	/// </summary>
	public InjectedInputKeyOptions KeyOptions { get; set; }
}
