using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.WebGpu;
using WebGpuDemo.Views;
using Xunit;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace WebGpuDemo.PlayTests;

public sealed class AppFixture : IAsyncLifetime
{
    public PlayTestApplication Application { get; private set; }
    public MainPage View { get; private set; }

    public async ValueTask InitializeAsync()
    {
        App.InitializeLogging();
        // The demo's pages are WebGpuRenderViews: give them real WebGPU devices. The launch proves an adapter and a
        // device exist here and fails with the engine's reason when they do not.
        CodeBrixPlayTestWebGpu.Register();
        Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
        {
            ConfigurationAssembly = typeof(AppFixture).Assembly,
        });
    }

    public async Task ResetAsync(ScreenOrientation? orientation) =>
        await Application.Page.SetContentAsync(() => View = new MainPage(), orientation);

    public async ValueTask DisposeAsync()
    {
        if (Application != null) await Application.DisposeAsync();
    }
}
