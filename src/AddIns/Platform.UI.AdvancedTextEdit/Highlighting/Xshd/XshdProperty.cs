#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdProperty.cs in the AvalonEdit repo (MIT).
//[Serializable] was dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A property in an Xshd file.
/// </summary>
public class XshdProperty : XshdElement
{
	/// <summary>
	/// Gets/sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets/sets the value.
	/// </summary>
	public string? Value { get; set; }

	/// <summary>
	/// Creates a new XshdColor instance.
	/// </summary>
	public XshdProperty()
	{
	}

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return null;
		//			return visitor.VisitProperty(this);
	}
}
