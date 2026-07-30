#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/CaretWeakEventHandler.cs in the AvalonEdit repo
//(MIT). The nested manager derives from this port's Utils.WeakEventManagerBase shim instead of
//the WPF WeakEventManager base, same as the other weak event managers in this port; as upstream,
//DeliverEvent attaches directly to the source event.

/// <summary>
/// Contains classes for handling weak events on the Caret class.
/// </summary>
public static class CaretWeakEventManager
{
	/// <summary>
	/// Handles the Caret.PositionChanged event.
	/// </summary>
	public sealed class PositionChanged : WeakEventManagerBase<PositionChanged, Caret>
	{
		/// <inheritdoc/>
		protected override void StartListening(Caret source)
		{
			source.PositionChanged += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(Caret source)
		{
			source.PositionChanged -= DeliverEvent;
		}
	}
}
