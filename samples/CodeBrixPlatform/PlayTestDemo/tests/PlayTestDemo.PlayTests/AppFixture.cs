using System;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.OpenGL;
using PlayTestDemo.ViewModels;
using PlayTestDemo.Views;
using Xunit;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace PlayTestDemo.PlayTests;

public sealed class AppFixture : IAsyncLifetime
{
    public PlayTestApplication Application { get; private set; }
    public MainPage View { get; private set; }
    public MainViewModel Model => View.ViewModel;
    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "CodeBrix.PlayTestDemo", Guid.NewGuid().ToString("N"));

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(DataDirectory);
        App.InitializeLogging();
        // The demo has an OpenGL page: give its elements real contexts.
        CodeBrixPlayTestOpenGL.Register();
        Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
        {
            ConfigurationAssembly = typeof(AppFixture).Assembly,
        });
    }

    public async Task ResetAsync(ScreenOrientation? orientation)
    {
        Application.FilePickers.Clear();
        Application.Launcher.Clear();
        await Application.Page.SetContentAsync(() => View = new MainPage(), orientation);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Application != null) await Application.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
        }
    }
}
