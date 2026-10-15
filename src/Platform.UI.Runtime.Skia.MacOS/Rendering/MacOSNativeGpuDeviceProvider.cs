using System.Collections.Generic;
using Microsoft.UI.Xaml;
using SkiaSharp;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS;

/// <summary>
/// The macOS head's <see cref="INativeGpuDeviceProvider"/>: the window's OWN Metal device, the command queue its
/// compositor submits on and its compositor <see cref="SkiaSharp.GRContext"/>, so an add-in can composite GPU content
/// on that device without a read-back. Unlike <see cref="MacOSNativeSkiaGpuContext"/> (a separate context on its own
/// queue, for off-screen renderers), nothing here is created or owned: the handles belong to the window.
/// </summary>
/// <remarks>
/// Registered for every XamlRoot; the builder answers null - no provider - when the head is in software mode
/// (<see cref="RenderSurfaceType.Software"/>), so a deliberately forced software head is respected. A resolved
/// provider is live and reports unavailable once the window closes. Metal has no thread-current state, so
/// <see cref="BeginFrame"/> is a no-op scope.
/// </remarks>
internal sealed class MacOSNativeGpuDeviceProvider : INativeGpuDeviceProvider
{
	private const string ClosedReason = "The window has closed; its Metal device is no longer available.";

	private readonly MacOSWindowHost _host;

	// The constructor must NOT throw: ApiExtensibility builders run while an element resolves its extensions.
	private MacOSNativeGpuDeviceProvider(MacOSWindowHost host) => _host = host;

	/// <inheritdoc />
	public NativeGpuDeviceKind Kind => NativeGpuDeviceKind.Metal;

	/// <inheritdoc />
	public bool IsAvailable => _host.TryGetMetalHandles(out _, out _);

	/// <inheritdoc />
	public string? Reason => IsAvailable ? null : ClosedReason;

	/// <inheritdoc />
	public nint VkInstance => 0;

	/// <inheritdoc />
	public nint VkPhysicalDevice => 0;

	/// <inheritdoc />
	public nint VkDevice => 0;

	/// <inheritdoc />
	public nint VkQueue => 0;

	/// <inheritdoc />
	public uint VkQueueFamilyIndex => 0;

	/// <inheritdoc />
	public uint VkQueueIndex => 0;

	/// <inheritdoc />
	public uint VkApiVersion => 0;

	/// <inheritdoc />
	public IReadOnlyList<string> VkInstanceExtensions => [];

	/// <inheritdoc />
	public IReadOnlyList<string> VkDeviceExtensions => [];

	/// <inheritdoc />
	public nint VkGetInstanceProcAddr => 0;

	/// <inheritdoc />
	public nint MetalDevice => _host.TryGetMetalHandles(out var device, out _) ? device : 0;

	/// <inheritdoc />
	public nint MetalCommandQueue => _host.TryGetMetalHandles(out _, out var queue) ? queue : 0;

	/// <inheritdoc />
	public GRContext? GRContext => IsAvailable ? _host.GrContext : null;

	/// <inheritdoc />
	// Metal has no thread-current concept, so the frame scope is a no-op.
	public IDisposable BeginFrame() => Disposable.Empty;

	// Null (no provider) unless the XamlRoot's window renders with Metal.
	internal static object? For(XamlRoot xamlRoot)
		=> XamlRootMap.GetHostForRoot(xamlRoot) is MacOSWindowHost host && host.TryGetMetalHandles(out _, out _)
			? new MacOSNativeGpuDeviceProvider(host)
			: null;

	public static void Register() =>
		ApiExtensibility.Register<XamlRoot>(typeof(INativeGpuDeviceProvider), xamlRoot => For(xamlRoot)!);
}
