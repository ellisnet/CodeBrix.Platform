#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI;

using CodeBrix.Platform.UI.AdvancedTextEdit.Engine;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingBrush.cs in the AvalonEdit repo (MIT).
//Brush/SolidColorBrush/Color are now the Microsoft.UI.Xaml.Media / Windows.UI types. Binary
//serialization ([Serializable]/ISerializable) was dropped; it is dead on modern .NET.
//SimpleHighlightingBrush no longer freezes its brush (this framework's brushes have no Freeze());
//the brush instance is simply never mutated. SystemColorHighlightingBrush no longer reflects over
//the WPF System.Windows.SystemColors class; it resolves the color name from a fixed internal table
//of common system-color names instead (this framework has no live system-color broker).
//WPE1 C8 (the adapters form, decision D-M4): the built-in brushes STORE a neutral colour (SKColor)
//and create their XAML brush on first GetBrush; GetColorValue (internal) is the neutral read the
//highlighting engine and a CodeBrix.Mobile editor use. The public members are unchanged adapters.

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

	/// <summary>
	/// The neutral colour of the brush (WPE1 C8): what <see cref="GetColor"/> returns for a null context, as an
	/// <see cref="SKColor"/>. The built-in brushes answer from their neutral storage, with no XAML type involved; any
	/// other brush answers through its <see cref="GetColor"/> (and so needs the XAML object model).
	/// </summary>
	/// <returns>The colour, or null when the brush has no single colour.</returns>
	internal virtual SKColor? GetColorValue() => GetColorValueThroughXaml();

	[MethodImpl(MethodImplOptions.NoInlining)] //keeps the WinRT Color out of callers that never get here
	SKColor? GetColorValueThroughXaml()
	{
		Color? color = GetColor(null);
		return color is { } c ? new SKColor(c.R, c.G, c.B, c.A) : null;
	}

	/// <summary>The WinRT colour of a neutral colour (the XAML-side adapters).</summary>
	internal static Color ToWinRTColor(SKColor color) => Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);

	/// <summary>The neutral colour of a WinRT colour (the XAML-side adapters).</summary>
	internal static SKColor ToSKColor(Color color) => new SKColor(color.R, color.G, color.B, color.A);
}

/// <summary>
/// Highlighting brush implementation that takes a fixed brush.
/// </summary>
public sealed class SimpleHighlightingBrush : HighlightingBrush
{
	//The neutral storage (WPE1 C8); the SolidColorBrush is created on the first GetBrush and then always returned.
	readonly SKColor color;
	object? brush;

	/// <summary>
	/// Creates a new HighlightingBrush with the specified color.
	/// </summary>
	public SimpleHighlightingBrush(Color color) : this(ToSKColor(color))
	{
	}

	/// <summary>Creates a brush of a neutral colour (the highlighting engine's constructor: xshd loading).</summary>
	internal SimpleHighlightingBrush(SKColor color)
	{
		this.color = color;
	}

	/// <inheritdoc/>
	public override Brush GetBrush(ITextRunConstructionContext? context)
	{
		//was previously: the brush was created (and frozen) in the constructor; this framework's brushes have no
		//Freeze(), so the brush is treated as immutable by convention (it is never handed out for mutation).
		object? existing = Volatile.Read(ref brush);
		if (existing == null)
		{
			existing = new SolidColorBrush(ToWinRTColor(color));
			existing = Interlocked.CompareExchange(ref brush, existing, null) ?? existing;
		}
		return (Brush)existing;
	}

	/// <inheritdoc/>
	internal override SKColor? GetColorValue() => color;

	/// <inheritdoc/>
	public override string ToString()
	{
		//was previously: brush.ToString(); the WPF SolidColorBrush stringified to its color code, but
		//this framework's SolidColorBrush.ToString() does not, so the color is stringified directly
		//(as Windows.UI.Color.ToString() does: "#AARRGGBB").
		return HighlightingValues.ToColorString(color);
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		SimpleHighlightingBrush? other = obj as SimpleHighlightingBrush;
		if (other == null)
		{
			return false;
		}
		return this.color == other.color;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		//Windows.UI.Color's hash: the packed ARGB value, which is exactly what SKColor holds
		return unchecked((int)(uint)color);
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
	//(Neutral colours since WPE1 C8: the table is read by the highlighting engine with no XAML type involved.)
	static readonly Dictionary<string, SKColor> knownColors = new Dictionary<string, SKColor>(StringComparer.Ordinal) {
		{ "ActiveCaptionText", new SKColor(0x00, 0x00, 0x00, 0xFF) },
		{ "Control", new SKColor(0xF0, 0xF0, 0xF0, 0xFF) },
		{ "ControlText", new SKColor(0x00, 0x00, 0x00, 0xFF) },
		{ "GrayText", new SKColor(0x6D, 0x6D, 0x6D, 0xFF) },
		{ "Highlight", new SKColor(0x00, 0x78, 0xD7, 0xFF) },
		{ "HighlightText", new SKColor(0xFF, 0xFF, 0xFF, 0xFF) },
		{ "Info", new SKColor(0xFF, 0xFF, 0xE1, 0xFF) },
		{ "InfoText", new SKColor(0x00, 0x00, 0x00, 0xFF) },
		{ "Menu", new SKColor(0xF0, 0xF0, 0xF0, 0xFF) },
		{ "MenuText", new SKColor(0x00, 0x00, 0x00, 0xFF) },
		{ "Window", new SKColor(0xFF, 0xFF, 0xFF, 0xFF) },
		{ "WindowText", new SKColor(0x00, 0x00, 0x00, 0xFF) },
	};

	readonly string name;
	object? brush;

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
		return (Brush)(brush ??= new SolidColorBrush(ToWinRTColor(knownColors[name])));
	}

	internal override SKColor? GetColorValue() => knownColors[name];

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
