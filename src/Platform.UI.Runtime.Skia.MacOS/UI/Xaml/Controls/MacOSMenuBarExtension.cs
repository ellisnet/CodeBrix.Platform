using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Collections;
using Windows.System;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS;

internal sealed partial class MacOSMenuBarExtension(MenuBar bar) : INativeMenuBarExtension
{
	private static readonly Dictionary<long, MacOSMenuBarExtension> Instances = new();
	private static readonly Dictionary<MenuBar, MacOSMenuBarExtension> Candidates = new();
	private static bool _selecting;
	private static long _nextInstance;
	private readonly Dictionary<FrameworkElement, long> _ids = new();
	private readonly Dictionary<long, FrameworkElement> _elements = new();
	private readonly HashSet<FrameworkElement> _loaded = new();
	private readonly Dictionary<FrameworkElement, FrameworkElement> _parented = new();
	private readonly HashSet<long> _openMenus = new();
	private readonly List<Action> _unsubscribe = new();
	private long _nextId;
	private long _id;
	private nint _native;
	private MacOSWindowHost? _host;
	private bool _refreshQueued;
	private bool _refreshing;
	private bool _blockedByDialog;

	internal static unsafe void Register()
	{
		NativeMenus.codebrix_menu_set_callback(&OnNativeEvent);
		ApiExtensibility.Register<MenuBar>(typeof(INativeMenuBarExtension), owner => new MacOSMenuBarExtension(owner));
	}

	public void Load()
	{
		if (_host is not null || bar.XamlRoot is not { } root || XamlRootMap.GetHostForRoot(root) is not MacOSWindowHost host)
		{
			return;
		}
		_host = host;
		Candidates.Add(bar, this);
		host.Window.Closed += OnWindowClosed;
		bar.LayoutUpdated += OnLayoutUpdated;
		Select(host);
	}

	private void OnLayoutUpdated(object? sender, object args)
	{
		if (_host is { } host) { Select(host); }
		if (_native != 0 && HasOpenDialog() != _blockedByDialog) { RequestRefresh(); }
	}

	private bool HasOpenDialog() => bar.XamlRoot is { } root
		&& VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Any(popup => popup.Child is ContentDialog);

	private static void Select(MacOSWindowHost host)
	{
		if (_selecting) { return; }
		_selecting = true;
		try
		{
			MacOSMenuBarExtension? Find(DependencyObject? element)
			{
				if (element is null || element is UIElement { Visibility: not Visibility.Visible }) { return null; }
				if (element is MenuBar candidate && candidate.IsLoaded && Candidates.TryGetValue(candidate, out var menu)) { return menu; }
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
				{
					if (Find(VisualTreeHelper.GetChild(element, i)) is { } found) { return found; }
				}
				return null;
			}
			var selected = Find(host.RootElement);
			foreach (var candidate in Candidates.Values.Where(menu => menu._host == host).ToArray())
			{
				if (candidate != selected) { candidate.Deactivate(); }
			}
			selected?.Activate();
		}
		finally { _selecting = false; }
	}

	private void Activate()
	{
		if (_native != 0 || _host is not { } host) { return; }
		_id = ++_nextInstance;
		Instances.Add(_id, this);
		_native = NativeMenus.codebrix_menu_create(host.NativeHandle, _id,
			MacSkiaHost.Current.SystemAppName ?? Windows.ApplicationModel.Package.Current.DisplayName);
		bar.SetNativePresentation(true);
		Refresh();
	}

	private void OnWindowClosed(object sender, WindowEventArgs args) => Unload();

	public void Unload()
	{
		var host = _host;
		if (host is null) { return; }
		host.Window.Closed -= OnWindowClosed;
		bar.LayoutUpdated -= OnLayoutUpdated;
		Candidates.Remove(bar);
		Deactivate();
		_host = null;
		Select(host);
	}

