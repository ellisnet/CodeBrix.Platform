#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/ExtensionMethods.cs in the AvalonEdit repo (MIT).
//The WPF-free helpers (epsilon comparisons, CoerceValue, collection and XML helpers, debug
//logging) port unchanged; the Size overload of IsClose now operates on Windows.Foundation.Size.
//The WPF-only members were dropped - each drop is noted at its original position below.

static class ExtensionMethods
{
	#region Epsilon / IsClose / CoerceValue
	/// <summary>
	/// Epsilon used for <c>IsClose()</c> implementations.
	/// We can use up quite a few digits in front of the decimal point (due to visual positions being relative to document origin),
	/// and there's no need to be too accurate (we're dealing with pixels here),
	/// so we will use the value 0.01.
	/// Previously we used 1e-8 but that was causing issues:
	/// http://community.sharpdevelop.net/forums/t/16048.aspx
	/// </summary>
	public const double Epsilon = 0.01;

	/// <summary>
	/// Returns true if the doubles are close (difference smaller than 0.01).
	/// </summary>
	public static bool IsClose(this double d1, double d2)
	{
		if (d1 == d2) // required for infinities
			return true;
		return Math.Abs(d1 - d2) < Epsilon;
	}

	/// <summary>
	/// Returns true if the doubles are close (difference smaller than 0.01).
	/// </summary>
	public static bool IsClose(this Size d1, Size d2)
	{
		return IsClose(d1.Width, d2.Width) && IsClose(d1.Height, d2.Height);
	}

	//was previously: IsClose(this Vector, Vector) - dropped; System.Windows.Vector has no
	//counterpart here (PORT-RULES maps Vector to plain double/Point arithmetic).

	/// <summary>
	/// Forces the value to stay between minimum and maximum.
	/// </summary>
	/// <returns>minimum, if value is less than minimum.
	/// Maximum, if value is greater than maximum.
	/// Otherwise, value.</returns>
	public static double CoerceValue(this double value, double minimum, double maximum)
	{
		return Math.Max(Math.Min(value, maximum), minimum);
	}

	/// <summary>
	/// Forces the value to stay between minimum and maximum.
	/// </summary>
	/// <returns>minimum, if value is less than minimum.
	/// Maximum, if value is greater than maximum.
	/// Otherwise, value.</returns>
	public static int CoerceValue(this int value, int minimum, int maximum)
	{
		return Math.Max(Math.Min(value, maximum), minimum);
	}
	#endregion

	//was previously: CreateTypeface(this FrameworkElement) - dropped; WPF Typeface construction
	//is replaced by the shared text engine's font handling in this port.

	#region AddRange / Sequence
	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
	{
		foreach (T e in elements)
			collection.Add(e);
	}

	/// <summary>
	/// Creates an IEnumerable with a single value.
	/// </summary>
	public static IEnumerable<T> Sequence<T>(T value)
	{
		yield return value;
	}
	#endregion

	#region XML reading
	/// <summary>
	/// Gets the value of the attribute, or null if the attribute does not exist.
	/// </summary>
	public static string? GetAttributeOrNull(this XmlElement element, string attributeName)
	{
		XmlAttribute? attr = element.GetAttributeNode(attributeName);
		return attr != null ? attr.Value : null;
	}

	/// <summary>
	/// Gets the value of the attribute as boolean, or null if the attribute does not exist.
	/// </summary>
	public static bool? GetBoolAttribute(this XmlElement element, string attributeName)
	{
		XmlAttribute? attr = element.GetAttributeNode(attributeName);
		return attr != null ? (bool?)XmlConvert.ToBoolean(attr.Value) : null;
	}

	/// <summary>
	/// Gets the value of the attribute as boolean, or null if the attribute does not exist.
	/// </summary>
	public static bool? GetBoolAttribute(this XmlReader reader, string attributeName)
	{
		string? attributeValue = reader.GetAttribute(attributeName);
		if (attributeValue == null)
			return null;
		else
			return XmlConvert.ToBoolean(attributeValue);
	}
	#endregion

	//was previously: the "DPI independence" region - TransformToDevice(this Rect, Visual),
	//TransformFromDevice(this Rect, Visual), TransformToDevice(this Size, Visual),
	//TransformFromDevice(this Size, Visual), TransformToDevice(this Point, Visual) and
	//TransformFromDevice(this Point, Visual) - all dropped; they required WPF's
	//PresentationSource/CompositionTarget device transforms. DPI handling in this framework
	//comes from the visual scale (see the PixelSnapHelpers rewrite).

	//was previously: the "System.Drawing <-> WPF conversions" region - ToSystemDrawing(this Point),
	//ToWpf(this System.Drawing.Point), ToWpf(this System.Drawing.Size) and
	//ToWpf(this System.Drawing.Rectangle) - all dropped; they only served the Win32/IME interop
	//code that is out of scope for this port.

	//was previously: VisualAncestorsAndSelf(this DependencyObject) - dropped; it walked the WPF
	//visual/logical tree. Callers are re-expressed against this framework's visual tree per
	//PORT-RULES.

	//was previously: CheckIsFrozen(Freezable) - dropped; WPF's Freezable does not exist here (the
	//editor's own IFreezable types have no equivalent performance warning).

	[Conditional("DEBUG")]
	public static void Log(bool condition, string format, params object[] args)
	{
		if (condition)
		{
			string output = DateTime.Now.ToString("hh:MM:ss") + ": " + string.Format(format, args) + Environment.NewLine + Environment.StackTrace;
			Console.WriteLine(output);
			Debug.WriteLine(output);
		}
	}
}
