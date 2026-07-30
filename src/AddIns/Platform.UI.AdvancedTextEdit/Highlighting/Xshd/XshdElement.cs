#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdElement.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// An element in a XSHD rule set.
/// </summary>
public abstract class XshdElement
{
	/// <summary>
	/// Gets the line number in the .xshd file.
	/// </summary>
	public int LineNumber { get; set; }

	/// <summary>
	/// Gets the column number in the .xshd file.
	/// </summary>
	public int ColumnNumber { get; set; }

	/// <summary>
	/// Applies the visitor to this element.
	/// </summary>
	public abstract object? AcceptVisitor(IXshdVisitor visitor);
}
