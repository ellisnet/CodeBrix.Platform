#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.UI.Xaml.Media;
using Windows.UI;

using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingBrush.cs in the AvalonEdit repo (MIT).
//Brush/SolidColorBrush/Color are now the Microsoft.UI.Xaml.Media / Windows.UI types. Binary
//serialization ([Serializable]/ISerializable) was dropped; it is dead on modern .NET.
//SimpleHighlightingBrush no longer freezes its brush (this framework's brushes have no Freeze());
//the brush instance is simply never mutated. SystemColorHighlightingBrush no longer reflects over
//the WPF System.Windows.SystemColors class; it resolves the color name from a fixed internal table
//of common system-color names instead (this framework has no live system-color broker).

/// <summary>
/// A brush used for syntax highlighting. Can retrieve a real brush on-demand.
/// </summary>
public abstract class HighlightingBrush
{
	/// <summary>
	/// Gets the real brush.
	/// </summary>
	/// <param name="context">The construction context. context can be null!</param>
	public abstract Brush? GetBrush(ITextRunConstructionContext? context);

	/// <summary>
	/// Gets the color of the brush.
	/// </summary>
	/// <param name="context">The construction context. context can be null!</param>
	public virtual Color? GetColor(ITextRunConstructionContext? context)
	{
		SolidColorBrush? scb = GetBrush(context) as SolidColorBrush;
		if (scb != null)
		{
			return scb.Color;
		}
		else
		{
			return null;
		}
	}
}

/// <summary>
/// Highlighting brush implementation that takes a fixed brush.
/// </summary>
public sealed class SimpleHighlightingBrush : HighlightingBrush
{
	readonly SolidColorBrush brush;

	internal SimpleHighlightingBrush(SolidColorBrush brush)
	{
		//was previously: called brush.Freeze(); this framework's brushes have no Freeze(), so the
		//brush is treated as immutable by convention (it is never handed out for mutation).
		this.brush = brush;
	}

	/// <summary>
	/// Creates a new HighlightingBrush with the specified color.
	/// </summary>
	public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color))
	{
	}

	/// <inheritdoc/>
	public override Brush GetBrush(ITextRunConstructionContext? context)
	{
		return brush;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		//was previously: brush.ToString(); the WPF SolidColorBrush stringified to its color code, but
		//this framework's SolidColorBrush.ToString() does not, so the color is stringified directly.
		return brush.Color.ToString();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		SimpleHighlightingBrush? other = obj as SimpleHighlightingBrush;
		if (other == null)
		{
			return false;
		}
		return this.brush.Color.Equals(other.brush.Color);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return brush.Color.GetHashCode();
	}
}

/// <summary>
/// HighlightingBrush implementation that resolves a named system color.
/// </summary>
sealed class SystemColorHighlightingBrush : HighlightingBrush
{
	//was previously: the brush reflected a Brush property off System.Windows.SystemColors at draw
	//time, so it always tracked the live OS theme. This framework has no equivalent broker, so the
	//port resolves the name from this fixed table of common system-color names (Windows light-theme
	//default values). No built-in highlighting definition references a system color; this type only
	//serves user-supplied definitions that use the "SystemColors.<Name>" syntax.
	static readonly Dictionary<string, Color> knownColors = new Dictionary<string, Color>(StringComparer.Ordinal) {
		{ "ActiveCaptionText", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
		{ "Control", Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0) },
		{ "ControlText", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
		{ "GrayText", Color.FromArgb(0xFF, 0x6D, 0x6D, 0x6D) },
		{ "Highlight", Color.FromArgb(0xFF, 0x00, 0x78, 0xD7) },
		{ "HighlightText", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) },
		{ "Info", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xE1) },
		{ "InfoText", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
		{ "Menu", Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0) },
		{ "MenuText", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
		{ "Window", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) },
		{ "WindowText", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
	};

	readonly string name;
	SolidColorBrush? brush;

	/// <summary>
	/// Gets whether the specified system-color name (without the "SystemColors." prefix)
	/// is known to this brush implementation.
	/// </summary>
	internal static bool IsKnownColorName(string name)
	{
		return knownColors.ContainsKey(name);
	}

	public SystemColorHighlightingBrush(string name)
	{
		if (!knownColors.ContainsKey(name))
		{
			throw new ArgumentException("Unknown system color '" + name + "'.", nameof(name));
		}
		this.name = name;
	}

	public override Brush GetBrush(ITextRunConstructionContext? context)
	{
		return brush ??= new SolidColorBrush(knownColors[name]);
	}

	public override string ToString()
	{
		//was previously: returned the reflected System.Windows.SystemColors property name
		//(e.g. "WindowTextBrush"); the port returns the name as it appears in .xshd files.
		return "SystemColors." + name;
	}

	public override bool Equals(object? obj)
	{
		SystemColorHighlightingBrush? other = obj as SystemColorHighlightingBrush;
		if (other == null)
		{
			return false;
		}
		return this.name == other.name;
	}

	public override int GetHashCode()
	{
		return name.GetHashCode();
	}
}
