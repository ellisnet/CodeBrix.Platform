using System.Runtime.InteropServices;
using System.Windows.Input;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Windows.System;

internal static class Program
{
	internal static string Mode = "native";
	internal const string SystemName = "CodeBrix Café ✓";
	internal static string OriginalName = "";
	[STAThread]
	private static void Main(string[] args)
	{
		CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
		CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));
		CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans-Regular.ttf";
		Mode = args.FirstOrDefault() ?? "native";
		OriginalName = Native.ProcessName;
		if (Mode == "validation")
		{
			foreach (string? invalid in new string?[] { null, "", " \t\r\n", "Name\0Suffix" })
			{
				try { new MacOSHostBuilder().UseSystemAppName(invalid!); }
				catch (ArgumentException error) when (error.ParamName == "name") { continue; }
				throw new InvalidOperationException("Invalid application name was accepted");
			}
			var mac = new MacOSHostBuilder();
			if (!ReferenceEquals(mac, mac.UseSystemAppName(SystemName)) || Native.ProcessName != OriginalName)
				throw new InvalidOperationException("Configuration must be fluent and have no process-wide effect before startup");
			Console.WriteLine("PASS: invalid names rejected; configuration is fluent and deferred until startup");
			return;
		}
		var builder = CodeBrixPlatformHostBuilder.Create().App(() => new ProbeApp());
		if (Mode == "default") { builder.UseMacOS(); }
		else if (Mode == "name") { builder.UseMacOS(mac => mac.UseSystemAppName(SystemName)); }
		else if (Mode == "name-menu") { builder.UseMacOS(mac => mac.UseSystemAppName(SystemName).UseSystemMenuBar()); }
		else if (Mode == "name-menu-reversed") { builder.UseMacOS(mac => mac.UseSystemMenuBar().UseSystemAppName(SystemName)); }
		else { builder.UseMacOS(mac => mac.UseSystemMenuBar()); }
		if (Mode != "composition") { builder.UseDirectSkiaCanvasMode(); }
		builder.Build().Run();
	}
}

