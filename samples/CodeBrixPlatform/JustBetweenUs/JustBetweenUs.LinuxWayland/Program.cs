using System;
using CodeBrix.Platform.UI.Hosting;

// The NATIVE WAYLAND head of the JustBetweenUs sample. It requires a running Wayland
// compositor and never falls back to X11/XWayland; launched from an X11 session it
// prints a clean "This application requires a Wayland compositor." message and exits
// non-zero. Use the JustBetweenUs.Linux (X11) head for X11/XWayland environments.

namespace JustBetweenUs;

internal class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		App.InitializeLogging();

		var host = CodeBrixPlatformHostBuilder.Create()
			.App(() => new App())
			.UseLinuxWayland()
			.Build();

		host.Run();
	}
}
