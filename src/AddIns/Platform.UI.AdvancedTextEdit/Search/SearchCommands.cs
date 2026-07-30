#nullable enable

using System;
using System.Collections.Generic;

using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Search;

//was previously: ICSharpCode.AvalonEdit/Search/SearchCommands.cs in the AvalonEdit repo (MIT).
//The WPF RoutedCommands became EditorCommand instances on the port's own command system (the
//gestures ride on the commands as default gestures); the Find command is the shared
//EditorCommands.Find (upstream: ApplicationCommands.Find). CanExecuteWithOpenSearchPanel
//expresses WPF's ContinueRouting by leaving the can-execute query unhandled, which lets the
//key-dispatch walk continue to other handlers.

/// <summary>
/// The search-related commands of the editor. The Ctrl+F command that shows the panel is the
/// shared <see cref="EditorCommands.Find"/>.
/// </summary>
public static class SearchCommands
{
	/// <summary>
	/// Finds the next occurrence in the file. Default gesture: F3.
	/// </summary>
	public static readonly EditorCommand FindNext = new EditorCommand("FindNext",
		new KeyGesture(VirtualKey.F3));

	/// <summary>
	/// Finds the previous occurrence in the file. Default gesture: Shift+F3.
	/// </summary>
	public static readonly EditorCommand FindPrevious = new EditorCommand("FindPrevious",
		new KeyGesture(VirtualKey.F3, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Closes the SearchPanel. Default gesture: Escape.
	/// </summary>
	public static readonly EditorCommand CloseSearchPanel = new EditorCommand("CloseSearchPanel",
		new KeyGesture(VirtualKey.Escape));
}

/// <summary>
/// TextAreaInputHandler that registers all search-related commands.
/// </summary>
public class SearchInputHandler : TextAreaInputHandler
{
	internal SearchInputHandler(TextArea textArea, SearchPanel panel)
		: base(textArea)
	{
		RegisterCommands(this.CommandBindings);
		this.panel = panel;
	}

	internal void RegisterGlobalCommands(ICollection<EditorCommandBinding> commandBindings)
	{
		commandBindings.Add(new EditorCommandBinding(EditorCommands.Find, ExecuteFind));
		commandBindings.Add(new EditorCommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
		commandBindings.Add(new EditorCommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
	}

	void RegisterCommands(ICollection<EditorCommandBinding> commandBindings)
	{
		commandBindings.Add(new EditorCommandBinding(EditorCommands.Find, ExecuteFind));
		commandBindings.Add(new EditorCommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
		commandBindings.Add(new EditorCommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
		commandBindings.Add(new EditorCommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
	}

	readonly SearchPanel panel;

	void ExecuteFind(object sender, ExecutedEditorCommandEventArgs e)
	{
		panel.Open();
		if (!(TextArea.Selection.IsEmpty || TextArea.Selection.IsMultiline))
			panel.SearchPattern = TextArea.Selection.GetText();
		//was previously: Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, ...);
		//without a dispatcher (host-free unit tests) the reactivation runs inline.
		var dispatcherQueue = TextArea.DispatcherQueue;
		if (dispatcherQueue != null)
			dispatcherQueue.TryEnqueue(() => panel.Reactivate());
		else
			panel.Reactivate();
	}

	void CanExecuteWithOpenSearchPanel(object sender, CanExecuteEditorCommandEventArgs e)
	{
		if (panel.IsClosed)
		{
			e.CanExecute = false;
			// Leave the query unhandled so that the key gesture can be consumed by another handler.
			//was previously: e.ContinueRouting = true.
		}
		else
		{
			e.CanExecute = true;
			e.Handled = true;
		}
	}

	void ExecuteFindNext(object sender, ExecutedEditorCommandEventArgs e)
	{
		if (!panel.IsClosed)
		{
			panel.FindNext();
			e.Handled = true;
		}
	}

	void ExecuteFindPrevious(object sender, ExecutedEditorCommandEventArgs e)
	{
		if (!panel.IsClosed)
		{
			panel.FindPrevious();
			e.Handled = true;
		}
	}

	void ExecuteCloseSearchPanel(object sender, ExecutedEditorCommandEventArgs e)
	{
		if (!panel.IsClosed)
		{
			panel.Close();
			e.Handled = true;
		}
	}

	/// <summary>
	/// Fired when SearchOptions are modified inside the SearchPanel.
	/// </summary>
	public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged {
		add { panel.SearchOptionsChanged += value; }
		remove { panel.SearchOptionsChanged -= value; }
	}
}
