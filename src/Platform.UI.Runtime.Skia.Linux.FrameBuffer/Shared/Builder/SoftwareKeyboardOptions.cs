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
	/// Whether the keyboard shows a lock key — an open/closed padlock — as the
	/// far-left key of the number row on every page and layout. Off by default.
	/// The lock starts open (unlocked) and the keyboard behaves exactly as
	/// without it. Tapping the lock while the keyboard is visible LOCKS it: the
	/// keyboard then stays on screen at all times — the dismiss key, focus
	/// leaving text entry and the application's own InputPane.TryHide() are all
	/// refused — until the user taps the lock again (or the application exits).
	/// On unlocking, the normal rules re-apply immediately: with no text-entry
	/// control focused the keyboard hides right away, otherwise it stays for
	/// the control being typed into and hides by the usual rules thereafter.
	/// The one exception: a portrait &lt;-&gt; landscape orientation change
	/// forcibly unlocks and hides the keyboard (and unfocuses the text control)
	/// before rotating — a keyboard strip cannot survive the aspect swap — and
	/// the user re-summons it by tapping back into a text control.
	/// </summary>
	public bool AllowLockOn { get; set; }

	/// <summary>
	/// The rendered height of the keys, PER ORIENTATION:
	/// <see cref="SoftwareKeyHeight.PortraitFullLandscapeFull"/> (the default)
	/// for standard keys everywhere, through
	/// <see cref="SoftwareKeyHeight.PortraitFullLandscapeHalf"/> and its
	/// siblings to mix standard and half-height keys by orientation — the
	/// spaces between keys keep their standard size, so a "half" strip shrinks
	/// to a little over half its full height. Rotating the device re-fits the
	/// keyboard to the new orientation's setting.
	/// </summary>
	public SoftwareKeyHeight KeyHeight { get; set; } = SoftwareKeyHeight.PortraitFullLandscapeFull;
}
