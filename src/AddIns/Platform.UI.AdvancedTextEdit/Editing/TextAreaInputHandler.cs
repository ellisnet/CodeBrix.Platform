#nullable enable

using System;
using System.Collections.Generic;

using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/TextAreaInputHandler.cs in the AvalonEdit repo
//(MIT). The WPF CommandBinding/InputBinding types are replaced by this port's own
//EditorCommandBinding/KeyBinding (Editing/Input); the collections keep upstream names and
//Attach/Detach semantics (attached handlers mirror their bindings into the text area's
//aggregated CommandBindings/InputBindings collections). Because there is no WPF command routing,
//this class adds the key-dispatch walk itself: TextArea calls HandleKeyDown(key, modifiers) on
//its active input handler, which resolves key bindings and command default gestures against the
//command bindings of this handler and its nested handlers (see the KEY DISPATCH SEAM region).
//TextAreaStackedInputHandler's OnPreviewKeyDown/OnPreviewKeyUp now take the key and modifiers
//directly and return whether the event was handled, instead of receiving WPF KeyEventArgs.

/// <summary>
/// A set of input bindings and event handlers for the text area.
/// </summary>
/// <remarks>
/// <para>
/// There is one active input handler per text area (<see cref="TextArea.ActiveInputHandler"/>),
/// plus a number of active stacked input handlers.
/// </para>
/// <para>
/// The text area also stores a reference to a default input handler, but that is not necessarily active.
/// </para>
/// <para>
/// Stacked input handlers work in addition to the set of currently active handlers (without detaching them).
/// They are detached in the reverse order of being attached.
/// </para>
/// </remarks>
public interface ITextAreaInputHandler
{
	/// <summary>
	/// Gets the text area that the input handler belongs to.
	/// </summary>
	TextArea TextArea
	{
		get;
	}

	/// <summary>
	/// Attaches an input handler to the text area.
	/// </summary>
	void Attach();

	/// <summary>
	/// Detaches the input handler from the text area.
	/// </summary>
	void Detach();
}

/// <summary>
/// Stacked input handler.
/// Uses OnEvent-methods instead of registering event handlers to ensure that the events are handled in the correct order.
/// </summary>
public abstract class TextAreaStackedInputHandler : ITextAreaInputHandler
{
	readonly TextArea textArea;

	/// <inheritdoc/>
	public TextArea TextArea
	{
		get { return textArea; }
	}

	/// <summary>
	/// Creates a new TextAreaStackedInputHandler.
	/// </summary>
	protected TextAreaStackedInputHandler(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;
	}

	/// <inheritdoc/>
	public virtual void Attach()
	{
	}

	/// <inheritdoc/>
	public virtual void Detach()
	{
	}

	/// <summary>
	/// Called by the text area for a key-down event before the active input handler runs.
	/// Stacked handlers are called in the reverse order of being pushed.
	/// </summary>
	/// <param name="key">The key that was pressed.</param>
	/// <param name="modifiers">The modifier keys active for the key press.</param>
	/// <returns>
	/// True to mark the key press handled (the active input handler and text input then do not
	/// see it); false to let processing continue.
	/// </returns>
	public virtual bool OnPreviewKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		return false;
	}

	/// <summary>
	/// Called by the text area for a key-up event before the active input handler runs.
	/// Stacked handlers are called in the reverse order of being pushed.
	/// </summary>
	/// <param name="key">The key that was released.</param>
	/// <param name="modifiers">The modifier keys active for the key release.</param>
	/// <returns>True to mark the key release handled; false to let processing continue.</returns>
	public virtual bool OnPreviewKeyUp(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		return false;
	}
}

/// <summary>
/// Default-implementation of <see cref="ITextAreaInputHandler"/>.
/// </summary>
/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
public class TextAreaInputHandler : ITextAreaInputHandler
{
	readonly ObserveAddRemoveCollection<EditorCommandBinding> commandBindings;
	readonly ObserveAddRemoveCollection<KeyBinding> inputBindings;
	readonly ObserveAddRemoveCollection<ITextAreaInputHandler> nestedInputHandlers;
	readonly TextArea textArea;
	bool isAttached;

	/// <summary>
	/// Creates a new TextAreaInputHandler.
	/// </summary>
	public TextAreaInputHandler(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;
		commandBindings = new ObserveAddRemoveCollection<EditorCommandBinding>(CommandBinding_Added, CommandBinding_Removed);
		inputBindings = new ObserveAddRemoveCollection<KeyBinding>(InputBinding_Added, InputBinding_Removed);
		nestedInputHandlers = new ObserveAddRemoveCollection<ITextAreaInputHandler>(NestedInputHandler_Added, NestedInputHandler_Removed);
	}

