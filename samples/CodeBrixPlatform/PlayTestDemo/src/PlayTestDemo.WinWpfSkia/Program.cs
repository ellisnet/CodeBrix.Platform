using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf;

namespace PlayTestDemo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();
        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWindowsWpf()
            .Build();

        if (host is WpfHost wpfHost) wpfHost.RenderSurfaceType = RenderSurfaceType.Software;

        host.Run();
    }
}
