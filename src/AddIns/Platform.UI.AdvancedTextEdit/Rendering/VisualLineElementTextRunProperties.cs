#nullable enable

using System;
using System.Globalization;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineElementTextRunProperties.cs in the
//AvalonEdit repo (MIT), where it subclassed the WPF TextRunProperties type. This framework has no
//such base type, so the class is now a standalone mutable property bag: Typeface became
//FontFamily/FontWeight/FontStyle/FontStretch, FontRenderingEmSize became FontSize (SetFontSize),
//and the TextDecorationCollection became two booleans (Underline/Strikethrough) with
//SetTextDecorations kept as a flags-based additive setter. FontHintingEmSize, BaselineAlignment,
//TextEffects, TypographyProperties and NumberSubstitution have no engine backing and were dropped.

/// <summary>
/// Mutable text run properties. A unique <see cref="VisualLineElementTextRunProperties"/> instance
/// is used for each <see cref="VisualLineElement"/>; colorizing code may assume that modifying the
/// <see cref="VisualLineElementTextRunProperties"/> will affect only this
/// <see cref="VisualLineElement"/>.
/// </summary>
public class VisualLineElementTextRunProperties : ICloneable
{
	Brush? backgroundBrush;
	Brush? foregroundBrush;
	string? fontFamily;
	double fontSize;
	FontWeight fontWeight;
	FontStyle fontStyle;
	FontStretch fontStretch;
	bool underline;
	bool strikethrough;
	CultureInfo cultureInfo;

	/// <summary>
	/// Creates a new VisualLineElementTextRunProperties instance that copies its values
	/// from the specified global properties.
	/// </summary>
	/// <exception cref="ArgumentNullException"><paramref name="textRunProperties"/> is null.</exception>
	public VisualLineElementTextRunProperties(GlobalTextRunProperties textRunProperties)
	{
		if (textRunProperties == null)
			throw new ArgumentNullException(nameof(textRunProperties));
		backgroundBrush = textRunProperties.BackgroundBrush;
		foregroundBrush = textRunProperties.ForegroundBrush;
		fontFamily = textRunProperties.FontFamily;
		fontSize = textRunProperties.FontSize;
		fontWeight = textRunProperties.FontWeight;
		fontStyle = textRunProperties.FontStyle;
		fontStretch = textRunProperties.FontStretch;
		cultureInfo = textRunProperties.CultureInfo;
	}

	/// <summary>
	/// Creates a new VisualLineElementTextRunProperties instance that copies its values
	/// from another instance.
	/// </summary>
	/// <exception cref="ArgumentNullException"><paramref name="textRunProperties"/> is null.</exception>
	public VisualLineElementTextRunProperties(VisualLineElementTextRunProperties textRunProperties)
	{
		if (textRunProperties == null)
			throw new ArgumentNullException(nameof(textRunProperties));
		backgroundBrush = textRunProperties.backgroundBrush;
		foregroundBrush = textRunProperties.foregroundBrush;
		fontFamily = textRunProperties.fontFamily;
		fontSize = textRunProperties.fontSize;
		fontWeight = textRunProperties.fontWeight;
		fontStyle = textRunProperties.fontStyle;
		fontStretch = textRunProperties.fontStretch;
		underline = textRunProperties.underline;
		strikethrough = textRunProperties.strikethrough;
		cultureInfo = textRunProperties.cultureInfo;
	}

