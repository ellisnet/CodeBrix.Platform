#nullable enable

using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingRuleSet.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A highlighting rule set describes a set of spans that are valid at a given code location.
/// </summary>
public class HighlightingRuleSet
{
	/// <summary>
	/// Creates a new RuleSet instance.
	/// </summary>
	public HighlightingRuleSet()
	{
		this.Spans = new NullSafeCollection<HighlightingSpan>();
		this.Rules = new NullSafeCollection<HighlightingRule>();
	}

	/// <summary>
	/// Gets/Sets the name of the rule set.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets the list of spans.
	/// </summary>
	public IList<HighlightingSpan> Spans { get; private set; }

	/// <summary>
	/// Gets the list of rules.
	/// </summary>
	public IList<HighlightingRule> Rules { get; private set; }

	/// <inheritdoc/>
	public override string ToString()
	{
		return "[" + GetType().Name + " " + Name + "]";
	}
}
