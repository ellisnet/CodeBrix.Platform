using System;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.UI;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;
using WUX = Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Hosting;

/// <summary>
/// The TEST TARGET host: the emulated frame-buffer head with the emulator taken
/// out of it. It renders the application onto a fixed panel in this process's
/// own memory and takes touch and key input from the process that is driving it,
/// so a test can look at what the application actually drew.
/// <para>
/// It is the emulator host's <c>InnerInitialize</c> minus every concern that
/// belongs to a device being emulated for a person: there is no launch contract
/// to validate and therefore no process exit, no emulated system language, no
/// font isolation, no pickers, no software keyboard, no clipboard, no mouse
/// cursor and no runtime rotation. The panel never turns: a portrait run is
/// simply a portrait-shaped buffer with the application laid out upright in it,
/// so the canvas is never rotated. Display scale is fixed at 1.0, and the
/// environment's scale override is deliberately not consulted, so a logical
/// pixel is a device pixel.
/// </para>
/// </summary>
internal sealed class TestTargetHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
{
	[ThreadStatic]
	private static bool _isDispatcherThread;

	private readonly EventLoop _eventLoop;
	private readonly CoreApplicationExtension _coreApplicationExtension;
	private readonly ManualResetEvent _terminationGate = new(false);
	private readonly Func<WUX.Application> _appBuilder;
	private readonly TestTargetSession _session;
	private readonly InProcessTransport _transport;

	private EmulatedRenderer? _renderer;

	internal TestTargetHost(Func<WUX.Application> appBuilder, TestTargetHostBuilder builder)
	{
		_appBuilder = appBuilder;
		_session = builder.Session;
		_eventLoop = new EventLoop();
		_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
		_transport = new InProcessTransport(_session.Width, _session.Height);
		_session.AttachHost(this);
	}

	/// <summary>The panel this host renders onto.</summary>
	internal InProcessTransport Transport => _transport;

	/// <summary>Runs <paramref name="action"/> on the UI thread.</summary>
	/// <param name="action">The work to run.</param>
	internal void RunOnUIThread(Action action)
		=> _eventLoop.Schedule(action);

	/// <summary>
	/// Asks for a new frame to be rendered and published, and returns the
	/// invalidation generation the request moved the renderer to: a published
	/// frame whose render generation is that number or larger was drawn entirely
	/// after this call. What that pass DRAWS is the picture the compositor
	/// recorded last, so one pass is not enough to see a change that was just
	/// applied - see <see cref="TestTarget.TestTargetSession.RequestFrameAsync"/>.
	/// </summary>
	/// <returns>The invalidation generation this request produced.</returns>
	internal long RequestRenderAndGetGeneration()
		=> _renderer?.InvalidateRenderAndGetGeneration()
			?? throw new InvalidOperationException(
				"The test target host has not created its renderer yet.");

	protected override void Initialize()
	{
		// Nothing to validate and nobody to tell: the panel exists because this
		// process made it. Straight onto the UI thread.
		_eventLoop.Schedule(InnerInitialize);
	}

	protected override Task RunLoop()
	{
		_terminationGate.WaitOne();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application is exiting");
		}

		return Task.CompletedTask;
	}

	private void InnerInitialize()
	{
		_isDispatcherThread = true;

		// The panel's own shape is the only thing the orientation says here: the
		// application is mounted upright on it, with auto-rotation off, so the
		// canvas is never rotated and the buffer is never transposed.
		FrameBufferWindowWrapper.Init(DisplayOrientations.Landscape, isPreferredOrientation: false,
			autoRotationOrientations: null, autoRotationDisabled: true);
		var keyboardSource = new EmulatedKeyboardInputSource();
		FrameBufferPointerInputSource.Instance.Configure(keyboardSource.GetCurrentModifiersState);

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
		ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension);
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixKeyboardInputSource), o => { keyboardSource.SetHost(o); return keyboardSource; });
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
		// Scale 1.0, and the environment override is not consulted: a test reads
		// device pixels, so logical pixels have to be the same thing.
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension),
			o => new DisplayInformationExtension(o, 1.0f, honorEnvironmentOverride: false));

		// Offscreen EGL GL context for GLCanvasElement, registered lazily exactly as on the
		// emulated head, so it only ever initializes if the application uses a GL element.
		ApiExtensibility.Register<Microsoft.UI.Xaml.XamlRoot>(typeof(CodeBrix.Platform.Graphics.INativeOpenGLWrapper), _ => new FrameBufferNativeOpenGLWrapper());

		void Dispatch(System.Action d, NativeDispatcherPriority p)
			=> _eventLoop.Schedule(d);

		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;

			// Force the first render once the app has been setup
			Dispatch(() => _renderer!.InvalidateRender(), NativeDispatcherPriority.High);
		}

		Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

		// The renderer's constructor is what first hands the panel's size to the
		// window wrapper, so the panel is resolved before any input can arrive.
		_renderer = new EmulatedRenderer(this, _transport);
		_transport.StartInputLoop(
			FrameBufferPointerInputSource.Instance.ProcessEmulatedTouch,
			keyboardSource.ProcessEmulatedKey,
			_ => { });

		WUX.Application.Start(CreateApp);
	}

	void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

	WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

	/// <summary>Frees the panel's pixel slots.</summary>
	public void Dispose() => _transport.Dispose();
}
