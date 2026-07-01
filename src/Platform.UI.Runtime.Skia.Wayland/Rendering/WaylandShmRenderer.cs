using System;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Software renderer: Skia draws into a memfd-backed wl_shm buffer that is attached and
/// committed to the window's wl_surface. Double-buffered; wl_buffer release events recycle
/// buffers and wl_callback frame events pace redraws to the compositor (no free-running loop).
/// </summary>
internal sealed class WaylandShmRenderer : IWaylandRenderer
{
	// ARGB8888 in Wayland's little-endian byte order is exactly SkiaSharp's Bgra8888/premul.
	private const WlShm.FormatEnum BufferFormat = WlShm.FormatEnum.Argb8888;
	private const SKColorType SurfaceColorType = SKColorType.Bgra8888;
	private const int BytesPerPixel = 4;
	private const int SlotCount = 2;

	private sealed class BufferSlot
	{
		public WlBuffer? Buffer;
		public SKSurface? Surface;
		public bool Busy;
	}

	private readonly object _gate = new();
	private readonly IXamlRootHost _host;
	private readonly WaylandConnection _connection;
	private readonly WlSurface _wlSurface;
	private readonly BufferSlot[] _slots = new BufferSlot[SlotCount];

	private SKColor _background = SKColors.White;
	private WlShmPool? _pool;
	private int _shmFd = -1;
	private IntPtr _shmAddress;
	private long _shmSize;
	// _width/_height are the BUFFER (physical pixel) dimensions; the logical size passed to
	// Resize is multiplied by the integer output scale so HiDPI displays render sharp.
	private int _width;
	private int _height;
	private int _bufferScale = 1;
	private bool _framePending;
	private bool _redrawRequested;
	private int _renderCount;
	private bool _disposed;

	public WaylandShmRenderer(IXamlRootHost host, WaylandConnection connection, WlSurface wlSurface)
	{
		_host = host;
		_connection = connection;
		_wlSurface = wlSurface;
		for (var i = 0; i < SlotCount; i++)
		{
			_slots[i] = new BufferSlot();
		}
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	public void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		if (_host is WaylandXamlRootHost { IsClosed: true })
		{
			return;
		}

		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}

			if (_framePending)
			{
				// The compositor hasn't consumed the previous frame yet; redraw when the
				// wl_callback fires instead of racing ahead of the display.
				_redrawRequested = true;
				return;
			}

			var slot = GetFreeSlot();
			if (slot == null)
			{
				_redrawRequested = true;
				return;
			}

			var canvas = slot.Surface?.Canvas;
			canvas?.Clear(_background);

			_ = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(canvas, size =>
			{
				Resize((int)size.Width, (int)size.Height);
				slot = GetFreeSlot()!; // fresh buffers are all free
				slot.Surface!.Canvas.Clear(_background);
				return slot.Surface.Canvas;
			});

			if (slot.Surface == null || slot.Buffer == null)
			{
				return; // never sized — nothing to present
			}