	/// <inheritdoc/>
	public TextArea TextArea
	{
		get { return textArea; }
	}

	/// <summary>
	/// Gets whether the input handler is currently attached to the text area.
	/// </summary>
	public bool IsAttached
	{
		get { return isAttached; }
	}

	#region CommandBindings / InputBindings
	/// <summary>
	/// Gets the command bindings of this input handler.
	/// </summary>
	public ICollection<EditorCommandBinding> CommandBindings
	{
		get { return commandBindings; }
	}

	void CommandBinding_Added(EditorCommandBinding commandBinding)
	{
		if (isAttached)
			textArea.CommandBindings.Add(commandBinding);
	}

	void CommandBinding_Removed(EditorCommandBinding commandBinding)
	{
		if (isAttached)
			textArea.CommandBindings.Remove(commandBinding);
	}

	/// <summary>
	/// Gets the input bindings of this input handler.
	/// </summary>
	public ICollection<KeyBinding> InputBindings
	{
		get { return inputBindings; }
	}

	void InputBinding_Added(KeyBinding inputBinding)
	{
		if (isAttached)
			textArea.InputBindings.Add(inputBinding);
	}

	void InputBinding_Removed(KeyBinding inputBinding)
	{
		if (isAttached)
			textArea.InputBindings.Remove(inputBinding);
	}

	/// <summary>
	/// Adds a command and input binding.
	/// </summary>
	/// <param name="command">The command ID.</param>
	/// <param name="modifiers">The modifiers of the keyboard shortcut.</param>
	/// <param name="key">The key of the keyboard shortcut.</param>
	/// <param name="handler">The event handler to run when the command is executed.</param>
	public void AddBinding(EditorCommand command, VirtualKeyModifiers modifiers, VirtualKey key, ExecutedEditorCommandEventHandler handler)
	{
		this.CommandBindings.Add(new EditorCommandBinding(command, handler));
		this.InputBindings.Add(new KeyBinding(command, key, modifiers));
	}
	#endregion

	#region NestedInputHandlers
	/// <summary>
	/// Gets the collection of nested input handlers. NestedInputHandlers are activated and deactivated
	/// together with this input handler.
	/// </summary>
	public ICollection<ITextAreaInputHandler> NestedInputHandlers
	{
		get { return nestedInputHandlers; }
	}

