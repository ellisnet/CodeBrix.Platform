#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/SnippetContainerElement.cs in the AvalonEdit
//repo (MIT). The ToTextRun override is not ported (see SnippetElement.cs).

/// <summary>
/// A snippet element that has sub-elements.
/// </summary>
[Serializable]
public class SnippetContainerElement : SnippetElement
{
	readonly NullSafeCollection<SnippetElement> elements = new NullSafeCollection<SnippetElement>();

	/// <summary>
	/// Gets the list of child elements.
	/// </summary>
	public IList<SnippetElement> Elements {
		get { return elements; }
	}

	/// <inheritdoc/>
	public override void Insert(InsertionContext context)
	{
		foreach (SnippetElement e in this.Elements)
		{
			e.Insert(context);
		}
	}
}
