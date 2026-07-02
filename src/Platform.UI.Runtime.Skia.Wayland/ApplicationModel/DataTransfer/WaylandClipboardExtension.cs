// The Wayland clipboard model here (wl_data_device_manager / wl_data_device /
// wl_data_source / wl_data_offer, driven by input serials + keyboard focus) follows SDL's
// Wayland data-device backend (src/video/wayland/SDL_waylanddatamanager.c). SDL is zlib.

// zlib License
//
// Copyright (C) 1997-2024 Sam Lantinga <slouken@libsdl.org>
//
// This software is provided 'as-is', without any express or implied warranty. In no event
// will the authors be held liable for any damages arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose, including commercial
// applications, and to alter it and redistribute it freely, subject to the following
// restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not claim that you
//    wrote the original software. If you use this software in a product, an acknowledgment in
//    the product documentation would be appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be misrepresented
//    as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using CodeBrix.Platform.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Text clipboard over the Wayland data-device protocol. Copying installs a wl_data_source
/// on the selection (advertising text mime types and streaming the bytes when a paste
/// requests them); pasting reads the current wl_data_offer through a pipe. Requires an input
/// serial and keyboard focus, so it only works while a window of this app is focused —
/// intrinsic to Wayland's security model, not a limitation of this port.
/// </summary>
/// <remarks>
/// Image/HTML/URI formats and drag-and-drop are a later effort; only plain text is wired.
/// </remarks>
internal sealed class WaylandClipboardExtension : IClipboardExtension
{
	private const string MimeTextPlainUtf8 = "text/plain;charset=utf-8";
	private const string MimeTextPlain = "text/plain";
	private const string MimeUtf8String = "UTF8_STRING";
	private const string MimeText = "TEXT";

	private static readonly string[] OfferedTextMimes = { MimeTextPlainUtf8, MimeUtf8String, MimeTextPlain, MimeText };

	/// <summary>The process-wide instance (also handed out by the ApiExtensibility factory).</summary>
	/// <remarks>
	/// The application host touches this right after the Wayland connection is established:
	/// the compositor only delivers selection offers to data devices that already exist when
	/// keyboard focus enters (or when the selection changes while focused), so the device must
	/// be bound before the first window — a device bound lazily by the first paste has never
	/// received an offer, and that paste would come back empty.
	/// </remarks>
	internal static WaylandClipboardExtension Instance { get; } = new();

	private WaylandClipboardExtension()
	{
		_ = Connection; // bind the data device eagerly (no-op when no compositor is present)
	}

	private readonly object _gate = new();

	// Signals the first wl_data_device.selection event (even a nil one) ever received.
	private readonly ManualResetEventSlim _selectionReceived = new(false);
	private WaylandConnection? _connection;
	private WlDataDevice? _dataDevice;

	// The offer currently held by the compositor's selection (from wl_data_device.selection).
	private WlDataOffer? _currentOffer;

	// The offer most recently introduced by wl_data_device.data_offer; it becomes the
	// selection (or a drag-and-drop offer, unhandled here) in a follow-up event.
	private WlDataOffer? _pendingOffer;

	// The text we are currently the selection owner of (streamed on wl_data_source.send).
	private string? _ownedText;
	private WlDataSource? _ownedSource;

	public event EventHandler<object>? ContentChanged;

	private WaylandConnection? Connection
	{
		get
		{
			lock (_gate)
			{
				if (_connection == null)
				{
					try
					{
						_connection = WaylandConnection.Instance;
						EnsureDataDevice();
					}
					catch (WaylandCompositorMissingException)
					{
						return null;
					}
				}
				return _connection;
			}
		}
	}

	// Must hold _gate.
	private void EnsureDataDevice()
	{
		if (_dataDevice != null || _connection is not { } connection)
		{
			return;
		}

		if (connection.DataDeviceManager is not { } manager || connection.SeatManager.Seat is not { } seat)
		{
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("The Wayland compositor does not expose a data-device manager / seat; the clipboard is unavailable.");
			}
			return;
		}

