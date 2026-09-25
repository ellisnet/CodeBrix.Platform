#nullable enable

using System;
using System.Globalization;
using CodeBrix.Platform.UI.TextLayout;
using SkiaSharp;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Engine;

/// <summary>
/// The highlighting ENGINE's value conversions (WPE1 C8): the invariant strings of .xshd files (colours, font weights,
/// font styles) to and from the neutral storage the highlighting types keep since the adapters change - colours as
/// <see cref="SKColor"/>, weights as the numeric weight, styles as <see cref="TextFontStyle"/> (the same values as the
/// WinUI FontStyle). Names no XAML or WinRT type, so an xshd definition loads without the XAML object model.
/// </summary>
/// <remarks>
/// <see cref="ParseColor"/> is the framework's Colors.Parse (Microsoft.UI.Colors, src/Platform.UI/UI/Colors.cs) ported
/// verbatim onto <see cref="SKColor"/>: same accepted forms, same named colours, same exceptions and messages - keep the
/// two in step.
/// </remarks>
internal static class HighlightingValues
{
	/// <summary>Formats a colour the way Windows.UI.Color.ToString() does: "#AARRGGBB", upper-case hex.</summary>
	/// <param name="color">The colour.</param>
	/// <returns>The string.</returns>
	internal static string ToColorString(SKColor color) =>
		string.Format(null, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.Alpha, color.Red, color.Green, color.Blue);

	/// <summary>
	/// Converts an invariant font-weight string (a well-known weight name, or a number 1-999) to a numeric weight.
	/// </summary>
	/// <param name="fontWeight">The string.</param>
	/// <returns>The weight (the value of the WinUI FontWeights member of that name).</returns>
	internal static ushort ParseFontWeight(string fontWeight)
	{
		switch (fontWeight.ToLowerInvariant())
		{
			case "thin":
				return 100;
			case "extralight":
			case "ultralight":
				return 200;
			case "light":
				return 300;
			case "semilight":
				return 350;
			case "normal":
			case "regular":
				return 400;
			case "medium":
				return 500;
			case "semibold":
			case "demibold":
				return 600;
			case "bold":
				return 700;
			case "extrabold":
			case "ultrabold":
				return 800;
			case "black":
			case "heavy":
				return 900;
			case "extrablack":
			case "ultrablack":
				return 950;
			default:
				int numericWeight;
				if (int.TryParse(fontWeight, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericWeight)
					&& numericWeight >= 1 && numericWeight <= 999)
				{
					return (ushort)numericWeight;
				}
				throw new FormatException("'" + fontWeight + "' is not a valid font weight.");
		}
	}

