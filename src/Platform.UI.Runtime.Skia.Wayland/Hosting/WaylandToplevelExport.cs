//CodeBrix warning-cleanup 2026-07-10: synchronous call retained deliberately (hosting/event-loop, disposal, or build-time tooling where sync execution is intended); CA1849 suppressed rather than changing async timing.
#pragma warning disable CA1849
using System;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.XdgForeignUnstableV2;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// A toplevel surface handle exported through xdg-foreign-unstable-v2, so another process
/// can reference our window — e.g. the FileChooser portal's parent_window option takes
/// "wayland:HANDLE". Dispose to revoke the handle (zxdg_exported_v2.destroy) once the
/// foreign relationship is no longer needed; for a portal dialog that is after the
/// dialog's response arrives, not before (destroying invalidates the handle).
/// </summary>
internal sealed class WaylandToplevelExport : IDisposable
{
	private readonly WaylandConnection _connection;
	private readonly ZxdgExportedV2 _exported;
	private bool _disposed;

	/// <summary>The compositor-issued handle, without the "wayland:" prefix.</summary>
	public string Handle { get; }

	private WaylandToplevelExport(WaylandConnection connection, ZxdgExportedV2 exported, string handle)
	{
		_connection = connection;
		_exported = exported;
		Handle = handle;
	}

	/// <summary>
	/// Exports the toplevel surface of <paramref name="host"/>'s window and waits for the
	/// compositor to issue its handle. Returns null when the compositor does not advertise
	/// zxdg_exporter_v2, the window has no surface yet, or the handle does not arrive
	/// within <paramref name="timeout"/> — callers then proceed without a parent handle.
	/// </summary>
	public static async Task<WaylandToplevelExport?> TryExportAsync(WaylandXamlRootHost host, TimeSpan timeout)
	{
		if (host.Connection is not { } connection || connection.Exporter is not { } exporter)
		{
			if (host.Log().IsEnabled(LogLevel.Debug))
			{
				host.Log().Debug("The compositor does not advertise zxdg_exporter_v2; toplevel handles cannot be exported.");
			}
			return null;
		}

		if (host.ShellSurface?.Surface is not { } surface)
		{
			return null;
		}

		// The handle event arrives on the Wayland event-pump thread (inside a dispatch that
		// holds the libdecor gate); RunContinuationsAsynchronously keeps awaiting callers
		// from resuming inline on the pump.
		var handleTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var exported = exporter.ExportToplevel(surface, new ZxdgExportedV2.Listener.Relay
		{
			OnHandle = (_, handle) => handleTcs.TrySetResult(handle),
		});
		connection.Flush();

		var timeoutTask = Task.Delay(timeout);
		var finished = await Task.WhenAny(handleTcs.Task, timeoutTask);
		if (finished == timeoutTask)
		{
			if (host.Log().IsEnabled(LogLevel.Error))
			{
				host.Log().Error($"Timed out waiting for the zxdg_exported_v2 handle after {timeout.TotalMilliseconds:F0} ms.");
			}

			exported.Destroy();
			connection.Flush();
			return null;
		}

		return new WaylandToplevelExport(connection, exported, handleTcs.Task.Result);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		_exported.Destroy();
		_connection.Flush();
	}
}
