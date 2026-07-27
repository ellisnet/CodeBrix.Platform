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

	/// <summary>
	/// Whether the keyboard shows a dismiss key — a downward-pointing triangle —
	/// as the top-right key of every page and layout. When true (the default) the
	/// top row's keys narrow slightly to make room for it; set false to omit the
	/// key entirely. Tapping it hides the keyboard and keeps it hidden while the
	/// same text control holds or regains focus; the keyboard returns when the
	/// user taps back inside that control or when a different editable text
	/// control gains focus. The setting is application-global.
	/// </summary>
	public bool ShowDismissKey { get; set; } = true;

	/// <summary>
	/// The rendered height of the keys: <see cref="SoftwareKeyHeight.FullHeight"/>
	/// (the default), or <see cref="SoftwareKeyHeight.HalfHeight"/> to render every
	/// key at half its standard height — the spaces between keys keep their
	/// standard size, so the strip shrinks to a little over half its full height.
	/// </summary>
	public SoftwareKeyHeight KeyHeight { get; set; } = SoftwareKeyHeight.FullHeight;
}
