#nullable enable

using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdRuleSet.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A rule set in a XSHD file.
/// </summary>
public class XshdRuleSet : XshdElement
{
	/// <summary>
	/// Gets/Sets the name of the rule set.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets/sets whether the case is ignored in expressions inside this rule set.
	/// </summary>
	public bool? IgnoreCase { get; set; }

	readonly NullSafeCollection<XshdElement> elements = new NullSafeCollection<XshdElement>();

	/// <summary>
	/// Gets the collection of elements.
	/// </summary>
	public IList<XshdElement> Elements
	{
		get { return elements; }
	}

	/// <summary>
	/// Applies the visitor to all elements.
	/// </summary>
	public void AcceptElements(IXshdVisitor visitor)
	{
		foreach (XshdElement element in Elements)
		{
			element.AcceptVisitor(visitor);
		}
	}

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitRuleSet(this);
	}
}