internal sealed class ProbeApp : Application
{
	public ProbeApp() => Resources.MergedDictionaries.Add(new XamlControlsResources());
	private Window _window = null!;
	private int _checks;
	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		_window = new Window { Title = "CodeBrix native menu regression probe" };
		var panel = new StackPanel();
		var hidden = MakeBar("Hidden"); hidden.Visibility = Visibility.Collapsed;
		var first = MakeBar("First");
		var second = MakeBar("Second");
		var status = new TextBlock { Text = "Native menu checks running…" };
		panel.Children.Add(hidden); panel.Children.Add(first); panel.Children.Add(second); panel.Children.Add(status);
		_window.Content = panel;
		_window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });
		_window.Activate();
		var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		timer.Tick += async (_, _) =>
		{
			timer.Stop();
			try
			{
				await Run(panel, hidden, first, second, status);
				Console.WriteLine($"PASS: {_checks} checks ({Program.Mode})");
				if (Program.Mode == "close") { _window.Close(); }
				else { Exit(); }
			}
			catch (Exception error)
			{
				Console.Error.WriteLine(error);
				Environment.Exit(1);
			}
		};
		timer.Start();
	}

	private static MenuBar MakeBar(string title)
	{
		var bar = new MenuBar();
		var menu = new MenuBarItem { Title = title };
		menu.Items.Add(new MenuFlyoutItem { Text = "Original" });
		bar.Items.Add(menu);
		return bar;
	}

	private void Check(bool condition, string name)
	{
		if (!condition) { throw new InvalidOperationException("FAIL: " + name); }
		_checks++;
		Console.WriteLine("PASS: " + name);
	}

	private static Task Settle() => Task.Delay(150);

	private async Task Run(StackPanel panel, MenuBar hidden, MenuBar first, MenuBar second, TextBlock status)
	{
		Check(Native.ProcessName == (Program.Mode.StartsWith("name") ? Program.SystemName : Program.OriginalName),
			"Cocoa application name respects the explicit override or existing default");
		Check(typeof(Program).Assembly.GetName().Name == "MacOSMenuBarProbe", "application naming leaves assembly identity unchanged");
		if (Program.Mode.StartsWith("name"))
		{
			Check(Native.BundleName == Program.SystemName, "AppKit's bundle name receives the Unicode override");
			Check(Native.RunningApplicationName == Program.SystemName, "macOS running-application identity receives the override");
		}
		if (Program.Mode is "default" or "name")
		{
			Check(first.DesiredSize.Height > 0 && second.DesiredSize.Height > 0, "default keeps both bars in the window");
			Check(Native.Find(Native.MainMenu, "First") == 0, "default does not install system menus");
			return;
		}
		Native.ActivateWindow(_window.Title);
		if (Program.Mode.StartsWith("name"))
		{
			var applicationItem = Native.Send(Native.MainMenu, "itemAtIndex:", 0);
			Check(Native.String(Native.Send(applicationItem, "title")) == Program.SystemName
				&& Native.Find(Native.Submenu(applicationItem), "Hide " + Program.SystemName) != 0,
				"application menu and Hide action use the explicit system name");
			Check(Native.String(Native.Send(applicationItem, "accessibilityTitle")) == Program.SystemName,
				"actual AppKit application-menu title uses the override");
		}
		Check(first.DesiredSize.Height == 0, "first visible bar has zero layout height");
		Check(second.DesiredSize.Height > 0, "second bar retains its normal height");
		Check(status.TransformToVisual(panel).TransformPoint(default).Y == second.ActualHeight, "no empty menu row remains");
		Check(Native.Find(Native.MainMenu, "First") != 0 && Native.Find(Native.MainMenu, "Second") == 0, "only first visible bar is projected");
		Check(!first.Items[0].Focus(FocusState.Programmatic), "projected menu cannot take in-window keyboard focus");
		var hitTest = typeof(UIElement).GetMethod("GetHitTestVisibility", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
		Check(hitTest.Invoke(first, null)!.ToString() == "Collapsed", "projected menu cannot intercept in-window pointer input");
		first.Height = 55; first.Margin = new Thickness(7);
		await Settle();
		Check(first.DesiredSize.Height == 0 && first.Height == 55 && first.Margin.Top == 7, "explicit height and margin collapse without overwriting properties");
		first.Visibility = Visibility.Collapsed;
		await Settle();
		Check(second.DesiredSize.Height == 0 && Native.Find(Native.MainMenu, "Second") != 0, "hiding first bar promotes second");
		first.Visibility = Visibility.Visible;
		await Settle();
		Check(first.DesiredSize.Height == 0 && second.DesiredSize.Height > 0, "showing first bar restores visual order");
		hidden.Visibility = Visibility.Visible;
		await Settle();
		Check(hidden.DesiredSize.Height == 0 && first.DesiredSize.Height > 0 && Native.Find(Native.MainMenu, "Hidden") != 0, "formerly hidden preceding bar takes priority");
		panel.Children.Remove(hidden);
		await Settle();
		Check(Native.Find(Native.MainMenu, "First") != 0, "removing selected bar promotes next");

		int clicks = 0, executes = 0, toggles = 0, opens = 0;
		var command = new ProbeCommand(() => executes++);
		var item = new MenuFlyoutItem { Text = "Run", Command = command, CommandParameter = "parameter" };
		item.Click += (_, _) => clicks++;
		item.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.R, Modifiers = VirtualKeyModifiers.Control });
		var toggle = new ToggleMenuFlyoutItem { Text = "Toggle" };
		toggle.Click += (_, _) => toggles++;
		var dynamic = new MenuFlyoutSubItem { Text = "Dynamic" };
		dynamic.Loaded += (_, _) => { opens++; dynamic.Items.Clear(); dynamic.Items.Add(new MenuFlyoutItem { Text = "Generation " + opens }); };
		var top = first.Items[0];
		top.Items.Add(new MenuFlyoutSeparator()); top.Items.Add(item); top.Items.Add(toggle); top.Items.Add(dynamic);
		top.Items.Add(new MenuFlyoutItem { Text = null! });
		await Settle();
		var menu = Native.Submenu(Native.Find(Native.MainMenu, "First"));
		Native.Open(menu);
		Check(Native.Find(menu, "Run") != 0 && Native.Find(Native.Submenu(Native.Find(menu, "Dynamic")), "Generation 1") != 0, "Loaded populates dynamic submenu");
		Check(Native.Find(menu, "") != 0, "null menu labels are accepted");
		Check(Native.String(Native.Send(Native.Find(menu, "Run"), "keyEquivalent")) == "r", "keyboard equivalent exported");
		Native.Perform(menu, "Run"); Native.Close(menu);
		await Settle();
		Check(clicks == 1 && executes == 1 && command.LastParameter as string == "parameter", "native action invokes Click and Command once with parameter");
		menu = Native.Submenu(Native.Find(Native.MainMenu, "First"));
		Native.Open(menu);
		Check(Native.Find(Native.Submenu(Native.Find(menu, "Dynamic")), "Generation 2") != 0, "dynamic submenu refreshes on reopening");
		Native.Perform(menu, "Toggle"); Native.Close(menu);
		await Settle();
		Check(toggle.IsChecked && toggles == 1, "native toggle uses managed toggle behavior");
		menu = Native.Submenu(Native.Find(Native.MainMenu, "First"));
		Check(Native.Send(Native.Find(menu, "Toggle"), "state") == 1, "check mark follows managed state");
		command.Enabled = false;
		await Settle();
		menu = Native.Submenu(Native.Find(Native.MainMenu, "First"));
		Check(Native.Send(Native.Find(menu, "Run"), "isEnabled") == 0, "CanExecute updates native enabled state");
		item.Text = "Renamed"; toggle.Visibility = Visibility.Collapsed;
		await Settle();
		menu = Native.Submenu(Native.Find(Native.MainMenu, "First"));
		Check(Native.Find(menu, "Renamed") != 0 && Native.Find(menu, "Run") == 0 && Native.Find(menu, "Toggle") == 0, "text and visibility update");
		panel.Children.Remove(first);
		await Settle();
		Check(Native.Find(Native.MainMenu, "Second") != 0, "unload releases projection and promotes next");
		panel.Children.Insert(0, first);
		await Settle();
		Check(Native.Find(Native.MainMenu, "First") != 0, "reloaded bar regains projection");
		panel.Children.Remove(first);
		var wrapper = new Border { Child = first };
		panel.Children.Insert(0, wrapper);
		await Settle();
		wrapper.Visibility = Visibility.Collapsed;
		await Settle();
		Check(Native.Find(Native.MainMenu, "Second") != 0, "collapsed ancestor excludes a preceding menu bar");
		wrapper.Visibility = Visibility.Visible;
		await Settle();
		Check(Native.Find(Native.MainMenu, "First") != 0, "showing ancestor restores the first bar");
		var dialog = new ContentDialog { Title = "Modal check", Content = "Menu commands should be disabled.", CloseButtonText = "Close", XamlRoot = first.XamlRoot };
		var pendingDialog = dialog.ShowAsync();
		await Settle();
		Check(Native.Send(Native.Find(Native.MainMenu, "First"), "isEnabled") == 0, "content dialog disables underlying native menu");
		dialog.Hide();
		await pendingDialog;
		await Settle();
		Check(Native.Send(Native.Find(Native.MainMenu, "First"), "isEnabled") != 0, "closing dialog restores native menu");
		var other = new Window { Title = "Second menu window", Content = MakeBar("Other window") };
		other.Activate();
		Native.ActivateWindow("Second menu window");
		await Settle();
		Check(Native.Find(Native.MainMenu, "Other window") != 0, "second window owns the system menu when active");
		Native.ActivateWindow(_window.Title);
		await Settle();
		Check(Native.Find(Native.MainMenu, "First") != 0, "switching windows restores each window's menu");
		var services = Native.Submenu(Native.Find(Native.Submenu(Native.Send(Native.MainMenu, "itemAtIndex:", 0)), "Services"));
		Check(services != 0 && services == Native.ServicesMenu, "Services belongs to the active window's application menu");
		other.Close();
		await Settle();
		Check(Native.Find(Native.MainMenu, "First") != 0, "closing another window keeps the remaining menu");

		var help = new MenuBarItem { Title = "Help" };
		int helpClicks = 0;
		var about = new MenuFlyoutItem { Text = "About Probe" };
		about.Click += (_, _) => helpClicks++;
		help.Items.Add(about);
		first.Items.Add(help);
		await Settle();
		var nativeHelp = Native.Submenu(Native.Find(Native.MainMenu, "Help"));
		var nativeAbout = Native.Find(nativeHelp, "About Probe");
		Check(Native.HelpMenu != 0 && Native.HelpMenu != nativeHelp && Native.Send(Native.HelpMenu, "supermenu") == 0,
			"AppKit automatic Help search is suppressed using an unlisted menu");
		for (int i = 1; i <= 3; i++)
		{
			// This invokes real menu-bar tracking, including AppKit's special Help
			// handling. Calling menuNeedsUpdate manually missed the original bug.
			int tracking = Native.PressMenu(Native.MainMenu, "Help");
			Check(tracking == 7, $"Help opens, stays open and dismisses (pass {i}, tracking={tracking})");
			await Settle();
			Check(nativeHelp == Native.Submenu(Native.Find(Native.MainMenu, "Help"))
				&& nativeAbout == Native.Find(nativeHelp, about.Text), "Help objects survive opening and full refresh");
			about.Text = "About Probe " + i;
			await Settle();
		}
		Native.Open(nativeHelp); Native.Perform(nativeHelp, about.Text); Native.Close(nativeHelp);
		await Settle();
		Check(helpClicks == 1 && Native.Send(nativeHelp, "numberOfItems") == 1, "Help contains only the application item and invokes it once");
		// Emulate an AppKit-owned item to guard against deleting native additions
		// (e.g. Services/window entries) during an otherwise unrelated refresh.
		var systemItem = Native.Send(Native.Class("NSMenuItem"), "separatorItem");
		Native.Send(nativeHelp, "addItem:", systemItem);
		about.Text = "About after refresh";
		await Settle();
		Check(Native.Send(systemItem, "menu") == nativeHelp, "refresh preserves items owned by AppKit");
		Native.Send(nativeHelp, "removeItem:", systemItem);
		help.Items.Add(new MenuFlyoutItem { Text = "Second Help item" });
		await Settle();
		help.Items.Remove(about); help.Items.Add(about);
		await Settle();
		Check(Native.String(Native.Send(Native.Send(nativeHelp, "itemAtIndex:", 0), "title")) == "Second Help item"
			&& Native.String(Native.Send(Native.Send(nativeHelp, "itemAtIndex:", 1), "title")) == about.Text,
			"native reconciliation follows managed item reordering");
		first.Items.Remove(help);
		await Settle();
		Check(Native.Find(Native.MainMenu, "Help") == 0, "removed Help menu is released from the menu bar");
		var unlistedHelp = Native.HelpMenu;
		first.Visibility = Visibility.Collapsed; second.Visibility = Visibility.Collapsed;
		await Settle();
		Check(Native.HelpMenu != unlistedHelp, "deactivating projection restores the host's Help menu policy");
		first.Visibility = Visibility.Visible;
		await Settle();
		Check(Native.HelpMenu == unlistedHelp, "reactivating projection suppresses automatic Help search again");
	}
}

