namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;

/// <summary>
/// The shape of the test target's panel, chosen once for the life of the
/// process. The application is always laid out upright on it: this picks the
/// panel's proportions, never a rotation of the application's canvas.
/// </summary>
public enum TestDisplayOrientation
{
	/// <summary>A landscape panel: 1920 x 1080 device pixels.</summary>
	Landscape,

	/// <summary>A portrait panel: 1080 x 1920 device pixels.</summary>
	Portrait,
}
