#nullable enable

using System;
using CodeBrix.Platform.Foundation;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.Core;

public partial class CoreWindow
{
	internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyDownReceived;
	internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyUpReceived;

	internal void RaiseNativeKeyDownReceived(KeyEventArgs args)
	{
		NativeKeyDownReceived?.Invoke(this, args);
		KeyDown?.Invoke(this, args);
	}
	internal void RaiseNativeKeyUpReceived(KeyEventArgs args)
	{
		NativeKeyUpReceived?.Invoke(this, args);
		KeyUp?.Invoke(this, args);
	}
}
