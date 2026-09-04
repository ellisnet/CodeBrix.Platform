using System.Linq;
using CodeBrix.Platform.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.TabViewTests
{
	[TestClass]
	public class Given_TabViewItem
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();

			// Registers the theme's default styles, so that TabView, TabViewItem and CheckBox
			// get their real templates in the unit-test host.
			_ = new XamlControlsResources();
		}

		[TestMethod]
		public void When_Content_Reattached_With_String_Header()
		{
			//Arrange
			var (host, panel, checkBoxA, checkBoxB, textBox) = BuildLiveContent();

			// The CheckBox template presents its own content in a part named "ContentPresenter",
			// which is also the part name a TabViewItem looks for when presenting its Header.
			Assert.AreEqual("B", DisplayedContent(checkBoxB), "The CheckBox should be templated before the tab takes the content.");

			host.Children.Remove(panel);

			var item = CreateTabViewItem();
			item.Header = "Layout";

			//Act
			item.Content = panel;

			//Assert
			Assert.AreEqual("B", DisplayedContent(checkBoxB), "The last CheckBox in the tab's content must keep showing its own content.");
			Assert.AreEqual("A", DisplayedContent(checkBoxA));
			Assert.AreEqual("B", checkBoxB.Content);
			Assert.AreEqual(string.Empty, textBox.Text);
			Assert.AreEqual("Layout", item.Header);
		}

		[TestMethod]
		public void When_Content_Reattached_With_TextBlock_Header()
		{
			//Arrange
			var (host, panel, checkBoxA, checkBoxB, _) = BuildLiveContent();

			host.Children.Remove(panel);

			var headerBlock = new TextBlock { Text = "Layout" };
			var item = CreateTabViewItem();
			item.Header = headerBlock;

			//Act
			item.Content = panel;

			//Assert
			Assert.IsNull(VisualTreeHelper.GetParent(headerBlock), "The header element must not be re-parented into the tab's content.");
			Assert.AreEqual("B", DisplayedContent(checkBoxB));
			Assert.AreEqual("A", DisplayedContent(checkBoxA));
		}

		[TestMethod]
		public void When_Removed_And_ReAdded_To_TabView()
		{
			//Arrange
			var checkBoxA = new CheckBox { Content = "A" };
			var checkBoxB = new CheckBox { Content = "B" };
			var textBox = new TextBox();
			var panel = new StackPanel();
			panel.Children.Add(checkBoxA);
			panel.Children.Add(checkBoxB);
			panel.Children.Add(textBox);

			var item = CreateTabViewItem();
			item.Header = "Layout";

			var tabView = new TabView();
			tabView.TabItems.Add(item);

			var host = new Grid();
			host.Children.Add(tabView);
			UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
			host.ForceLoaded();
			Layout(host);

			item.Content = panel;
			Layout(host);
			tabView.SelectedItem = item;
			Layout(host);

			//Act
			tabView.TabItems.Remove(item);
			Layout(host);
			tabView.TabItems.Add(item);
			Layout(host);
			tabView.SelectedItem = item;
			Layout(host);

			//Assert
			Assert.AreEqual("B", DisplayedContent(checkBoxB));
			Assert.AreEqual("A", DisplayedContent(checkBoxA));
			Assert.AreEqual("Layout", HeaderPresenterContent(item));
		}

		[TestMethod]
		public void When_Panel_Moved_To_A_New_Tab()
		{
			//Arrange
			var checkBoxA = new CheckBox { Content = "A" };
			var checkBoxB = new CheckBox { Content = "B" };
			var textBox = new TextBox();
			var panel = new StackPanel();
			panel.Children.Add(checkBoxA);
			panel.Children.Add(checkBoxB);
			panel.Children.Add(textBox);

			var firstItem = CreateTabViewItem();
			firstItem.Header = "Layout";

			var tabView = new TabView();
			tabView.TabItems.Add(firstItem);

			var host = new Grid();
			host.Children.Add(tabView);
			UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
			host.ForceLoaded();
			Layout(host);

			firstItem.Content = panel;
			Layout(host);
			tabView.SelectedItem = firstItem;
			Layout(host);

			//Act
			tabView.TabItems.Remove(firstItem);
			firstItem.Content = null;
			Layout(host);

			var secondItem = CreateTabViewItem();
			secondItem.Header = "Layout";
			secondItem.Content = panel;
			tabView.TabItems.Add(secondItem);
			Layout(host);
			tabView.SelectedItem = secondItem;
			Layout(host);

			//Assert
			Assert.AreEqual("B", DisplayedContent(checkBoxB), "The last CheckBox must not show the new tab's header.");
			Assert.AreEqual("A", DisplayedContent(checkBoxA));
			Assert.AreEqual("Layout", HeaderPresenterContent(secondItem));
		}

		private static (Grid host, StackPanel panel, CheckBox checkBoxA, CheckBox checkBoxB, TextBox textBox) BuildLiveContent()
		{
			var checkBoxA = new CheckBox { Content = "A" };
			var checkBoxB = new CheckBox { Content = "B" };
			var textBox = new TextBox();

			var panel = new StackPanel();
			panel.Children.Add(checkBoxA);
			panel.Children.Add(checkBoxB);
			panel.Children.Add(textBox);

			var host = new Grid();
			host.Children.Add(panel);
			UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
			host.ForceLoaded();
			Layout(host);

			return (host, panel, checkBoxA, checkBoxB, textBox);
		}

		private static TabViewItem CreateTabViewItem()
		{
			var item = new TabViewItem();

			// The unit-test host has no theme dictionary merged into the application resources,
			// so the tab geometry lookup performed while the template is applied needs this key.
			item.Resources["OverlayCornerRadius"] = new CornerRadius(4);

			return item;
		}

		private static void Layout(FrameworkElement host)
		{
			host.Measure(new Size(1000, 1000));
			host.Arrange(new Rect(0, 0, 1000, 1000));
		}

		private static object DisplayedContent(DependencyObject element) => FindPresenterContent(element);

		private static object HeaderPresenterContent(TabViewItem item)
			=> item.TemplatedRoot is DependencyObject root ? FindPresenterContent(root) : null;

		private static object FindPresenterContent(DependencyObject root)
			=> root
				.GetAllChildren(includeCurrent: false)
				.OfType<ContentPresenter>()
				.FirstOrDefault(presenter => presenter.Name == "ContentPresenter")
				?.Content;
	}
}
