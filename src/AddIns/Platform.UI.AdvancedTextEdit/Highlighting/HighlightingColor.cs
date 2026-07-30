#nullable enable

using System;
using System.Globalization;
using System.Text;

using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingColor.cs in the AvalonEdit repo (MIT).
//FontWeight/FontStyle/FontFamily are now the Windows.UI.Text / Microsoft.UI.Xaml.Media types.
//Binary serialization ([Serializable]/ISerializable and the serialization constructor) was
//dropped; it is dead on modern .NET. ToCss() stringifies the font weight through the port's own
//name table (V2Loader.ConvertFontWeightToString) because Windows.UI.Text.FontWeight has no
//name-producing ToString(); the produced CSS is unchanged for all standard weights.

/// <summary>
/// A highlighting color is a set of font properties and foreground and background color.
/// </summary>
public class HighlightingColor : IFreezable, ICloneable, IEquatable<HighlightingColor>
{
	internal static readonly HighlightingColor Empty = FreezableHelper.FreezeAndReturn(new HighlightingColor());

	string? name;
	FontFamily? fontFamily = null;
	int? fontSize;
	FontWeight? fontWeight;
	FontStyle? fontStyle;
	bool? underline;
	bool? strikethrough;
	HighlightingBrush? foreground;
	HighlightingBrush? background;
	bool frozen;

	/// <summary>
	/// Gets/Sets the name of the color.
	/// </summary>
	public string? Name
	{
		get
		{
			return name;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			name = value;
		}
	}

	/// <summary>
	/// Gets/sets the font family. Null if the highlighting color does not change the font style.
	/// </summary>
	public FontFamily? FontFamily
	{
		get
		{
			return fontFamily;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			fontFamily = value;
		}
	}

	/// <summary>
	/// Gets/sets the font size. Null if the highlighting color does not change the font style.
	/// </summary>
	public int? FontSize
	{
		get
		{
			return fontSize;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			fontSize = value;
		}
	}

	/// <summary>
	/// Gets/sets the font weight. Null if the highlighting color does not change the font weight.
	/// </summary>
	public FontWeight? FontWeight
	{
		get
		{
			return fontWeight;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			fontWeight = value;
		}
	}

	/// <summary>
	/// Gets/sets the font style. Null if the highlighting color does not change the font style.
	/// </summary>
	public FontStyle? FontStyle
	{
		get
		{
			return fontStyle;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			fontStyle = value;
		}
	}

	/// <summary>
	///  Gets/sets the underline flag. Null if the underline status does not change the font style.
	/// </summary>
	public bool? Underline
	{
		get
		{
			return underline;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			underline = value;
		}
	}

	/// <summary>
	///  Gets/sets the strikethrough flag. Null if the strikethrough status does not change the font style.
	/// </summary>
	public bool? Strikethrough
	{
		get
		{
			return strikethrough;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			strikethrough = value;
		}
	}

	/// <summary>
	/// Gets/sets the foreground color applied by the highlighting.
	/// </summary>
	public HighlightingBrush? Foreground
	{
		get
		{
			return foreground;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			foreground = value;
		}
	}

	/// <summary>
	/// Gets/sets the background color applied by the highlighting.
	/// </summary>
	public HighlightingBrush? Background
	{
		get
		{
			return background;
		}
		set
		{
			if (frozen)
			{
				throw new InvalidOperationException();
			}
			background = value;
		}
	}

	/// <summary>
	/// Creates a new HighlightingColor instance.
	/// </summary>
	public HighlightingColor()
	{
	}

