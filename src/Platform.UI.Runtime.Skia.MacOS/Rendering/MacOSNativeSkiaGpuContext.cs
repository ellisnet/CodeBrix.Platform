using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using SkiaSharp;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS; //Was previously: Uno.UI.Runtime.Skia.MacOS

/// <summary>
/// The macOS head's <b>Skia-on-Metal</b> GPU-context provider, resolved through
/// <see cref="ApiExtensibility"/> exactly like <see cref="MacOSNativeOpenGLWrapper"/>. It gives an
/// off-screen renderer — for example the CodeBrix.Platform.GameEngine GPU render tier — a Skia
/// <see cref="GRContext"/> backed by Metal, so it renders on the GPU on macOS, where the stock
/// SkiaSharp binary has no OpenGL(ES) interface and the OpenGL path cannot build a GRContext.
/// <para>
/// Design: a <b>separate</b> <see cref="GRContext"/> on its <b>own</b> <c>MTLCommandQueue</c>,
/// created on the <b>same</b> <c>MTLDevice</c> the window's compositor already uses (obtained from
/// the native window). Sharing the device keeps all rendering on one GPU — correct on multi-GPU and
/// Intel Macs — while the private queue and context keep the engine's GPU work isolated from the
/// compositor's, so enabling GPU rendering can never perturb the UI or a CPU-rendering game.
/// </para>
/// <para>
/// Only usable when the head is in Metal mode. In software-rendering mode
/// (<see cref="RenderSurfaceType.Software"/>) <see cref="CreateGrContext"/> throws, and the
/// <c>SkiaGpuContext</c> facade turns that into the caller's CPU-rendering fallback — so a developer
/// who deliberately forced software rendering is respected.
/// </para>
/// </summary>
internal sealed class MacOSNativeSkiaGpuContext : INativeSkiaGpuContext
{
	private readonly XamlRoot _xamlRoot;
	private nint _ownQueue;

	// The constructor must NOT throw: ApiExtensibility invokes the builder inside a lock. All fallible
	// work is deferred to CreateGrContext, which the SkiaGpuContext facade wraps in try/catch.
	public MacOSNativeSkiaGpuContext(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	/// <inheritdoc />
	public SkiaGpuBackend Backend => SkiaGpuBackend.Metal;

	/// <inheritdoc />
	public GRContext CreateGrContext()
	{
		if (MacSkiaHost.Current.RenderSurfaceType != RenderSurfaceType.Metal)
		{
			throw new InvalidOperationException(
				"macOS is rendering in software mode; no Metal GPU context is available.");
		}

		if (XamlRootMap.GetHostForRoot(_xamlRoot) is not MacOSWindowHost host || !host.TryGetMetalDevice(out var device))
		{
			throw new InvalidOperationException("No Metal device is available for the given XamlRoot.");
		}

		// Our own command queue on the compositor's device isolates the engine's GPU submissions from
		// the compositor's queue while sharing the single GPU.
		_ownQueue = MacOSMetalInterop.NewCommandQueue(device);
		if (_ownQueue == 0)
		{
			throw new InvalidOperationException("Failed to create a Metal command queue on the device.");
		}

		var backendContext = new GRMtlBackendContext
		{
			DeviceHandle = device,
			QueueHandle = _ownQueue,
		};
		return GRContext.CreateMetal(backendContext)
			?? throw new InvalidOperationException("GRContext.CreateMetal returned null.");
	}

	/// <inheritdoc />
	// Metal has no thread-current concept, so the frame scope is a no-op.
	public IDisposable BeginFrame() => Disposable.Empty;

	/// <inheritdoc />
	public void Dispose()
	{
		// Release our own command queue; the device belongs to the compositor and must NOT be released.
		if (_ownQueue != 0)
		{
			MacOSMetalInterop.Release(_ownQueue);
			_ownQueue = 0;
		}
	}

	public static void Register() =>
		ApiExtensibility.Register<XamlRoot>(typeof(INativeSkiaGpuContext), xamlRoot => new MacOSNativeSkiaGpuContext(xamlRoot));
}

/// <summary>
/// Minimal Objective-C interop to create a Metal command queue on an existing <c>MTLDevice</c>,
/// avoiding a native-library rebuild. Only <c>[device newCommandQueue]</c> and <c>-release</c> are
/// needed, and both return an <c>id</c>/<c>void</c> with no arguments — so a single
/// <c>objc_msgSend</c> is correct on Apple Silicon (arm64) and Intel/Rosetta (x86-64) alike, since
/// the calling convention only diverges for struct-returning messages, of which there are none here.
/// (The library and function paths resolve only on macOS, where this provider is ever used.)
/// </summary>
internal static class MacOSMetalInterop
{
	private const string Objc = "/usr/lib/libobjc.A.dylib";

	[DllImport(Objc, EntryPoint = "sel_registerName")]
	private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

	[DllImport(Objc, EntryPoint = "objc_msgSend")]
	private static extern nint objc_msgSend(nint receiver, nint selector);

	/// <summary><c>[device newCommandQueue]</c> — a new, owned (+1) command queue; release it when done.</summary>
	public static nint NewCommandQueue(nint device) => objc_msgSend(device, sel_registerName("newCommandQueue"));

	/// <summary><c>[obj release]</c>, tolerating null.</summary>
	public static void Release(nint obj)
	{
		if (obj != 0)
		{
			objc_msgSend(obj, sel_registerName("release"));
		}
	}
}
