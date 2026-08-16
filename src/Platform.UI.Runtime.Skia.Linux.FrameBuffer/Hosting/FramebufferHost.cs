using System;
using System.IO;
using System.Reflection;
using System.Threading;
using CodeBrix.Platform.Extensions.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Helpers;
using WUX = Microsoft.UI.Xaml;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer //Was previously: Uno.UI.Runtime.Skia.Linux.FrameBuffer
{
	public class FrameBufferHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, IDisposable
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		// Overrides the host builder's UseDRM when set: "0"/"false"/"off" forces
		// the software /dev/fb0 renderer, "1"/"true"/"on" forces DRM, and unset
		// (or any other value) leaves the host builder's choice in place.
		private const string EnvironmentCodeBrixUseDrm = "CODEBRIX_FRAMEBUFFER_USE_DRM";

		private readonly EventLoop _eventLoop;
		private readonly CoreApplicationExtension? _coreApplicationExtension;

		private Func<Application> _appBuilder;
		private FrameBufferRenderer? _renderer;
		private Thread? _consoleInterceptionThread;
		private ManualResetEvent _terminationGate = new(false);
		private readonly FramebufferHostBuilder _hostBuilder;
		private FileStream? _instanceLock;

		/// <summary>
		/// Creates a host for a CodeBrix Skia FrameBuffer application.
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
			AcquireSingleInstanceLock();

			StartConsoleInterception();

			_eventLoop.Schedule(InnerInitialize);
		}

		// Two instances of the same FrameBuffer application would share the one
		// framebuffer — both blitting their frames, so the screen "flashes"
		// between them — and each would receive every touch, because evdev fans
		// events out to all readers. That is virtually always an accident, so a
		// second instance refuses to start unless the application opted in with
		// AllowMultipleApplicationInstances(). The guard is an advisory file
		// lock keyed on the entry assembly name: the OS releases it however the
		// process dies, so a crash can never leave a stale lock behind. A lock
		// file that cannot be created at all (odd permissions, read-only /tmp)
		// only logs a warning — an environmental quirk must not stop a
		// legitimate application from starting.
		private void AcquireSingleInstanceLock()
		{
			if (_hostBuilder.AllowMultipleInstances)
			{
				return;
			}

			var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "application";
			foreach (var invalid in Path.GetInvalidFileNameChars())
			{
				appName = appName.Replace(invalid, '_');
			}
			var lockPath = Path.Combine(Path.GetTempPath(), $"codebrix-framebuffer-{appName}.lock");
			try
			{
				_instanceLock = new FileStream(lockPath,
					FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				throw new InvalidOperationException(
					$"Another instance of {appName} is already running on this device " +
					$"(lock file: {lockPath}). A second instance would share the screen and the " +
					"touch input with the first, so it refuses to start. Stop the running instance " +
					"first — or opt in with AllowMultipleApplicationInstances() on the " +
					"UseLinuxFrameBuffer host builder if concurrent instances are really wanted.");
			}
			catch (Exception e)
			{
				this.Log().LogWarning(
					$"Could not create the single-instance lock file '{lockPath}' ({e.Message}); " +
					"continuing without duplicate-instance protection.");
			}
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

		private void StartConsoleInterception()
		{
			// ANSI escape sequence to hide the blinking caret
			Console.WriteLine("\u001b[?25l");

			// Only use the keyboard interception if the input is not redirected, to support
			// starting the app without a pty.
			if (!Console.IsInputRedirected && Console.KeyAvailable)
			{
				_consoleInterceptionThread = new(() =>
				{
					// Loop until Application.Current.Exit() is invoked
					while (!_coreApplicationExtension!.ExitRequested)
					{
						// Read the console keys without showing them on screen.
						// The keyboard input is handled by libinput.
						Console.ReadKey(true);
					}

					// The process asked to exit
					_terminationGate.Set();
				});

				// The thread must not block the process from exiting
				_consoleInterceptionThread.IsBackground = true;

				_consoleInterceptionThread.Start();
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Console input is redirected, skipping input interception");
				}
			}
		}

		private void InnerInitialize()
		{
			_isDispatcherThread = true;
			FrameBufferWindowWrapper.Init(_hostBuilder.DisplayOrientation, _hostBuilder.IsPreferredOrientation,
				_hostBuilder.AutoRotationOrientations, _hostBuilder.AutoRotationDisabled);
			var keyboardSource = new FrameBufferKeyboardInputSource(this, _hostBuilder.KeymapParams);

			ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension(this));
			ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), o => _coreApplicationExtension!);
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixCorePointerInputSource), o => { FrameBufferPointerInputSource.Instance.SetHost(o); return FrameBufferPointerInputSource.Instance; });
			ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixKeyboardInputSource), o => keyboardSource);
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new ApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new DisplayInformationExtension(o, DisplayScale));

			// Offscreen EGL GL context for GLCanvasElement (Graphics3DGL AddIn): DRM/GBM render
			// node when a GPU exists, otherwise Mesa's surfaceless platform (llvmpipe software
			// rendering — requires Mesa's software GL to be installed on GPU-less systems).
			ApiExtensibility.Register<Microsoft.UI.Xaml.XamlRoot>(typeof(CodeBrix.Platform.Graphics.INativeOpenGLWrapper), _ => new FrameBufferNativeOpenGLWrapper());

			// The in-application pickers, the software keyboard and the simple text
			// clipboard exist ONLY when the host builder opted in; otherwise no
			// registration happens and the pickers keep throwing
			// NotSupportedException (and the clipboard stays unimplemented)
			// exactly as before.
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
				ApiExtensibility.Register(typeof(CodeBrix.Platform.UI.Xaml.Controls.Extensions.ITextInputFocusNotificationsSingleton), o => keyboardController);
			}
			if (_hostBuilder.SimpleTextClipboardEnabled)
			{
				ApiExtensibility.Register(
					typeof(CodeBrix.Platform.ApplicationModel.DataTransfer.IClipboardExtension),
					_ => CodeBrix.Platform.UI.Runtime.Skia.SimpleTextClipboardExtension.Instance);
			}

			void Dispatch(System.Action d, NativeDispatcherPriority p)
				=> _eventLoop.Schedule(d);

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;

				// Force the first render once the app has been setup
				Dispatch(() => _renderer.InvalidateRender(), NativeDispatcherPriority.High);
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			FrameBufferInputProvider.Instance.Initialize();

			var drmInitOptions = new DRMRenderer.DRMInitOptions(_hostBuilder.DRMCardPath, _hostBuilder.DRMConnectorChooser, _hostBuilder.GBMSurfaceColorFormat);
			var mouseIndicatorOptions = new FrameBufferRenderer.MouseIndicatorOptions(_hostBuilder.ShowMouseCursor, _hostBuilder.MouseCursorRadius, _hostBuilder.MouseCursorColor);

			// A launcher can pin the renderer via CODEBRIX_FRAMEBUFFER_USE_DRM,
			// overriding the host builder's UseDRM. This is what lets an SSH
			// remote run force software /dev/fb0 rendering, since DRM master is
			// never available to a process that is not the active console.
			var useDrm = ResolveUseDrm(_hostBuilder.UseDRM);
			if (useDrm ?? false)
			{
				_renderer = new DRMRenderer(this, drmInitOptions, mouseIndicatorOptions);
			}
			else if (useDrm is null)
			{
				try
				{
					_renderer = new DRMRenderer(this, drmInitOptions, mouseIndicatorOptions);
				}
				catch (Exception e)
				{
					this.LogError()?.Error($"Failed to create an OpenGLES context with error '{e.Message}', falling back to software rendering");
					_renderer = new SoftwareRenderer(this, mouseIndicatorOptions);
				}
			}
			else
			{
				_renderer = new SoftwareRenderer(this, mouseIndicatorOptions);
			}

			// Runtime orientation instructions (the sensor via iio-sensor-proxy,
			// or CodeBrix.Develop's testing signal — see DeviceOrientationSource)
			// are applied on the event loop exactly as the Emulated head applies
			// its transport-driven rotations.
			DeviceOrientationSource.Start(_hostBuilder, orientation => _eventLoop.Schedule(() =>
			{
				if (FrameBufferWindowWrapper.Instance.SetDeviceOrientation(orientation))
				{
					_renderer?.InvalidateRender();
				}
			}));

			WUX.Application.Start(CreateApp);
		}

		// Applies the CODEBRIX_FRAMEBUFFER_USE_DRM override to the host builder's
		// UseDRM: an explicit env value wins, otherwise the builder's value stands.
		private static bool? ResolveUseDrm(bool? fromBuilder)
		{
			var value = Environment.GetEnvironmentVariable(EnvironmentCodeBrixUseDrm);
			if (string.IsNullOrEmpty(value))
			{
				return fromBuilder;
			}

			if (value == "0"
				|| string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (value == "1"
				|| string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return fromBuilder;
		}

		void IXamlRootHost.InvalidateRender() => _renderer?.InvalidateRender();

		WUX.UIElement? IXamlRootHost.RootElement => FrameBufferWindowWrapper.Instance.Window?.RootElement;

		public void Dispose()
		{
			_instanceLock?.Dispose();
			_instanceLock = null;
		}
	}
}
