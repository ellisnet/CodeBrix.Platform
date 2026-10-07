using System;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Whether OpenGL is offered to the application under test (<see cref="PlayTestOptions.OpenGL"/>).</summary>
public enum PlayTestOpenGL
{
    /// <summary>The default: OpenGL elements get a real context from the registered provider
    /// (<c>CodeBrixPlayTestOpenGL.Register()</c> from CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever).
    /// With no provider registered, the first OpenGL element the application initializes fails the test.</summary>
    Available,
    /// <summary>No OpenGL for this launch: OpenGL elements report that initialization failed and the
    /// application's own fallback runs, whether or not a provider is registered.</summary>
    Unavailable,
}

/// <summary>A source of off-screen OpenGL contexts for the PlayTest head. One provider is registered per
/// process with <see cref="PlayTestOpenGLProviders.Register"/>; the head disposes it when the application
/// is disposed.</summary>
public interface IPlayTestOpenGLProvider : IDisposable
{
    /// <summary>A short description of the provider and its mechanism, reported by
    /// <see cref="PlayTestOpenGLInfo.Provider"/> and in failure messages.</summary>
    string Name { get; }

    /// <summary>Creates a new off-screen context. Called on the application's UI thread, which is the thread
    /// every later <see cref="IPlayTestOpenGLContext.MakeCurrent"/> runs on.</summary>
    /// <returns>A context that is not current on any thread.</returns>
    /// <exception cref="Exception">No context can be created; the message states the concrete reason.</exception>
    IPlayTestOpenGLContext CreateContext();
}

/// <summary>One off-screen OpenGL context created by an <see cref="IPlayTestOpenGLProvider"/>.</summary>
public interface IPlayTestOpenGLContext : IDisposable
{
    /// <summary>True when the context speaks OpenGL ES; false for desktop OpenGL.</summary>
    bool IsGles { get; }

    /// <summary>Returns the address of a GL (or platform) entry point, or zero when the implementation does
    /// not provide it. Never throws: callers probe names that legitimately do not exist.</summary>
    /// <param name="name">The entry point's name, for example <c>glGetString</c>.</param>
    /// <returns>The address, or zero.</returns>
    nint GetProcAddress(string name);

    /// <summary>Makes the context current on the calling thread.</summary>
    /// <returns>A scope whose disposal restores the context (or the absence of one) that was current before.</returns>
    IDisposable MakeCurrent();
}

/// <summary>The process-wide registry of the PlayTest head's OpenGL provider.</summary>
public static class PlayTestOpenGLProviders
{
    internal static PlayTestOpenGLProviderRegistry Registry { get; } = new();

    /// <summary>True once a provider has been registered in this process.</summary>
    public static bool IsRegistered => Registry.IsRegistered;

    /// <summary>Registers the provider that gives OpenGL elements their contexts. Registering a second
    /// provider of the same type does nothing; call it before <see cref="PlayTestApplication.LaunchAsync"/>.</summary>
    /// <param name="provider">The provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A provider of a different type is already registered.</exception>
    public static void Register(IPlayTestOpenGLProvider provider) => Registry.Register(provider);
}

// The registry behind PlayTestOpenGLProviders, as an instance so the head's tests can use an empty one.
internal sealed class PlayTestOpenGLProviderRegistry
{
    private readonly object _lock = new();
    private IPlayTestOpenGLProvider? _provider;

    internal bool IsRegistered { get { lock (_lock) return _provider != null; } }
    internal IPlayTestOpenGLProvider? Provider { get { lock (_lock) return _provider; } }

    internal void Register(IPlayTestOpenGLProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_lock)
        {
            if (_provider == null) _provider = provider;
            else if (_provider.GetType() != provider.GetType())
                throw new InvalidOperationException(
                    $"An OpenGL provider of type {_provider.GetType().FullName} is already registered; PlayTest uses one provider per process.");
        }
    }
}

/// <summary>What the launch found out about OpenGL (<see cref="PlayTestApplication.OpenGL"/>).</summary>
public sealed class PlayTestOpenGLInfo
{
    private static readonly string[] SoftwareMarkers =
        ["llvmpipe", "softpipe", "swrast", "SwiftShader", "Microsoft Basic Render Driver", "GDI Generic"];

    internal PlayTestOpenGLInfo(string? provider, string? renderer, string? version, bool isGles, string? unavailableReason)
    {
        Provider = provider;
        Renderer = renderer;
        Version = version;
        IsGles = isGles;
        UnavailableReason = unavailableReason;
        IsAvailable = unavailableReason == null;
        IsSoftware = renderer != null && Array.Exists(SoftwareMarkers, m => renderer.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>True when OpenGL elements get a real context in this run.</summary>
    public bool IsAvailable { get; }
    /// <summary>The registered provider's name, or null when none is registered.</summary>
    public string? Provider { get; }
    /// <summary>GL_RENDERER of a probe context, or null when OpenGL is unavailable.</summary>
    public string? Renderer { get; }
    /// <summary>GL_VERSION of a probe context, or null when OpenGL is unavailable.</summary>
    public string? Version { get; }
    /// <summary>True when the contexts speak OpenGL ES.</summary>
    public bool IsGles { get; }
    /// <summary>True when the renderer is a CPU rasterizer (for example Mesa llvmpipe).</summary>
    public bool IsSoftware { get; }
    /// <summary>Why OpenGL is unavailable, or null when it is available.</summary>
    public string? UnavailableReason { get; }

    /// <summary>Describes the result for diagnostics.</summary>
    /// <returns>The provider, renderer and version, or the reason OpenGL is unavailable.</returns>
    public override string ToString() => IsAvailable
        ? $"{Provider}: {Renderer} ({Version})"
        : $"OpenGL unavailable: {UnavailableReason}";
}
