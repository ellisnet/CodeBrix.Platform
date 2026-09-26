#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.System;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.MenuBarTests;

// The application-wide pass that lets a menu item's KeyboardAccelerator fire while its menu is closed
// (MenuBar.TryInvokeMenuItemAccelerator, called from the root KeyDown handler when nothing handled the key).
[TestClass]
public class Given_MenuBar_KeyboardAccelerators
{
	// The unit-test host has no real visual tree (removed elements never unload), so each test starts
	// from an empty menu-bar registry. Unloading itself is covered by MenuBar re-checking IsLoaded.
	[TestInitialize]
	public void Init()
	{
		UnitTestsApp.App.EnsureApplication();
		MenuBar.ResetForTests();
	}

	[TestCleanup]
	public void Cleanup()
	{
		MenuBar.ResetForTests();
		UnitTestsApp.App.EnsureApplication().HostView.Children.Clear();
	}

	[TestMethod]
	public void When_Menu_Is_Closed_Then_Item_Accelerator_Invokes_The_Item()
	{
		//Arrange
		var clicked = 0;
		var item = AddItem(VirtualKey.F1, VirtualKeyModifiers.None, out var root, out _);
		item.Click += (_, _) => clicked++;

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.F1, VirtualKeyModifiers.None);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
	}

	[TestMethod]
	public void When_Accelerator_Is_Invoked_Then_Its_Invoked_Event_Is_Raised_First()
	{
		//Arrange
		var clicked = 0;
		var invoked = 0;
		var item = AddItem(VirtualKey.S, VirtualKeyModifiers.Control, out var root, out _);
		item.Click += (_, _) => clicked++;
		item.KeyboardAccelerators[0].Invoked += (_, args) =>
		{
			invoked++;
			args.Handled = true;
		};

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.S, VirtualKeyModifiers.Control);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
		Assert.AreEqual(0, clicked, "an Invoked handler that marks the event handled replaces the item's Click");
	}

	[TestMethod]
	public void When_Modifiers_Differ_Then_Nothing_Is_Invoked()
	{
		//Arrange
		var clicked = 0;
		var item = AddItem(VirtualKey.S, VirtualKeyModifiers.Control, out var root, out _);
		item.Click += (_, _) => clicked++;

		//Act
		var plain = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.S, VirtualKeyModifiers.None);
		var shifted = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		//Assert
		Assert.IsFalse(plain);
		Assert.IsFalse(shifted);
		Assert.AreEqual(0, clicked);
	}

	[TestMethod]
	public void When_Item_Is_Disabled_Then_Nothing_Is_Invoked()
	{
		//Arrange
		var clicked = 0;
		var item = AddItem(VirtualKey.F1, VirtualKeyModifiers.None, out var root, out _);
		item.Click += (_, _) => clicked++;
		item.IsEnabled = false;

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.F1, VirtualKeyModifiers.None);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, clicked);
	}

	[TestMethod]
	public void When_Item_Is_Collapsed_Then_Nothing_Is_Invoked()
	{
		//Arrange
		var clicked = 0;
		var item = AddItem(VirtualKey.F1, VirtualKeyModifiers.None, out var root, out _);
		item.Click += (_, _) => clicked++;
		item.Visibility = Visibility.Collapsed;

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.F1, VirtualKeyModifiers.None);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, clicked);
	}

	[TestMethod]
	public void When_Accelerator_Is_On_A_Submenu_Item_Then_It_Is_Invoked()
	{
		//Arrange
		var clicked = 0;
		var item = new MenuFlyoutItem { Text = "Deep" };
		item.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.D, Modifiers = VirtualKeyModifiers.Control });
		item.Click += (_, _) => clicked++;
		var subItem = new MenuFlyoutSubItem { Text = "More" };
		subItem.Items.Add(item);
		var root = AddMenuBar(subItem, out _);

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.D, VirtualKeyModifiers.Control);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
	}

	[TestMethod]
	public void When_Accelerator_Is_On_A_Toggle_Item_Then_It_Toggles()
	{
		//Arrange
		var item = new ToggleMenuFlyoutItem { Text = "Grid" };
		item.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.G, Modifiers = VirtualKeyModifiers.Control });
		var root = AddMenuBar(item, out _);

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(root, VirtualKey.G, VirtualKeyModifiers.Control);

		//Assert
		Assert.IsTrue(handled);
		Assert.IsTrue(item.IsChecked);
	}

	[TestMethod]
	public void When_A_Menu_Is_Open_And_Another_Menus_Accelerator_Is_Invoked_Then_The_Open_Menu_Closes()
	{
		//Arrange - File is open; F1 belongs to an item of the (closed) Help menu
		var app = UnitTestsApp.App.EnsureApplication();
		var host = new Grid();
		app.HostView.Children.Add(host);
		var fileMenu = new MenuBarItem { Title = "File" };
		fileMenu.Items.Add(new MenuFlyoutItem { Text = "Save" });
		var about = new MenuFlyoutItem { Text = "About" };
		about.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.F1 });
		var clicked = 0;
		about.Click += (_, _) => clicked++;
		var helpMenu = new MenuBarItem { Title = "Help" };
		helpMenu.Items.Add(about);
		var menuBar = new MenuBar();
		menuBar.Items.Add(fileMenu);
		menuBar.Items.Add(helpMenu);
		host.Children.Add(menuBar);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));
		fileMenu.Invoke();

		//Act
		var handled = MenuBar.TryInvokeMenuItemAccelerator(host.XamlRoot!, VirtualKey.F1, VirtualKeyModifiers.None);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
		Assert.IsFalse(fileMenu.IsMenuReallyOpen(), "running a command closes the open menu, as a click on an item does");
	}

	private static MenuFlyoutItem AddItem(VirtualKey key, VirtualKeyModifiers modifiers, out XamlRoot root, out Grid host)
	{
		var item = new MenuFlyoutItem { Text = "Item" };
		item.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = key, Modifiers = modifiers });
		root = AddMenuBar(item, out host);
		return item;
	}

	// Adds a loaded menu bar with one (closed) menu holding the given item
	private static XamlRoot AddMenuBar(MenuFlyoutItemBase item, out Grid host)
	{
		var app = UnitTestsApp.App.EnsureApplication();
		host = new Grid();
		app.HostView.Children.Add(host);

		var menuBarItem = new MenuBarItem { Title = "File" };
		menuBarItem.Items.Add(item);
		var menuBar = new MenuBar();
		menuBar.Items.Add(menuBarItem);
		host.Children.Add(menuBar);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		Assert.IsTrue(menuBar.IsLoaded, "the menu bar must be loaded for its accelerators to be live");
		var root = host.XamlRoot;
		Assert.IsNotNull(root, "the unit-test host view must have a XamlRoot");
		return root!;
	}
}
