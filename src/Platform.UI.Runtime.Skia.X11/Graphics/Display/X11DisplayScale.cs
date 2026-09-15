using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;
using CodeBrix.Platform.Extensions.Disposables;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

/// <summary>
/// The X11 head's two sources of a display scale - the <c>CODEBRIX_DISPLAY_SCALE_OVERRIDE</c>
/// environment variable and the <c>Xft.dpi</c> X resource - read in one place so that
/// <see cref="X11DisplayInformationExtension"/> (which reports the scale to the framework once a
/// window exists) and <see cref="X11XamlRootHost"/> (which needs one BEFORE the first window exists,
/// to size it) cannot drift apart.
/// </summary>
internal static class X11DisplayScale
{
	private const string EnvironmentCodeBrixDisplayScaleOverride = "CODEBRIX_DISPLAY_SCALE_OVERRIDE";
	private const string XftDotdpi = "Xft.dpi";

	/// <summary>
	/// Reads the <c>CODEBRIX_DISPLAY_SCALE_OVERRIDE</c> environment variable, which forces the scale
	/// for the whole process regardless of what the X server reports.
	/// </summary>
	/// <param name="scale">The override, when one is set and parses.</param>
	/// <returns>True when an override is in effect.</returns>
	internal static bool TryGetScaleOverride([NotNullWhen(true)] out double? scale)
	{
		if (double.TryParse(
			Environment.GetEnvironmentVariable(EnvironmentCodeBrixDisplayScaleOverride),
			NumberStyles.Any,
			CultureInfo.InvariantCulture,
			out var environmentScaleOverride))
		{
			scale = environmentScaleOverride;
			return true;
		}

		scale = null;
		return false;
	}

	/// <summary>
	/// Reads the <c>Xft.dpi</c> X resource - what a desktop environment writes when the user picks a
	/// scaling factor - and expresses it as a multiple of <see cref="DisplayInformation.BaseDpi"/>.
	/// </summary>
	/// <param name="scaling">The scale implied by the resource, when it is present and parses.</param>
	/// <returns>True when the resource was found.</returns>
	internal static bool TryGetXResourceScale([NotNullWhen(true)] out double? scaling)
		=> TryGetXResource(XftDotdpi, out scaling);

	/// <summary>
	/// The scale to size a window with before the window - and therefore the display it lands on -
	/// exists. This is the process-wide answer: the environment override if one is set, otherwise the
	/// <c>Xft.dpi</c> resource, otherwise 1. It is deliberately the same chain
	/// <see cref="X11DisplayInformationExtension"/> uses on the XRandR path, so the size a window is
	/// created at agrees with the scale the framework goes on to lay it out with.
	/// </summary>
	/// <returns>Raw pixels per effective pixel.</returns>
	internal static double GetLaunchScale()
	{
		if (TryGetScaleOverride(out var scaleOverride))
		{
			return scaleOverride.Value;
		}

		return TryGetXResourceScale(out var xrdbScaling) ? xrdbScaling.Value : 1;
	}

	private static bool TryGetXResource(string resourceName, [NotNullWhen(true)] out double? scaling)
	{
		// For some reason, querying the resources with a preexisting display yields outdated values, so
		// we have to open a new display here.
		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
		using var displayDisposable = new DisposableStruct<IntPtr>(static d => { _ = XLib.XCloseDisplay(d); }, display);
		var xdefs = X11Helper.XResourceManagerString(display);
		if (xdefs != IntPtr.Zero)
		{
			IntPtr xrdb = X11Helper.XrmGetStringDatabase(xdefs);
			using var databaseDisposable = new DisposableStruct<IntPtr>(X11Helper.XrmDestroyDatabase, xrdb);
			var resourceNamePtr = Marshal.StringToHGlobalAnsi(resourceName);
			using var resourceNameDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, resourceNamePtr);
			var found = X11Helper.XrmGetResource(xrdb, resourceNamePtr, resourceNamePtr, out _, out X11Helper.XrmValue value);
			// don't free value.addr. It's managed by the X server.
			if (found && value.addr != IntPtr.Zero)
			{
				if (Marshal.PtrToStringAnsi(value.addr, (int)value.size) is { } str &&
					int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				{
					scaling = result / DisplayInformation.BaseDpi;
					return true;
				}
			}
		}

		scaling = null;
		return false;
	}
}
