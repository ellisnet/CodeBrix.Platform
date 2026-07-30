#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: the WPF System.Windows.IWeakEventListener contract (WindowsBase), which this
//framework does not provide. Re-declared here with the same shape so the editor's weak-event
//pattern - and every upstream consumer of it - ports mechanically.

/// <summary>
/// Receives events dispatched by a <see cref="WeakEventManagerBase{TManager, TEventSource}"/>
/// without the event source holding a strong reference to the receiver.
/// </summary>
public interface IWeakEventListener
{
	/// <summary>
	/// Handles an event delivered by a weak event manager.
	/// </summary>
	/// <param name="managerType">The type of the manager that delivered the event.</param>
	/// <param name="sender">The object that raised the event.</param>
	/// <param name="e">The event data.</param>
	/// <returns>
	/// True if the listener handled the event; false if it does not recognize the manager type.
	/// Returning false is treated as a programming error by the delivering manager.
	/// </returns>
	bool ReceiveWeakEvent(Type managerType, object? sender, EventArgs e);
}
