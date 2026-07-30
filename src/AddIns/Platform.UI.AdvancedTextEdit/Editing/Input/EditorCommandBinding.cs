#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: no direct counterpart file - replaces System.Windows.Input.CommandBinding plus
//the ExecutedRoutedEventArgs/CanExecuteRoutedEventArgs pair and their handler delegates for the
//editor's own command system. The args expose the members the ported handler bodies use
//(Parameter/Handled, and CanExecute for the query args). WPF's implicit rule that a binding with
//an Executed handler but no CanExecute handler is always executable is preserved in
//<c>OnCanExecute</c>; WPF's rule that invoking the Executed handler marks the command dispatch
//handled is preserved in <c>OnExecuted</c> (upstream handlers that never set Handled, such as
//the overstrike toggle, relied on it).

/// <summary>
/// Represents the method that handles the execution of an <see cref="EditorCommand"/>.
/// </summary>
/// <param name="sender">The text area the command executes against.</param>
/// <param name="e">The event data.</param>
public delegate void ExecutedEditorCommandEventHandler(object sender, ExecutedEditorCommandEventArgs e);

/// <summary>
/// Represents the method that determines whether an <see cref="EditorCommand"/> can execute.
/// </summary>
/// <param name="sender">The text area the command would execute against.</param>
/// <param name="e">The event data.</param>
public delegate void CanExecuteEditorCommandEventHandler(object sender, CanExecuteEditorCommandEventArgs e);

/// <summary>
/// Event data for the execution of an <see cref="EditorCommand"/>.
/// </summary>
public sealed class ExecutedEditorCommandEventArgs : EventArgs
{
	/// <summary>
	/// Creates new event data for executing the given command.
	/// </summary>
	public ExecutedEditorCommandEventArgs(EditorCommand command, object? parameter)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		this.Command = command;
		this.Parameter = parameter;
	}

	/// <summary>
	/// Gets the command being executed.
	/// </summary>
	public EditorCommand Command { get; }

	/// <summary>
	/// Gets the command parameter, if any.
	/// </summary>
	public object? Parameter { get; }

	/// <summary>
	/// Gets or sets whether the command execution has been handled. Once set, no further
	/// command bindings are invoked for this dispatch.
	/// </summary>
	public bool Handled { get; set; }
}

/// <summary>
/// Event data for querying whether an <see cref="EditorCommand"/> can execute.
/// </summary>
public sealed class CanExecuteEditorCommandEventArgs : EventArgs
{
	/// <summary>
	/// Creates new event data for querying the given command.
	/// </summary>
	public CanExecuteEditorCommandEventArgs(EditorCommand command, object? parameter)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		this.Command = command;
		this.Parameter = parameter;
	}

	/// <summary>
	/// Gets the command being queried.
	/// </summary>
	public EditorCommand Command { get; }

	/// <summary>
	/// Gets the command parameter, if any.
	/// </summary>
	public object? Parameter { get; }

	/// <summary>
	/// Gets or sets whether the command can execute. Defaults to false.
	/// </summary>
	public bool CanExecute { get; set; }

	/// <summary>
	/// Gets or sets whether the query has been answered conclusively. Once set, no further
	/// command bindings are asked.
	/// </summary>
	public bool Handled { get; set; }
}

/// <summary>
/// Binds an <see cref="EditorCommand"/> to the handlers that implement it. Bindings are
/// registered in <see cref="TextAreaInputHandler.CommandBindings"/>; a binding instance holds no
/// per-editor state, so the built-in handlers share their bindings between all editor instances.
/// </summary>
public sealed class EditorCommandBinding
{
	/// <summary>
	/// Creates a binding with no handlers attached yet.
	/// </summary>
	/// <param name="command">The command to bind.</param>
	public EditorCommandBinding(EditorCommand command)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		this.Command = command;
	}

	/// <summary>
	/// Creates a binding with an execution handler.
	/// </summary>
	/// <param name="command">The command to bind.</param>
	/// <param name="executed">The handler that executes the command.</param>
	public EditorCommandBinding(EditorCommand command, ExecutedEditorCommandEventHandler? executed)
		: this(command)
	{
		if (executed != null)
			this.Executed += executed;
	}

	/// <summary>
	/// Creates a binding with an execution handler and a can-execute handler.
	/// </summary>
	/// <param name="command">The command to bind.</param>
	/// <param name="executed">The handler that executes the command.</param>
	/// <param name="canExecute">The handler that determines whether the command can execute.</param>
	public EditorCommandBinding(EditorCommand command, ExecutedEditorCommandEventHandler? executed, CanExecuteEditorCommandEventHandler? canExecute)
		: this(command, executed)
	{
		if (canExecute != null)
			this.CanExecute += canExecute;
	}

	/// <summary>
	/// Gets the command this binding implements.
	/// </summary>
	public EditorCommand Command { get; }

	/// <summary>
	/// Occurs when the bound command executes.
	/// </summary>
	public event ExecutedEditorCommandEventHandler? Executed;

	/// <summary>
	/// Occurs when the bound command is queried for whether it can execute. When no handler is
	/// attached here but <see cref="Executed"/> has one, the command counts as always executable.
	/// </summary>
	public event CanExecuteEditorCommandEventHandler? CanExecute;

	/// <summary>
	/// Asks this binding whether the command can execute. Does nothing when
	/// <paramref name="e"/> is already handled. When no <see cref="CanExecute"/> handler is
	/// attached but an <see cref="Executed"/> handler is, answers "yes" and marks the query
	/// handled.
	/// </summary>
	/// <param name="sender">The text area the command would execute against.</param>
	/// <param name="e">The query event data.</param>
	public void OnCanExecute(object sender, CanExecuteEditorCommandEventArgs e)
	{
		if (e == null)
			throw new ArgumentNullException(nameof(e));
		if (e.Handled)
			return;
		CanExecuteEditorCommandEventHandler? canExecute = CanExecute;
		if (canExecute != null)
		{
			canExecute(sender, e);
			if (e.CanExecute)
				e.Handled = true;
		}
		else if (Executed != null)
		{
			e.CanExecute = true;
			e.Handled = true;
		}
	}

	/// <summary>
	/// Executes the command through this binding. Does nothing when <paramref name="e"/> is
	/// already handled or no <see cref="Executed"/> handler is attached; otherwise invokes the
	/// handler and marks the dispatch handled.
	/// </summary>
	/// <param name="sender">The text area the command executes against.</param>
	/// <param name="e">The execution event data.</param>
	public void OnExecuted(object sender, ExecutedEditorCommandEventArgs e)
	{
		if (e == null)
			throw new ArgumentNullException(nameof(e));
		if (e.Handled)
			return;
		ExecutedEditorCommandEventHandler? executed = Executed;
		if (executed != null)
		{
			executed(sender, e);
			e.Handled = true;
		}
	}
}
