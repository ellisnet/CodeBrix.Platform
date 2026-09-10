#nullable enable

using System;
using System.Windows.Input;
using Microsoft.UI.Xaml.Input;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Headless.Tests;

/// <summary>
/// <see cref="StandardUICommand"/> as an <see cref="ICommand"/>: what it reports, what it raises,
/// and how it defers to a child command.
/// </summary>
/// <remarks>
/// <para>
/// Ported from src/Platform.UI.RuntimeTests/Tests/Windows_UI_Xaml_Input/Given_StandardUICommand.cs.
/// A command is a plain dependency object, so none of this needs a Window, a XamlRoot or a layout
/// pass, and nothing here varies by operating system.
/// </para>
/// <para>
/// NOT PORTED, and why: the original's <c>When_Predefined_StandardUICommand</c>,
/// <c>When_StandardUICommand_In_Xaml</c> and the two <c>When_Kind_Changes_*</c> tests all set
/// <c>Kind</c>, which makes the command populate its label, description and accelerator from the
/// framework's localized string resources. MEASURED: that path reaches
/// <c>ResourceLoader.GetString</c> to <c>ApplicationLanguages</c> to <c>ApplicationData</c> and
/// throws InvalidOperationException("The Package.Id is not initialized yet.") - a package identity
/// is something an application head supplies and a host-free process does not have. Giving the
/// process a fake manifest would also make it create real ApplicationData folders on disk, which is
/// not something a unit suite should do. Those tests stay in Platform.UI.RuntimeTests until a
/// harness with a real head can run them.
/// </para>
/// </remarks>
public class StandardUICommandTests
{
	[Fact]
	public void When_KeyboardAccelerators_Retrieved()
	{
		//Arrange + Act
		var command = new StandardUICommand();

		//Assert
		command.KeyboardAccelerators.Should().NotBeNull();
	}

	[Fact]
	public void When_CanExecute_Default()
	{
		//Arrange
		ICommand command = new StandardUICommand();

		//Act + Assert
		command.CanExecute(null).Should().BeTrue();
	}

	[Fact]
	public void When_Execute()
	{
		//Arrange
		var sut = new StandardUICommand();
		var executed = false;
		sut.ExecuteRequested += (s, e) => executed = true;

		//Act
		((ICommand)sut).Execute(null);

		//Assert
		executed.Should().BeTrue();
	}

	[Fact]
	public void When_CanExecute_Handled()
	{
		//Arrange
		var sut = new StandardUICommand();
		sut.CanExecuteRequested += (sender, args) => args.CanExecute = false;

		//Act + Assert
		((ICommand)sut).CanExecute(null).Should().BeFalse();
	}

	[Fact]
	public void When_CanExecute_Changed()
	{
		//Arrange
		var sut = new StandardUICommand();
		var raised = false;
		((ICommand)sut).CanExecuteChanged += (sender, args) => raised = true;

		//Act
		sut.NotifyCanExecuteChanged();

		//Assert
		raised.Should().BeTrue();
	}

	[Fact]
	public void When_Child_Command_CanExecute()
	{
		//Arrange
		//A StandardUICommand given a child Command answers for it: while the child refuses to run,
		//the parent reports that it cannot execute and nothing runs.
		const string parameter = "Test string";
		var executeCalled = false;
		var childCommand = new ToggleableCommand(_ => executeCalled = true) { CanExecuteEnabled = false };
		var sut = new StandardUICommand { Command = childCommand };

		//Act + Assert
		sut.CanExecute(parameter).Should().BeFalse();

		sut.Execute(parameter);
		executeCalled.Should().BeFalse();

		childCommand.CanExecuteEnabled = true;
		sut.CanExecute(parameter).Should().BeTrue();

		sut.Execute(parameter);
		executeCalled.Should().BeTrue();
	}

	/// <summary>
	/// A minimal <see cref="ICommand"/> whose <see cref="ICommand.CanExecute"/> answer can be
	/// switched, and which - like the framework's own delegate command - declines to run while that
	/// answer is false.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The original test used the framework's internal <c>DelegateCommand&lt;T&gt;</c> here. What it
	/// actually verifies is that a <see cref="StandardUICommand"/> defers to whatever
	/// <see cref="ICommand"/> it is given, so any command with a switchable answer proves the same
	/// thing - and a local double avoids widening the framework's internal surface with another
	/// InternalsVisibleTo grant for a test-only concern, the same decision DispatcherInitializer
	/// records.
	/// </para>
	/// <para>
	/// MEASURED: the self-guard in <see cref="Execute"/> is load-bearing and matches the type it
	/// stands in for. The parent's Execute does not itself consult CanExecute before passing the
	/// call on, so without the guard the child runs even while it reports that it cannot - and the
	/// original test, which expected nothing to run, would fail.
	/// </para>
	/// </remarks>
	/// <param name="execute">What the command does when it is allowed to run.</param>
	private sealed class ToggleableCommand(Action<object?> execute) : ICommand
	{
		private bool _canExecute = true;

		/// <summary>Whether the command currently allows itself to run.</summary>
		public bool CanExecuteEnabled
		{
			get => _canExecute;
			set
			{
				_canExecute = value;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <inheritdoc />
		public event EventHandler? CanExecuteChanged;

		/// <inheritdoc />
		public bool CanExecute(object? parameter) => _canExecute;

		/// <inheritdoc />
		public void Execute(object? parameter)
		{
			if (_canExecute)
			{
				execute(parameter);
			}
		}
	}
}