	private void Deactivate()
	{
		if (_native == 0) { return; }
		Instances.Remove(_id);
		NativeMenus.codebrix_menu_destroy(_native);
		_native = 0;
		ClearSubscriptions();
		foreach (var element in _loaded.ToArray()) { UnloadElement(element); }
		foreach (var element in _parented.Keys.ToArray()) { ReleaseParent(element); }
		_parented.Clear();
		_ids.Clear();
		_elements.Clear();
		_openMenus.Clear();
		bar.SetNativePresentation(false);
	}

	private void ReleaseParent(FrameworkElement element)
	{
		if (_parented.Remove(element, out var parent) && ReferenceEquals(element.GetParent(), parent))
		{
			element.SetParent(null);
			element.VisualTreeCache = null;
		}
	}

	private void RequestRefresh()
	{
		if (_native == 0 || _refreshQueued || _refreshing) { return; }
		_refreshQueued = true;
		bar.DispatcherQueue.TryEnqueue(() =>
		{
			_refreshQueued = false;
			if (_native != 0 && _openMenus.Count == 0) { Refresh(); }
		});
	}

	private static IEnumerable<FrameworkElement> Children(FrameworkElement element) => element switch
	{
		MenuBar menu => menu.Items,
		MenuBarItem menu => menu.Items,
		MenuFlyoutSubItem menu => menu.Items,
		_ => Array.Empty<FrameworkElement>(),
	};

	private void ClearSubscriptions()
	{
		foreach (var unsubscribe in _unsubscribe) { unsubscribe(); }
		_unsubscribe.Clear();
	}

	private void Watch(DependencyObject element, DependencyProperty property)
	{
		var token = element.RegisterPropertyChangedCallback(property, (_, _) => RequestRefresh());
		_unsubscribe.Add(() => element.UnregisterPropertyChangedCallback(property, token));
	}

	private void WatchCollection(object collection)
	{
		if (collection is INotifyCollectionChanged observable)
		{
			NotifyCollectionChangedEventHandler changed = (_, _) => RequestRefresh();
			observable.CollectionChanged += changed;
			_unsubscribe.Add(() => observable.CollectionChanged -= changed);
		}
		else if (collection is IObservableVector<MenuFlyoutItemBase> vector)
		{
			VectorChangedEventHandler<MenuFlyoutItemBase> changed = (_, _) => RequestRefresh();
			vector.VectorChanged += changed;
			_unsubscribe.Add(() => vector.VectorChanged -= changed);
		}
	}

	private void WatchTree(FrameworkElement element, HashSet<FrameworkElement> live)
	{
		live.Add(element);
		Watch(element, UIElement.VisibilityProperty);
		Watch(element, Control.IsEnabledProperty);
		switch (element)
		{
			case MenuBar menu: WatchCollection(menu.Items); break;
			case MenuBarItem menu:
				Watch(menu, MenuBarItem.TitleProperty);
				WatchCollection(menu.Items);
				break;
			case MenuFlyoutSubItem menu:
				Watch(menu, MenuFlyoutSubItem.TextProperty);
				WatchCollection(menu.Items);
				break;
			case MenuFlyoutItem item:
				Watch(item, MenuFlyoutItem.TextProperty);
				Watch(item, MenuFlyoutItem.CommandProperty);
				Watch(item, MenuFlyoutItem.CommandParameterProperty);
				if (item.Command is { } command)
				{
					EventHandler changed = (_, _) => RequestRefresh();
					command.CanExecuteChanged += changed;
					_unsubscribe.Add(() => command.CanExecuteChanged -= changed);
				}
				if (item is ToggleMenuFlyoutItem) { Watch(item, ToggleMenuFlyoutItem.IsCheckedProperty); }
				WatchCollection(item.KeyboardAccelerators);
				foreach (var accelerator in item.KeyboardAccelerators)
				{
					Watch(accelerator, KeyboardAccelerator.KeyProperty);
					Watch(accelerator, KeyboardAccelerator.ModifiersProperty);
					Watch(accelerator, KeyboardAccelerator.IsEnabledProperty);
				}
				break;
		}
		foreach (var child in Children(element).ToArray())
		{
			if (child.GetParent() is null)
			{
				child.SetParent(element);
				child.VisualTreeCache = bar.XamlRoot?.VisualTree;
				_parented[child] = element;
			}
			WatchTree(child, live);
		}
	}