	/// <summary>
	/// Gets CSS code for the color.
	/// </summary>
	public virtual string ToCss()
	{
		StringBuilder b = new StringBuilder();
		if (Foreground != null)
		{
			Color? c = Foreground.GetColor(null);
			if (c != null)
			{
				b.AppendFormat(CultureInfo.InvariantCulture, "color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
			}
		}
		if (Background != null)
		{
			Color? c = Background.GetColor(null);
			if (c != null)
			{
				b.AppendFormat(CultureInfo.InvariantCulture, "background-color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
			}
		}
		if (FontWeight != null)
		{
			b.Append("font-weight: ");
			b.Append(V2Loader.ConvertFontWeightToString(FontWeight.Value).ToLowerInvariant());
			b.Append("; ");
		}
		if (FontStyle != null)
		{
			b.Append("font-style: ");
			b.Append(FontStyle.Value.ToString().ToLowerInvariant());
			b.Append("; ");
		}
		if (Underline != null)
		{
			b.Append("text-decoration: ");
			b.Append(Underline.Value ? "underline" : "none");
			b.Append("; ");
		}
		if (Strikethrough != null)
		{
			if (Underline == null)
			{
				b.Append("text-decoration:  ");
			}

			b.Remove(b.Length - 1, 1);
			b.Append(Strikethrough.Value ? " line-through" : " none");
			b.Append("; ");
		}
		return b.ToString();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return "[" + GetType().Name + " " + (string.IsNullOrEmpty(this.Name) ? ToCss() : this.Name) + "]";
	}

	/// <summary>
	/// Prevent further changes to this highlighting color.
	/// </summary>
	public virtual void Freeze()
	{
		frozen = true;
	}

	/// <summary>
	/// Gets whether this HighlightingColor instance is frozen.
	/// </summary>
	public bool IsFrozen
	{
		get { return frozen; }
	}

	/// <summary>
	/// Clones this highlighting color.
	/// If this color is frozen, the clone will be unfrozen.
	/// </summary>
	public virtual HighlightingColor Clone()
	{
		HighlightingColor c = (HighlightingColor)MemberwiseClone();
		c.frozen = false;
		return c;
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	/// <inheritdoc/>
	public override sealed bool Equals(object? obj)
	{
		return Equals(obj as HighlightingColor);
	}

	/// <inheritdoc/>
	public virtual bool Equals(HighlightingColor? other)
	{
		if (other == null)
		{
			return false;
		}
		return this.name == other.name && this.fontWeight == other.fontWeight
			&& this.fontStyle == other.fontStyle && this.underline == other.underline && this.strikethrough == other.strikethrough
			&& object.Equals(this.foreground, other.foreground) && object.Equals(this.background, other.background)
			&& object.Equals(this.fontFamily, other.fontFamily) && object.Equals(this.FontSize, other.FontSize);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		int hashCode = 0;
		unchecked
		{
			if (name != null)
			{
				hashCode += 1000000007 * name.GetHashCode();
			}
			hashCode += 1000000009 * fontWeight.GetHashCode();
			hashCode += 1000000021 * fontStyle.GetHashCode();
			if (foreground != null)
			{
				hashCode += 1000000033 * foreground.GetHashCode();
			}
			if (background != null)
			{
				hashCode += 1000000087 * background.GetHashCode();
			}
			if (fontFamily != null)
			{
				hashCode += 1000000123 * fontFamily.GetHashCode();
			}
			if (fontSize != null)
			{
				hashCode += 1000000167 * fontSize.GetHashCode();
			}
		}
		return hashCode;
	}

	/// <summary>
	/// Overwrites the properties in this HighlightingColor with those from the given color;
	/// but maintains the current values where the properties of the given color return <c>null</c>.
	/// </summary>
	public void MergeWith(HighlightingColor color)
	{
		FreezableHelper.ThrowIfFrozen(this);
		if (color.fontWeight != null)
		{
			this.fontWeight = color.fontWeight;
		}
		if (color.fontStyle != null)
		{
			this.fontStyle = color.fontStyle;
		}
		if (color.foreground != null)
		{
			this.foreground = color.foreground;
		}
		if (color.background != null)
		{
			this.background = color.background;
		}
		if (color.underline != null)
		{
			this.underline = color.underline;
		}
		if (color.strikethrough != null)
		{
			this.strikethrough = color.strikethrough;
		}
		if (color.fontFamily != null)
		{
			this.fontFamily = color.fontFamily;
		}
		if (color.fontSize != null)
		{
			this.fontSize = color.fontSize;
		}
	}

	internal bool IsEmptyForMerge
	{
		get
		{
			return fontWeight == null && fontStyle == null && underline == null
				&& strikethrough == null && foreground == null && background == null
				&& fontFamily == null && fontSize == null;
		}
	}
}
