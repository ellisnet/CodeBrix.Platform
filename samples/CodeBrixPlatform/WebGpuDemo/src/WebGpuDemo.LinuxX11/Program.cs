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
            .UseLinuxX11()
            // Or choose the X11 head's Vulkan renderer explicitly, in place of .UseLinuxX11() (the line above already prefers it when no renderer is chosen):
            // .UseLinuxX11(x11 => x11.RenderingBackend(X11RenderingBackend.Vulkan))
            .Build();

        host.Run();
    }
}
