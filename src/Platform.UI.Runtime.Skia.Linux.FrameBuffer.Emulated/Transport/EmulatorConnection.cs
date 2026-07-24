using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net.Sockets;
using System.Threading;
using CodeBrix.Platform.UI.Runtime.Skia.Native;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;

/// <summary>
/// The head's side of the emulator transport: the mmapped shared-memory frame
/// buffer standing in for /dev/fb0, and the socket standing in for libinput.
/// The IDE created both before launching this process; their paths and the
/// device resolution arrive through the launch-contract environment variables.
/// <para>
/// THE POWER CORD: the socket reaching end-of-file — or failing in any way —
/// means CodeBrix.Develop is gone, and this device just lost power. The
/// response is libc _exit, immediately, from whichever thread noticed: no
/// finalizers, no cleanup, exactly like a kiosk with its plug pulled. That
/// also guarantees a kill -9 of the IDE never leaves an orphaned app behind.
/// </para>
/// </summary>
internal sealed class EmulatorConnection
{
	private readonly MemoryMappedFile _sharedMemory;
	private readonly MemoryMappedViewAccessor _accessor;
	private readonly IntPtr _basePointer;
	private readonly int _slot0Offset;
	private readonly int _slot1Offset;
	private readonly Socket _socket;
	private readonly object _sendLock = new();

	private EmulatorConnection(int width, int height, MemoryMappedFile sharedMemory,
		MemoryMappedViewAccessor accessor, IntPtr basePointer, int slot0Offset, int slot1Offset,
		Socket socket)
	{
		Width = width;
		Height = height;
		Stride = width * 4;
		_sharedMemory = sharedMemory;
		_accessor = accessor;
		_basePointer = basePointer;
		_slot0Offset = slot0Offset;
		_slot1Offset = slot1Offset;
		_socket = socket;
	}

	/// <summary>The device width, in pixels, fixed for the process's life.</summary>
	public int Width { get; }

	/// <summary>The device height, in pixels, fixed for the process's life.</summary>
	public int Height { get; }

	/// <summary>Bytes per pixel row (width * 4).</summary>
	public int Stride { get; }

