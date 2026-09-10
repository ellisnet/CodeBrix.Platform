using System;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;

/// <summary>
/// What the emulated head's renderer and hosts actually need from a transport:
/// a fixed-size pixel destination made of two alternating slots, a way to
/// publish a finished frame by sequence number, and an input source. The
/// socket-and-shared-memory <see cref="EmulatorConnection"/> is one
/// implementation (the CodeBrix.Develop emulator); <see cref="InProcessTransport"/>
/// is the other (the in-process test target, which never leaves the process).
/// </summary>
internal interface IEmulatorTransport
{
	/// <summary>The panel width, in device pixels, fixed for the process's life.</summary>
	int Width { get; }

	/// <summary>The panel height, in device pixels, fixed for the process's life.</summary>
	int Height { get; }

	/// <summary>Bytes per pixel row (<see cref="Width"/> * 4).</summary>
	int Stride { get; }

	/// <summary>
	/// The pixel slot frame <paramref name="sequence"/> must be rendered into
	/// (frame N lives in slot N % 2).
	/// </summary>
	/// <param name="sequence">The frame's sequence number.</param>
	/// <returns>A pointer to the first byte of the slot.</returns>
	IntPtr GetSlotPointer(long sequence);

	/// <summary>
	/// Publishes frame <paramref name="sequence"/>: the slot it was rendered
	/// into now holds a complete frame and may be consumed.
	/// </summary>
	/// <param name="sequence">The frame's sequence number.</param>
	/// <param name="renderGeneration">
	/// The invalidation generation the render this frame came from started from.
	/// It only ever grows, and a frame whose generation is <c>g</c> or larger was
	/// rendered entirely after invalidation <c>g</c> was requested — which is how
	/// a consumer tells a fresh frame from one that was already in flight when it
	/// asked for a repaint.
	/// </param>
	void PublishFrame(long sequence, long renderGeneration);

	/// <summary>
	/// Starts delivering input. Touch messages carry (message type, pointer id,
	/// device x, device y); key messages carry (pressed, VirtualKey, hardware
	/// keycode, Unicode codepoint or 0); the orientation message carries the
	/// device's new orientation as a protocol wire value. Callees marshal onto
	/// the UI thread themselves.
	/// </summary>
	/// <param name="onTouch">Receives touch messages.</param>
	/// <param name="onKey">Receives key messages.</param>
	/// <param name="onDeviceOrientation">Receives device-orientation messages.</param>
	void StartInputLoop(Action<uint, int, int, int> onTouch,
		Action<bool, uint, uint, uint> onKey,
		Action<uint> onDeviceOrientation);
}
