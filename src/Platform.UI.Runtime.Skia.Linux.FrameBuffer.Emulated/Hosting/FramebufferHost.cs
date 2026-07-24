using System;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.UI;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;
using WUX = Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer //Was previously: Uno.UI.Runtime.Skia.Linux.FrameBuffer
{
	/// <summary>
	/// The EMULATED frame-buffer host: a compile-time drop-in for the real
	/// FrameBuffer head that renders offscreen at exactly one fixed resolution
	/// for its whole life and exchanges frames and touch input with the
	/// CodeBrix.Develop frame-buffer emulator over shared memory and a socket.
	/// The application must never learn the emulator window exists: it sees
	/// the configured resolution, never a resize, and powers off — libc _exit —
	/// when the transport dies.
	/// <para>
	/// It can only be launched by CodeBrix.Develop (the launch contract arrives
	/// in environment variables); started any other way it prints one clear
	/// line and exits with code 1.
	/// </para>
	/// </summary>
	public class FrameBufferHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly EventLoop _eventLoop;
		private readonly CoreApplicationExtension? _coreApplicationExtension;

		private Func<Application> _appBuilder;
		private EmulatedRenderer? _renderer;
		private EmulatorConnection? _connection;
		private ManualResetEvent _terminationGate = new(false);
		private readonly FramebufferHostBuilder _hostBuilder;

		/// <summary>
		/// Creates a host for a CodeBrix Skia FrameBuffer application running
		/// under the CodeBrix.Develop frame-buffer emulator.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <remarks>
		/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
		/// </remarks>
		public FrameBufferHost(Func<WUX.Application> appBuilder) : this(appBuilder, new FramebufferHostBuilder())
		{
		}

		internal FrameBufferHost(Func<WUX.Application> appBuilder, FramebufferHostBuilder builder)
		{
			_appBuilder = appBuilder;
			_hostBuilder = builder;

			_eventLoop = new EventLoop();
			_coreApplicationExtension = new CoreApplicationExtension(_terminationGate);
		}

		/// <summary>
		/// Provides a display scale to override framebuffer default scale
		/// </summary>
		/// <remarks>This value can be overriden by the CODEBRIX_DISPLAY_SCALE_OVERRIDE environment variable</remarks>
		public float? DisplayScale { get; set; }

		protected override void Initialize()
		{
			// The launch contract is validated before anything is spun up, so
			// a double-click outside the IDE fails fast with one clear line.
			if (!EmulatorConnection.TryCreate(out var connection, out var error))
			{
				Console.Error.WriteLine(error);
				Environment.Exit(1);
				return;
			}
			_connection = connection;

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
			var connection = _connection!;

			_isDispatcherThread = true;
			// The emulated device's orientation is baked into the resolution
			// the IDE launched us with, so rendering never rotates; the
			// builder's Orientation option is deliberately inert here.
			FrameBufferWindowWrapper.Init(DisplayOrientations.Landscape);
			var keyboardSource = new EmulatedKeyboardInputSource();
			FrameBufferPointerInputSource.Instance.Configure(keyboardSource.GetCurrentModifiersState);

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixKeyboardInputSource), o => keyboardSource);
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new DisplayInformationExtension(o, DisplayScale));

			// Offscreen EGL GL context for GLCanvasElement (Graphics3DGL AddIn), exactly as on
			// the real FrameBuffer head: DRM/GBM render node when a GPU exists, otherwise Mesa's
			// surfaceless platform (llvmpipe software rendering on GPU-less systems).
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

			_renderer = new EmulatedRenderer(this, connection);
			connection.StartInputLoop(FrameBufferPointerInputSource.Instance.ProcessEmulatedTouch);

			WUX.Application.Start(CreateApp);
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

		public void Dispose()
		{
		}
	}
}