		_dataDevice = manager.GetDataDevice(seat, new WlDataDevice.Listener.Relay
		{
			// The new-id MUST be consumed: otherwise the interop destroys the offer proxy
			// as it goes out of scope, and the selection event that follows resolves its
			// (now dead) offer argument to null — paste would never see any content.
			OnDataOffer = (_, id) => OnDataOffer(id.GetAndConsume()),
			OnSelection = (_, offer) => OnSelection(offer),
		});
		connection.Flush();
	}

	// Event-pump thread.
	private void OnDataOffer(WlDataOffer offer)
	{
		lock (_gate)
		{
			// A newly introduced offer supersedes a pending one that never became the selection.
			_pendingOffer?.Destroy();
			_pendingOffer = offer;
		}
	}

	// Event-pump thread.
	private void OnSelection(WlDataOffer? offer)
	{
		lock (_gate)
		{
			if (_pendingOffer != null && !ReferenceEquals(_pendingOffer, offer))
			{
				_pendingOffer.Destroy();
			}
			_pendingOffer = null;

			// Per protocol, the previous selection offer must be destroyed on this event.
			if (_currentOffer != null && !ReferenceEquals(_currentOffer, offer))
			{
				_currentOffer.Destroy();
			}
			_currentOffer = offer;
		}
		_selectionReceived.Set();
		ContentChanged?.Invoke(this, new object());
	}

	public void StartContentChanged()
	{
		_ = Connection; // ensure the data device is listening
	}

	public void StopContentChanged()
	{
	}

	public void Clear() => SetContent(new DataPackage());

	public void Flush()
	{
		// Wayland has no "outlive the app" clipboard: the selection is bound to the live
		// wl_data_source, which dies with us. Nothing to persist.
	}

	public void SetContent(DataPackage content)
	{
		if (Connection is not { } connection || _dataDevice is null)
		{
			return;
		}

		string? text = null;
		var view = content?.GetView();
		if (view != null && view.Contains(StandardDataFormats.Text))
		{
			try
			{
				text = view.GetTextAsync().GetResults();
			}
			catch
			{
				text = null;
			}
		}

		lock (_gate)
		{
			_ownedText = text;

			// Replace any previous source we owned.
			_ownedSource?.Destroy();
			_ownedSource = null;

			if (text == null)
			{
				// Clearing: set an empty selection.
				_dataDevice.SetSelection(null, connection.SeatManager.LastInputSerial);
				connection.Flush();
				return;
			}

			var source = connection.DataDeviceManager!.CreateDataSource(new WlDataSource.Listener.Relay
			{
				OnSend = (src, mimeType, fd) => OnSourceSend(mimeType, fd.Consume()),
				OnCancelled = OnSourceCancelled,
			});

			foreach (var mime in OfferedTextMimes)
			{
				source.Offer(mime);
			}

			_ownedSource = source;
			_dataDevice.SetSelection(source, connection.SeatManager.LastInputSerial);
			connection.Flush();
		}
	}

	// Event-pump thread: a paster asked us to stream the selection to fd.
	private void OnSourceSend(string mimeType, int fd)
	{
		string? text;
		lock (_gate)
		{
			text = _ownedText;
		}

		try
		{
			if (text != null)
			{
				var bytes = Encoding.UTF8.GetBytes(text);
				var offset = 0;
				while (offset < bytes.Length)
				{
					var written = (int)Libc.write(fd, new ReadOnlySpan<byte>(bytes, offset, bytes.Length - offset), (nuint)(bytes.Length - offset));
					if (written <= 0)
					{
						break; // reader closed the pipe / error
					}
					offset += written;
				}
			}
		}
		finally
		{
			_ = Libc.close(fd);
		}
	}

	private void OnSourceCancelled(WlDataSource source)
	{
		// Another client took the selection (or we replaced it). Drop our ownership.
		lock (_gate)
		{
			if (ReferenceEquals(source, _ownedSource))
			{
				_ownedSource = null;
				_ownedText = null;
			}
		}
		source.Destroy();
	}

	public DataPackageView GetContent()
	{
		var package = new DataPackage();

		// Goes through the property so a paste-first process still wires the data device.
		if (Connection is { } connection)
		{
			// Selection offers arrive asynchronously on the event pump; when none has ever
			// been delivered yet (device bound moments ago), give the first paste a bounded
			// window instead of reading a guaranteed-empty state.
			if (!_selectionReceived.IsSet)
			{
				_ = _selectionReceived.Wait(TimeSpan.FromMilliseconds(150));
			}

			WlDataOffer? offer;
			lock (_gate)
			{
				offer = _currentOffer;
			}

			if (offer != null)
			{
				var text = ReceiveText(offer, connection);
				if (text != null)
				{
					package.SetText(text);
				}
			}
		}

		return package.GetView();
	}

	private string? ReceiveText(WlDataOffer offer, WaylandConnection connection)
	{
		// Create a pipe, hand the write end to the compositor via wl_data_offer.receive,
		// flush, then read the selection bytes from the read end. (SDL does the same.)
		Span<int> fds = stackalloc int[2];
		if (Libc.pipe2(fds, Libc.O_CLOEXEC) != 0)
		{
			return null;
		}

		var readFd = fds[0];
		var writeFd = fds[1];
		try
		{
			offer.Receive(MimeTextPlainUtf8, writeFd);
			connection.Flush();

			// Close our copy of the write end so the read sees EOF when the source is done.
			_ = Libc.close(writeFd);
			writeFd = -1;

			var buffer = new byte[4096];
			var result = new System.IO.MemoryStream();
			while (true)
			{
				var read = (int)Libc.read(readFd, buffer, (nuint)buffer.Length);
				if (read <= 0)
				{
					break;
				}
				result.Write(buffer, 0, read);
				if (result.Length > 32 * 1024 * 1024)
				{
					break; // sanity cap
				}
			}

			return Encoding.UTF8.GetString(result.ToArray());
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to read the Wayland selection", e);
			}
			return null;
		}
		finally
		{
			if (writeFd >= 0)
			{
				_ = Libc.close(writeFd);
			}
			_ = Libc.close(readFd);
		}
	}
}
