#if CODEBRIX_REFERENCE_API
using System;
namespace CodeBrix.Platform.UI.NativeElementHosting; //Was previously: Uno.UI.NativeElementHosting

public sealed class X11NativeWindow
{
	public IntPtr WindowId { get; }

	public X11NativeWindow(IntPtr windowId)
	{
		WindowId = windowId;
	}
}
#endif