			slot.Busy = true;
			// The compositor needs to know the buffer is _bufferScale× the logical size so it
			// scales it down for display (integer HiDPI). DamageBuffer coordinates are in buffer
			// (physical) pixels, which is what _width/_height hold.
			_wlSurface.SetBufferScale(_bufferScale);
			_wlSurface.Attach(slot.Buffer, 0, 0);
			_wlSurface.DamageBuffer(0, 0, _width, _height);
			_framePending = true;
			_ = _wlSurface.Frame(new WlCallback.Listener.Relay { OnDone = OnFrameDone });
			_wlSurface.Commit();
		}

		_connection.Flush();
	}

	private void OnFrameDone(WlCallback callback, uint _)
	{
		// "done" is a destructor event: the native proxy is finished; free the managed wrapper.
		callback.Dispose();

		bool redraw;
		lock (_gate)
		{
			_framePending = false;
			redraw = _redrawRequested;
			_redrawRequested = false;
		}

		if (redraw)
		{
			_host.InvalidateRender();
		}
	}

	private void OnBufferReleased(WlBuffer buffer)
	{
		bool redraw;
		lock (_gate)
		{
			foreach (var slot in _slots)
			{
				if (ReferenceEquals(slot.Buffer, buffer))
				{
					slot.Busy = false;
					break;
				}
			}

			redraw = _redrawRequested && !_framePending;
			if (redraw)
			{
				_redrawRequested = false;
			}
		}

		if (redraw)
		{
			_host.InvalidateRender();
		}
	}

	private BufferSlot? GetFreeSlot()
	{
		foreach (var slot in _slots)
		{
			if (!slot.Busy)
			{
				return slot;
			}
		}

		return null;
	}

	// Must be called under _gate.
	private void Resize(int width, int height)
	{
		if (width <= 0 || height <= 0 || (width == _width && height == _height && _pool != null))
		{
			return;
		}

		ReleaseBuffers();

		// The integer output scale the buffers are rendered at. The size arriving here is
		// already physical pixels (Uno renders at logical × RasterizationScale), so the buffer
		// dimensions are physical and we advertise the scale via wl_surface.set_buffer_scale.
		_bufferScale = Math.Max(1, _connection.PrimaryOutput.Scale);

		_width = width;
		_height = height;
		var stride = width * BytesPerPixel;
		var bufferSize = (long)stride * height;
		_shmSize = bufferSize * SlotCount;

		_shmFd = Libc.memfd_create("codebrix-wayland-shm", Libc.MFD_CLOEXEC);
		if (_shmFd < 0)
		{
			throw new InvalidOperationException("memfd_create failed for the wl_shm buffer pool.");
		}

		if (Libc.ftruncate(_shmFd, _shmSize) != 0)
		{
			throw new InvalidOperationException("ftruncate failed for the wl_shm buffer pool.");
		}

		_shmAddress = Libc.mmap(IntPtr.Zero, (UIntPtr)(ulong)_shmSize, Libc.PROT_READ | Libc.PROT_WRITE, Libc.MAP_SHARED, _shmFd, 0);
		if (_shmAddress == Libc.MAP_FAILED)
		{
			throw new InvalidOperationException("mmap failed for the wl_shm buffer pool.");
		}

		// wl_shm duplicates the fd server-side; the pool takes (a copy of) our fd.
		_pool = _connection.Shm.CreatePool(_shmFd, (int)_shmSize);

		var info = new SKImageInfo(width, height, SurfaceColorType, SKAlphaType.Premul);
		for (var i = 0; i < SlotCount; i++)
		{
			var offset = (int)(bufferSize * i);
			_slots[i].Buffer = _pool.CreateBuffer(offset, width, height, stride, BufferFormat,
				new WlBuffer.Listener.Relay { OnRelease = OnBufferReleased });
			_slots[i].Surface = SKSurface.Create(info, _shmAddress + offset, stride);
			_slots[i].Busy = false;
		}
	}

	// Must be called under _gate.
	private void ReleaseBuffers()
	{
		foreach (var slot in _slots)
		{
			slot.Surface?.Dispose();
			slot.Surface = null;
			// Destroy() sends the protocol destructor; safe even while the compositor still
			// scans out the buffer (it keeps the pool's pages alive via its own fd mapping).
			slot.Buffer?.Destroy();
			slot.Buffer = null;
			slot.Busy = false;
		}

		_pool?.Destroy();
		_pool = null;

		if (_shmAddress != IntPtr.Zero)
		{
			_ = Libc.munmap(_shmAddress, (UIntPtr)(ulong)_shmSize);
			_shmAddress = IntPtr.Zero;
		}

		if (_shmFd >= 0)
		{
			_ = Libc.close(_shmFd);
			_shmFd = -1;
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			ReleaseBuffers();
		}
	}
}