	void NestedInputHandler_Added(ITextAreaInputHandler handler)
	{
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));
		if (handler.TextArea != textArea)
			throw new ArgumentException("The nested handler must be working for the same text area!");
		if (isAttached)
			handler.Attach();
	}

	void NestedInputHandler_Removed(ITextAreaInputHandler handler)
	{
		if (isAttached)
			handler.Detach();
	}
	#endregion

	#region Attach/Detach
	/// <inheritdoc/>
	public virtual void Attach()
	{
		if (isAttached)
			throw new InvalidOperationException("Input handler is already attached");
		isAttached = true;

		foreach (EditorCommandBinding b in commandBindings)
			textArea.CommandBindings.Add(b);
		foreach (KeyBinding b in inputBindings)
			textArea.InputBindings.Add(b);
		foreach (ITextAreaInputHandler handler in nestedInputHandlers)
			handler.Attach();
	}

	/// <inheritdoc/>
	public virtual void Detach()
	{
		if (!isAttached)
			throw new InvalidOperationException("Input handler is not attached");
		isAttached = false;

		foreach (EditorCommandBinding b in commandBindings)
			textArea.CommandBindings.Remove(b);
		foreach (KeyBinding b in inputBindings)
			textArea.InputBindings.Remove(b);
		foreach (ITextAreaInputHandler handler in nestedInputHandlers)
			handler.Detach();
	}
	#endregion

	#region KEY DISPATCH SEAM (replaces WPF command routing)
	//was previously: WPF routed key presses to the bindings the attached handlers had mirrored
	//into the text area's collections. This port dispatches explicitly: TextArea's key-down
	//processing must (1) offer the key press to the stacked input handlers' OnPreviewKeyDown, in
	//reverse push order, then (2) call HandleKeyDown(key, modifiers) on the ACTIVE input handler,
	//and mark the framework key event handled when it returns true. Typed-character input is
	//processed only for key presses no handler claimed.

	/// <summary>
	/// Dispatches a key press against this input handler. This is the seam the text area's
	/// key-down processing calls on its active input handler.
	/// </summary>
	/// <remarks>
	/// The walk order is: (1) this handler's <see cref="InputBindings"/> (a matching gesture
	/// dispatches its command); (2) the <see cref="EditorCommand.DefaultGestures"/> of the
	/// commands bound in this handler's <see cref="CommandBindings"/> (this replaces WPF's
	/// built-in command gestures, e.g. Ctrl+C firing Copy without an explicit key binding);
	/// (3) the nested input handlers, in order. A command dispatched from a key press executes
	/// only when its can-execute query answers true; a false answer lets the walk continue.
	/// </remarks>
	/// <param name="key">The key that was pressed.</param>
	/// <param name="modifiers">The modifier keys active for the key press.</param>
	/// <returns>True when a command consumed the key press; false otherwise.</returns>
	public bool HandleKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		foreach (KeyBinding keyBinding in inputBindings)
		{
			if (keyBinding.Gesture.Matches(key, modifiers))
			{
				if (ExecuteCommand(keyBinding.Command, keyBinding.CommandParameter))
					return true;
			}
		}
		foreach (EditorCommandBinding commandBinding in commandBindings)
		{
			foreach (KeyGesture gesture in commandBinding.Command.DefaultGestures)
			{
				if (gesture.Matches(key, modifiers))
				{
					if (ExecuteCommand(commandBinding.Command, null))
						return true;
					break;
				}
			}
		}
		foreach (ITextAreaInputHandler nested in nestedInputHandlers)
		{
			if (nested is TextAreaInputHandler nestedHandler && nestedHandler.HandleKeyDown(key, modifiers))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Executes a command against the command bindings of this handler and its nested handlers.
	/// The can-execute query runs first; when it answers false the command does not execute.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="parameter">The command parameter, or null.</param>
	/// <returns>True when a binding executed the command; false otherwise.</returns>
	public bool ExecuteCommand(EditorCommand command, object? parameter)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		CanExecuteEditorCommandEventArgs canExecuteArgs = new CanExecuteEditorCommandEventArgs(command, parameter);
		QueryCanExecuteCore(canExecuteArgs);
		if (!canExecuteArgs.CanExecute)
			return false;
		ExecutedEditorCommandEventArgs executedArgs = new ExecutedEditorCommandEventArgs(command, parameter);
		ExecuteCore(executedArgs);
		return executedArgs.Handled;
	}

	/// <summary>
	/// Queries whether a command can execute against the command bindings of this handler and
	/// its nested handlers.
	/// </summary>
	/// <param name="command">The command to query.</param>
	/// <param name="parameter">The command parameter, or null.</param>
	/// <param name="canExecute">Receives whether the command can execute.</param>
	/// <returns>
	/// True when some binding answered the query conclusively; false when no binding for the
	/// command answered (callers may then fall through to other handlers).
	/// </returns>
	public bool CanExecuteCommand(EditorCommand command, object? parameter, out bool canExecute)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		CanExecuteEditorCommandEventArgs canExecuteArgs = new CanExecuteEditorCommandEventArgs(command, parameter);
		QueryCanExecuteCore(canExecuteArgs);
		canExecute = canExecuteArgs.CanExecute;
		return canExecuteArgs.Handled;
	}

	void QueryCanExecuteCore(CanExecuteEditorCommandEventArgs e)
	{
		foreach (EditorCommandBinding binding in commandBindings)
		{
			if (binding.Command == e.Command)
			{
				binding.OnCanExecute(textArea, e);
				if (e.Handled)
					return;
			}
		}
		foreach (ITextAreaInputHandler nested in nestedInputHandlers)
		{
			if (nested is TextAreaInputHandler nestedHandler)
			{
				nestedHandler.QueryCanExecuteCore(e);
				if (e.Handled)
					return;
			}
		}
	}

	void ExecuteCore(ExecutedEditorCommandEventArgs e)
	{
		foreach (EditorCommandBinding binding in commandBindings)
		{
			if (binding.Command == e.Command)
			{
				binding.OnExecuted(textArea, e);
				if (e.Handled)
					return;
			}
		}
		foreach (ITextAreaInputHandler nested in nestedInputHandlers)
		{
			if (nested is TextAreaInputHandler nestedHandler)
			{
				nestedHandler.ExecuteCore(e);
				if (e.Handled)
					return;
			}
		}
	}
	#endregion
}
