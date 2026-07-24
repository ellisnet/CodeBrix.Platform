using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.Runtime.Skia.Native; //Was previously: Uno.UI.Runtime.Skia.Native

internal static class Libc
{
	public const int O_RDWR = 0x0002;

	[DllImport("libc", SetLastError = true)]
	public static extern int open(string pathname, int flags, int mode);

	[DllImport("libc", SetLastError = true)]
	public static extern int close(int fd);

	[DllImport("libc", CharSet = CharSet.Ansi)]
	public static extern IntPtr strerror(int errno);

	/// <summary>
	/// Terminates the process immediately — no finalizers, no process-exit
	/// handlers, no crash dump. This is the faithful primitive for the emulated
	/// device losing power: a kiosk whose plug is pulled never gets to run
	/// graceful cleanup, so neither does this head when its transport socket
	/// reaches end-of-file.
	/// </summary>
	[DllImport("libc", EntryPoint = "_exit")]
	public static extern void ExitImmediately(int status);
}
