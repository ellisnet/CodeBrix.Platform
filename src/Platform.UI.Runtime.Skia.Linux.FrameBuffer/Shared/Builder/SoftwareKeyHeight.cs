// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// The rendered height of the software keyboard's keys, set with
/// <see cref="SoftwareKeyboardOptions.KeyHeight"/> — per orientation, so an
/// application can keep the roomy full-height keys in portrait while giving
/// landscape (where the keyboard eats far more of the shorter screen) the
/// compact half-height ones, or any other combination. "Full" is the standard
/// key height; "half" renders every key face at half that height while the
/// spaces between keys keep their standard size, so the strip takes a little
/// over half its full footprint.
/// </summary>
public enum SoftwareKeyHeight
{
	/// <summary>Full-height keys in both orientations. The default.</summary>
	PortraitFullLandscapeFull = 0,

	/// <summary>Half-height keys in both orientations.</summary>
	PortraitHalfLandscapeHalf = 1,

	/// <summary>
	/// Full-height keys in portrait, half-height keys in landscape — the
	/// combination for keeping typing roomy while a landscape keyboard leaves
	/// the application most of the screen.
	/// </summary>
	PortraitFullLandscapeHalf = 2,

	/// <summary>Half-height keys in portrait, full-height keys in landscape.</summary>
	PortraitHalfLandscapeFull = 3,

	/// <summary>
	/// The standard key height in both orientations.
	/// </summary>
	[Obsolete("Use PortraitFullLandscapeFull — the same behavior, named for the per-orientation options.")]
	FullHeight = PortraitFullLandscapeFull,

	/// <summary>
	/// Half-height keys in both orientations.
	/// </summary>
	[Obsolete("Use PortraitHalfLandscapeHalf — the same behavior, named for the per-orientation options.")]
	HalfHeight = PortraitHalfLandscapeHalf,
}
