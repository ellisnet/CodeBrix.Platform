#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Document;

//was previously: ICSharpCode.AvalonEdit/Document/TextDocumentWeakEventManager.cs in the AvalonEdit repo (MIT).
//Upstream's managers derive from a WeakEventManagerBase built on WPF's WeakEventManager; here they derive
//from this add-in's Utils.WeakEventManagerBase shim, whose DeliverEvent(object?, EventArgs) attaches to the
//document events directly (method-group contravariance covers the EventHandler<DocumentChangeEventArgs> events).

/// <summary>
/// Contains weak event managers for the TextDocument events.
/// </summary>
public static class TextDocumentWeakEventManager
{
	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.UpdateStarted"/> event.
	/// </summary>
	public sealed class UpdateStarted : WeakEventManagerBase<UpdateStarted, TextDocument>
	{
		/// <inheritdoc/>
		protected override void StartListening(TextDocument source)
		{
			source.UpdateStarted += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(TextDocument source)
		{
			source.UpdateStarted -= DeliverEvent;
		}
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.UpdateFinished"/> event.
	/// </summary>
	public sealed class UpdateFinished : WeakEventManagerBase<UpdateFinished, TextDocument>
	{
		/// <inheritdoc/>
		protected override void StartListening(TextDocument source)
		{
			source.UpdateFinished += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(TextDocument source)
		{
			source.UpdateFinished -= DeliverEvent;
		}
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.Changing"/> event.
	/// </summary>
	public sealed class Changing : WeakEventManagerBase<Changing, TextDocument>
	{
		/// <inheritdoc/>
		protected override void StartListening(TextDocument source)
		{
			source.Changing += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(TextDocument source)
		{
			source.Changing -= DeliverEvent;
		}
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.Changed"/> event.
	/// </summary>
	public sealed class Changed : WeakEventManagerBase<Changed, TextDocument>
	{
		/// <inheritdoc/>
		protected override void StartListening(TextDocument source)
		{
			source.Changed += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(TextDocument source)
		{
			source.Changed -= DeliverEvent;
		}
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.TextChanged"/> event.
	/// </summary>
	public sealed class TextChanged : WeakEventManagerBase<TextChanged, TextDocument>
	{
		/// <inheritdoc/>
		protected override void StartListening(TextDocument source)
		{
			source.TextChanged += DeliverEvent;
		}

		/// <inheritdoc/>
		protected override void StopListening(TextDocument source)
		{
			source.TextChanged -= DeliverEvent;
		}
	}
}
