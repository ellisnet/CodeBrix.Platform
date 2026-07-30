#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdColor.cs in the AvalonEdit repo (MIT).
//FontWeight/FontStyle/FontFamily are now the Windows.UI.Text / Microsoft.UI.Xaml.Media types.
//Binary serialization ([Serializable]/ISerializable, the serialization constructor and
//GetObjectData) was dropped; it is dead on modern .NET.

/// <summary>
/// A color in an Xshd file.
/// </summary>
public class XshdColor : XshdElement
{
	/// <summary>
	/// Gets/sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets/sets the font family
	/// </summary>
	public FontFamily? FontFamily { get; set; }

	/// <summary>
	/// Gets/sets the font size.
	/// </summary>
	public int? FontSize { get; set; }

	/// <summary>
	/// Gets/sets the foreground brush.
	/// </summary>
	public HighlightingBrush? Foreground { get; set; }

	/// <summary>
	/// Gets/sets the background brush.
	/// </summary>
	public HighlightingBrush? Background { get; set; }

	/// <summary>
	/// Gets/sets the font weight.
	/// </summary>
	public FontWeight? FontWeight { get; set; }

	/// <summary>
	/// Gets/sets the underline flag
	/// </summary>
	public bool? Underline { get; set; }

	/// <summary>
	/// Gets/sets the strikethrough flag
	/// </summary>
	public bool? Strikethrough { get; set; }

	/// <summary>
	/// Gets/sets the font style.
	/// </summary>
	public FontStyle? FontStyle { get; set; }

	/// <summary>
	/// Gets/Sets the example text that demonstrates where the color is used.
	/// </summary>
	public string? ExampleText { get; set; }

	/// <summary>
	/// Creates a new XshdColor instance.
	/// </summary>
	public XshdColor()
	{
	}

	/// <inheritdoc/>
	public override object? AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitColor(this);
	}
}
