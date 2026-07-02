using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

/// <summary>
/// Sonames of the system libraries this AddIn binds. Nothing is bundled in the package;
/// the WPE WebKit engine comes from the distro (see <see cref="AptInstallHint"/>).
/// </summary>
internal static class NativeLibraries
{
	public const string LibWpe = "libwpe-1.0.so.1";
	public const string WpeBackendFdo = "libWPEBackend-fdo-1.0.so.1";
	public const string WpeWebKit = "libWPEWebKit-2.0.so.1";
	public const string WaylandServer = "libwayland-server.so.0";
	public const string GLib = "libglib-2.0.so.0";
	public const string GObject = "libgobject-2.0.so.0";

	public const string AptInstallHint = "sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1";

	/// <summary>
	/// Probes each required engine library individually so a missing one can be named precisely.
	/// Returns null when everything is present, otherwise the exception the WebView should throw.
	/// </summary>
	public static Exception? Probe()
	{
		(string soname, string package)[] required =
		[
			(LibWpe, "libwpe-1.0-1"),
			(WpeBackendFdo, "libwpebackend-fdo-1.0-1"),
			(WpeWebKit, "libwpewebkit-2.0-1"),
		];

		foreach (var (soname, package) in required)
		{
			if (!NativeLibrary.TryLoad(soname, out _))
			{
				return new PlatformNotSupportedException(
					$"WebView on Linux requires the system WPE WebKit engine, and the library '{soname}' (Debian package '{package}') was not found. " +
					$"To install everything needed on Debian-based distros, run: {AptInstallHint}");
			}
		}

		return null;
	}
}
