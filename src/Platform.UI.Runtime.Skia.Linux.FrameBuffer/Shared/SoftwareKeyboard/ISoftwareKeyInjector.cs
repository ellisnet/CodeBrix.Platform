// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using Windows.System;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// The software keyboard's injection seam into a head's keyboard input source.
/// Each head's source implements this by raising its normal KeyDown/KeyUp events
/// with the given values, so a software key flows through the exact same pipeline
/// as a hardware key and every control works unmodified.
/// </summary>
internal interface ISoftwareKeyInjector
{
	/// <param name="pressed">True for key-down, false for key-up.</param>
	/// <param name="key">The virtual key, or VirtualKey.None for a pure character.</param>
	/// <param name="unicodeKey">The character the key types, if any. Carried only
	/// on key-down, mirroring the hardware paths.</param>
	void InjectSoftwareKey(bool pressed, VirtualKey key, char? unicodeKey);
}
