#nullable enable

using System.Collections.Generic;

using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/SnippetInputHandler.cs in the AvalonEdit repo
//(MIT). OnPreviewKeyDown receives the key and modifiers directly and returns whether the key was
//handled (this port's TextAreaStackedInputHandler shape) instead of mutating WPF KeyEventArgs;
//WPF's Key.Return maps to VirtualKey.Enter.

sealed class SnippetInputHandler : TextAreaStackedInputHandler
{
	readonly InsertionContext context;

	public SnippetInputHandler(InsertionContext context)
		: base(context.TextArea)
	{
		this.context = context;
	}

	public override void Attach()
	{
		base.Attach();

		SelectElement(FindNextEditableElement(-1, false));
	}

	public override void Detach()
	{
		base.Detach();
		context.Deactivate(new SnippetEventArgs(DeactivateReason.InputHandlerDetached));
	}

	public override bool OnPreviewKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		if (key == VirtualKey.Escape)
		{
			context.Deactivate(new SnippetEventArgs(DeactivateReason.EscapePressed));
			return true;
		}
		else if (key == VirtualKey.Enter)
		{
			context.Deactivate(new SnippetEventArgs(DeactivateReason.ReturnPressed));
			return true;
		}
		else if (key == VirtualKey.Tab)
		{
			bool backwards = modifiers == VirtualKeyModifiers.Shift;
			SelectElement(FindNextEditableElement(TextArea.Caret.Offset, backwards));
			return true;
		}
		return base.OnPreviewKeyDown(key, modifiers);
	}

	void SelectElement(IActiveElement? element)
	{
		if (element != null && element.Segment is ISegment segment)
		{
			TextArea.Selection = Selection.Create(TextArea, segment);
			TextArea.Caret.Offset = segment.EndOffset;
		}
	}

	IActiveElement? FindNextEditableElement(int offset, bool backwards)
	{
		// (element, segment) pairs so the null-filtered segments stay available without re-checking
		List<(IActiveElement Element, ISegment Segment)> elements = new List<(IActiveElement, ISegment)>();
		foreach (IActiveElement element in context.ActiveElements)
		{
			if (element.IsEditable && element.Segment is ISegment segment)
				elements.Add((element, segment));
		}
		if (backwards)
		{
			elements.Reverse();
			foreach ((IActiveElement element, ISegment segment) in elements)
			{
				if (offset > segment.EndOffset)
					return element;
			}
		}
		else
		{
			foreach ((IActiveElement element, ISegment segment) in elements)
			{
				if (offset < segment.Offset)
					return element;
			}
		}
		return elements.Count > 0 ? elements[0].Element : null;
	}
}
