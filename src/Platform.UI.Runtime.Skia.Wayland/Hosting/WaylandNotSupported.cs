using System;
using System.Collections.Generic;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// One-time Warning-level notices for APIs that are permanent no-ops on Wayland (protocol
/// gaps, not missing implementation). One warning per API per process — loud enough that a
/// developer testing on X11 learns the difference the first time the code path runs on a
/// Wayland session, without flooding the logs.
/// </summary>
internal static class WaylandNotSupported
{
	private static readonly object _gate = new();
	private static readonly HashSet<string> _warned = new();

	/// <param name="sourceType">The type reported as the log source.</param>
	/// <param name="api">The user-facing API name(s), e.g. "AppWindow.Move".</param>
	/// <param name="reason">Why the API cannot work on Wayland.</param>
	internal static void WarnOnce(Type sourceType, string api, string reason)
	{
		lock (_gate)
		{
			if (!_warned.Add(api))
			{
				return;
			}
		}

		if (sourceType.Log().IsEnabled(LogLevel.Warning))
		{
			sourceType.Log().Warn(
				$"{api} has no effect on Wayland: {reason} " +
				"This is a permanent platform difference, not a bug — see the " +
				"\"Permanent Wayland differences\" section in AGENT-README.txt. " +
				"(This warning is shown once per process.)");
		}
	}
}
