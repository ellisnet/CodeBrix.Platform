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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using CodeBrix.Platform.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using SkiaSharp;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Clipboard over the Wayland data-device protocol. Copying installs a wl_data_source on the
/// selection (advertising the package's formats and streaming the bytes when a paste requests
/// them); pasting reads the current wl_data_offer through a pipe. Requires an input serial and
/// keyboard focus, so it only works while a window of this app is focused — intrinsic to
/// Wayland's security model, not a limitation of this port.
/// </summary>
/// <remarks>
/// Format parity follows the X11 head: text (several aliases), text/html, image/png (+jpeg on
/// paste), text/uri-list mapped to StorageItems, and pass-through of custom byte[]/string
/// formats under their own MIME names. Drag-and-drop rides its own wl_data_device events and
/// lives in <c>WaylandDragDropExtension</c>.
/// </remarks>
internal sealed class WaylandClipboardExtension : IClipboardExtension
{
	private const string MimeTextPlainUtf8 = "text/plain;charset=utf-8";
	private const string MimeTextPlain = "text/plain";
	private const string MimeUtf8String = "UTF8_STRING";
	private const string MimeText = "TEXT";
	private const string MimeHtml = "text/html";
	private const string MimePng = "image/png";
	private const string MimeUriList = "text/uri-list";

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

	// The MIME types each live offer advertised (wl_data_offer.offer events).
	private readonly Dictionary<WlDataOffer, List<string>> _offerMimes = new();

