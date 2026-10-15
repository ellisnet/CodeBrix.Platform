using System;
using CodeBrix.Platform.UI.Hosting;

namespace WebGpuDemo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();
        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            // Opt in to direct GPU rendering (EXPERIMENTAL, app-wide, one-way): the WebGpuRenderView then uses the head's own GPU device where the head offers one.
            // .UseDirectGpuRenderingMode()
            .UseLinuxWayland()
            .Build();

        host.Run();
    }
}
