// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// The rendered height of the software keyboard's keys, set with
/// <see cref="SoftwareKeyboardOptions.KeyHeight"/>.
/// </summary>
public enum SoftwareKeyHeight
{
	/// <summary>
	/// The standard key height: the keyboard strip takes its per-orientation
	/// fraction of the screen. The default.
	/// </summary>
	FullHeight,

	/// <summary>
	/// Every key renders at half its standard height while the spaces between
	/// keys keep their standard size, so the whole strip takes a little over
	/// half its <see cref="FullHeight"/> footprint.
	/// </summary>
	HalfHeight,
}
