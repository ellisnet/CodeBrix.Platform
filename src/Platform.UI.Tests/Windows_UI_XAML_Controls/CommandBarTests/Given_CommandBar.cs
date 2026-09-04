using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.CommandBarTests
{
	[TestClass]
	public class Given_CommandBar
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();

			// Registers the theme's default styles, so the bar and its buttons get their real
			// templates in the unit-test host.
			_ = new XamlControlsResources();
		}

		[TestMethod]
		public void When_Commands_Are_Declared()
		{
			//Arrange
			var bar = new CommandBar();

			//Act
			bar.PrimaryCommands.Add(new AppBarButton { Label = "New" });
			bar.PrimaryCommands.Add(new AppBarSeparator());
			bar.PrimaryCommands.Add(new AppBarToggleButton { Label = "Magnifier" });
			bar.SecondaryCommands.Add(new AppBarButton { Label = "Settings" });

			//Assert
			Assert.AreEqual(3, bar.PrimaryCommands.Count);
			Assert.AreEqual(1, bar.SecondaryCommands.Count);
			Assert.IsInstanceOfType(bar.PrimaryCommands[1], typeof(AppBarSeparator));
			Assert.IsInstanceOfType(bar.PrimaryCommands[2], typeof(AppBarToggleButton));
			Assert.AreEqual("Settings", ((AppBarButton)bar.SecondaryCommands[0]).Label);
		}

		[TestMethod]
		public void When_The_Two_Collections_Are_Separate()
		{
			//Arrange
			var bar = new CommandBar();
			var button = new AppBarButton { Label = "Save" };

			//Act
			bar.PrimaryCommands.Add(button);

			//Assert
			Assert.AreEqual(1, bar.PrimaryCommands.Count);
			Assert.AreEqual(0, bar.SecondaryCommands.Count,
				"A primary command must not appear among the secondary ones.");
		}

		[TestMethod]
		public void When_Dynamic_Overflow_Has_No_Room()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);
			Layout(host, 1000);

			Assert.AreEqual(0, buttons.Count(b => b.IsInOverflow),
				"With room for everything, nothing belongs in the overflow.");

			//Act
			Layout(host, 200);

			//Assert
			Assert.IsTrue(buttons.Count(b => b.IsInOverflow) > 0,
				"A bar narrower than its commands must move the trailing ones into the overflow.");
			Assert.IsFalse(buttons[0].IsInOverflow,
				"The overflow is filled from the END of the bar, so the first command stays.");
		}

		[TestMethod]
		public void When_The_Room_Comes_Back()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);
			Layout(host, 200);
			Assert.IsTrue(buttons.Count(b => b.IsInOverflow) > 0);

			//Act
			Layout(host, 1000);

			//Assert
			Assert.AreEqual(0, buttons.Count(b => b.IsInOverflow),
				"Commands must return to the bar when the width returns.");
		}

		[TestMethod]
		public void When_Dynamic_Overflow_Is_Disabled()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);
			bar.IsDynamicOverflowEnabled = false;

			//Act
			Layout(host, 120);

			//Assert
			Assert.AreEqual(0, buttons.Count(b => b.IsInOverflow),
				"With dynamic overflow off, a narrow bar must keep every command where it was put.");
		}

		[TestMethod]
		public void When_DefaultLabelPosition_Is_Bottom()
		{
			//Arrange
			// The bar's default is not written onto the button as a property; it is pushed into the
			// button, which then reports where its label sits. That report is the framework's own
			// answer to "where is this label", so it is what the fence reads.
			var bar = BuildLiveBar(out var host, out var buttons);

			//Act
			bar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
			Layout(host, 1000);

			//Assert
			Assert.IsTrue(HasBottomLabel(buttons[0]), "The bar's default must reach the button.");
			Assert.IsFalse(HasRightLabel(buttons[0]));
		}

		[TestMethod]
		public void When_DefaultLabelPosition_Is_Right()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);

			//Act
			bar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
			Layout(host, 1000);

			//Assert
			Assert.AreEqual(CommandBarDefaultLabelPosition.Right, bar.DefaultLabelPosition);
			Assert.IsTrue(HasRightLabel(buttons[0]), "Every button must follow the bar's default.");
			Assert.IsTrue(HasRightLabel(buttons[4]));
			Assert.IsFalse(HasBottomLabel(buttons[0]));
		}

		[TestMethod]
		public void When_DefaultLabelPosition_Is_Collapsed()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);

			//Act
			bar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
			Layout(host, 1000);

			//Assert
			Assert.IsFalse(HasBottomLabel(buttons[0]), "A collapsed label sits nowhere.");
			Assert.IsFalse(HasRightLabel(buttons[0]));
		}

		[TestMethod]
		public void When_A_Button_States_Its_Own_LabelPosition()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out var buttons);
			buttons[0].LabelPosition = CommandBarLabelPosition.Collapsed;

			//Act
			bar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
			Layout(host, 1000);

			//Assert
			Assert.AreEqual(CommandBarLabelPosition.Collapsed, buttons[0].LabelPosition,
				"A button's own label position wins over the bar's default.");
			Assert.IsFalse(HasRightLabel(buttons[0]),
				"The button that asked for no label must not take the bar's default.");
			Assert.IsTrue(HasRightLabel(buttons[1]),
				"A button that states nothing keeps taking the bar's default.");
		}

		[TestMethod]
		public void When_ClosedDisplayMode_Changes()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out _);
			Layout(host, 1000);
			var compact = bar.DesiredSize.Height;

			//Act
			bar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			Layout(host, 1000);
			var minimal = bar.DesiredSize.Height;
			bar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
			Layout(host, 1000);
			var hidden = bar.DesiredSize.Height;

			//Assert
			Assert.IsTrue(compact > minimal, $"Compact ({compact}) must be taller than minimal ({minimal}).");
			Assert.IsTrue(minimal > hidden, $"Minimal ({minimal}) must be taller than hidden ({hidden}).");
			Assert.AreEqual(0d, hidden, "A hidden closed bar takes no height.");
		}

		[TestMethod]
		public void When_Opened_And_Closed()
		{
			//Arrange
			var bar = BuildLiveBar(out var host, out _);
			bar.SecondaryCommands.Add(new AppBarButton { Label = "Settings" });
			Layout(host, 1000);

			//Act
			bar.IsOpen = true;
			Layout(host, 1000);
			var openedCount = bar.SecondaryCommands.Count;
			bar.IsOpen = false;
			Layout(host, 1000);

			//Assert
			Assert.AreEqual(1, openedCount);
			Assert.IsFalse(bar.IsOpen);
		}

		private static CommandBar BuildLiveBar(out Grid host, out AppBarButton[] buttons)
		{
			var bar = new CommandBar
			{
				IsDynamicOverflowEnabled = true,
				ClosedDisplayMode = AppBarClosedDisplayMode.Compact,
				DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom,
			};

			buttons =
			[
				new AppBarButton { Label = "One" },
				new AppBarButton { Label = "Two" },
				new AppBarButton { Label = "Three" },
				new AppBarButton { Label = "Four" },
				new AppBarButton { Label = "Five" },
			];

			foreach (var button in buttons)
			{
				bar.PrimaryCommands.Add(button);
			}

			host = new Grid();
			host.Children.Add(bar);
			UnitTestsApp.App.EnsureApplication().HostView.Children.Add(host);
			host.ForceLoaded();

			return bar;
		}

		private static bool HasBottomLabel(AppBarButton button)
			=> ((ICommandBarLabeledElement)button).GetHasBottomLabel();

		private static bool HasRightLabel(AppBarButton button)
			=> ((ICommandBarLabeledElement)button).GetHasRightLabel();

		private static void Layout(FrameworkElement host, double width)
		{
			host.Width = width;
			host.Measure(new Size(width, 1000));
			host.Arrange(new Rect(0, 0, width, 1000));
			host.UpdateLayout();
		}
	}
}
