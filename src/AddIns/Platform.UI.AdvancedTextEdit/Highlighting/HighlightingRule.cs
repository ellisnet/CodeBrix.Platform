#nullable enable

using System.Text.RegularExpressions;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingRule.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A highlighting rule.
/// </summary>
public class HighlightingRule
{
	/// <summary>
	/// Gets/Sets the regular expression for the rule.
	/// </summary>
	public Regex? Regex { get; set; }

	/// <summary>
	/// Gets/Sets the highlighting color.
	/// </summary>
	public HighlightingColor? Color { get; set; }

	/// <inheritdoc/>
	public override string ToString()
	{
		return "[" + GetType().Name + " " + Regex + "]";
	}
}
