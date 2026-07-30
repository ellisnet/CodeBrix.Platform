#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

//was previously: ICSharpCode.AvalonEdit/Highlighting/Xshd/V2Loader.cs in the AvalonEdit repo (MIT).
//Color/FontWeight/FontStyle/FontFamily parsing now targets the Windows.UI / Windows.UI.Text /
//Microsoft.UI.Xaml.Media types: the WPF ColorConverter/FontWeightConverter/FontStyleConverter
//instances are replaced by the static Convert* helpers below (same accepted inputs: #hex or named
//colors, WPF font-weight names or a 1-999 number, and normal/italic/oblique). System-color
//references resolve through the port's fixed system-color table (see SystemColorHighlightingBrush).

/// <summary>
/// Loads .xshd files, version 2.0.
/// Version 2.0 files are recognized by the namespace.
/// </summary>
static class V2Loader
{
	//was previously: the same URI. This is the XSHD version-2 file-format identifier: every V2
	//syntax definition (including all embedded built-ins and ModeV2.xsd) declares it as its XML
	//namespace, so it must not be renamed even though it references the upstream project's domain.
	public const string Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008";

	static XmlSchemaSet? schemaSet;

	static XmlSchemaSet SchemaSet
	{
		get
		{
			if (schemaSet == null)
			{
				schemaSet = HighlightingLoader.LoadSchemaSet(new XmlTextReader(
					Resources.OpenStream("ModeV2.xsd")));
			}
			return schemaSet;
		}
	}

	public static XshdSyntaxDefinition LoadDefinition(XmlReader reader, bool skipValidation)
	{
		reader = HighlightingLoader.GetValidatingReader(reader, true, skipValidation ? null : SchemaSet);
		reader.Read();
		return ParseDefinition(reader);
	}

	static XshdSyntaxDefinition ParseDefinition(XmlReader reader)
	{
		Debug.Assert(reader.LocalName == "SyntaxDefinition");
		XshdSyntaxDefinition def = new XshdSyntaxDefinition();
		def.Name = reader.GetAttribute("name");
		string? extensions = reader.GetAttribute("extensions");
		if (extensions != null)
		{
			def.Extensions.AddRange(extensions.Split(';'));
		}
		ParseElements(def.Elements, reader);
		Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
		Debug.Assert(reader.LocalName == "SyntaxDefinition");
		return def;
	}

	static void ParseElements(ICollection<XshdElement> c, XmlReader reader)
	{
		if (reader.IsEmptyElement)
		{
			return;
		}
		while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
		{
			Debug.Assert(reader.NodeType == XmlNodeType.Element);
			if (reader.NamespaceURI != Namespace)
			{
				if (!reader.IsEmptyElement)
				{
					reader.Skip();
				}
				continue;
			}
			switch (reader.Name)
			{
				case "RuleSet":
					c.Add(ParseRuleSet(reader));
					break;
				case "Property":
					c.Add(ParseProperty(reader));
					break;
				case "Color":
					c.Add(ParseNamedColor(reader));
					break;
				case "Keywords":
					c.Add(ParseKeywords(reader));
					break;
				case "Span":
					c.Add(ParseSpan(reader));
					break;
				case "Import":
					c.Add(ParseImport(reader));
					break;
				case "Rule":
					c.Add(ParseRule(reader));
					break;
				default:
					throw new NotSupportedException("Unknown element " + reader.Name);
			}
		}
	}

	static XshdElement ParseProperty(XmlReader reader)
	{
		XshdProperty property = new XshdProperty();
		SetPosition(property, reader);
		property.Name = reader.GetAttribute("name");
		property.Value = reader.GetAttribute("value");
		return property;
	}

	static XshdRuleSet ParseRuleSet(XmlReader reader)
	{
		XshdRuleSet ruleSet = new XshdRuleSet();
		SetPosition(ruleSet, reader);
		ruleSet.Name = reader.GetAttribute("name");
		ruleSet.IgnoreCase = reader.GetBoolAttribute("ignoreCase");

		CheckElementName(reader, ruleSet.Name);
		ParseElements(ruleSet.Elements, reader);
		return ruleSet;
	}

