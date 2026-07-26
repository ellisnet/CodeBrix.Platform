// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// Host-level settings for the on-screen software keyboard enabled with
/// <see cref="FramebufferHostBuilder.EnableSoftwareKeyboard"/>.
/// </summary>
public class SoftwareKeyboardOptions
{
	/// <summary>
	/// Pins the keyboard layout for the entire application, by BCP-47 language tag
	/// (for example "de", "fr", "en-GB", "uk", "el"). When null or empty the layout
	/// is resolved from the system instead, in this order: the XKB_DEFAULT_LAYOUT
	/// environment variable (an operator's explicit keyboard setting, consistent
	/// with how the hardware keymap resolves), then the locale environment
	/// (LC_ALL / LC_CTYPE / LANG), then US English as the last resort.
	/// </summary>
	public string? Layout { get; set; }

	/// <summary>
	/// The layouts the user can cycle through with the keyboard's globe key, by
	/// BCP-47 language tag. The globe key is shown only when more than one distinct
	/// layout is enabled. Null or empty means only the active layout is available
	/// and no globe key is shown.
	/// </summary>
	public IList<string>? EnabledLayouts { get; set; }
}
