using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

#if HAS_CODEBRIX_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private static long _pseudoNextFrameId;
		private readonly uint _pseudoFrameId = (uint)Interlocked.Increment(ref _pseudoNextFrameId);
		private readonly ulong _pseudoTimestamp = (ulong)DateTime.UtcNow.Ticks;
		private readonly Point _point;

		public PointerRoutedEventArgs(Point point) : this()
		{
			_point = point;

			FrameId = _pseudoFrameId;
		}

		/// <summary>
		/// Initializes a new instance carrying a modifier mask, the way the backends that build these
		/// arguments from a system pointer event do.
		/// </summary>
		/// <param name="point">The pointer's position.</param>
		/// <param name="keyModifiers">The modifier keys the system reports as held.</param>
		internal PointerRoutedEventArgs(Point point, global::Windows.System.VirtualKeyModifiers keyModifiers) : this(point)
		{
			KeyModifiers = keyModifiers;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = global::Windows.Devices.Input.PointerDevice.For(PointerDeviceType.Mouse);
			var translation = relativeTo.TransformToVisual(null) as TranslateTransform;
			var offset = new Point(_point.X - translation.X, _point.Y - translation.Y);
			var properties = new PointerPointProperties() { IsInRange = true, IsPrimary = true };

			return new PointerPoint(FrameId, _pseudoTimestamp, device, 0, offset, offset, true, properties);
		}
	}
}
