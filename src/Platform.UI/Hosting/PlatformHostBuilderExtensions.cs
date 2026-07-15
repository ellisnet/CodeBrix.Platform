#nullable enable

using System;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class CodeBrixPlatformHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Microsoft.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	public static ICodeBrixPlatformHostBuilder App<TApplication>(this ICodeBrixPlatformHostBuilder builder, Func<TApplication> appBuilder)
		where TApplication : Microsoft.UI.Xaml.Application
	{
		builder.AppBuilder = appBuilder;
		builder.SetAppType<TApplication>();
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the CodeBrixPlatformHost has been initialized, and before the run loop starts.
	/// </summary>
	public static ICodeBrixPlatformHostBuilder AfterInit(this ICodeBrixPlatformHostBuilder builder, Action action)
	{
		builder.AfterInitAction = action;
		return builder;
	}

	/// <summary>
	/// Opts the whole application into the direct <see cref="SkiaSharp.Views.Windows.SKXamlCanvas"/>
	/// present path: <c>SKXamlCanvas</c> draws each frame straight into its on-screen
	/// <c>WriteableBitmap</c> buffer instead of drawing into an intermediate staging buffer and
	/// copying, removing one full-frame copy per paint. - EXPERIMENTAL
	/// </summary>
	/// <remarks>
	/// Call this once in the host-builder chain (order relative to the <c>Use…</c> head call does not
	/// matter). It is a one-way, app-wide latch: it cannot be turned off, and there is no per-canvas
	/// override — the whole app either runs in this mode or does not. If this is not called, the
	/// <c>SKXamlCanvas</c> present path is unchanged. See <see cref="DirectSkiaCanvasMode"/>.
	/// </remarks>
	/// <param name="builder">The host builder.</param>
	/// <returns>The same host builder, for chaining.</returns>
	public static ICodeBrixPlatformHostBuilder UseDirectSkiaCanvasMode(this ICodeBrixPlatformHostBuilder builder)
	{
		DirectSkiaCanvasMode.Enable();
		return builder;
	}
}