	private void Refresh(long menuId = 0)
	{
		if (_native == 0 || _refreshing) { return; }
		_refreshing = true;
		try
		{
			ClearSubscriptions();
			var live = new HashSet<FrameworkElement>();
			WatchTree(bar, live);
			for (var parent = VisualTreeHelper.GetParent(bar); parent is not null; parent = VisualTreeHelper.GetParent(parent))
			{
				Watch(parent, UIElement.VisibilityProperty);
				if (parent is Control) { Watch(parent, Control.IsEnabledProperty); }
			}
			foreach (var removed in _ids.Keys.Where(element => !live.Contains(element)).ToArray())
			{
				UnloadElement(removed);
				ReleaseParent(removed);
				_elements.Remove(_ids[removed]);
				_ids.Remove(removed);
			}
			foreach (var removed in _parented.Keys.Where(element => !live.Contains(element)).ToArray()) { ReleaseParent(removed); }
			var owner = menuId == 0 ? bar : _elements.GetValueOrDefault(menuId);
			if (owner is null) { return; }
			_blockedByDialog = HasOpenDialog();
			var entries = BuildEntries(owner, !_blockedByDialog && IsAvailable(bar, checkEnabled: true));
			NativeMenus.codebrix_menu_set_items(_native, menuId, JsonSerializer.Serialize(entries));
			NativeMenus.codebrix_menu_set_visible(_native, IsAvailable(bar, checkEnabled: false) ? 1 : 0);
		}
		finally { _refreshing = false; }
	}

	private long Id(FrameworkElement element)
	{
		if (!_ids.TryGetValue(element, out var id))
		{
			_ids.Add(element, id = ++_nextId);
			_elements.Add(id, element);
		}
		return id;
	}

	private List<MenuEntry> BuildEntries(FrameworkElement owner, bool parentEnabled)
	{
		var entries = new List<MenuEntry>();
		foreach (var element in Children(owner).ToArray())
		{
			if (element.Visibility != Visibility.Visible) { continue; }
			var enabled = parentEnabled && (element is not Control control || control.IsEnabled);
			var command = element as MenuFlyoutItem;
			enabled &= command?.Command?.CanExecute(command.CommandParameter) ?? true;
			var accelerator = command?.KeyboardAccelerators.FirstOrDefault(a => a.IsEnabled && a.ScopeOwner is null);
			entries.Add(new MenuEntry(
				Id(element),
				(element switch { MenuBarItem top => top.Title, MenuFlyoutSubItem sub => sub.Text, MenuFlyoutItem item => item.Text, _ => "" }) ?? "",
				element is MenuFlyoutSeparator, enabled, element is ToggleMenuFlyoutItem { IsChecked: true },
				accelerator is null ? "" : KeyEquivalent(accelerator.Key),
				(int)(accelerator?.Modifiers ?? VirtualKeyModifiers.None),
				element is MenuBarItem or MenuFlyoutSubItem ? BuildEntries(element, enabled) : null));
		}
		return entries;
	}

	private sealed record MenuEntry(long Id, string Title, bool Separator, bool Enabled, bool Checked, string Key, int Modifiers, List<MenuEntry>? Children);

	internal static string KeyEquivalent(VirtualKey key) => key switch
	{
		>= VirtualKey.A and <= VirtualKey.Z => ((char)((int)key + 32)).ToString(),
		>= VirtualKey.Number0 and <= VirtualKey.Number9 => ((char)key).ToString(),
		>= VirtualKey.F1 and <= VirtualKey.F24 => ((char)(0xf704 + key - VirtualKey.F1)).ToString(),
		VirtualKey.Enter => "\r", VirtualKey.Tab => "\t", VirtualKey.Space => " ",
		VirtualKey.Escape => "\u001b", VirtualKey.Back => "\b", VirtualKey.Delete => "\uf728",
		VirtualKey.Up => "\uf700", VirtualKey.Down => "\uf701", VirtualKey.Left => "\uf702", VirtualKey.Right => "\uf703",
		VirtualKey.Home => "\uf729", VirtualKey.End => "\uf72b", VirtualKey.PageUp => "\uf72c", VirtualKey.PageDown => "\uf72d",
		_ => "",
	};