	/// <summary>
	/// Reads the launch contract, maps the shared memory, validates the header
	/// against the environment (resolution lockstep), connects the socket and
	/// sends the Hello handshake. On any failure, <paramref name="error"/> is
	/// one clear line for stderr and the caller must exit with code 1.
	/// </summary>
	public static bool TryCreate([NotNullWhen(true)] out EmulatorConnection? connection,
		[NotNullWhen(false)] out string? error)
	{
		connection = null;

		var shmPath = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.ShmPathVariable);
		var socketPath = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.SocketPathVariable);
		var widthText = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.WidthVariable);
		var heightText = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.HeightVariable);
		if (string.IsNullOrEmpty(shmPath) || string.IsNullOrEmpty(socketPath)
			|| !int.TryParse(widthText, NumberStyles.None, CultureInfo.InvariantCulture, out var width)
			|| !int.TryParse(heightText, NumberStyles.None, CultureInfo.InvariantCulture, out var height)
			|| width <= 0 || height <= 0)
		{
			error = "This application was built for the CodeBrix.Develop frame-buffer emulator and must be launched by it.";
			return false;
		}

		try
		{
			var stream = new FileStream(shmPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			var sharedMemory = MemoryMappedFile.CreateFromFile(stream, null, 0,
				MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: false);
			var accessor = sharedMemory.CreateViewAccessor();

			var magic = accessor.ReadUInt32(FrameBufferEmulatorProtocol.MagicOffset);
			var version = accessor.ReadUInt32(FrameBufferEmulatorProtocol.VersionOffset);
			var headerWidth = accessor.ReadUInt32(FrameBufferEmulatorProtocol.WidthOffset);
			var headerHeight = accessor.ReadUInt32(FrameBufferEmulatorProtocol.HeightOffset);
			if (magic != FrameBufferEmulatorProtocol.Magic
				|| version != FrameBufferEmulatorProtocol.Version)
			{
				error = $"The emulator's shared memory at '{shmPath}' does not speak protocol version {FrameBufferEmulatorProtocol.Version}.";
				return false;
			}
			if (headerWidth != (uint) width || headerHeight != (uint) height)
			{
				error = $"Resolution lockstep violated: the environment says {width}x{height} but the emulator's shared memory says {headerWidth}x{headerHeight}.";
				return false;
			}
			var slot0Offset = (int) accessor.ReadUInt32(FrameBufferEmulatorProtocol.Slot0Offset);
			var slot1Offset = (int) accessor.ReadUInt32(FrameBufferEmulatorProtocol.Slot1Offset);

			IntPtr basePointer;
			unsafe
			{
				byte* pointer = null;
				accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
				basePointer = (IntPtr) pointer;
			}

			var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
			socket.Connect(new UnixDomainSocketEndPoint(socketPath));

			connection = new EmulatorConnection(width, height, sharedMemory, accessor,
				basePointer, slot0Offset, slot1Offset, socket);
			connection.SendMessage(FrameBufferEmulatorProtocol.HelloMessage,
				FrameBufferEmulatorProtocol.Version, (uint) Environment.ProcessId, 0);
			error = null;
			return true;
		}
		catch (Exception e) when (e is IOException or SocketException or UnauthorizedAccessException)
		{
			error = $"Could not reach the CodeBrix.Develop frame-buffer emulator: {e.Message}";
			return false;
		}
	}

	/// <summary>
	/// The pixel slot frame <paramref name="sequence"/> must be rendered into
	/// (frame N lives in slot N % 2).
	/// </summary>
	public IntPtr GetSlotPointer(long sequence)
		=> _basePointer + (sequence % 2 == 0 ? _slot0Offset : _slot1Offset);

	/// <summary>
	/// Publishes frame <paramref name="sequence"/>: atomically stores the
	/// sequence number in the header (the authoritative signal), then sends
	/// the advisory FrameReady message.
	/// </summary>
	public void PublishFrame(long sequence)
	{
		unsafe
		{
			Volatile.Write(
				ref *(long*) ((byte*) _basePointer + FrameBufferEmulatorProtocol.LatestSequenceOffset),
				sequence);
		}
		SendMessage(FrameBufferEmulatorProtocol.FrameReadyMessage,
			(uint) sequence, (uint) ((ulong) sequence >> 32), (uint) (sequence % 2));
	}

	/// <summary>
	/// Starts the input thread: blocks on the socket, dispatches touch
	/// messages to <paramref name="onTouch"/> (message type, device x, device
	/// y — on the input thread; the callee marshals), and powers the device
	/// off on end-of-file.
	/// </summary>
	public void StartInputLoop(Action<uint, int, int> onTouch)
	{
		new Thread(() =>
		{
			var buffer = new byte[FrameBufferEmulatorProtocol.MessageSize];
			while (true)
			{
				if (!TryReceiveExactly(buffer))
				{
					PowerOff();
				}
				var (type, a, b, _) = FrameBufferEmulatorProtocol.ReadMessage(buffer);
				if (type is FrameBufferEmulatorProtocol.TouchPressMessage
					or FrameBufferEmulatorProtocol.TouchMoveMessage
					or FrameBufferEmulatorProtocol.TouchReleaseMessage)
				{
					onTouch(type, (int) a, (int) b);
				}
				// Unknown message types are ignored, for forward compatibility.
			}
		})
		{
			IsBackground = true,
			Name = "FrameBuffer.Emulated input thread"
		}.Start();
	}

	private bool TryReceiveExactly(byte[] buffer)
	{
		var received = 0;
		while (received < buffer.Length)
		{
			int count;
			try
			{
				count = _socket.Receive(buffer, received, buffer.Length - received, SocketFlags.None);
			}
			catch (SocketException)
			{
				return false;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			if (count == 0)
			{
				return false;
			}
			received += count;
		}
		return true;
	}

	private void SendMessage(uint type, uint a, uint b, uint c)
	{
		Span<byte> message = stackalloc byte[FrameBufferEmulatorProtocol.MessageSize];
		FrameBufferEmulatorProtocol.WriteMessage(message, type, a, b, c);
		lock (_sendLock)
		{
			try
			{
				_socket.Send(message);
			}
			catch (SocketException)
			{
				PowerOff();
			}
			catch (ObjectDisposedException)
			{
				PowerOff();
			}
		}
	}

	[DoesNotReturn]
	private static void PowerOff()
	{
		// Loss of power, not a shutdown: see the class remarks.
		Libc.ExitImmediately(0);
		throw new InvalidOperationException("unreachable");
	}
}
