#if CODEBRIX_REFERENCE_API
using System;
namespace CodeBrix.Platform.UI.NativeElementHosting; //Was previously: Uno.UI.NativeElementHosting

public sealed class Win32NativeWindow
{
	public IntPtr Hwnd { get; }

	public Win32NativeWindow(IntPtr hwnd)
	{
		Hwnd = hwnd;
	}
}
#endif
