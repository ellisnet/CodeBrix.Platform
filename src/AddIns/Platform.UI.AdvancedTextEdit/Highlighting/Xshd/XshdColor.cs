#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Engine;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/XshdColor.cs in the AvalonEdit repo (MIT).
//FontWeight/FontStyle/FontFamily are now the Windows.UI.Text / Microsoft.UI.Xaml.Media types.
//Binary serialization ([Serializable]/ISerializable, the serialization constructor and
//GetObjectData) was dropped; it is dead on modern .NET.
//WPE1 C8 (the adapters form): the font family, weight and style are STORED neutrally (as in
//HighlightingColor), so an xshd file loads without the XAML object model; the public XAML-typed
//properties are adapters over that storage.

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
	public FontFamily? FontFamily
	{
		get => (FontFamily?)FontFamilyValue?.GetOrCreateInstance(HighlightingColor.CreateFontFamily);
		set => FontFamilyValue = HighlightingColor.FromFontFamily(value);
	}

	/// <summary>The font family, neutral (WPE1 C8): its name and the FontFamily instance created on demand.</summary>
	internal FontFamilyValue? FontFamilyValue { get; set; }

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
	public FontWeight? FontWeight
	{
		get => FontWeightValue is ushort weight ? new FontWeight(weight) : null;
		set => FontWeightValue = value?.Weight;
	}

	/// <summary>The font weight, neutral (WPE1 C8): the numeric weight, or null.</summary>
	internal ushort? FontWeightValue { get; set; }

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
	public FontStyle? FontStyle
	{
		get => FontStyleValue is TextFontStyle style ? (FontStyle)(int)style : null;
		set => FontStyleValue = value is FontStyle style ? (TextFontStyle)(int)style : null;
	}

	/// <summary>The font style, neutral (WPE1 C8): TextLayout's style (the same values as the WinUI FontStyle), or null.</summary>
	internal TextFontStyle? FontStyleValue { get; set; }

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