	private static bool IsAvailable(FrameworkElement element, bool checkEnabled)
	{
		for (DependencyObject? current = element; current is not null; current = current.GetParent() as DependencyObject)
		{
			if (current is UIElement { Visibility: not Visibility.Visible } || checkEnabled && current is Control { IsEnabled: false }) { return false; }
		}
		return true;
	}

	private void LoadElement(FrameworkElement element)
	{
		if (!element.IsLoaded) { _loaded.Add(element); element.RaiseLoaded(); }
	}

	private void UnloadElement(FrameworkElement element)
	{
		if (_loaded.Remove(element)) { element.OnElementUnloaded(); }
	}

	private void Open(long menuId)
	{
		if (!_elements.TryGetValue(menuId, out var owner)) { return; }
		LoadElement(owner);
		foreach (var child in Children(owner).ToArray()) { LoadElement(child); }
		// Loaded handlers may populate dynamic submenus or refresh command state.
		Refresh(menuId);
	}

	private void Close(long menuId)
	{
		if (_elements.TryGetValue(menuId, out var owner))
		{
			void UnloadChildren(FrameworkElement parent)
			{
				foreach (var child in Children(parent).ToArray()) { UnloadChildren(child); UnloadElement(child); }
			}
			UnloadChildren(owner);
			if (owner is MenuBarItem) { UnloadElement(owner); }
		}
		_openMenus.Remove(menuId);
		RequestRefresh();
	}

	private void Invoke(long itemId, bool keyboard)
	{
		if (!_elements.TryGetValue(itemId, out var element) || element is not MenuFlyoutItem item) { return; }
		// AppKit is still unwinding menu tracking. Run dialogs and asynchronous
		// application commands on the normal dispatcher after that has finished.
		bar.DispatcherQueue.TryEnqueue(() =>
		{
			if (_native == 0 || !bar.IsLoaded || HasOpenDialog() || !IsAvailable(bar, true) || !IsAvailable(item, true)
				|| !(item.Command?.CanExecute(item.CommandParameter) ?? true)) { return; }
			if (keyboard && item.KeyboardAccelerators.FirstOrDefault(a => a.IsEnabled && a.ScopeOwner is null) is { } accelerator)
			{
				KeyboardAcceleratorUtility.RaiseKeyboardAcceleratorInvoked(accelerator, item);
			}
			else { item.Invoke(); }
			RequestRefresh();
		});
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeEvent(long instance, long item, int kind)
	{
		try
		{
			if (!Instances.TryGetValue(instance, out var menu)) { return; }
			switch (kind)
			{
				case 0: menu.Open(item); break;
				case 1: menu.Close(item); break;
				case 2: menu.Invoke(item, false); break;
				case 3: menu._openMenus.Add(item); break;
				case 4: menu.Invoke(item, true); break;
			}
		}
		catch (Exception error) { Application.Current.RaiseRecoverableUnhandledException(error); }
	}
}

internal static partial class NativeMenus
{
	private const string Library = "CodeBrixNativeMac";
	[LibraryImport(Library)]
	internal static unsafe partial void codebrix_menu_set_callback(delegate* unmanaged[Cdecl]<long, long, int, void> callback);
	[LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint codebrix_menu_create(nint window, long context, string title);
	[LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void codebrix_menu_set_items(nint menu, long parent, string json);
	[LibraryImport(Library)]
	internal static partial void codebrix_menu_set_visible(nint menu, int visible);
	[LibraryImport(Library)]
	internal static partial void codebrix_menu_destroy(nint menu);
}
