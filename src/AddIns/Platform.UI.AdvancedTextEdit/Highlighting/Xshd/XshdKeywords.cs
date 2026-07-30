#nullable enable

using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdKeywords.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A list of keywords.
/// </summary>
public class XshdKeywords : XshdElement
{
	/// <summary>
	/// The color.
	/// </summary>
	public XshdReference<XshdColor> ColorReference { get; set; }

	readonly NullSafeCollection<string> words = new NullSafeCollection<string>();

	/// <summary>
	/// Gets the list of key words.
	/// </summary>
	public IList<string> Words
	{
		get { return words; }
	}

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitKeywords(this);
	}
}
