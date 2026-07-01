using System;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Thrown when this native-Wayland-only runtime host cannot connect to a Wayland
/// compositor (wl_display_connect returned NULL). This head never falls back to
/// X11/XWayland; applications that must run in X11 environments should reference the
/// X11 head package (CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever) instead.
/// </summary>
public sealed class WaylandCompositorMissingException : Exception
{
	internal const string UserFacingMessage =
		"This application requires a Wayland compositor.\n" +
		"No Wayland display could be reached (wl_display_connect failed). If you are in an " +
		"X11 session, log into a Wayland session and try again.";

	internal const string DeveloperHint =
		"Developer hint: this application uses the native-Wayland-only CodeBrix.Platform head " +
		"(CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever). For X11/XWayland " +
		"environments, build against the X11 head (CodeBrix.Platform.Runtime.Skia.X11." +
		"ApacheLicenseForever) instead.";

	internal WaylandCompositorMissingException(Exception? inner)
		: base(UserFacingMessage, inner)
	{
	}
}
