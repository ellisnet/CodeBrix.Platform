using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.Helpers.Theming;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using CodeBrix.Platform.Extensions.Storage.Pickers;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

public partial class WaylandApplicationHost : SkiaHost, ISkiaApplicationHost, IDisposable
{
	[ThreadStatic] private static bool _isDispatcherThread;
	private readonly EventLoop _eventLoop;

	private readonly Func<Application> _appBuilder;

	static WaylandApplicationHost()
	{
		ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), _ => new WaylandCoreApplicationExtension());
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WaylandApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new WaylandDisplayInformationExtension(o));

		ApiExtensibility.Register<IXamlRootHost>(typeof(ICodeBrixCorePointerInputSource), o => new WaylandPointerInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(ICodeBrixKeyboardInputSource), o => new WaylandKeyboardInputSource(o));

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => new WaylandNativeWindowFactoryExtension());

		ApiExtensibility.Register(typeof(CodeBrix.Platform.Extensions.System.ILauncherExtension), o => new CodeBrix.Platform.UI.Runtime.Skia.Extensions.System.LinuxLauncherExtension(o));

		// File pickers ride xdg-desktop-portal over DBus — display-server-agnostic, so these
		// are the same Linux* portal extensions the X11 head uses.
		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new LinuxFilePickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new LinuxFilePickerExtension(o));
		ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new LinuxFileSaverExtension(o));

		ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.DataTransfer.IClipboardExtension), _ => new WaylandClipboardExtension());

		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), _ => LinuxSystemThemeHelper.Instance);

		CompositionTarget.FrameRenderingOptions = (true, true);
	}

	public WaylandApplicationHost(Func<Application> appBuilder, int renderFrameRate = 60)
	{
		_appBuilder = appBuilder;

		if (RenderFrameRate != default && renderFrameRate != RenderFrameRate)
		{
			throw new InvalidOperationException("Wayland's render frame rate should only be set once.");
		}
		RenderFrameRate = renderFrameRate;

		_eventLoop = new EventLoop();
		_eventLoop.Schedule(() => { Thread.CurrentThread.Name = "CodeBrix Event Loop"; });

		_eventLoop.Schedule(() =>
		{
			_isDispatcherThread = true;
		});
		CoreDispatcher.DispatchOverride = (a, p) => _eventLoop.Schedule(a);
		CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
	}

	internal static int RenderFrameRate { get; private set; }

	protected override Task RunLoop()
	{
		Thread.CurrentThread.Name = "Main Thread (keep-alive)";

		// Fail fast (plan decision 2.(6)): the AUTHORITATIVE check is the wl_display_connect
		// result — WAYLAND_DISPLAY can be unset with a live default socket, or set but stale.
		// A clean, on-brand message and a non-zero exit code; no raw stack trace.
		try
		{
			_ = WaylandConnection.ConnectOrThrow();
		}
		catch (WaylandCompositorMissingException e)
		{
			Console.Error.WriteLine(e.Message);
			Console.Error.WriteLine(WaylandCompositorMissingException.DeveloperHint);
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(e.Message);
			}
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		_eventLoop.Schedule(StartApp);

		while (!WaylandXamlRootHost.AllWindowsDone())
		{
			Thread.Sleep(100);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{nameof(WaylandApplicationHost)} is exiting");
		}

		return Task.CompletedTask;
	}

	private void StartApp()
	{
		void CreateApp(ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		Application.Start(CreateApp);
	}

	protected override void Initialize()
	{
	}

	public void Dispose()
	{
	}
}