	/// <summary>
	/// Creates a copy of this instance.
	/// </summary>
	public virtual VisualLineElementTextRunProperties Clone()
	{
		return new VisualLineElementTextRunProperties(this);
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	/// <summary>
	/// Gets the brush painted behind the element's text, or null for no background.
	/// </summary>
	public Brush? BackgroundBrush {
		get { return backgroundBrush; }
	}

	/// <summary>
	/// Sets the <see cref="BackgroundBrush"/>.
	/// </summary>
	public void SetBackgroundBrush(Brush? value)
	{
		backgroundBrush = value;
	}

	/// <summary>
	/// Gets the brush the element's text is painted with, or null to use the view's default color.
	/// </summary>
	public Brush? ForegroundBrush {
		get { return foregroundBrush; }
	}

	/// <summary>
	/// Sets the <see cref="ForegroundBrush"/>.
	/// </summary>
	public void SetForegroundBrush(Brush? value)
	{
		foregroundBrush = value;
	}

	/// <summary>
	/// Gets the foreground as a Skia color, derived from <see cref="ForegroundBrush"/> when that is
	/// a <see cref="SolidColorBrush"/>; null when there is no brush or the brush has no single color.
	/// </summary>
	public SKColor? ForegroundColor {
		get { return GetSolidColor(foregroundBrush); }
	}

	/// <summary>
	/// Gets the font family name to resolve, or null for the platform default family.
	/// </summary>
	public string? FontFamily {
		get { return fontFamily; }
	}

	/// <summary>
	/// Sets the <see cref="FontFamily"/>.
	/// </summary>
	public void SetFontFamily(string? value)
	{
		fontFamily = value;
	}

	/// <summary>
	/// Gets the em size, in device-independent pixels.
	/// </summary>
	public double FontSize {
		get { return fontSize; }
	}

	/// <summary>
	/// Sets the <see cref="FontSize"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not greater than zero.</exception>
	public void SetFontSize(double value)
	{
		if (!(value > 0))
			throw new ArgumentOutOfRangeException(nameof(value), value, "Font size must be greater than zero.");
		fontSize = value;
	}

	/// <summary>
	/// Gets the font weight.
	/// </summary>
	public FontWeight FontWeight {
		get { return fontWeight; }
	}

	/// <summary>
	/// Sets the <see cref="FontWeight"/>.
	/// </summary>
	public void SetFontWeight(FontWeight value)
	{
		fontWeight = value;
	}

	/// <summary>
	/// Gets the font style.
	/// </summary>
	public FontStyle FontStyle {
		get { return fontStyle; }
	}

	/// <summary>
	/// Sets the <see cref="FontStyle"/>.
	/// </summary>
	public void SetFontStyle(FontStyle value)
	{
		fontStyle = value;
	}

	/// <summary>
	/// Gets the font stretch.
	/// </summary>
	public FontStretch FontStretch {
		get { return fontStretch; }
	}

	/// <summary>
	/// Sets the <see cref="FontStretch"/>.
	/// </summary>
	public void SetFontStretch(FontStretch value)
	{
		fontStretch = value;
	}

	/// <summary>
	/// Gets whether the element's text is underlined.
	/// </summary>
	public bool Underline {
		get { return underline; }
	}

	/// <summary>
	/// Sets <see cref="Underline"/>.
	/// </summary>
	public void SetUnderline(bool value)
	{
		underline = value;
	}

	/// <summary>
	/// Gets whether the element's text is struck through.
	/// </summary>
	public bool Strikethrough {
		get { return strikethrough; }
	}

	/// <summary>
	/// Sets <see cref="Strikethrough"/>.
	/// </summary>
	public void SetStrikethrough(bool value)
	{
		strikethrough = value;
	}

	/// <summary>
	/// Gets the decorations currently applied, as flags derived from <see cref="Underline"/> and
	/// <see cref="Strikethrough"/>.
	/// </summary>
	public TextDecorations TextDecorations {
		get {
			var result = TextDecorations.None;
			if (underline)
				result |= TextDecorations.Underline;
			if (strikethrough)
				result |= TextDecorations.Strikethrough;
			return result;
		}
	}

	/// <summary>
	/// Adds the specified decorations to the element.
	/// </summary>
	/// <remarks>
	/// Like the original API this is additive: decorations already applied stay applied. Use
	/// <see cref="SetUnderline"/>/<see cref="SetStrikethrough"/> to remove one.
	/// </remarks>
	public void SetTextDecorations(TextDecorations value)
	{
		if ((value & TextDecorations.Underline) != 0)
			underline = true;
		if ((value & TextDecorations.Strikethrough) != 0)
			strikethrough = true;
	}

	/// <summary>
	/// Gets the culture used for culture-sensitive text operations.
	/// </summary>
	public CultureInfo CultureInfo {
		get { return cultureInfo; }
	}

	/// <summary>
	/// Sets the <see cref="CultureInfo"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
	public void SetCultureInfo(CultureInfo value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		cultureInfo = value;
	}

	/// <summary>
	/// Extracts the color from a brush, when the brush is a <see cref="SolidColorBrush"/>.
	/// </summary>
	internal static SKColor? GetSolidColor(Brush? brush)
	{
		if (brush is SolidColorBrush solid)
		{
			var c = solid.Color;
			return new SKColor(c.R, c.G, c.B, c.A);
		}
		return null;
	}

	/// <summary>
	/// Maps a framework font weight onto the engine's weight scale, rounding to the nearest
	/// hundred and clamping into the 100-900 range the engine models.
	/// </summary>
	internal static TextFontWeight ToTextFontWeight(FontWeight weight)
	{
		int bucket = (int)Math.Round(weight.Weight / 100.0) * 100;
		return (TextFontWeight)Math.Clamp(bucket, 100, 900);
	}

	/// <summary>
	/// Maps a framework font style onto the engine's style enum.
	/// </summary>
	internal static TextFontStyle ToTextFontStyle(FontStyle style)
	{
		return style switch
		{
			FontStyle.Oblique => TextFontStyle.Oblique,
			FontStyle.Italic => TextFontStyle.Italic,
			_ => TextFontStyle.Normal,
		};
	}

	/// <summary>
	/// Maps a framework font stretch onto the engine's stretch enum. The numeric values of the two
	/// enums line up (Undefined = 0 through UltraExpanded = 9).
	/// </summary>
	internal static TextFontStretch ToTextFontStretch(FontStretch stretch)
	{
		int value = (int)stretch;
		if (value < (int)TextFontStretch.Undefined || value > (int)TextFontStretch.UltraExpanded)
			return TextFontStretch.Normal;
		return (TextFontStretch)value;
	}
}
