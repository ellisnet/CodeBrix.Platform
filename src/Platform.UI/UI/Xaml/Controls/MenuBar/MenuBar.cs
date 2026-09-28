using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using CodeBrix.Platform.UI.Helpers.WinUI;
using CodeBrix.Platform.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuBar : Control
	{
		private Grid m_layoutRoot;
		private ItemsControl m_contentRoot;

		public IList<MenuBarItem> Items
		{
			get => (IList<MenuBarItem>)this.GetValue(ItemsProperty);
			private set => this.SetValue(ItemsProperty, value);
		}

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuBarItem>),
				typeof(MenuBar),
				new FrameworkPropertyMetadata(null)
		);

		// The menu bars currently loaded in any visual tree (weakly held). While a menu is closed its items
		// are not in the visual tree, so their KeyboardAccelerators never join the content root's live
		// accelerator list; the application-wide accelerator pass asks the loaded menu bars instead
		// (see TryInvokeMenuItemAccelerator), which makes menu shortcuts work app-wide as in WinUI.
		private static readonly List<WeakReference<MenuBar>> _loadedMenuBars = new();

		public MenuBar() : base()
		{
			Items = new ObservableCollection<MenuBarItem>();

			this.SetDefaultStyleKey();

			Loaded += (_, _) => RegisterLoaded(this);
			Unloaded += (_, _) => UnregisterLoaded(this);

			// handledEventsToo: record where focus was before it entered the menus, whoever moved it
			AddHandler(GettingFocusEvent, new TypedEventHandler<UIElement, GettingFocusEventArgs>(OnGettingFocus), true);
		}

		// Where keyboard focus was before it entered the menu bar (weakly held), so leaving the menu bar
		// (Escape on a title with no menu open) can put it back - on a game surface, say.
		private WeakReference<UIElement> _focusBeforeMenuBar;

		private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			// Focus moving within the menu bar, or out of one of its open menus, is not "entering" it.
			if (args.OldFocusedElement is UIElement oldElement && !IsInMenuBarOrMenu(oldElement))
			{
				_focusBeforeMenuBar = new WeakReference<UIElement>(oldElement);
			}
		}

		private bool IsInMenuBarOrMenu(DependencyObject element)
		{
			for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
			{
				if (ReferenceEquals(current, this) || current is MenuFlyoutPresenter || current is Popup)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Leaves the menu bar: keyboard focus goes back to the element that had it before the menus were
		/// entered, or - when that element is gone - to the next focusable element outside the menu bar.
		/// </summary>
		/// <returns>True when focus left the menu bar.</returns>
		internal bool LeaveMenuBar()
		{
			ResetHighlights();

			if (_focusBeforeMenuBar != null &&
				_focusBeforeMenuBar.TryGetTarget(out var target) &&
				target.XamlRoot == XamlRoot &&
				target.Focus(FocusState.Programmatic))
			{
				return true;
			}

			if (XamlRoot?.Content is { } content &&
				FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new FindNextElementOptions { SearchRoot = content }))
			{
				return FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focused || !IsInMenuBarOrMenu(focused);
			}

			return false;
		}

		private void CloseOpenMenus()
		{
			foreach (var item in Items)
			{
				if (item.IsMenuReallyOpen())
				{
					item.CloseMenuFlyout();
				}
			}
		}

		private void ResetHighlights()
		{
			foreach (var item in Items)
			{
				item.ResetHighlight();
			}

			IsFlyoutOpen = false;
		}

		private bool IsInMenuBar(DependencyObject element)
		{
			for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
			{
				if (ReferenceEquals(current, this))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Called when an Escape key press reached the window root unhandled (nothing focused used it). For every
		/// loaded menu bar under <paramref name="xamlRoot"/> with no menu actually open, clears every title's
		/// highlight and - when keyboard focus is still on the menu bar - leaves it, so an Escape never leaves a
		/// menu bar looking active when no menu is open.
		/// </summary>
		/// <returns>True when focus left a menu bar.</returns>
		internal static bool OnUnhandledEscape(XamlRoot xamlRoot)
		{
			if (_loadedMenuBars.Count == 0 || xamlRoot is null)
			{
				return false;
			}

			var focusLeft = false;
			foreach (var entry in _loadedMenuBars.ToArray())
			{
				if (!entry.TryGetTarget(out var menuBar) || !menuBar.IsLoaded || menuBar.XamlRoot != xamlRoot)
				{
					continue;
				}

				var anyMenuOpen = false;
				foreach (var item in menuBar.Items)
				{
					anyMenuOpen |= item.IsMenuReallyOpen();
				}

				if (anyMenuOpen)
				{
					continue;
				}

				if (FocusManager.GetFocusedElement(xamlRoot) is DependencyObject focused && menuBar.IsInMenuBar(focused))
				{
					focusLeft |= menuBar.LeaveMenuBar();
				}
				else
				{
					menuBar.ResetHighlights();
				}
			}

			return focusLeft;
		}

		private static void RegisterLoaded(MenuBar menuBar)
		{
			UnregisterLoaded(menuBar);
			_loadedMenuBars.Add(new WeakReference<MenuBar>(menuBar));
		}

		private static void UnregisterLoaded(MenuBar menuBar) =>
			_loadedMenuBars.RemoveAll(entry => !entry.TryGetTarget(out var target) || ReferenceEquals(target, menuBar));

		/// <summary>
		/// Forgets every registered menu bar. The unit-test host has no real visual tree, so menu bars removed
		/// by an earlier test stay "loaded" there; tests call this to start from an empty registry.
		/// </summary>
		internal static void ResetForTests() => _loadedMenuBars.Clear();

		/// <summary>
		/// Offers a key press that nothing else handled to the KeyboardAccelerators of the menu items of every
		/// loaded, visible and enabled menu bar under <paramref name="xamlRoot"/> (including submenu items),
		/// whether or not the item's menu is open. Disabled or collapsed items, menus and menu bars are skipped,
		/// as are accelerators with a ScopeOwner. The first match is invoked (its Invoked event, then the item's
		/// default action - Click / Command, or the toggle of a ToggleMenuFlyoutItem).
		/// </summary>
		/// <returns>True when a matching accelerator was found and invoked.</returns>
		internal static bool TryInvokeMenuItemAccelerator(XamlRoot xamlRoot, VirtualKey key, VirtualKeyModifiers modifiers)
		{
			if (_loadedMenuBars.Count == 0 || xamlRoot is null)
			{
				return false;
			}

			foreach (var entry in _loadedMenuBars.ToArray())
			{
				// IsLoaded is re-checked here, so a menu bar whose Unloaded was missed never fires.
				if (!entry.TryGetTarget(out var menuBar) ||
					!menuBar.IsLoaded ||
					menuBar.XamlRoot != xamlRoot ||
					!menuBar.IsEnabled ||
					!FocusProperties.IsVisible(menuBar) ||
					!FocusProperties.AreAllAncestorsVisible(menuBar))
				{
					continue;
				}

				foreach (var menuBarItem in menuBar.Items)
				{
					if (menuBarItem.IsEnabled &&
						menuBarItem.Visibility == Visibility.Visible &&
						TryInvokeAccelerator(menuBar, menuBarItem.Items, key, modifiers))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool TryInvokeAccelerator(MenuBar menuBar, IList<MenuFlyoutItemBase> items, VirtualKey key, VirtualKeyModifiers modifiers)
		{
			foreach (var item in items)
			{
				if (!item.IsEnabled || item.Visibility != Visibility.Visible)
				{
					continue;
				}

				if (item is MenuFlyoutSubItem subItem)
				{
					if (TryInvokeAccelerator(menuBar, subItem.Items, key, modifiers))
					{
						return true;
					}

					continue;
				}

				foreach (var accelerator in item.KeyboardAccelerators)
				{
					if (accelerator.IsEnabled &&
						accelerator.ScopeOwner is null &&
						accelerator.Key == key &&
						accelerator.Modifiers == modifiers)
					{
						// A menu of this bar may be open (the shortcut of an item in another menu was pressed):
						// running a command closes the menus, as a click on an item does.
						menuBar.CloseOpenMenus();

						// Found it - even if nothing handles the invoke, no other accelerator is looked for.
						return KeyboardAcceleratorUtility.RaiseKeyboardAcceleratorInvoked(accelerator, item);
					}
				}
			}

			return false;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			m_layoutRoot = GetTemplateChild("LayoutRoot") as Grid;

			if (GetTemplateChild("ContentRoot") is ItemsControl contentRoot)
			{
				contentRoot.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

				contentRoot.ItemsSource = Items;

				m_contentRoot = contentRoot;
			}
		}

		internal bool IsFlyoutOpen { get; set; }
	}
}
