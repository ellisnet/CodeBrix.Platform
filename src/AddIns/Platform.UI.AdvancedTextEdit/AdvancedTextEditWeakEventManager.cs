#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit;

//was previously: ICSharpCode.AvalonEdit/TextEditorWeakEventManager.cs in the AvalonEdit repo
//(MIT), where the container class was named TextEditorWeakEventManager; renamed per the port
//naming rules. The nested managers derive from this port's Utils.WeakEventManagerBase shim
//instead of the framework weak event manager the upstream file used; as upstream, the protected
//DeliverEvent method attaches directly to the source events (for OptionChanged, delegate
//contravariance lets a (object, EventArgs) handler serve a PropertyChangedEventHandler event).

/// <summary>
/// Contains weak event managers for <see cref="ITextEditorComponent"/>.
/// </summary>
public static class AdvancedTextEditWeakEventManager
{
	/// <summary>
	/// Weak event manager for the <see cref="ITextEditorComponent.DocumentChanged"/> event.
	/// </summary>
	public sealed class DocumentChanged : WeakEventManagerBase<DocumentChanged, ITextEditorComponent>
	{
		/// <inheritdoc/>
		protected override void StartListening(ITextEditorComponent source)
		{
			source.DocumentChanged += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(ITextEditorComponent source)
		{
			source.DocumentChanged -= DeliverEvent;
		}
	}

	/// <summary>
	/// Weak event manager for the <see cref="ITextEditorComponent.OptionChanged"/> event.
	/// </summary>
	public sealed class OptionChanged : WeakEventManagerBase<OptionChanged, ITextEditorComponent>
	{
		/// <inheritdoc/>
		protected override void StartListening(ITextEditorComponent source)
		{
			source.OptionChanged += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(ITextEditorComponent source)
		{
			source.OptionChanged -= DeliverEvent;
		}
	}
}
