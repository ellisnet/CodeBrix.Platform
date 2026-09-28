#nullable enable

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.MenuBarTests;

// Leaving the menu bar (Escape on a menu title with no menu open) puts keyboard focus back where it was
// before the menus were entered.
[TestClass]
public class Given_MenuBar_Leave
{
	[TestInitialize]
	public void Init()
	{
		var app = UnitTestsApp.App.EnsureApplication();

		// The unit-test app is one process: flyouts an earlier test left open stay in FlyoutBase's open list,
		// and a flyout that is not first in that list never raises Closed.
		foreach (var flyout in Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.OpenFlyouts.ToArray())
		{
			flyout.Hide();
		}

		Pump(app.HostView);
	}

	[TestCleanup]
	public void Cleanup()
	{
		UnitTestsApp.App.EnsureApplication().HostView.Children.Clear();
	}

	[TestMethod]
	public void When_Leaving_Then_Focus_Returns_To_The_Element_Focused_Before_The_Menu_Bar()
	{
		//Arrange
		var (menuBar, fileMenu, helpMenu, surface) = AddMenuBarAndSurface();
		surface.Focus(FocusState.Programmatic);
		fileMenu.Focus(FocusState.Keyboard);
		helpMenu.Focus(FocusState.Keyboard); // moving along the bar is not "entering" it again

		//Act
		var left = menuBar.LeaveMenuBar();

		//Assert
		Assert.IsTrue(left);
		Assert.AreSame(surface, FocusManager.GetFocusedElement(surface.XamlRoot!));
	}

	[TestMethod]
	public void When_Leaving_Then_Focus_Is_No_Longer_In_The_Menu_Bar()
	{
		//Arrange
		var (menuBar, fileMenu, _, surface) = AddMenuBarAndSurface();
		surface.Focus(FocusState.Programmatic);
		fileMenu.Focus(FocusState.Keyboard);

		//Act
		menuBar.LeaveMenuBar();

		//Assert
		Assert.AreEqual(FocusState.Unfocused, fileMenu.FocusState);
	}

	[TestMethod]
	public void When_An_Unhandled_Escape_Arrives_With_Focus_On_A_Title_Then_Focus_Leaves_The_Menu_Bar()
	{
		//Arrange
		MenuBar.ResetForTests();
		var (menuBar, fileMenu, _, surface) = AddMenuBarAndSurface();
		surface.Focus(FocusState.Programmatic);
		fileMenu.Focus(FocusState.Keyboard);

		//Act
		var left = MenuBar.OnUnhandledEscape(menuBar.XamlRoot!);

		//Assert
		Assert.IsTrue(left);
		Assert.AreSame(surface, FocusManager.GetFocusedElement(surface.XamlRoot!));
	}

	[TestMethod]
	public void When_An_Unhandled_Escape_Arrives_With_Focus_Elsewhere_Then_Focus_Stays()
	{
		//Arrange
		MenuBar.ResetForTests();
		var (menuBar, _, _, surface) = AddMenuBarAndSurface();
		surface.Focus(FocusState.Programmatic);

		//Act
		var left = MenuBar.OnUnhandledEscape(menuBar.XamlRoot!);

		//Assert
		Assert.IsFalse(left);
		Assert.AreSame(surface, FocusManager.GetFocusedElement(surface.XamlRoot!));
	}

	[TestMethod]
	public void When_A_Menu_Closes_Because_An_Item_Ran_Then_Focus_Returns_To_The_Element_Focused_Before()
	{
		//Arrange - the menu was entered from the surface and opened
		var (menuBar, fileMenu, _, surface) = AddMenuBarAndSurface();
		surface.Focus(FocusState.Programmatic);
		fileMenu.Invoke();
		Pump(menuBar);
		var save = (MenuFlyoutItem)fileMenu.Items[0];

		//Act - the item runs (Enter or a click); the menu closes and hands focus back to its title
		save.Invoke();
		Pump(menuBar);

		//Assert
		Assert.IsFalse(fileMenu.IsMenuReallyOpen());
		Assert.AreSame(surface, FocusManager.GetFocusedElement(surface.XamlRoot!));
	}

	private static void Pump(UIElement element)
	{
		for (var i = 0; i < 3; i++)
		{
			element.Dispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
		}
	}

	private static (MenuBar MenuBar, MenuBarItem FileMenu, MenuBarItem HelpMenu, Button Surface) AddMenuBarAndSurface()
	{
		var fileMenu = new MenuBarItem { Title = "File" };
		fileMenu.Items.Add(new MenuFlyoutItem { Text = "Save" });
		var helpMenu = new MenuBarItem { Title = "Help" };
		helpMenu.Items.Add(new MenuFlyoutItem { Text = "About" });
		var menuBar = new MenuBar();
		menuBar.Items.Add(fileMenu);
		menuBar.Items.Add(helpMenu);
		var surface = new Button { Content = "Game" };

		// The surface comes BEFORE the menu bar and another button after it, so "the next focusable
		// element" (the fallback when nothing was recorded) is not the surface.
		var host = new StackPanel();
		host.Children.Add(surface);
		host.Children.Add(menuBar);
		host.Children.Add(new Button { Content = "After" });
		UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		return (menuBar, fileMenu, helpMenu, surface);
	}
}
