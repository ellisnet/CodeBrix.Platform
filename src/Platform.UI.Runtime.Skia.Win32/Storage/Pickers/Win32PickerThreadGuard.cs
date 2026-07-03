using System;
using System.Threading;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32.Storage.Pickers;

/// <summary>
/// Guards the Win32 file/folder pickers against being invoked from the wrong COM apartment.
/// </summary>
/// <remarks>
/// The pickers show the shell Common Item Dialog (IFileDialog.Show), which MUST be called from a
/// single-threaded-apartment (STA) UI thread. On an MTA thread Show() blocks without ever running
/// its modal loop, so the application appears to hang with no dialog and has to be force-killed.
/// We check the apartment up-front and throw an actionable exception instead of hanging.
/// </remarks>
internal static class Win32PickerThreadGuard
{
	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> with actionable guidance when the calling
	/// thread is not an STA. No-op on non-Windows platforms (this runtime is Windows-only, but the
	/// OS guard keeps the check from ever affecting other heads).
	/// </summary>
	/// <param name="dialogDescription">
	/// Human-readable name of the dialog being shown, woven into the message (e.g. "file/folder
	/// picker dialog" or "file save dialog").
	/// </param>
	internal static void EnsureStaThread(string dialogDescription)
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			return;
		}

		throw new InvalidOperationException(
			$"The {dialogDescription} could not be shown because the host's UI thread is a " +
			$"{Thread.CurrentThread.GetApartmentState()} apartment, but the Windows file dialog " +
			"requires a single-threaded apartment (STA). A console application's Main runs as MTA " +
			"by default, so BOTH of the following are required in your app's entry point:" + Environment.NewLine +
			"  1. Decorate Main with the [STAThread] attribute (this alone is easy to miss - " +
			"without it the thread is MTA even for a synchronous Main)." + Environment.NewLine +
			"  2. Use a synchronous 'static void Main' that calls 'host.Run()' - NOT an " +
			"'async Task Main' with 'await host.RunAsync()', because [STAThread] is silently " +
			"ignored on an async Main and the thread reverts to MTA." + Environment.NewLine +
			"Example:" + Environment.NewLine +
			"    [STAThread]" + Environment.NewLine +
			"    public static void Main(string[] args)" + Environment.NewLine +
			"    {" + Environment.NewLine +
			"        var host = CodeBrixPlatformHostBuilder.Create().App(() => new App()).UseWindowsWin32().Build();" + Environment.NewLine +
			"        host.Run();" + Environment.NewLine +
			"    }");
	}
}
