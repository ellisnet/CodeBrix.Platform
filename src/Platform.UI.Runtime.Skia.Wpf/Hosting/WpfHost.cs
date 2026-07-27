#nullable enable

using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.UI.Controls;
using CodeBrix.Platform.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfApplication = System.Windows.Application;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf; //Was previously: Uno.UI.Runtime.Skia.Wpf

public class WpfHost : SkiaHost, IWpfApplicationHost
{
	private readonly Dispatcher _dispatcher;
	private readonly Func<WinUIApplication> _appBuilder;
	private readonly WpfApplication? _wpfApp;

	[ThreadStatic] private static WpfHost? _current;

	private bool _ignorePixelScaling;

	static WpfHost()
		=> WpfExtensionsRegistrar.Register();

	public WpfHost(Dispatcher dispatcher, Func<WinUIApplication> appBuilder)
	{
		_current = this;
		_dispatcher = dispatcher;
		_appBuilder = appBuilder;
	}

	internal WpfHost(
		Func<WinUIApplication> appBuilder,
		Func<WpfApplication>? wpfAppBuilder,
		WpfDispatcherScheduling dispatcherScheduling = WpfDispatcherScheduling.RenderFirst)
	{
		_wpfApp = wpfAppBuilder?.Invoke() ?? new WpfApplication();

		_current = this;
		_dispatcher = _wpfApp.Dispatcher;
		_appBuilder = appBuilder;
		DispatcherScheduling = dispatcherScheduling;
	}

	internal static WpfHost? Current => _current;

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	/// <summary>
	/// Gets or sets the WPF dispatcher priority tier the CodeBrix dispatcher pump runs at.
	/// </summary>
	/// <remarks>
	/// Read once, when the host initializes (from <c>Run()</c>), so it can be set either through
	/// <c>UseWindowsWpf(wpf =&gt; wpf.DispatcherScheduling(...))</c> or directly on the host after
	/// <c>Build()</c>. Defaults to <see cref="WpfDispatcherScheduling.RenderFirst"/>.
	/// </remarks>
	public WpfDispatcherScheduling DispatcherScheduling { get; set; } = WpfDispatcherScheduling.RenderFirst;

	public bool IgnorePixelScaling
	{
		get => _ignorePixelScaling;
		set
		{
			_ignorePixelScaling = value;
			if (WpfApplication.Current.MainWindow is CodeBrixWpfWindow window)
			{
				window.InvalidateVisual();
			}
		}
	}

	protected override void Initialize()
	{
		InitializeDispatcher();
	}

	protected override Task RunLoop()
	{
		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
		StartApp();

		_wpfApp?.Run();

		return Task.CompletedTask;
	}

	private void InitializeDispatcher()
	{
		// The WPF tier the CodeBrix dispatcher pump is posted at. Note the pump callback is
		// NativeDispatcher.DispatchItems, which runs ONE queued item per invocation and re-posts
		// itself while items remain — so this choice only decides how CodeBrix competes with WPF's
		// own queues, never how CodeBrix orders its own work (its four internal priority queues and
		// render-fairness accounting still do that).
		//
		// RenderFirst (default) posts at Render (7), which outranks Input (5) — the tier WPF
		// delivers keyboard and pointer input on. An app that schedules UI work continuously keeps a
		// Render item pending at all times, so WPF never descends to the Input queue and input is
		// starved outright rather than delayed: the app keeps rendering but stops responding.
		// InputFair posts at Input instead, so pump items and input events share one FIFO tier and
		// interleave, which makes that starvation structurally impossible.
		var pumpPriority = DispatcherScheduling == WpfDispatcherScheduling.InputFair
			? DispatcherPriority.Input
			: DispatcherPriority.Render;

		Windows.UI.Core.CoreDispatcher.DispatchOverride = (d, p) => _dispatcher.BeginInvoke(d, p == CodeBrix.Platform.UI.Dispatching.NativeDispatcherPriority.Idle ? DispatcherPriority.SystemIdle : pumpPriority);
		Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = _dispatcher.CheckAccess;
	}

	private void StartApp()
	{
		void CreateApp(WinUI.ApplicationInitializationCallbackParams _)
		{
			var app = _appBuilder();
			app.Host = this;
		}

		WinUIApplication.Start(CreateApp);
	}

	public override string ToString() =>
		"If you are seeing this, make sure to follow the \"Migrating WpfHost\" section of Migrating from " +
		"previous releases article in the CodeBrix Platform documentation at " +
		"https://aka.platform.uno/uno5-wpfhost-migration. " +
		"WpfHost is used at the application level instead of window level.";
}
