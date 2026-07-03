using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Drop-target support over the wl_data_device drag events (enter/motion/leave/drop), at
/// parity with the X11 head: ACCEPTING drops works, initiating a native drag does not (the
/// X11 head's StartNativeDrag throws too). The clipboard extension owns the data device and
/// routes the drag events here via <see cref="WaylandXamlRootHost.DragDropExtension"/>;
/// all XAML-facing work runs on the UI thread (same QueueAction model as the X11 head).
/// </summary>
internal class WaylandDragDropExtension : IDragDropExtension
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly WaylandXamlRootHost _host;
	private readonly CoreDragDropManager _coreDragDropManager;

	// UI thread only.
	private WlDataOffer? _offer;
	private string[] _mimes = Array.Empty<string>();
	private uint _enterSerial;
	private bool _accepted;
	private bool _dropped;
	private Point _lastPosition;

	public WaylandDragDropExtension(DragDropManager manager)
	{
		if (manager.ContentRoot.GetOrCreateXamlRoot().HostWindow is not { } window)
		{
			throw new InvalidOperationException($"Couldn't find a window associated with the {nameof(WaylandDragDropExtension)}");
		}
		_host = WaylandXamlRootHost.GetHostFromWindow(window) ?? throw new InvalidOperationException($"Couldn't find a {nameof(WaylandXamlRootHost)} associated with the {nameof(WaylandDragDropExtension)}");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(((IXamlRootHost)_host).RootElement!.XamlRoot);

		_host.SetDragDropExtension(this);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Wayland drop-target support attached to the window.");
		}
	}

	// All Process* methods run on the UI thread (queued by the clipboard's data-device relay).

	internal void ProcessDragEnter(WlDataOffer? offer, string[] mimes, uint serial, Point position)
	{
		if (offer == null)
		{
			return;
		}

		// A stray previous session (no leave received) is aborted first.
		if (_offer != null)
		{
			ProcessDragLeave();
		}

		_offer = offer;
		_mimes = mimes;
		_enterSerial = serial;
		_dropped = false;
		_lastPosition = position;

		// Cache the payload now, like the X11 head: the offer dies with the session, so
		// lazily-fetched data would be gone by the time a handler reads it after the drop.
		var package = new DataPackage();
		if (WaylandConnection.Instance is { } connection)
		{
			WaylandClipboardExtension.Instance.FillDataPackageFromOffer(package, offer, mimes, connection);
		}

		// Accept + announce copy up-front so the compositor/source show a droppable state;
		// per-position feedback below narrows it when the XAML target refuses.
		offer.Accept(_enterSerial, mimes.FirstOrDefault());
		_accepted = true;
		if (offer.Version >= 3)
		{
			offer.SetActions(WlDataDeviceManager.DndActionEnum.Copy, WlDataDeviceManager.DndActionEnum.Copy);
		}
		WaylandConnection.Instance?.Flush();

		var info = new CoreDragInfo(new DragEventSource(position), package.GetView(), DataPackageOperation.Copy);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Wayland DnD enter at {position} with formats [{string.Join(", ", mimes)}].");
		}

		_coreDragDropManager.DragStarted(info);
	}

	internal void ProcessDragMotion(Point position)
	{
		if (_offer is not { } offer)
		{
			return;
		}

		_lastPosition = position;
		var acceptedOperations = _coreDragDropManager.ProcessMoved(new DragEventSource(position));

		// wl_data_offer.accept is sticky; only send changes.
		var accept = acceptedOperations != DataPackageOperation.None;
		if (accept != _accepted)
		{
			offer.Accept(_enterSerial, accept ? _mimes.FirstOrDefault() : null);
			_accepted = accept;
			WaylandConnection.Instance?.Flush();
		}
	}

	internal void ProcessDragLeave()
	{
		if (_offer is not { } offer)
		{
			return;
		}

		// The compositor also sends leave to close out a completed drop; that session was
		// already finished in ProcessDrop and must not be aborted.
		if (!_dropped)
		{
			_coreDragDropManager.ProcessAborted(_fakePointerId);
		}

		DestroySessionOffer(offer);
		_offer = null;
	}

	internal void ProcessDrop()
	{
		if (_offer is not { } offer)
		{
			return;
		}

		_dropped = true;
		var acceptedOperation = _coreDragDropManager.ProcessDropped(new DragEventSource(_lastPosition));

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Wayland DnD drop at {_lastPosition}: acceptedOperation={acceptedOperation}.");
		}

		// The payload was cached at enter, so the transfer can be finished immediately.
		if (offer.Version >= 3 && acceptedOperation != DataPackageOperation.None)
		{
			offer.Finish();
		}

		DestroySessionOffer(offer);
		_offer = null;
		WaylandConnection.Instance?.Flush();
	}

	private static void DestroySessionOffer(WlDataOffer offer)
	{
		WaylandClipboardExtension.Instance.ForgetOffer(offer);
		offer.Destroy();
	}

	// Parity with the X11 head, whose StartNativeDrag also throws: drag-source (dragging
	// from this app to another) is out of scope for drop-target parity.
	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted) => throw new NotImplementedException();

	private readonly struct DragEventSource(Point location) : IDragEventSource
	{
		private static long _nextFrameId;
		private readonly Point _location = location;

		public long Id => _fakePointerId;

		public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

		/// <inheritdoc />
		public (Point location, DragDropModifiers modifier) GetState() => (_location, DragDropModifiers.None);

		/// <inheritdoc />
		public Point GetPosition(object? relativeTo)
		{
			if (relativeTo is null)
			{
				return _location;
			}

			if (relativeTo is UIElement elt)
			{
				var eltToRoot = UIElement.GetTransform(elt, null);
				var rootToElt = eltToRoot.Inverse();

				return rootToElt.Transform(_location);
			}

			throw new InvalidOperationException("The relative to must be a UIElement.");
		}
	}
}
