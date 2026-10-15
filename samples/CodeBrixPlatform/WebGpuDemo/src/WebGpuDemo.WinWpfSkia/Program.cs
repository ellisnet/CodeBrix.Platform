using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf;

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
            .UseWindowsWpf()
            .Build();

        if (host is WpfHost wpfHost) wpfHost.RenderSurfaceType = RenderSurfaceType.Software;

        host.Run();
    }
}
