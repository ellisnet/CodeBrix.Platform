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

			// Before anything is built: the emulated device's own language, so
			// the application starts out in it rather than switching later.
			if (EmulatedSystemLanguage.Apply() is { } language
				&& this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Emulated device language: {language}");
			}

			// Also before anything is built, and for the same reason: resolved
			// fonts are cached, so text measured before this is set would keep
			// the host fonts it was given.
			if (EmulatedFontIsolation.Apply() && this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(
					"Emulated device font isolation: on — only the application's own fonts are used.");
			}

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
			// This is the UI thread, and it was already running when the
			// language was applied — give it the emulated device's culture too,
			// or it renders the right layout with the wrong words.
			EmulatedSystemLanguage.ApplyToCurrentThread();
			// How the application is mounted on the emulated panel, exactly as
			// on the real head. The frame buffer's dimensions come from the
			// IDE and never change, so a portrait mount does not resize
			// anything — it draws the transposed application rotated into that
			// same fixed buffer, and the application appears sideways, which is
			// what a crosswise mount looks like on real hardware.
			FrameBufferWindowWrapper.Init(_hostBuilder.DisplayOrientation, _hostBuilder.IsPreferredOrientation,
				_hostBuilder.AutoRotationOrientations, _hostBuilder.AutoRotationDisabled);
			var keyboardSource = new EmulatedKeyboardInputSource();
			FrameBufferPointerInputSource.Instance.Configure(keyboardSource.GetCurrentModifiersState);

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixKeyboardInputSource), o => { keyboardSource.SetHost(o); return keyboardSource; });
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new DisplayInformationExtension(o, DisplayScale));

			// Offscreen EGL GL context for GLCanvasElement (Graphics3DGL AddIn), exactly as on
			// the real FrameBuffer head: DRM/GBM render node when a GPU exists, otherwise Mesa's
			// surfaceless platform (llvmpipe software rendering on GPU-less systems).
			ApiExtensibility.Register<Microsoft.UI.Xaml.XamlRoot>(typeof(CodeBrix.Platform.Graphics.INativeOpenGLWrapper), _ => new FrameBufferNativeOpenGLWrapper());

			// The in-application pickers and the software keyboard exist ONLY when the
			// host builder opted in, exactly as on the real FrameBuffer head; otherwise
			// no registration happens and the pickers keep throwing
			// NotSupportedException exactly as before.
			if (_hostBuilder.FileOpenPickerEnabled)
			{
				var fileOpenOptions = _hostBuilder.FileOpenPickerOptions;
				ApiExtensibility.Register<Windows.Storage.Pickers.FileOpenPicker>(
					typeof(CodeBrix.Platform.Extensions.Storage.Pickers.IFileOpenPickerExtension),
					o => new CodeBrix.Platform.UI.Runtime.Skia.Pickers.FrameBufferFileOpenPickerExtension(o, fileOpenOptions));
			}
			if (_hostBuilder.FileSavePickerEnabled)
			{
				var fileSaveOptions = _hostBuilder.FileSavePickerOptions;
				ApiExtensibility.Register<Windows.Storage.Pickers.FileSavePicker>(
					typeof(CodeBrix.Platform.Extensions.Storage.Pickers.IFileSavePickerExtension),
					o => new CodeBrix.Platform.UI.Runtime.Skia.Pickers.FrameBufferFileSavePickerExtension(o, fileSaveOptions));
			}
			if (_hostBuilder.FolderPickerEnabled)
			{
				var folderOptions = _hostBuilder.FolderPickerOptions;
				ApiExtensibility.Register<Windows.Storage.Pickers.FolderPicker>(
					typeof(CodeBrix.Platform.Extensions.Storage.Pickers.IFolderPickerExtension),
					o => new CodeBrix.Platform.UI.Runtime.Skia.Pickers.FrameBufferFolderPickerExtension(o, folderOptions));
			}
			if (_hostBuilder.SoftwareKeyboardEnabled)
			{
				var keyboardController = new CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard.SoftwareKeyboardController(
					this, keyboardSource, _hostBuilder.SoftwareKeyboardOptions);
				ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IInputPaneExtension), o => keyboardController);
				ApiExtensibility.Register(typeof(CodeBrix.Platform.UI.Xaml.Controls.Extensions.ITextBoxNotificationsProviderSingleton), o => keyboardController);
			}

			void Dispatch(System.Action d, NativeDispatcherPriority p)
				=> _eventLoop.Schedule(d);

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;

				// Application's constructor calls ApplicationLanguages.ApplyCulture(),
				// which resets the culture to the FIRST of its language list — and that
				// list is led by the HOST machine's installed UI culture. Re-assert the
				// emulated device's language here, after the application exists: doing it
				// through PrimaryLanguageOverride instead would work, but its setter
				// persists the value into the application's own LocalSettings, so an
				// emulator run in German would leave a real run German too.
				EmulatedSystemLanguage.ApplyToCurrentThread();

				// Force the first render once the app has been setup
				Dispatch(() => _renderer!.InvalidateRender(), NativeDispatcherPriority.High);
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			// The renderer's constructor is what first hands the frame buffer's size to
			// the window wrapper, so the panel is resolved before the input loop below
			// can deliver an orientation that depends on knowing it.
			_renderer = new EmulatedRenderer(this, connection);
			connection.StartInputLoop(
				FrameBufferPointerInputSource.Instance.ProcessEmulatedTouch,
				keyboardSource.ProcessEmulatedKey,
				ProcessEmulatedDeviceOrientation);

			WUX.Application.Start(CreateApp);
		}

		// The emulator says the device has been turned. Arrives on the transport's
		// input thread, so it is marshalled onto the dispatcher — honoring it
		// re-lays-out the application — and a repaint is forced afterwards, because a
		// rotation changes how the visual tree is drawn without changing the tree.
		private void ProcessEmulatedDeviceOrientation(uint wireOrientation)
		{
			var orientation = wireOrientation switch
			{
				FrameBufferEmulatorProtocol.OrientationPortrait => DisplayOrientations.Portrait,
				FrameBufferEmulatorProtocol.OrientationLandscapeFlipped => DisplayOrientations.LandscapeFlipped,
				FrameBufferEmulatorProtocol.OrientationPortraitFlipped => DisplayOrientations.PortraitFlipped,
				_ => DisplayOrientations.Landscape,
			};
			_eventLoop.Schedule(() =>
			{
				if (FrameBufferWindowWrapper.Instance.SetDeviceOrientation(orientation))
				{
					_renderer?.InvalidateRender();
				}
			});
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

		public void Dispose()
		{
		}
	}
}
