#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.Devices.Input
{
	public partial class MouseDevice
	{
		private static readonly object _gate = new();
		private static MouseDevice? _instance;

		private TypedEventHandler<MouseDevice, MouseEventArgs>? _mouseMoved;
		private ICodeBrixRelativePointerSource? _activeSource;

		public static MouseDevice GetForCurrentView()
		{
			lock (_gate)
			{
				return _instance ??= new MouseDevice();
			}
		}

		/// <summary>
		/// Occurs when the mouse pointer is moved, reporting relative (delta) motion.
		/// </summary>
		/// <remarks>
		/// While at least one handler is attached, the pointer is confined to the window and
		/// raw motion deltas are delivered (a "relative mouse session"); when the last handler
		/// is removed, the session ends and the default pointer behavior is restored. Events
		/// are raised on the UI thread of the window. Heads that cannot deliver relative
		/// motion simply never raise the event.
		/// </remarks>
		public event TypedEventHandler<MouseDevice, MouseEventArgs> MouseMoved
		{
			add
			{
				lock (_gate)
				{
					var hadSubscribers = _mouseMoved is not null;
					_mouseMoved += value;
					if (!hadSubscribers)
					{
						TryActivate();
					}
				}
			}
			remove
			{
				lock (_gate)
				{
					_mouseMoved -= value;
					if (_mouseMoved is null)
					{
						Deactivate();
					}
				}
			}
		}

		/// <summary>
		/// Called when a window's pointer input source is registered, so a subscription made
		/// before the source existed activates as soon as one becomes available.
		/// </summary>
		internal static void NotifyPointerInputSourceChanged()
		{
			lock (_gate)
			{
				if (_instance is { } instance && instance._mouseMoved is not null)
				{
					instance.TryActivate();
				}
			}
		}

		internal void RaiseMouseMoved(int deltaX, int deltaY)
			=> _mouseMoved?.Invoke(this, new MouseEventArgs(new MouseDelta { X = deltaX, Y = deltaY }));

		private void TryActivate()
		{
			if (CoreWindow.GetForCurrentThreadSafe()?.PointersSource is not ICodeBrixRelativePointerSource source
				|| ReferenceEquals(source, _activeSource))
			{
				return;
			}

			_activeSource?.StopRelativeMouse();
			_activeSource = source;
			source.StartRelativeMouse(this);
		}

		private void Deactivate()
		{
			_activeSource?.StopRelativeMouse();
			_activeSource = null;
		}
	}
}
