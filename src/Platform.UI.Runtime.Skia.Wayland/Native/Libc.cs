using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// The few libc entry points the Wayland head needs directly: anonymous shared-memory
/// files for wl_shm buffer pools, and poll() for the display event pump.
/// </summary>
internal static partial class Libc
{
	private const string LibcName = "libc";

	internal const uint MFD_CLOEXEC = 0x0001;

	internal const int PROT_READ = 0x1;
	internal const int PROT_WRITE = 0x2;
	internal const int MAP_SHARED = 0x01;

	internal const short POLLIN = 0x001;
	internal const short POLLERR = 0x008;
	internal const short POLLHUP = 0x010;

	internal static readonly IntPtr MAP_FAILED = new(-1);

	[LibraryImport(LibcName, SetLastError = true, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
	internal static partial int memfd_create(string name, uint flags);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial int ftruncate(int fd, long length);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial IntPtr mmap(IntPtr addr, UIntPtr length, int prot, int flags, int fd, long offset);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial int munmap(IntPtr addr, UIntPtr length);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial int close(int fd);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial int poll(ref PollFd fds, uint nfds, int timeout);

	internal const int O_CLOEXEC = 0x80000;

	// int pipe2(int pipefd[2], int flags) — pipefd[0] read end, pipefd[1] write end.
	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial int pipe2(Span<int> pipefd, int flags);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial nint read(int fd, Span<byte> buffer, nuint count);

	[LibraryImport(LibcName, SetLastError = true)]
	internal static partial nint write(int fd, ReadOnlySpan<byte> buffer, nuint count);

	[StructLayout(LayoutKind.Sequential)]
	internal struct PollFd
	{
		public int fd;
		public short events;
		public short revents;
	}
}
