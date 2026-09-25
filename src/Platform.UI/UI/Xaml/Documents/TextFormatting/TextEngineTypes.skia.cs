#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// The text engine's own value types (WPE1 C5). The engine is ONE source compiled into two assemblies - the framework's
// Skia assembly and CodeBrix.Platform.UI.TextLayout.Core - and its types name no XAML/WinRT type: these stand in for
// FlowDirection, TextAlignment, TextWrapping, LineStackingStrategy (Microsoft.UI.Xaml), FontStretch and FontStyle
// (Windows.UI.Text) and Size/Rect/Point (Windows.Foundation). Every enum carries the SAME underlying values as the type it
// stands in for, so the framework converts with a cast; the geometry structs store float exactly like the Foundation
// structs do (their double setters round to float, Right/Bottom add in float), so every measurement is bit-identical.
// A font weight is its numeric value (ushort), as Windows.UI.Text.FontWeight.Weight is.

/// <summary>The engine's base or run direction (values of Microsoft.UI.Xaml.FlowDirection).</summary>
internal enum EngineFlowDirection
{
	/// <summary>Left-to-right.</summary>
	LeftToRight = 0,

	/// <summary>Right-to-left.</summary>
	RightToLeft = 1,
}

/// <summary>The engine's horizontal line alignment (values of Microsoft.UI.Xaml.TextAlignment).</summary>
internal enum EngineTextAlignment
{
	/// <summary>Centred.</summary>
	Center = 0,

	/// <summary>Left (Start).</summary>
	Left = 1,

	/// <summary>Right (End).</summary>
	Right = 2,

	/// <summary>Justified (laid out as Left; the caret maths rejects it, as before).</summary>
	Justify = 3,

	/// <summary>From the content (laid out as Left; the caret maths rejects it, as before).</summary>
	DetectFromContent = 4,
}

/// <summary>The engine's wrapping mode (values of Microsoft.UI.Xaml.TextWrapping).</summary>
internal enum EngineTextWrapping
{
	/// <summary>One line per hard line break.</summary>
	NoWrap = 1,

	/// <summary>Wrap at the available width, splitting a word that does not fit.</summary>
	Wrap = 2,

	/// <summary>Wrap at the available width, never splitting a word.</summary>
	WrapWholeWords = 3,
}

/// <summary>The engine's line stacking strategy (values of Microsoft.UI.Xaml.LineStackingStrategy).</summary>
internal enum EngineLineStackingStrategy
{
	/// <summary>Each line is as tall as its tallest font (or the line height, when larger).</summary>
	MaxHeight = 0,

	/// <summary>Every line is exactly the line height.</summary>
	BlockLineHeight = 1,

	/// <summary>Baselines are the line height apart.</summary>
	BaselineToBaseline = 2,
}

/// <summary>The engine's font stretch (values of Windows.UI.Text.FontStretch).</summary>
internal enum EngineFontStretch
{
	/// <summary>Unspecified (resolved as Normal).</summary>
	Undefined = 0,

	/// <summary>Ultra-condensed.</summary>
	UltraCondensed = 1,

	/// <summary>Extra-condensed.</summary>
	ExtraCondensed = 2,

	/// <summary>Condensed.</summary>
	Condensed = 3,

	/// <summary>Semi-condensed.</summary>
	SemiCondensed = 4,

	/// <summary>Normal.</summary>
	Normal = 5,

	/// <summary>Semi-expanded.</summary>
	SemiExpanded = 6,

	/// <summary>Expanded.</summary>
	Expanded = 7,

	/// <summary>Extra-expanded.</summary>
	ExtraExpanded = 8,

	/// <summary>Ultra-expanded.</summary>
	UltraExpanded = 9,
}

/// <summary>The engine's font style (values of Windows.UI.Text.FontStyle).</summary>
internal enum EngineFontStyle
{
	/// <summary>Upright.</summary>
	Normal = 0,

	/// <summary>Oblique.</summary>
	Oblique = 1,

	/// <summary>Italic.</summary>
	Italic = 2,
}

/// <summary>A size in the engine (Windows.Foundation.Size semantics: float storage, double access).</summary>
internal struct EngineSize
{
	private float _width;
	private float _height;

	/// <summary>Creates a size; each value is rounded to float, as Windows.Foundation.Size does.</summary>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public EngineSize(double width, double height)
	{
		_width = (float)width;
		_height = (float)height;
	}

	/// <summary>The width.</summary>
	public double Width
	{
		readonly get => _width;
		set => _width = (float)value;
	}

	/// <summary>The height.</summary>
	public double Height
	{
		readonly get => _height;
		set => _height = (float)value;
	}
}