	// The payloads we are currently the selection owner of, materialized at SetContent time
	// and streamed on wl_data_source.send (the send callback runs on the event-pump thread,
	// where reaching back into the DataPackage would be unsafe).
	private List<(string Mime, byte[] Bytes)> _ownedEntries = new();
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
			// A listener is attached at consumption so the offer's advertised MIME types
			// (which start arriving immediately after the intro event) are recorded.
			OnDataOffer = (_, id) => OnDataOffer(id.GetAndConsume(new WlDataOffer.Listener.Relay
			{
				OnOffer = (offer, mime) => OnOfferMime(offer, mime),
			})),
			OnSelection = (_, offer) => OnSelection(offer),
			// Drag-and-drop shares the data device; the drag events are routed to the
			// focused window's WaylandDragDropExtension on its UI thread.
			OnEnter = (_, serial, surface, x, y, offer) => OnDragEnter(serial, surface, (double)x, (double)y, offer),
			OnMotion = (_, _, x, y) => OnDragMotion((double)x, (double)y),
			OnLeave = _ => OnDragLeave(),
			OnDrop = _ => OnDragDrop(),
		});
		connection.Flush();
	}

	// The host whose surface the current drag session is over (event-pump thread).
	private WaylandXamlRootHost? _dragHost;

	// Event-pump thread.
	private void OnDragEnter(uint serial, WlSurface? surface, double x, double y, WlDataOffer? offer)
	{
		string[] mimes;
		lock (_gate)
		{
			if (offer != null && ReferenceEquals(_pendingOffer, offer))
			{
				// Ownership of the offer moves to the drag session (it is destroyed by the
				// drag extension when the session ends).
				_pendingOffer = null;
			}
			mimes = offer != null && _offerMimes.TryGetValue(offer, out var list)
				? list.ToArray()
				: Array.Empty<string>();
		}

		_dragHost = WaylandXamlRootHost.GetHostFromSurface(surface);
		if (_dragHost?.DragDropExtension is { } extension)
		{
			var position = new Windows.Foundation.Point(x, y);
			WaylandXamlRootHost.QueueAction(_dragHost, () => extension.ProcessDragEnter(offer, mimes, serial, position));
		}
		else if (offer != null)
		{
			// No drop target on this window: decline by destroying the offer.
			lock (_gate)
			{
				DestroyOffer(offer);
			}
			_dragHost = null;
		}
	}

	// Event-pump thread.
	private void OnDragMotion(double x, double y)
	{
		if (_dragHost is { DragDropExtension: { } extension } host)
		{
			var position = new Windows.Foundation.Point(x, y);
			WaylandXamlRootHost.QueueAction(host, () => extension.ProcessDragMotion(position));
		}
	}

	// Event-pump thread.
	private void OnDragLeave()
	{
		if (_dragHost is { DragDropExtension: { } extension } host)
		{
			WaylandXamlRootHost.QueueAction(host, extension.ProcessDragLeave);
		}
		_dragHost = null;
	}

	// Event-pump thread.
	private void OnDragDrop()
	{
		if (_dragHost is { DragDropExtension: { } extension } host)
		{
			WaylandXamlRootHost.QueueAction(host, extension.ProcessDrop);
		}
	}

	/// <summary>
	/// Drops any bookkeeping for an offer whose lifetime is managed elsewhere (the drag
	/// session destroys its own offer).
	/// </summary>
	internal void ForgetOffer(WlDataOffer offer)
	{
		lock (_gate)
		{
			_ = _offerMimes.Remove(offer);
			if (ReferenceEquals(_currentOffer, offer))
			{
				_currentOffer = null;
			}
			if (ReferenceEquals(_pendingOffer, offer))
			{
				_pendingOffer = null;
			}
		}
	}

	/// <summary>
	/// Reads the offer's advertised formats EAGERLY into the package (unlike the lazy
	/// clipboard paste): a drag offer dies when its session ends, so data fetched lazily
	/// after the drop would be gone. Same caching call the X11 head makes on XdndPosition.
	/// </summary>
	internal void FillDataPackageFromOffer(DataPackage package, WlDataOffer offer, string[] mimes, WaylandConnection connection)
	{
		const long MaxEagerBytes = 64 * 1024 * 1024;

		var cache = new Dictionary<string, byte[]>();
		long total = 0;
		foreach (var mime in mimes)
		{
			if (cache.ContainsKey(mime))
			{
				continue;
			}

			if (ReceiveBytes(offer, mime, connection) is { } bytes)
			{
				cache[mime] = bytes;
				package.SetData(mime, bytes);
				total += bytes.Length;
				if (total > MaxEagerBytes)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Drag payload exceeded {MaxEagerBytes} bytes; remaining formats were not cached.");
					}
					break;
				}
			}
		}

		if (mimes.FirstOrDefault(m => Array.IndexOf(OfferedTextMimes, m) >= 0) is { } textMime
			&& cache.TryGetValue(textMime, out var textBytes))
		{
			package.SetText(Encoding.UTF8.GetString(textBytes));
		}

		if (cache.TryGetValue(MimeUriList, out var uriListBytes))
		{
			var items = ProcessUriList(Encoding.UTF8.GetString(uriListBytes)).ToList();
			if (items.Count > 0)
			{
				package.SetStorageItems(items);
			}
		}
	}

	// Event-pump thread.
	private void OnDataOffer(WlDataOffer offer)
	{
		lock (_gate)
		{
			// A newly introduced offer supersedes a pending one that never became the selection.
			DestroyOffer(_pendingOffer);
			_pendingOffer = offer;
		}
	}

	// Event-pump thread.
	private void OnOfferMime(WlDataOffer offer, string mime)
	{
		lock (_gate)
		{
			if (!_offerMimes.TryGetValue(offer, out var mimes))
			{
				_offerMimes[offer] = mimes = new List<string>();
			}
			mimes.Add(mime);
		}
	}

	// Must hold _gate.
	private void DestroyOffer(WlDataOffer? offer)
	{
		if (offer != null)
		{
			_ = _offerMimes.Remove(offer);
			offer.Destroy();
		}
	}

	// Event-pump thread.
	private void OnSelection(WlDataOffer? offer)
	{
		lock (_gate)
		{
			if (_pendingOffer != null && !ReferenceEquals(_pendingOffer, offer))
			{
				DestroyOffer(_pendingOffer);
			}
			_pendingOffer = null;

			// Per protocol, the previous selection offer must be destroyed on this event.
			if (_currentOffer != null && !ReferenceEquals(_currentOffer, offer))
			{
				DestroyOffer(_currentOffer);
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

		var entries = BuildOfferedEntries(content);

		lock (_gate)
		{
			_ownedEntries = entries;

			// Replace any previous source we owned.
			_ownedSource?.Destroy();
			_ownedSource = null;

			if (entries.Count == 0)
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

			foreach (var mime in entries.Select(e => e.Mime).Distinct())
			{
				source.Offer(mime);
			}

			_ownedSource = source;
			_dataDevice.SetSelection(source, connection.SeatManager.LastInputSerial);
			connection.Flush();
		}
	}

	/// <summary>
	/// Materializes the package into (mime, bytes) pairs, mirroring the X11 head's format
	/// story: text under its interchange aliases (a Uri falls back to its string form), HTML
	/// as text/html, a bitmap normalized to image/png via Skia, storage items as a
	/// text/uri-list of file:// URIs, and custom byte[]/string formats under their own names.
	/// </summary>
	private List<(string Mime, byte[] Bytes)> BuildOfferedEntries(DataPackage content)
	{
		var entries = new List<(string Mime, byte[] Bytes)>();
		var view = content?.GetView();
		if (view == null)
		{
			return entries;
		}

		string? text = null;
		if (view.Contains(StandardDataFormats.Text))
		{
			text = TryGetData(view, StandardDataFormats.Text, static v => v.GetTextAsync().GetResults());
		}
		else if (view.Contains(StandardDataFormats.Uri))
		{
			text = TryGetData(view, StandardDataFormats.Uri, static v => v.GetUriAsync().GetResults()?.ToString());
		}
		if (text != null)
		{
			var textBytes = Encoding.UTF8.GetBytes(text);
			foreach (var mime in OfferedTextMimes)
			{
				entries.Add((mime, textBytes));
			}
		}

		if (view.Contains(StandardDataFormats.Html)
			&& TryGetData(view, StandardDataFormats.Html, static v => v.GetHtmlFormatAsync().GetResults()) is { } html)
		{
			entries.Add((MimeHtml, Encoding.UTF8.GetBytes(html)));
		}

		if (view.Contains(StandardDataFormats.Bitmap)
			&& TryGetData(view, StandardDataFormats.Bitmap, static v =>
			{
				var reference = v.GetBitmapAsync().GetResults();
				using var stream = reference.OpenReadAsync().GetResults().AsStreamForRead();
				using var memory = new MemoryStream();
				stream.CopyTo(memory);
				return memory.ToArray();
			}) is { } imageBytes)
		{
			// Normalize to PNG (Linux skia builds carry no bmp codec, same constraint the
			// X11 head notes); raw pass-through when the bytes do not decode.
			try
			{
				using var bitmap = SKBitmap.Decode(imageBytes);
				if (bitmap != null)
				{
					using var image = SKImage.FromBitmap(bitmap);
					using var png = image.Encode(SKEncodedImageFormat.Png, 100);
					entries.Add((MimePng, png.ToArray()));
				}
				else
				{
					entries.Add((MimePng, imageBytes));
				}
			}
			catch
			{
				entries.Add((MimePng, imageBytes));
			}
		}

		if (view.Contains(StandardDataFormats.StorageItems)
			&& TryGetData(view, StandardDataFormats.StorageItems, static v => v.GetStorageItemsAsync().GetResults()) is { } items)
		{
			var builder = new StringBuilder();
			foreach (var item in items)
			{
				if (!string.IsNullOrEmpty(item.Path))
				{
					builder.Append(new Uri(item.Path).AbsoluteUri).Append("\r\n");
				}
			}
			if (builder.Length > 0)
			{
				entries.Add((MimeUriList, Encoding.UTF8.GetBytes(builder.ToString())));
			}
		}

		// Custom formats under their own names (the X11 head's "last-ditch" pass-through).
		foreach (var format in view.AvailableFormats)
		{
			if (format == StandardDataFormats.Text || format == StandardDataFormats.Uri
				|| format == StandardDataFormats.Html || format == StandardDataFormats.Bitmap
				|| format == StandardDataFormats.StorageItems)
			{
				continue;
			}

			var data = TryGetData(view, format, v => v.GetDataAsync(format).GetResults());
			if (data is byte[] rawBytes)
			{
				entries.Add((format, rawBytes));
			}
			else if (data is string rawString)
			{
				entries.Add((format, Encoding.UTF8.GetBytes(rawString)));
			}
		}

		return entries;
	}

	private T? TryGetData<T>(DataPackageView view, string format, Func<DataPackageView, T?> getter) where T : class
	{
		try
		{
			return getter(view);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Could not materialize clipboard format '{format}'; it will not be offered.", e);
			}
			return null;
		}
	}

	// Event-pump thread: a paster asked us to stream the selection to fd.
	private void OnSourceSend(string mimeType, int fd)
	{
		byte[]? bytes = null;
		lock (_gate)
		{
			foreach (var entry in _ownedEntries)
			{
				if (entry.Mime == mimeType)
				{
					bytes = entry.Bytes;
					break;
				}
			}
		}

		try
		{
			if (bytes != null)
			{
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
				_ownedEntries = new List<(string Mime, byte[] Bytes)>();
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
			string[] mimes;
			lock (_gate)
			{
				offer = _currentOffer;
				mimes = offer != null && _offerMimes.TryGetValue(offer, out var list)
					? list.ToArray()
					: Array.Empty<string>();
			}

			if (offer != null)
			{
				// Every advertised MIME type is exposed under its own name as a lazy raw-bytes
				// provider — the same shape the X11 head gives pasted data (html and images
				// are consumed through these).
				foreach (var mime in mimes)
				{
					var mimeCopy = mime;
					package.SetDataProvider(mimeCopy, async ct =>
						await Task.Run(() => (object)(ReceiveBytes(offer, mimeCopy, connection) ?? Array.Empty<byte>()), ct));
				}

				// Text and file lists are materialized eagerly (X11 parity): text under the
				// best matching interchange alias, uri-lists as StorageItems.
				if (mimes.FirstOrDefault(m => Array.IndexOf(OfferedTextMimes, m) >= 0) is { } textMime)
				{
					var text = ReceiveText(offer, textMime, connection);
					if (text != null)
					{
						package.SetText(text);
					}
				}
				else if (mimes.Length == 0)
				{
					// Compatibility fallback for sources whose offer events were missed (the
					// pre-P3 behavior): probe for plain text directly.
					var text = ReceiveText(offer, MimeTextPlainUtf8, connection);
					if (!string.IsNullOrEmpty(text))
					{
						package.SetText(text);
					}
				}

				if (mimes.Contains(MimeUriList)
					&& ReceiveBytes(offer, MimeUriList, connection) is { } uriListBytes)
				{
					var items = ProcessUriList(Encoding.UTF8.GetString(uriListBytes)).ToList();
					if (items.Count > 0)
					{
						package.SetStorageItems(items);
					}
				}
			}
		}

		return package.GetView();
	}

	/// <summary>
	/// Maps a text/uri-list (one file:// URI per line, '#' comments) to storage items,
	/// mirroring the X11 head's conversion.
	/// </summary>
	private static IEnumerable<IStorageItem> ProcessUriList(string uriList)
	{
		foreach (var line in uriList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
		{
			var entry = line.Trim();
			if (entry.Length == 0 || entry.StartsWith('#'))
			{
				continue;
			}

			if (!Uri.TryCreate(entry, UriKind.Absolute, out var uri) || !uri.IsFile)
			{
				continue;
			}

			var path = uri.LocalPath;
			if (Directory.Exists(path))
			{
				yield return new StorageFolder(path);
			}
			else if (File.Exists(path))
			{
				yield return StorageFile.GetFileFromPath(path);
			}
		}
	}

	private string? ReceiveText(WlDataOffer offer, string mime, WaylandConnection connection)
		=> ReceiveBytes(offer, mime, connection) is { } bytes ? Encoding.UTF8.GetString(bytes) : null;

	private byte[]? ReceiveBytes(WlDataOffer offer, string mime, WaylandConnection connection)
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
			offer.Receive(mime, writeFd);
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

			return result.ToArray();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to read the Wayland selection as '{mime}'", e);
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
