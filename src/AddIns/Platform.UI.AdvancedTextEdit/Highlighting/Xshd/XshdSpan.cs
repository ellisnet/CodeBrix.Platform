#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdSpan.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// Specifies the type of the regex.
/// </summary>
public enum XshdRegexType
{
	/// <summary>
	/// Normal regex. Used when the regex was specified as attribute.
	/// </summary>
	Default,

	/// <summary>
	/// Ignore pattern whitespace / allow regex comments. Used when the regex was specified as text element.
	/// </summary>
	IgnorePatternWhitespace
}

/// <summary>
/// &lt;Span&gt; element.
/// </summary>
public class XshdSpan : XshdElement
{
	/// <summary>
	/// Gets/sets the begin regex.
	/// </summary>
	public string? BeginRegex { get; set; }

	/// <summary>
	/// Gets/sets the begin regex type.
	/// </summary>
	public XshdRegexType BeginRegexType { get; set; }

	/// <summary>
	/// Gets/sets the end regex.
	/// </summary>
	public string? EndRegex { get; set; }

	/// <summary>
	/// Gets/sets the end regex type.
	/// </summary>
	public XshdRegexType EndRegexType { get; set; }

	/// <summary>
	/// Gets/sets whether the span is multiline.
	/// </summary>
	public bool Multiline { get; set; }

	/// <summary>
	/// Gets/sets the rule set reference.
	/// </summary>
	public XshdReference<XshdRuleSet> RuleSetReference { get; set; }

	/// <summary>
	/// Gets/sets the span color.
	/// </summary>
	public XshdReference<XshdColor> SpanColorReference { get; set; }

	/// <summary>
	/// Gets/sets the span begin color.
	/// </summary>
	public XshdReference<XshdColor> BeginColorReference { get; set; }

	/// <summary>
	/// Gets/sets the span end color.
	/// </summary>
	public XshdReference<XshdColor> EndColorReference { get; set; }

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitSpan(this);
	}
}