/// <summary>A point in the engine (Windows.Foundation.Point semantics: float storage, double access).</summary>
internal struct EnginePoint
{
	private float _x;
	private float _y;

	/// <summary>Creates a point; each value is rounded to float, as Windows.Foundation.Point does.</summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	public EnginePoint(double x, double y)
	{
		_x = (float)x;
		_y = (float)y;
	}

	/// <summary>The x coordinate.</summary>
	public double X
	{
		readonly get => _x;
		set => _x = (float)value;
	}

	/// <summary>The y coordinate.</summary>
	public double Y
	{
		readonly get => _y;
		set => _y = (float)value;
	}
}

/// <summary>
/// A rectangle in the engine (Windows.Foundation.Rect semantics: float storage, double access, Right/Bottom summed in
/// float). It never validates a negative size: that check belongs to Windows.Foundation.Rect's constructor, which the
/// framework runs when it converts (and TextLayout replicates when it converts to an SKRect).
/// </summary>
internal struct EngineRect
{
	private float _x;
	private float _y;
	private float _width;
	private float _height;

	/// <summary>Creates a rectangle; each value is rounded to float, as Windows.Foundation.Rect does.</summary>
	/// <param name="x">The left edge.</param>
	/// <param name="y">The top edge.</param>
	/// <param name="width">The width.</param>
	/// <param name="height">The height.</param>
	public EngineRect(double x, double y, double width, double height)
	{
		_x = (float)x;
		_y = (float)y;
		_width = (float)width;
		_height = (float)height;
	}

	/// <summary>The left edge.</summary>
	public double X
	{
		readonly get => _x;
		set => _x = (float)value;
	}

	/// <summary>The top edge.</summary>
	public double Y
	{
		readonly get => _y;
		set => _y = (float)value;
	}

	/// <summary>The width.</summary>
	public double Width
	{
		readonly get => _width;
		set => _width = (float)value;
	}

	/// <summary>The height.</summary>
	public double Height
	{
		readonly get => _height;
		set => _height = (float)value;
	}

	/// <summary>The left edge.</summary>
	public readonly double Left => _x;

	/// <summary>The top edge.</summary>
	public readonly double Top => _y;

	/// <summary>The right edge (summed in float, as Windows.Foundation.Rect.Right is).</summary>
	public readonly double Right => _x + _width;

	/// <summary>The bottom edge (summed in float, as Windows.Foundation.Rect.Bottom is).</summary>
	public readonly double Bottom => _y + _height;
}

/// <summary>The engine's font conversions to SkiaSharp (the same mapping as the framework's FontWeight/FontStyle/FontStretch extensions).</summary>
internal static class EngineFontExtensions
{
	/// <summary>The Skia weight of a numeric weight (the same scale).</summary>
	/// <param name="weight">The numeric weight.</param>
	/// <returns>The Skia weight.</returns>
	public static SKFontStyleWeight ToSkiaWeight(this ushort weight) => (SKFontStyleWeight)weight;

	/// <summary>The Skia slant of a style.</summary>
	/// <param name="style">The style.</param>
	/// <returns>The Skia slant.</returns>
	public static SKFontStyleSlant ToSkiaSlant(this EngineFontStyle style) =>
		style switch
		{
			EngineFontStyle.Italic => SKFontStyleSlant.Italic,
			EngineFontStyle.Normal => SKFontStyleSlant.Upright,
			EngineFontStyle.Oblique => SKFontStyleSlant.Oblique,
			_ => SKFontStyleSlant.Upright
		};

	/// <summary>The Skia width of a stretch.</summary>
	/// <param name="stretch">The stretch.</param>
	/// <returns>The Skia width.</returns>
	public static SKFontStyleWidth ToSkiaWidth(this EngineFontStretch stretch) =>
		stretch switch
		{
			EngineFontStretch.Undefined => SKFontStyleWidth.Normal,
			EngineFontStretch.UltraCondensed => SKFontStyleWidth.UltraCondensed,
			EngineFontStretch.ExtraCondensed => SKFontStyleWidth.ExtraCondensed,
			EngineFontStretch.Condensed => SKFontStyleWidth.Condensed,
			EngineFontStretch.SemiCondensed => SKFontStyleWidth.SemiCondensed,
			EngineFontStretch.Normal => SKFontStyleWidth.Normal,
			EngineFontStretch.SemiExpanded => SKFontStyleWidth.SemiExpanded,
			EngineFontStretch.Expanded => SKFontStyleWidth.Expanded,
			EngineFontStretch.ExtraExpanded => SKFontStyleWidth.ExtraExpanded,
			EngineFontStretch.UltraExpanded => SKFontStyleWidth.UltraExpanded,
			_ => SKFontStyleWidth.Normal,
		};
}