	static XshdRule ParseRule(XmlReader reader)
	{
		XshdRule rule = new XshdRule();
		SetPosition(rule, reader);
		rule.ColorReference = ParseColorReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Read();
			if (reader.NodeType == XmlNodeType.Text)
			{
				rule.Regex = reader.ReadContentAsString();
				rule.RegexType = XshdRegexType.IgnorePatternWhitespace;
			}
		}
		return rule;
	}

	static XshdKeywords ParseKeywords(XmlReader reader)
	{
		XshdKeywords keywords = new XshdKeywords();
		SetPosition(keywords, reader);
		keywords.ColorReference = ParseColorReference(reader);
		reader.Read();
		while (reader.NodeType != XmlNodeType.EndElement)
		{
			Debug.Assert(reader.NodeType == XmlNodeType.Element);
			keywords.Words.Add(reader.ReadElementString());
		}
		return keywords;
	}

	static XshdImport ParseImport(XmlReader reader)
	{
		XshdImport import = new XshdImport();
		SetPosition(import, reader);
		import.RuleSetReference = ParseRuleSetReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Skip();
		}
		return import;
	}

	static XshdSpan ParseSpan(XmlReader reader)
	{
		XshdSpan span = new XshdSpan();
		SetPosition(span, reader);
		span.BeginRegex = reader.GetAttribute("begin");
		span.EndRegex = reader.GetAttribute("end");
		span.Multiline = reader.GetBoolAttribute("multiline") ?? false;
		span.SpanColorReference = ParseColorReference(reader);
		span.RuleSetReference = ParseRuleSetReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Read();
			while (reader.NodeType != XmlNodeType.EndElement)
			{
				Debug.Assert(reader.NodeType == XmlNodeType.Element);
				switch (reader.Name)
				{
					case "Begin":
						if (span.BeginRegex != null)
						{
							throw Error(reader, "Duplicate Begin regex");
						}
						span.BeginColorReference = ParseColorReference(reader);
						span.BeginRegex = reader.ReadElementString();
						span.BeginRegexType = XshdRegexType.IgnorePatternWhitespace;
						break;
					case "End":
						if (span.EndRegex != null)
						{
							throw Error(reader, "Duplicate End regex");
						}
						span.EndColorReference = ParseColorReference(reader);
						span.EndRegex = reader.ReadElementString();
						span.EndRegexType = XshdRegexType.IgnorePatternWhitespace;
						break;
					case "RuleSet":
						if (span.RuleSetReference.ReferencedElement != null)
						{
							throw Error(reader, "Cannot specify both inline RuleSet and RuleSet reference");
						}
						span.RuleSetReference = new XshdReference<XshdRuleSet>(ParseRuleSet(reader));
						reader.Read();
						break;
					default:
						throw new NotSupportedException("Unknown element " + reader.Name);
				}
			}
		}
		return span;
	}

	static Exception Error(XmlReader reader, string message)
	{
		return Error(reader as IXmlLineInfo, message);
	}

	static Exception Error(IXmlLineInfo? lineInfo, string message)
	{
		if (lineInfo != null)
		{
			return new HighlightingDefinitionInvalidException(HighlightingLoader.FormatExceptionMessage(message, lineInfo.LineNumber, lineInfo.LinePosition));
		}
		else
		{
			return new HighlightingDefinitionInvalidException(message);
		}
	}

	/// <summary>
	/// Sets the element's position to the XmlReader's position.
	/// </summary>
	static void SetPosition(XshdElement element, XmlReader reader)
	{
		IXmlLineInfo? lineInfo = reader as IXmlLineInfo;
		if (lineInfo != null)
		{
			element.LineNumber = lineInfo.LineNumber;
			element.ColumnNumber = lineInfo.LinePosition;
		}
	}

	static XshdReference<XshdRuleSet> ParseRuleSetReference(XmlReader reader)
	{
		string? ruleSet = reader.GetAttribute("ruleSet");
		if (ruleSet != null)
		{
			// '/' is valid in highlighting definition names, so we need the last occurrence
			int pos = ruleSet.LastIndexOf('/');
			if (pos >= 0)
			{
				return new XshdReference<XshdRuleSet>(ruleSet.Substring(0, pos), ruleSet.Substring(pos + 1));
			}
			else
			{
				return new XshdReference<XshdRuleSet>(null, ruleSet);
			}
		}
		else
		{
			return new XshdReference<XshdRuleSet>();
		}
	}

	static void CheckElementName(XmlReader reader, string? name)
	{
		if (name != null)
		{
			if (name.Length == 0)
			{
				throw Error(reader, "The empty string is not a valid name.");
			}
			if (name.IndexOf('/') >= 0)
			{
				throw Error(reader, "Element names must not contain a slash.");
			}
		}
	}

	#region ParseColor
	static XshdColor ParseNamedColor(XmlReader reader)
	{
		XshdColor color = ParseColorAttributes(reader);
		// check removed: invisible named colors may be useful now that apps can read highlighting data
		//if (color.Foreground == null && color.FontWeight == null && color.FontStyle == null)
		//	throw Error(reader, "A named color must have at least one element.");
		color.Name = reader.GetAttribute("name");
		CheckElementName(reader, color.Name);
		color.ExampleText = reader.GetAttribute("exampleText");
		return color;
	}

	static XshdReference<XshdColor> ParseColorReference(XmlReader reader)
	{
		string? color = reader.GetAttribute("color");
		if (color != null)
		{
			int pos = color.LastIndexOf('/');
			if (pos >= 0)
			{
				return new XshdReference<XshdColor>(color.Substring(0, pos), color.Substring(pos + 1));
			}
			else
			{
				return new XshdReference<XshdColor>(null, color);
			}
		}
		else
		{
			return new XshdReference<XshdColor>(ParseColorAttributes(reader));
		}
	}

	static XshdColor ParseColorAttributes(XmlReader reader)
	{
		XshdColor color = new XshdColor();
		SetPosition(color, reader);
		IXmlLineInfo? position = reader as IXmlLineInfo;
		color.Foreground = ParseColor(position, reader.GetAttribute("foreground"));
		color.Background = ParseColor(position, reader.GetAttribute("background"));
		color.FontWeight = ParseFontWeight(reader.GetAttribute("fontWeight"));
		color.FontStyle = ParseFontStyle(reader.GetAttribute("fontStyle"));
		color.Underline = reader.GetBoolAttribute("underline");
		color.Strikethrough = reader.GetBoolAttribute("strikethrough");
		color.FontFamily = ParseFontFamily(position, reader.GetAttribute("fontFamily"));
		color.FontSize = ParseFontSize(position, reader.GetAttribute("fontSize"));
		return color;
	}

	static HighlightingBrush? ParseColor(IXmlLineInfo? lineInfo, string? color)
	{
		if (string.IsNullOrEmpty(color))
		{
			return null;
		}
		if (color.StartsWith("SystemColors.", StringComparison.Ordinal))
		{
			return GetSystemColorBrush(lineInfo, color);
		}
		else
		{
			return FixedColorHighlightingBrush(ConvertColor(color));
		}
	}

	static int? ParseFontSize(IXmlLineInfo? lineInfo, string? size)
	{
		int value;
		return int.TryParse(size, out value)
			? value
			: (int?)null;
	}

	static FontFamily? ParseFontFamily(IXmlLineInfo? lineInfo, string? family)
	{
		if (!string.IsNullOrEmpty(family))
		{
			return new FontFamily(family);
		}
		else
		{
			return null;
		}
	}

	internal static SystemColorHighlightingBrush GetSystemColorBrush(IXmlLineInfo? lineInfo, string name)
	{
		Debug.Assert(name.StartsWith("SystemColors.", StringComparison.Ordinal));
		//was previously: reflected the "<shortName>Brush" property off System.Windows.SystemColors;
		//the port looks the short name up in SystemColorHighlightingBrush's fixed table.
		string shortName = name.Substring(13);
		if (!SystemColorHighlightingBrush.IsKnownColorName(shortName))
		{
			throw Error(lineInfo, "Cannot find '" + name + "'.");
		}
		return new SystemColorHighlightingBrush(shortName);
	}

	static HighlightingBrush? FixedColorHighlightingBrush(Color? color)
	{
		if (color == null)
		{
			return null;
		}
		return new SimpleHighlightingBrush(color.Value);
	}

	static FontWeight? ParseFontWeight(string? fontWeight)
	{
		if (string.IsNullOrEmpty(fontWeight))
		{
			return null;
		}
		return ConvertFontWeight(fontWeight);
	}

	static FontStyle? ParseFontStyle(string? fontStyle)
	{
		if (string.IsNullOrEmpty(fontStyle))
		{
			return null;
		}
		return ConvertFontStyle(fontStyle);
	}
	#endregion

	#region Converters
	//was previously: the WPF ColorConverter/FontWeightConverter/FontStyleConverter instances used
	//by this loader and by SaveXshdVisitor. Re-expressed as static conversion methods over the
	//mapped types, accepting the same invariant strings the WPF converters accepted.

	/// <summary>
	/// Converts an invariant color string (#AARRGGBB, #RRGGBB or a well-known color name) to a color.
	/// </summary>
	internal static Color ConvertColor(string color)
	{
		return Colors.Parse(color);
	}

	/// <summary>
	/// Converts an invariant font-weight string (a well-known weight name, or a number 1-999) to a font weight.
	/// </summary>
	internal static FontWeight ConvertFontWeight(string fontWeight)
	{
		switch (fontWeight.ToLowerInvariant())
		{
			case "thin":
				return FontWeights.Thin;
			case "extralight":
			case "ultralight":
				return FontWeights.ExtraLight;
			case "light":
				return FontWeights.Light;
			case "semilight":
				return FontWeights.SemiLight;
			case "normal":
			case "regular":
				return FontWeights.Normal;
			case "medium":
				return FontWeights.Medium;
			case "semibold":
			case "demibold":
				return FontWeights.SemiBold;
			case "bold":
				return FontWeights.Bold;
			case "extrabold":
			case "ultrabold":
				return FontWeights.ExtraBold;
			case "black":
			case "heavy":
				return FontWeights.Black;
			case "extrablack":
			case "ultrablack":
				return FontWeights.ExtraBlack;
			default:
				int numericWeight;
				if (int.TryParse(fontWeight, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericWeight)
					&& numericWeight >= 1 && numericWeight <= 999)
				{
					return new FontWeight((ushort)numericWeight);
				}
				throw new FormatException("'" + fontWeight + "' is not a valid font weight.");
		}
	}

	/// <summary>
	/// Converts a font weight back to its invariant string form (a well-known weight name, or a number).
	/// </summary>
	internal static string ConvertFontWeightToString(FontWeight fontWeight)
	{
		switch (fontWeight.Weight)
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
				return fontWeight.Weight.ToString(CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// Converts an invariant font-style string (normal, italic or oblique) to a font style.
	/// </summary>
	internal static FontStyle ConvertFontStyle(string fontStyle)
	{
		switch (fontStyle.ToLowerInvariant())
		{
			case "normal":
				return FontStyle.Normal;
			case "italic":
				return FontStyle.Italic;
			case "oblique":
				return FontStyle.Oblique;
			default:
				throw new FormatException("'" + fontStyle + "' is not a valid font style.");
		}
	}

	/// <summary>
	/// Converts a font style back to its invariant string form.
	/// </summary>
	internal static string ConvertFontStyleToString(FontStyle fontStyle)
	{
		return fontStyle.ToString();
	}
	#endregion
}
