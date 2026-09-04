using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.CommandBarTests
{
	[TestClass]
	public class Given_AppBarButton
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();

			_ = new XamlControlsResources();
		}

		[TestMethod]
		public void When_A_XamlUICommand_Is_Bound()
		{
			//Arrange
			var command = new XamlUICommand
			{
				Label = "Save",
				Description = "Write the score out",
				IconSource = new SymbolIconSource { Symbol = Symbol.Save },
			};
			var button = new AppBarButton();

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual("Save", button.Label, "The command's label must reach the button.");
			Assert.IsInstanceOfType(button.Icon, typeof(IconSourceElement),
				"The command's icon source must reach the button as an icon.");
		}

		[TestMethod]
		public void When_The_Button_States_Its_Own_Label()
		{
			//Arrange
			var command = new XamlUICommand { Label = "Save" };
			var button = new AppBarButton { Label = "Write" };

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual("Write", button.Label,
				"A label the button states for itself must win over the command's.");
		}

		[TestMethod]
		public void When_The_Command_Says_It_Cannot_Execute()
		{
			//Arrange
			var command = new TestCommand();
			var button = new AppBarButton { Command = command };
			Assert.IsTrue(button.IsEnabled);

			//Act
			command.SetCanExecute(false);

			//Assert
			Assert.IsFalse(button.IsEnabled, "The button must follow CanExecuteChanged.");

			//Act
			command.SetCanExecute(true);

			//Assert
			Assert.IsTrue(button.IsEnabled);
		}

		[TestMethod]
		public void When_The_Command_Is_Invoked()
		{
			//Arrange
			var command = new TestCommand();
			var button = new AppBarButton { Command = command, CommandParameter = "page" };

			//Act
			button.Command.Execute(button.CommandParameter);

			//Assert
			Assert.AreEqual(1, command.Executions);
			Assert.AreEqual("page", command.LastParameter);
		}

		[TestMethod]
		public void When_A_XamlUICommand_Carries_Keyboard_Accelerators()
		{
			//Arrange
			// The framework copies a command's accelerators onto the element it is bound to, so
			// Ctrl+S works while the button is in the tree. The copies used to be built and then
			// thrown away, and the button's collection stayed empty.
			var command = new XamlUICommand();
			command.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
			var button = new AppBarButton();

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual(1, button.KeyboardAccelerators.Count,
				"The command's accelerator must reach the button.");
			Assert.AreEqual(VirtualKey.S, button.KeyboardAccelerators[0].Key);
			Assert.AreEqual(VirtualKeyModifiers.Control, button.KeyboardAccelerators[0].Modifiers);
		}

		[TestMethod]
		public void When_The_Button_Already_Has_An_Accelerator()
		{
			//Arrange
			var command = new XamlUICommand();
			command.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
			var button = new AppBarButton();
			button.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.W, Modifiers = VirtualKeyModifiers.Control });

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual(1, button.KeyboardAccelerators.Count,
				"An accelerator the application registered must be left alone.");
			Assert.AreEqual(VirtualKey.W, button.KeyboardAccelerators[0].Key);
		}

		private sealed class TestCommand : System.Windows.Input.ICommand
		{
			private bool _canExecute = true;

			public event EventHandler CanExecuteChanged;

			public int Executions { get; private set; }

			public object LastParameter { get; private set; }

			public void SetCanExecute(bool value)
			{
				_canExecute = value;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}

			public bool CanExecute(object parameter) => _canExecute;

			public void Execute(object parameter)
			{
				Executions++;
				LastParameter = parameter;
			}
		}
	}
}
