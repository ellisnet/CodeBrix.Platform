using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Core;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.FlyoutTests;

// A flyout's close finishes on the dispatcher (Closed, then closing any flyout still open on top of it).
// That deferred step must only close flyouts that were open when it was queued: a MenuBar moving from one
// menu to the next (Right/Left) closes the first menu and opens the next one before the dispatcher runs.
[TestClass]
public class Given_Flyout_CloseThenOpen
{
	[TestInitialize]
	public void Init()
	{
		UnitTestsApp.App.EnsureApplication();
	}

	[TestCleanup]
	public void Cleanup()
	{
		UnitTestsApp.App.EnsureApplication().HostView.Children.Clear();
	}

	[TestMethod]
	public void When_A_Flyout_Opens_As_Another_Closes_Then_The_New_One_Stays_Open()
	{
		//Arrange
		var (host, firstTarget, secondTarget) = AddTargets();
		var first = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		var second = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		first.ShowAt(firstTarget);
		Pump(host);

		//Act - close one and open the next in the same turn, then let the dispatcher run
		first.Hide();
		second.ShowAt(secondTarget);
		Pump(host);

		//Assert
		Assert.IsFalse(first.IsOpen);
		Assert.IsTrue(second.IsOpen, "the deferred close of the first flyout must not close the one opened after it");

		second.Hide();
		Pump(host);
	}

	[TestMethod]
	public void When_A_Flyout_Closes_Then_A_Flyout_Open_On_Top_Of_It_Closes_Too()
	{
		//Arrange
		var (host, firstTarget, secondTarget) = AddTargets();
		var first = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		var onTop = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		first.ShowAt(firstTarget);
		onTop.ShowAt(secondTarget);
		Pump(host);

		//Act
		first.Hide();
		Pump(host);

		//Assert
		Assert.IsFalse(first.IsOpen);
		Assert.IsFalse(onTop.IsOpen);
	}

	private static (Grid Host, Button FirstTarget, Button SecondTarget) AddTargets()
	{
		var host = new Grid();
		var firstTarget = new Button { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
		var secondTarget = new Button { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Right };
		host.Children.Add(firstTarget);
		host.Children.Add(secondTarget);
		UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
		host.Measure(new Size(200, 100));
		host.Arrange(new Rect(0, 0, 200, 100));
		return (host, firstTarget, secondTarget);
	}

	private static void Pump(UIElement element) =>
		element.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
}