internal sealed class ProbeCommand(Action action) : ICommand
{
	private bool _enabled = true;
	public bool Enabled { get => _enabled; set { _enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
	public object? LastParameter { get; private set; }
	public event EventHandler? CanExecuteChanged;
	public bool CanExecute(object? parameter) => Enabled;
	public void Execute(object? parameter) { LastParameter = parameter; action(); }
}

internal static class Native
{
	[DllImport("MenuTrackingProbe", EntryPoint = "codebrix_probe_press_menu")]
	internal static extern int PressMenu(nint parent, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
	[DllImport("/usr/lib/libobjc.A.dylib")] private static extern nint objc_getClass(string name);
	[DllImport("/usr/lib/libobjc.A.dylib")] private static extern nint sel_registerName(string name);
	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static extern nint Send0(nint target, nint selector);
	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static extern nint Send1(nint target, nint selector, nint argument);
	internal static nint Send(nint target, string selector) => Send0(target, sel_registerName(selector));
	internal static nint Send(nint target, string selector, nint argument) => Send1(target, sel_registerName(selector), argument);
	internal static nint MainMenu => Send(Send(objc_getClass("NSApplication"), "sharedApplication"), "mainMenu");
	internal static nint ServicesMenu => Send(Send(objc_getClass("NSApplication"), "sharedApplication"), "servicesMenu");
	internal static nint HelpMenu => Send(Send(objc_getClass("NSApplication"), "sharedApplication"), "helpMenu");
	internal static nint Class(string name) => objc_getClass(name);
	internal static string ProcessName => String(Send(Send(Class("NSProcessInfo"), "processInfo"), "processName"));
	internal static string RunningApplicationName => String(Send(Send(Class("NSRunningApplication"), "currentApplication"), "localizedName"));
	internal static string BundleName => String(Send(Send(Class("NSBundle"), "mainBundle"), "objectForInfoDictionaryKey:", NSString("CFBundleName")));
	private static nint NSString(string value)
	{
		var utf8 = Marshal.StringToCoTaskMemUTF8(value);
		try { return Send(Class("NSString"), "stringWithUTF8String:", utf8); }
		finally { Marshal.FreeCoTaskMem(utf8); }
	}
	internal static void ActivateWindow(string title)
	{
		var app = Send(objc_getClass("NSApplication"), "sharedApplication");
		Send(app, "activateIgnoringOtherApps:", 1);
		var windows = Send(app, "windows");
		for (nint i = 0; i < Send(windows, "count"); i++)
		{
			var window = Send(windows, "objectAtIndex:", i);
			if (String(Send(window, "title")) == title) { Send(window, "makeKeyAndOrderFront:", 0); return; }
		}
		throw new InvalidOperationException("Missing window " + title);
	}
	internal static string String(nint value) => Marshal.PtrToStringUTF8(Send(value, "UTF8String")) ?? "";
	internal static nint Submenu(nint item) => Send(item, "submenu");
	internal static nint Find(nint menu, string title)
	{
		for (nint i = 0; i < Send(menu, "numberOfItems"); i++)
		{
			var item = Send(menu, "itemAtIndex:", i);
			if (String(Send(item, "title")) == title) { return item; }
		}
		return 0;
	}
	internal static void Open(nint menu)
	{
		Send(Send(menu, "delegate"), "menuWillOpen:", menu);
		Send(Send(menu, "delegate"), "menuNeedsUpdate:", menu);
	}
	internal static void Close(nint menu) => Send(Send(menu, "delegate"), "menuDidClose:", menu);
	internal static void Perform(nint menu, string title)
	{
		var item = Find(menu, title);
		if (item == 0) { throw new InvalidOperationException("Missing native item " + title); }
		Send(menu, "performActionForItemAtIndex:", Send(menu, "indexOfItem:", item));
	}
}
