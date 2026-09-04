using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.CommandingTests
{
	[TestClass]
	public class Given_CommandingHelpers
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_A_Command_Carries_One_Accelerator()
		{
			//Arrange
			// This is the whole of the defect: the helper copies a command's accelerators onto the
			// element the command is bound to, and the copies were built, bound, and dropped. Every
			// ButtonBase in the framework was affected, so a plain Button is the fence.
			var command = new XamlUICommand();
			command.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
			var button = new Button();

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual(1, button.KeyboardAccelerators.Count,
				"The command's accelerator must reach the button.");
			Assert.AreEqual(VirtualKey.S, button.KeyboardAccelerators[0].Key);
			Assert.AreEqual(VirtualKeyModifiers.Control, button.KeyboardAccelerators[0].Modifiers);
		}

		[TestMethod]
		public void When_A_Command_Carries_Several_Accelerators()
		{
			//Arrange
			var command = new XamlUICommand();
			command.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control });
			command.KeyboardAccelerators.Add(
				new KeyboardAccelerator { Key = VirtualKey.F12, Modifiers = VirtualKeyModifiers.None });
			var button = new Button();

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual(2, button.KeyboardAccelerators.Count);
			Assert.AreEqual(VirtualKey.S, button.KeyboardAccelerators[0].Key);
			Assert.AreEqual(VirtualKey.F12, button.KeyboardAccelerators[1].Key);
		}

		[TestMethod]
		public void When_The_Copies_Are_Not_The_Commands_Own_Accelerators()
		{
			//Arrange
			// A KeyboardAccelerator cannot have two parents, which is why the helper copies rather
			// than assigns; the copies follow the originals through bindings.
			var accelerator = new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control };
			var command = new XamlUICommand();
			command.KeyboardAccelerators.Add(accelerator);
			var button = new Button();

			//Act
			button.Command = command;

			//Assert
			Assert.AreNotSame(accelerator, button.KeyboardAccelerators[0],
				"The button must carry a copy, not the command's own accelerator.");

			//Act
			accelerator.Key = VirtualKey.W;

			//Assert
			Assert.AreEqual(VirtualKey.W, button.KeyboardAccelerators[0].Key,
				"A change to the command's accelerator must follow through to the copy.");
		}

		[TestMethod]
		public void When_A_Command_Carries_No_Accelerator()
		{
			//Arrange
			var command = new XamlUICommand();
			var button = new Button();

			//Act
			button.Command = command;

			//Assert
			Assert.AreEqual(0, button.KeyboardAccelerators.Count);
		}
	}
}
