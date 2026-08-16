// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// How much larger than their nominal size the application's user-interface
/// elements are drawn, set with
/// <see cref="FramebufferHostBuilder.ScaleUserInterface"/>. This is the
/// standard high-density-display treatment, NOT an upscale of a smaller
/// picture: the layout is measured and arranged in logical units — the panel's
/// pixels divided by this scale — while the drawing surface keeps every one of
/// the panel's real pixels, so text and vector content are rasterized at the
/// panel's full resolution and look as sharp as it can render them.
/// <para>
/// The scale a device wants follows from its pixel density. A 1200 x 1920
/// 8-inch panel is about 283 ppi against roughly 189 ppi for an 800 x 1280
/// panel of the same size, so <see cref="Percent150"/> there gives elements
/// the same PHYSICAL size as they have on the lower-density screen, laid out
/// in the same 800 x 1280 logical space, and rendered with half again as many
/// pixels.
/// </para>
/// <para>
/// The values are a subset of the scales the display pipeline supports
/// (Windows.Graphics.Display.ResolutionScale); more members can be added when
/// a device needs one.
/// </para>
/// </summary>
public enum UserInterfaceScale
{
	/// <summary>
	/// Elements are drawn at their nominal size — one logical unit to one
	/// panel pixel. The default, and what every head did before this option
	/// existed.
	/// </summary>
	Percent100 = 100,

	/// <summary>
	/// Elements are drawn half again as large. The scale for an 8-inch panel
	/// at 1200 x 1920 (e.g. the NuVision TM800W610L), which lays such a panel
	/// out in 800 x 1280 logical units.
	/// </summary>
	Percent150 = 150,

	/// <summary>Elements are drawn at twice their nominal size.</summary>
	Percent200 = 200,
}