	/// <summary>
	/// Converts a numeric font weight back to its invariant string form (a well-known weight name, or a number).
	/// </summary>
	/// <param name="fontWeight">The weight.</param>
	/// <returns>The string.</returns>
	internal static string FontWeightToString(ushort fontWeight)
	{
		switch (fontWeight)
		{
			case 100:
				return "Thin";
			case 200:
				return "ExtraLight";
			case 300:
				return "Light";
			case 350:
				return "SemiLight";
			case 400:
				return "Normal";
			case 500:
				return "Medium";
			case 600:
				return "SemiBold";
			case 700:
				return "Bold";
			case 800:
				return "ExtraBold";
			case 900:
				return "Black";
			case 950:
				return "ExtraBlack";
			default:
				return fontWeight.ToString(CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// Converts an invariant font-style string (normal, italic or oblique) to a font style.
	/// </summary>
	/// <param name="fontStyle">The string.</param>
	/// <returns>The style.</returns>
	internal static TextFontStyle ParseFontStyle(string fontStyle)
	{
		switch (fontStyle.ToLowerInvariant())
		{
			case "normal":
				return TextFontStyle.Normal;
			case "italic":
				return TextFontStyle.Italic;
			case "oblique":
				return TextFontStyle.Oblique;
			default:
				throw new FormatException("'" + fontStyle + "' is not a valid font style.");
		}
	}

	/// <summary>
	/// Converts a font style back to its invariant string form: the member name, exactly as the WinUI FontStyle's
	/// ToString() gives it (the two enums have the same names and values).
	/// </summary>
	/// <param name="fontStyle">The style.</param>
	/// <returns>The string.</returns>
	internal static string FontStyleToString(TextFontStyle fontStyle) => fontStyle.ToString();

	//The port keeps the original's (nullable-oblivious) code as is.
#nullable disable
	/// <summary>
	/// Parses a colour string: "#AARRGGBB", "#RRGGBB", "#ARGB", "#RGB" or a well-known colour name (Colors.Parse).
	/// </summary>
	/// <param name="colorCode">The string.</param>
	/// <returns>The colour.</returns>
	internal static SKColor ParseColor(string colorCode)
	{
		if (!string.IsNullOrEmpty(colorCode))
		{
			if (colorCode[0] == '#')
			{
				return FromARGB(colorCode);
			}
			else
			{
				uint color = colorCode.ToLowerInvariant() switch
				{
					"transparent" => 0x00FFFFFF,
					"aliceblue" => 0xFFF0F8FF,
					"antiquewhite" => 0xFFFAEBD7,
					"aqua" => 0xFF00FFFF,
					"aquamarine" => 0xFF7FFFD4,
					"azure" => 0xFFF0FFFF,
					"beige" => 0xFFF5F5DC,
					"bisque" => 0xFFFFE4C4,
					"black" => 0xFF000000,
					"blanchedalmond" => 0xFFFFEBCD,
					"blue" => 0xFF0000FF,
					"blueviolet" => 0xFF8A2BE2,
					"brown" => 0xFFA52A2A,
					"burlywood" => 0xFFDEB887,
					"cadetblue" => 0xFF5F9EA0,
					"chartreuse" => 0xFF7FFF00,
					"chocolate" => 0xFFD2691E,
					"coral" => 0xFFFF7F50,
					"cornflowerblue" => 0xFF6495ED,
					"cornsilk" => 0xFFFFF8DC,
					"crimson" => 0xFFDC143C,
					"cyan" => 0xFF00FFFF,
					"darkblue" => 0xFF00008B,
					"darkcyan" => 0xFF008B8B,
					"darkgoldenrod" => 0xFFB8860B,
					"darkgray" => 0xFFA9A9A9,
					"darkgreen" => 0xFF006400,
					"darkkhaki" => 0xFFBDB76B,
					"darkmagenta" => 0xFF8B008B,
					"darkolivegreen" => 0xFF556B2F,
					"darkorange" => 0xFFFF8C00,
					"darkorchid" => 0xFF9932CC,
					"darkred" => 0xFF8B0000,
					"darksalmon" => 0xFFE9967A,
					"darkseagreen" => 0xFF8FBC8F,
					"darkslateblue" => 0xFF483D8B,
					"darkslategray" => 0xFF2F4F4F,
					"darkturquoise" => 0xFF00CED1,
					"darkviolet" => 0xFF9400D3,
					"deeppink" => 0xFFFF1493,
					"deepskyblue" => 0xFF00BFFF,
					"dimgray" => 0xFF696969,
					"dodgerblue" => 0xFF1E90FF,
					"firebrick" => 0xFFB22222,
					"floralwhite" => 0xFFFFFAF0,
					"forestgreen" => 0xFF228B22,
					"fuchsia" => 0xFFFF00FF,
					"gainsboro" => 0xFFDCDCDC,
					"ghostwhite" => 0xFFF8F8FF,
					"gold" => 0xFFFFD700,
					"goldenrod" => 0xFFDAA520,
					"gray" => 0xFF808080,
					"green" => 0xFF008000,
					"greenyellow" => 0xFFADFF2F,
					"honeydew" => 0xFFF0FFF0,
					"hotpink" => 0xFFFF69B4,
					"indianred" => 0xFFCD5C5C,
					"indigo" => 0xFF4B0082,
					"ivory" => 0xFFFFFFF0,
					"khaki" => 0xFFF0E68C,
					"lavender" => 0xFFE6E6FA,
					"lavenderblush" => 0xFFFFF0F5,
					"lawngreen" => 0xFF7CFC00,
					"lemonchiffon" => 0xFFFFFACD,
					"lightblue" => 0xFFADD8E6,
					"lightcoral" => 0xFFF08080,
					"lightcyan" => 0xFFE0FFFF,
					"lightgoldenrodyellow" => 0xFFFAFAD2,
					"lightgray" => 0xFFD3D3D3,
					"lightgreen" => 0xFF90EE90,
					"lightpink" => 0xFFFFB6C1,
					"lightsalmon" => 0xFFFFA07A,
					"lightseagreen" => 0xFF20B2AA,
					"lightskyblue" => 0xFF87CEFA,
					"lightslategray" => 0xFF778899,
					"lightsteelblue" => 0xFFB0C4DE,
					"lightyellow" => 0xFFFFFFE0,
					"lime" => 0xFF00FF00,
					"limegreen" => 0xFF32CD32,
					"linen" => 0xFFFAF0E6,
					"magenta" => 0xFFFF00FF,
					"maroon" => 0xFF800000,
					"mediumaquamarine" => 0xFF66CDAA,
					"mediumblue" => 0xFF0000CD,
					"mediumorchid" => 0xFFBA55D3,
					"mediumpurple" => 0xFF9370DB,
					"mediumseagreen" => 0xFF3CB371,
					"mediumslateblue" => 0xFF7B68EE,
					"mediumspringgreen" => 0xFF00FA9A,
					"mediumturquoise" => 0xFF48D1CC,
					"mediumvioletred" => 0xFFC71585,
					"midnightblue" => 0xFF191970,
					"mintcream" => 0xFFF5FFFA,
					"mistyrose" => 0xFFFFE4E1,
					"moccasin" => 0xFFFFE4B5,
					"navajowhite" => 0xFFFFDEAD,
					"navy" => 0xFF000080,
					"oldlace" => 0xFFFDF5E6,
					"olive" => 0xFF808000,
					"olivedrab" => 0xFF6B8E23,
					"orange" => 0xFFFFA500,
					"orangered" => 0xFFFF4500,
					"orchid" => 0xFFDA70D6,
					"palegoldenrod" => 0xFFEEE8AA,
					"palegreen" => 0xFF98FB98,
					"paleturquoise" => 0xFFAFEEEE,
					"palevioletred" => 0xFFDB7093,
					"papayawhip" => 0xFFFFEFD5,
					"peachpuff" => 0xFFFFDAB9,
					"peru" => 0xFFCD853F,
					"pink" => 0xFFFFC0CB,
					"plum" => 0xFFDDA0DD,
					"powderblue" => 0xFFB0E0E6,
					"purple" => 0xFF800080,
					"red" => 0xFFFF0000,
					"rosybrown" => 0xFFBC8F8F,
					"royalblue" => 0xFF4169E1,
					"saddlebrown" => 0xFF8B4513,
					"salmon" => 0xFFFA8072,
					"sandybrown" => 0xFFF4A460,
					"seagreen" => 0xFF2E8B57,
					"seashell" => 0xFFFFF5EE,
					"sienna" => 0xFFA0522D,
					"silver" => 0xFFC0C0C0,
					"skyblue" => 0xFF87CEEB,
					"slateblue" => 0xFF6A5ACD,
					"slategray" => 0xFF708090,
					"snow" => 0xFFFFFAFA,
					"springgreen" => 0xFF00FF7F,
					"steelblue" => 0xFF4682B4,
					"tan" => 0xFFD2B48C,
					"teal" => 0xFF008080,
					"thistle" => 0xFFD8BFD8,
					"tomato" => 0xFFFF6347,
					"turquoise" => 0xFF40E0D0,
					"violet" => 0xFFEE82EE,
					"wheat" => 0xFFF5DEB3,
					"white" => 0xFFFFFFFF,
					"whitesmoke" => 0xFFF5F5F5,
					"yellow" => 0xFFFFFF00,
					"yellowgreen" => 0xFF9ACD32,
					_ => throw new InvalidOperationException($"The color {colorCode} is unknown")
				};

				return new SKColor(color);
			}
		}
		else
		{
			throw new InvalidOperationException($"Cannot parse an empty color string");
		}
	}

	/// <summary>
	/// Takes a color code as an ARGB, RGB, #ARGB, #RGB string and returns a color.
	///
	/// Remark: if single digits are used to define the color, they will
	/// be duplicated (example: FFD8 will become FFFFDD88)
	/// </summary>
	/// <param name="colorCode"></param>
	/// <returns></returns>
	private static SKColor FromARGB(string colorCode)
	{
		byte a, r, b, g;
		int offset;
		int len;

		if (colorCode is null)
		{
			len = 0;
			offset = 0;
		}
		else
		{
			len = colorCode.Length;
			// skip a starting `#` if present
			offset = (len > 0 && colorCode[0] == '#' ? 1 : 0);
			len -= offset;
		}

		// deal with an optional alpha value
		if (len == 4)
		{
			a = ToByte(colorCode[offset++]);
			a = (byte)(a << 4 + a);
			len = 3;
		}
		else if (len == 8)
		{
			a = (byte)((ToByte(colorCode[offset++]) << 4) + ToByte(colorCode[offset++]));
			len = 6;
		}
		else
		{
			a = 0xFF;
		}

		// then process the required R G and B values
		if (len == 3)
		{
			r = ToByte(colorCode[offset++]);
			r = (byte)(r << 4 + r);
			g = ToByte(colorCode[offset++]);
			g = (byte)(g << 4 + g);
			b = ToByte(colorCode[offset++]);
			b = (byte)(b << 4 + b);
		}
		else if (len == 6)
		{
			r = (byte)((ToByte(colorCode[offset++]) << 4) + ToByte(colorCode[offset++]));
			g = (byte)((ToByte(colorCode[offset++]) << 4) + ToByte(colorCode[offset++]));
			b = (byte)((ToByte(colorCode[offset++]) << 4) + ToByte(colorCode[offset++]));
		}
		else
		{
			throw new ArgumentException($"Cannot parse color '{colorCode}'.");
		}

		return new SKColor(r, g, b, a);
	}

	private static byte ToByte(char c)
	{
		if (c >= '0' && c <= '9')
		{
			return (byte)(c - '0');
		}
		else if (c >= 'a' && c <= 'f')
		{
			return (byte)(c - 'a' + 10);
		}
		else if (c >= 'A' && c <= 'F')
		{
			return (byte)(c - 'A' + 10);
		}
		else
		{
			throw new FormatException($"The character {c} is not valid for a Color string");
		}
	}
#nullable restore
}
