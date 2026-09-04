#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.System;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml.Input.AccessKeys;

[TestClass]
public class Given_AccessKeyManager
{
	[TestInitialize]
	public void Init()
	{
		UnitTestsApp.App.EnsureApplication();
		AccessKeyManager.ResetForTests();
		CloseOpenPopups();
	}

	[TestCleanup]
	public void Cleanup()
	{
		AccessKeyManager.ResetForTests();
		CloseOpenPopups();
		UnitTestsApp.App.EnsureApplication().HostView.Children.Clear();
	}

	// The unit-test app is one process: a popup left open by an earlier test would still be the
	// active access-key scope here.
	private static void CloseOpenPopups()
	{
		var popupRoot = UnitTestsApp.App.EnsureApplication().HostView.XamlRoot?.VisualTree.PopupRoot;
		if (popupRoot is null)
		{
			return;
		}

		foreach (var popup in popupRoot.GetOpenPopups())
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public void When_Alt_Letter_Then_AccessKeyInvoked_Is_Raised_And_Key_Is_Handled()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
	}

	[TestMethod]
	public void When_Alt_Letter_Then_Match_Is_Case_Insensitive()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "f" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
	}

	[TestMethod]
	public void When_AccessKeyInvoked_Is_Left_Unhandled_Then_The_Element_Is_Invoked_Through_Automation()
	{
		//Arrange
		var host = AddHost(out var root);
		var clicked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.Click += (_, _) => clicked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
	}

	[TestMethod]
	public void When_AccessKeyInvoked_Is_Handled_Then_The_Element_Is_Not_Invoked_Through_Automation()
	{
		//Arrange
		var host = AddHost(out var root);
		var clicked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.Click += (_, _) => clicked++;
		button.AccessKeyInvoked += (_, args) => args.Handled = true;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(0, clicked);
	}

	[TestMethod]
	public void When_Alt_Letter_Does_Not_Match_Then_Nothing_Is_Raised_And_Key_Is_Not_Handled()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.G, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, invoked);
	}

	[TestMethod]
	public void When_Letter_Without_Alt_Then_Key_Is_Not_Handled()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, invoked);
	}

	[TestMethod]
	public void When_AccessKey_Is_Cleared_Then_The_Element_Stops_Answering()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		button.AccessKey = string.Empty;
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, invoked);
	}

	[TestMethod]
	public void When_Registry_Is_Empty_Then_Key_Is_Not_Handled_And_Nothing_Is_Allocated()
	{
		//Arrange
		AddHost(out var root);

		// Warm the code path so the measurement is not dominated by first-call jitting.
		AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Act
		var before = GC.GetAllocatedBytesForCurrentThread();
		var handled = false;
		for (var i = 0; i < 1000; i++)
		{
			handled |= AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);
			handled |= AccessKeyManager.TryProcessKey(root, VirtualKey.Menu, VirtualKeyModifiers.None, false);
		}

		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, allocated, $"{allocated} bytes were allocated for 2000 key events with an empty registry.");
	}

	[TestMethod]
	public void When_Alt_Is_Tapped_Then_Display_Mode_Is_Entered_And_Requested_Is_Raised()
	{
		//Arrange
		var host = AddHost(out var root);
		var requested = 0;
		var displayModeChanged = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyDisplayRequested += (_, _) => requested++;
		host.Children.Add(button);
		void OnChanged(object sender, object args) => displayModeChanged++;
		AccessKeyManager.IsDisplayModeEnabledChanged += OnChanged;

		//Act
		var downHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.Menu, VirtualKeyModifiers.None, true);
		var upHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.Menu, VirtualKeyModifiers.Menu, false);

		//Assert
		AccessKeyManager.IsDisplayModeEnabledChanged -= OnChanged;
		Assert.IsFalse(downHandled, "the Alt key press itself must still reach the tree");
		Assert.IsTrue(upHandled);
		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled);
		Assert.AreEqual(1, requested);
		Assert.AreEqual(1, displayModeChanged);
	}

	[TestMethod]
	public void When_Escape_In_Display_Mode_Then_Display_Mode_Exits_And_Dismissed_Is_Raised()
	{
		//Arrange
		var host = AddHost(out var root);
		var dismissed = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyDisplayDismissed += (_, _) => dismissed++;
		host.Children.Add(button);
		AccessKeyManager.EnterDisplayMode(root);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.Escape, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsFalse(handled, "Escape must travel on so an open menu still closes");
		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled);
		Assert.AreEqual(1, dismissed);
	}

	[TestMethod]
	public void When_Letter_In_Display_Mode_Then_Element_Is_Invoked_Without_Alt_And_Display_Mode_Exits()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var dismissed = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		button.AccessKeyDisplayDismissed += (_, _) => dismissed++;
		host.Children.Add(button);
		AccessKeyManager.EnterDisplayMode(root);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled);
		Assert.AreEqual(1, dismissed);
	}

	[TestMethod]
	public void When_Unmatched_Letter_In_Display_Mode_Then_Display_Mode_Exits()
	{
		//Arrange
		var host = AddHost(out var root);
		var dismissed = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyDisplayDismissed += (_, _) => dismissed++;
		host.Children.Add(button);
		AccessKeyManager.EnterDisplayMode(root);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.G, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsFalse(handled, "a key the access-key pipeline did not act on must travel on");
		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled);
		Assert.AreEqual(1, dismissed);
	}

	[TestMethod]
	public void When_ExitDisplayModeOnAccessKeyInvoked_Is_False_Then_Display_Mode_Survives_The_Invoke()
	{
		//Arrange
		var host = AddHost(out var root);
		var button = new Button { Content = "File", AccessKey = "F", ExitDisplayModeOnAccessKeyInvoked = false };
		host.Children.Add(button);
		AccessKeyManager.EnterDisplayMode(root);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled);
	}

	[TestMethod]
	public void When_AccessKeyScopeOwner_Is_Another_Element_Then_The_Root_Scope_Does_Not_Answer()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var otherScope = new Border { IsAccessKeyScope = true };
		host.Children.Add(otherScope);
		var button = new Button { Content = "File", AccessKey = "F", AccessKeyScopeOwner = otherScope };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, invoked);
	}

	[TestMethod]
	public void When_AccessKeyScopeOwner_Is_The_Active_Scope_Then_The_Element_Answers()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		host.Children.Add(button);
		button.AccessKeyScopeOwner = AccessKeyManager.GetActiveScope(root);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
	}

	[TestMethod]
	public void When_An_Ancestor_Is_A_Nested_Scope_Then_The_Element_Does_Not_Answer_From_The_Root_Scope()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var nestedScope = new Border { IsAccessKeyScope = true };
		var button = new Button { Content = "File", AccessKey = "F" };
		button.AccessKeyInvoked += (_, _) => invoked++;
		nestedScope.Child = button;
		host.Children.Add(nestedScope);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsFalse(handled);
		Assert.AreEqual(0, invoked);
	}

	[TestMethod]
	public void When_A_Popup_Is_Open_Then_Its_Child_Is_The_Active_Scope()
	{
		//Arrange
		var host = AddHost(out var root);
		var rootInvoked = 0;
		var popupInvoked = 0;
		var rootButton = new Button { Content = "File", AccessKey = "F" };
		rootButton.AccessKeyInvoked += (_, _) => rootInvoked++;
		host.Children.Add(rootButton);

		var popupButton = new Button { Content = "Exit", AccessKey = "X" };
		popupButton.AccessKeyInvoked += (_, _) => popupInvoked++;
		var popup = new Popup
		{
			XamlRoot = root,
			Child = new Grid { Children = { popupButton } },
		};

		//Act
		popup.IsOpen = true;
		var popupHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.X, VirtualKeyModifiers.Menu, true);
		var rootHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);
		popup.IsOpen = false;

		//Assert
		Assert.IsTrue(popupHandled, "the open popup's child must be the active scope");
		Assert.AreEqual(1, popupInvoked);
		Assert.IsFalse(rootHandled, "the root scope must be hidden while a popup is open");
		Assert.AreEqual(0, rootInvoked);
	}

	[TestMethod]
	public void When_The_Popup_Closes_Then_The_Root_Scope_Answers_Again()
	{
		//Arrange
		var host = AddHost(out var root);
		var rootInvoked = 0;
		var rootButton = new Button { Content = "File", AccessKey = "F" };
		rootButton.AccessKeyInvoked += (_, _) => rootInvoked++;
		host.Children.Add(rootButton);

		var popup = new Popup
		{
			XamlRoot = root,
			Child = new Grid { Children = { new Button { Content = "Exit", AccessKey = "X" } } },
		};

		//Act
		popup.IsOpen = true;
		popup.IsOpen = false;
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, rootInvoked);
	}

	[TestMethod]
	public void When_MenuBarItem_Has_An_AccessKey_Then_Alt_Letter_Reaches_It()
	{
		//Arrange
		var host = AddHost(out var root);
		var invoked = 0;
		var menuBarItem = new MenuBarItem { Title = "File", AccessKey = "F" };
		menuBarItem.Items.Add(new MenuFlyoutItem { Text = "Exit" });
		menuBarItem.AccessKeyInvoked += (_, _) => invoked++;
		var menuBar = new MenuBar();
		menuBar.Items.Add(menuBarItem);
		host.Children.Add(menuBar);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);
		var flyoutOpen = menuBarItem.IsFlyoutOpen();

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, invoked);
		Assert.IsTrue(flyoutOpen, "Alt+F must open the File menu");
	}

	[TestMethod]
	public void When_A_MenuBar_Menu_Is_Open_Then_A_Bare_Letter_Invokes_One_Of_Its_Items()
	{
		//Arrange
		var host = AddHost(out var root);
		var clicked = 0;
		var exitItem = new MenuFlyoutItem { Text = "Exit", AccessKey = "X" };
		exitItem.Click += (_, _) => clicked++;
		var menuBarItem = new MenuBarItem { Title = "File", AccessKey = "F" };
		menuBarItem.Items.Add(exitItem);
		var menuBar = new MenuBar();
		menuBar.Items.Add(menuBarItem);
		host.Children.Add(menuBar);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		//Act
		AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.X, VirtualKeyModifiers.None, true);

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
	}

	[TestMethod]
	public void When_A_MenuFlyout_Is_Open_Then_Its_Items_Answer_Access_Keys()
	{
		//Arrange
		var host = AddHost(out var root);
		var clicked = 0;
		var anchor = new Button { Content = "Anchor" };
		host.Children.Add(anchor);
		var item = new MenuFlyoutItem { Text = "Exit", AccessKey = "X" };
		item.Click += (_, _) => clicked++;
		var flyout = new MenuFlyout();
		flyout.Items.Add(item);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		//Act
		flyout.ShowAt(anchor);
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.X, VirtualKeyModifiers.Menu, true);
		flyout.Hide();

		//Assert
		Assert.IsTrue(handled);
		Assert.AreEqual(1, clicked);
	}

	[TestMethod]
	public void When_An_Invoke_Opens_A_Popup_Then_Access_Key_Mode_Follows_Into_It()
	{
		//Arrange
		var host = AddHost(out var root);
		var popupInvoked = 0;
		var popupButton = new Button { Content = "Exit", AccessKey = "X" };
		popupButton.AccessKeyInvoked += (_, args) => { popupInvoked++; args.Handled = true; };
		var popup = new Popup
		{
			XamlRoot = root,
			Child = new Grid { Children = { popupButton } },
		};

		var opener = new Button { Content = "File", AccessKey = "F" };
		opener.AccessKeyInvoked += (_, args) =>
		{
			popup.IsOpen = true;
			args.Handled = true;
		};
		host.Children.Add(opener);

		//Act
		var openHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);
		var displayModeAfterOpen = AccessKeyManager.IsDisplayModeEnabled;
		var itemHandled = AccessKeyManager.TryProcessKey(root, VirtualKey.X, VirtualKeyModifiers.None, true);
		popup.IsOpen = false;

		//Assert
		Assert.IsTrue(openHandled);
		Assert.IsTrue(displayModeAfterOpen, "opening a menu must keep access-key mode alive in it");
		Assert.IsTrue(itemHandled, "a bare letter must invoke an item of the open menu");
		Assert.AreEqual(1, popupInvoked);
	}

	[TestMethod]
	public void When_A_Popup_Scope_Is_Active_Then_An_Unmatched_Letter_Does_Not_End_Access_Key_Mode()
	{
		//Arrange
		var host = AddHost(out var root);
		var popup = new Popup
		{
			XamlRoot = root,
			Child = new Grid { Children = { new Button { Content = "Exit", AccessKey = "X" } } },
		};
		var opener = new Button { Content = "File", AccessKey = "F" };
		opener.AccessKeyInvoked += (_, args) =>
		{
			popup.IsOpen = true;
			args.Handled = true;
		};
		host.Children.Add(opener);
		AccessKeyManager.TryProcessKey(root, VirtualKey.F, VirtualKeyModifiers.Menu, true);

		//Act
		var handled = AccessKeyManager.TryProcessKey(root, VirtualKey.Q, VirtualKeyModifiers.None, true);
		var stillInDisplayMode = AccessKeyManager.IsDisplayModeEnabled;
		popup.IsOpen = false;

		//Assert
		Assert.IsFalse(handled);
		Assert.IsTrue(stillInDisplayMode, "a menu keeps its mnemonics live after an unmatched letter");
	}

	private static Grid AddHost(out XamlRoot root)
	{
		var app = UnitTestsApp.App.EnsureApplication();
		var host = new Grid();
		app.HostView.Children.Add(host);
		root = host.XamlRoot!;
		Assert.IsNotNull(root, "the unit-test host view must have a XamlRoot");
		return host;
	}
}
