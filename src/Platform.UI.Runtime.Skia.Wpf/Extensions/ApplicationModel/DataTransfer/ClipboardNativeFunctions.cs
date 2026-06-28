using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.Extensions.ApplicationModel.DataTransfer
{
	internal static class ClipboardNativeFunctions
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AddClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
	}
}
