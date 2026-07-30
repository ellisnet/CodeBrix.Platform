#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdRule.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// &lt;Rule&gt; element.
/// </summary>
public class XshdRule : XshdElement
{
	/// <summary>
	/// Gets/sets the rule regex.
	/// </summary>
	public string? Regex { get; set; }

	/// <summary>
	/// Gets/sets the rule regex type.
	/// </summary>
	public XshdRegexType RegexType { get; set; }

	/// <summary>
	/// Gets/sets the color reference.
	/// </summary>
	public XshdReference<XshdColor> ColorReference { get; set; }

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitRule(this);
	}
}
