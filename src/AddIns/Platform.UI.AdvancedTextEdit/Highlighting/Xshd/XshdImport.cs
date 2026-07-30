#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdImport.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// &lt;Import&gt; element.
/// </summary>
public class XshdImport : XshdElement
{
	/// <summary>
	/// Gets/sets the referenced rule set.
	/// </summary>
	public XshdReference<XshdRuleSet> RuleSetReference { get; set; }

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitImport(this);
	}
}
