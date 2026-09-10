using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;

/// <summary>
/// The in-process transport: the same two-slot pixel destination the emulator's
/// shared memory provides, but allocated in this process's own native memory and
/// consumed by this process. Publishing a frame COPIES the just-rendered slot
/// into a fresh immutable snapshot, so a consumer can hold a frame for as long
/// as it likes while the renderer keeps drawing — there is no tearing window at
/// all — and raises <see cref="FramePublished"/> with it.
/// <para>
/// There is no socket, no shared memory and no power cord: nothing in this class
/// can end the process. Input is not a thread here either —
/// <see cref="StartInputLoop"/> only records the callbacks, and the owner of the
/// transport invokes them directly (both callees marshal onto the UI thread
/// themselves, exactly as they do for the socket's input thread).
/// </para>
/// </summary>
internal sealed class InProcessTransport : IEmulatorTransport, IDisposable
{
	private readonly object _publishLock = new();
	private readonly IntPtr _slot0;
	private readonly IntPtr _slot1;
	private readonly int _slotSize;

	private Action<uint, int, int, int>? _onTouch;
	private Action<bool, uint, uint, uint>? _onKey;
	private long _latestSequence;
	private long _latestRenderGeneration;
	private byte[]? _latestSnapshot;
	private bool _disposed;

	/// <summary>
	/// Allocates the panel's two pixel slots.
	/// </summary>
	/// <param name="width">The panel width, in device pixels.</param>
	/// <param name="height">The panel height, in device pixels.</param>
	internal InProcessTransport(int width, int height)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), width, "The panel width must be positive.");
		}
		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(height), height, "The panel height must be positive.");
		}

		Width = width;
		Height = height;
		Stride = width * 4;
		_slotSize = Stride * height;
		// Native, not managed: the renderer writes through a raw pointer for the
		// whole life of the process, so the memory must never move.
		_slot0 = Marshal.AllocHGlobal(_slotSize);
		_slot1 = Marshal.AllocHGlobal(_slotSize);
		Zero(_slot0, _slotSize);
		Zero(_slot1, _slotSize);
	}

	/// <summary>
	/// Raised, on the rendering thread, once a frame has been copied out of its
	/// slot: the frame's sequence number, the invalidation generation the render
	/// started from, and its immutable BGRA8888 premultiplied pixels, top-down
	/// rows, <see cref="Stride"/> bytes each.
	/// </summary>
	internal event Action<long, long, byte[]>? FramePublished;

	/// <inheritdoc />
	public int Width { get; }

	/// <inheritdoc />
	public int Height { get; }

	/// <inheritdoc />
	public int Stride { get; }

	/// <summary>The sequence number of the most recently published frame, or 0 when there is none.</summary>
	internal long LatestSequence => Volatile.Read(ref _latestSequence);

	/// <summary>
	/// The invalidation generation the most recently published frame was rendered
	/// from, or 0 when there is none.
	/// </summary>
	internal long LatestRenderGeneration => Volatile.Read(ref _latestRenderGeneration);

	/// <summary>The most recently published frame's pixels, or null when there is none.</summary>
	internal byte[]? LatestSnapshot
	{
		get
		{
			lock (_publishLock)
			{
				return _latestSnapshot;
			}
		}
	}

	/// <inheritdoc />
	public IntPtr GetSlotPointer(long sequence)
		=> sequence % 2 == 0 ? _slot0 : _slot1;

	/// <inheritdoc />
	public void PublishFrame(long sequence, long renderGeneration)
	{
		var snapshot = new byte[_slotSize];
		Marshal.Copy(GetSlotPointer(sequence), snapshot, 0, _slotSize);
		lock (_publishLock)
		{
			_latestSnapshot = snapshot;
		}
		Volatile.Write(ref _latestRenderGeneration, renderGeneration);
		Volatile.Write(ref _latestSequence, sequence);
		FramePublished?.Invoke(sequence, renderGeneration, snapshot);
	}

	/// <inheritdoc />
	public void StartInputLoop(Action<uint, int, int, int> onTouch,
		Action<bool, uint, uint, uint> onKey,
		Action<uint> onDeviceOrientation)
	{
		// No thread and no wire: the callbacks are simply remembered so the
		// session can deliver input straight to them. The device is never turned
		// here — the panel is fixed for the life of the process — so the
		// orientation callback is deliberately dropped.
		_onTouch = onTouch;
		_onKey = onKey;
	}

	/// <summary>
	/// Delivers one touch message, exactly as the socket's input thread would.
	/// </summary>
	/// <param name="messageType">One of the protocol's touch message types.</param>
	/// <param name="pointerId">The pointer (finger) id.</param>
	/// <param name="deviceX">The x coordinate, in device pixels.</param>
	/// <param name="deviceY">The y coordinate, in device pixels.</param>
	internal void SendTouch(uint messageType, int pointerId, int deviceX, int deviceY)
		=> (_onTouch ?? throw new InvalidOperationException(
			"Input has not been started on this transport yet."))(messageType, pointerId, deviceX, deviceY);

	/// <summary>
	/// Delivers one key transition, exactly as the socket's input thread would.
	/// </summary>
	/// <param name="pressed">Whether the key went down (otherwise up).</param>
	/// <param name="virtualKey">The WinUI VirtualKey value.</param>
	/// <param name="hardwareKeyCode">The X11-style hardware keycode, or 0 for none.</param>
	/// <param name="unicodeCodepoint">The Unicode codepoint for text input, or 0 for none.</param>
	internal void SendKey(bool pressed, uint virtualKey, uint hardwareKeyCode, uint unicodeCodepoint)
		=> (_onKey ?? throw new InvalidOperationException(
			"Input has not been started on this transport yet."))(pressed, virtualKey, hardwareKeyCode, unicodeCodepoint);

	/// <summary>Frees the two pixel slots.</summary>
	public void Dispose()
	{
		lock (_publishLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_latestSnapshot = null;
		}
		Marshal.FreeHGlobal(_slot0);
		Marshal.FreeHGlobal(_slot1);
	}

	private static void Zero(IntPtr pointer, int length)
	{
		unsafe
		{
			new Span<byte>((void*) pointer, length).Clear();
		}
	}
}
