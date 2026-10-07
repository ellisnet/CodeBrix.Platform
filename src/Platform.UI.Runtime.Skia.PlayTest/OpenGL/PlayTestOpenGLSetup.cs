using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Graphics;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest;

internal enum PlayTestOpenGLMode
{
    // PlayTestOptions.OpenGL is Unavailable: nothing is registered, as before OpenGL support existed.
    OptedOut,
    // A provider is registered and the launch offers OpenGL.
    Provider,
    // No provider is registered: the first OpenGL element the application initializes fails the test.
    NotRegistered,
}

internal sealed class PlayTestOpenGLPlan
{
    internal PlayTestOpenGLPlan(PlayTestOpenGLMode mode, IPlayTestOpenGLProvider? provider)
    {
        Mode = mode;
        Provider = provider;
    }

    internal PlayTestOpenGLMode Mode { get; }
    internal IPlayTestOpenGLProvider? Provider { get; }
}

// The launch-time OpenGL decisions, kept apart from the host so the head's tests can drive them with
// their own registry: no second process and no skip is needed to reach the "not registered" path.
internal static class PlayTestOpenGLSetup
{
    internal const string PackageId = "CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever";
    private const int GL_VERSION = 0x1F02;
    private const int GL_RENDERER = 0x1F01;

    internal static PlayTestOpenGLPlan Plan(PlayTestOpenGL option, PlayTestOpenGLProviderRegistry registry)
    {
        if (!Enum.IsDefined(option)) throw new ArgumentOutOfRangeException(nameof(option));
        if (option == PlayTestOpenGL.Unavailable) return new(PlayTestOpenGLMode.OptedOut, null);
        return registry.Provider is { } provider
            ? new(PlayTestOpenGLMode.Provider, provider)
            : new(PlayTestOpenGLMode.NotRegistered, null);
    }

    internal static PlayTestException NotRegisteredError() => new(
        "The application initialized an OpenGL element, but no OpenGL provider is registered - reference "
        + PackageId + " and call CodeBrixPlayTestOpenGL.Register() before PlayTestApplication.LaunchAsync, "
        + "or launch with PlayTestOptions.OpenGL = PlayTestOpenGL.Unavailable to test the application without OpenGL.");

    // Runs on the UI thread before the application starts. A registered provider must produce a working
    // context now, so a machine without OpenGL fails the launch with the provider's own reason.
    internal static PlayTestOpenGLInfo Probe(PlayTestOpenGLPlan plan)
    {
        switch (plan.Mode)
        {
            case PlayTestOpenGLMode.OptedOut:
                return new(null, null, null, false, "PlayTestOptions.OpenGL is Unavailable for this launch.");
            case PlayTestOpenGLMode.NotRegistered:
                return new(null, null, null, false,
                    "No OpenGL provider is registered - reference " + PackageId + " and call CodeBrixPlayTestOpenGL.Register().");
        }
        var provider = plan.Provider!;
        IPlayTestOpenGLContext context;
        try { context = provider.CreateContext(); }
        catch (Exception e) { throw ProviderError(provider, e); }
        using (context)
        {
            string? renderer, version;
            try
            {
                using (context.MakeCurrent())
                {
                    renderer = GetString(context, GL_RENDERER);
                    version = GetString(context, GL_VERSION);
                }
            }
            catch (Exception e) { throw ProviderError(provider, e); }
            return new(provider.Name, renderer, version, context.IsGles, null);
        }
    }

    // The INativeOpenGLWrapper builder the host registers for every XamlRoot; null registers nothing.
    internal static Func<XamlRoot, object>? WrapperFactory(PlayTestOpenGLPlan plan, Action<PlayTestException> notRegistered)
    {
        switch (plan.Mode)
        {
            case PlayTestOpenGLMode.Provider:
                var provider = plan.Provider!;
                return _ => new PlayTestNativeOpenGLWrapper(provider.CreateContext());
            case PlayTestOpenGLMode.NotRegistered:
                // The OpenGL elements catch every failure and report "initialization failed", so the
                // exception thrown here never reaches the test on its own. notRegistered records it as the
                // run's failure, which every following PlayTest call reports.
                return _ =>
                {
                    var error = NotRegisteredError();
                    notRegistered(error);
                    throw error;
                };
            default:
                return null;
        }
    }

    private static PlayTestException ProviderError(IPlayTestOpenGLProvider provider, Exception e) => new(
        $"The OpenGL provider \"{provider.Name}\" could not create a context on {RuntimeInformation.OSDescription}: {e.Message}", e);

    private static unsafe string? GetString(IPlayTestOpenGLContext context, int name)
    {
        var address = context.GetProcAddress("glGetString");
        if (address == 0) return null;
        var value = ((delegate* unmanaged<int, byte*>)address)(name);
        return value == null ? null : Marshal.PtrToStringUTF8((nint)value);
    }
}

// The head's INativeOpenGLWrapper: one provider context per wrapper, with its declared flavour.
internal sealed class PlayTestNativeOpenGLWrapper : INativeOpenGLWrapper
{
    private readonly IPlayTestOpenGLContext _context;

    internal PlayTestNativeOpenGLWrapper(IPlayTestOpenGLContext context) => _context = context;

    public bool? UsesGles => _context.IsGles;

    public nint GetProcAddress(string proc) => TryGetProcAddress(proc, out var address)
        ? address
        : throw new InvalidOperationException($"The OpenGL implementation has no entry point named {proc}.");

    public bool TryGetProcAddress(string proc, out nint addr)
    {
        addr = _context.GetProcAddress(proc);
        return addr != 0;
    }

    public IDisposable MakeCurrent() => _context.MakeCurrent();

    public void Dispose() => _context.Dispose();
}
