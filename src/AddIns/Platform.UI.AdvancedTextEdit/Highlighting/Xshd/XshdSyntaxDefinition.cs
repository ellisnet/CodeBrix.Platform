#nullable enable

using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdSyntaxDefinition.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A &lt;SyntaxDefinition&gt; element.
/// </summary>
public class XshdSyntaxDefinition
{
	/// <summary>
	/// Creates a new XshdSyntaxDefinition object.
	/// </summary>
	public XshdSyntaxDefinition()
	{
		this.Elements = new NullSafeCollection<XshdElement>();
		this.Extensions = new NullSafeCollection<string>();
	}

	/// <summary>
	/// Gets/sets the definition name
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets the associated extensions.
	/// </summary>
	public IList<string> Extensions { get; private set; }

	/// <summary>
	/// Gets the collection of elements.
	/// </summary>
	public IList<XshdElement> Elements { get; private set; }

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
}
