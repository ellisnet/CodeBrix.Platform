using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.OpenGL;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using PlayTestDemo.Views;
using SilverAssertions;
using Xunit;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace PlayTestDemo.NoOpenGL.PlayTests;

public sealed class NoOpenGLFixture : IAsyncLifetime
{
    public PlayTestApplication Application { get; private set; }
    public MainPage View { get; private set; }

    public async ValueTask InitializeAsync()
    {
        App.InitializeLogging();
        CodeBrixPlayTestOpenGL.Register();
        Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
        {
            ConfigurationAssembly = typeof(NoOpenGLFixture).Assembly,
            OpenGL = PlayTestOpenGL.Unavailable,
        });
    }

    public Task ResetAsync(ScreenOrientation? orientation) =>
        Application.Page.SetContentAsync(() => View = new MainPage(), orientation);

    public async ValueTask DisposeAsync()
    {
        if (Application != null) await Application.DisposeAsync();
    }
}

public sealed class NoOpenGLTests(NoOpenGLFixture fixture) : PageTest(fixture.Application), IClassFixture<NoOpenGLFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var test = (Xunit.v3.IXunitTest)TestContext.Current.Test;
        test.Traits.TryGetValue(PlayTestOrientationAttribute.CaseTraitName, out var orientations);
        await fixture.ResetAsync(PlayTestOrientationAttribute.Resolve(test.TestMethod.Method, orientations));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Without_OpenGL_the_page_shows_its_fallback()
    {
        var info = fixture.Application.OpenGL;
        info.IsAvailable.Should().BeFalse();
        info.UnavailableReason.Should().Contain("Unavailable");
        info.Renderer.Should().BeNull();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Try OpenGL", Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("GLFallback")).ToHaveTextAsync("3D preview unavailable on this machine");
        await Expect(Page.GetByTestId("GLStatus")).ToContainTextAsync("OpenGL unavailable");
        await Expect(Page.GetByTestId("SkiaGLStatus")).ToHaveTextAsync("GPU Skia unavailable");
        var view = await Page.EvaluateAsync(() => (OpenGLView)fixture.View.Content);
        (await Page.EvaluateAsync(() => view.Square.GetGLInitializationState().Status)).Should().Be(GLInitializationStatus.InitializationFailed);
        (await Page.EvaluateAsync(() => view.Circle.IsGpuInitialized)).Should().Be(false);
    }
}
