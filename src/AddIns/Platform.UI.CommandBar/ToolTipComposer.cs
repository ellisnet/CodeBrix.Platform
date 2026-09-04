using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Builds the text a tool bar item shows in its tooltip and reads out to a screen reader.
/// </summary>
/// <remarks>
/// <para>
/// A tool bar is icons, so the tooltip carries what the icon cannot: what the button does, and the
/// keystroke that does the same thing without the mouse. The composer puts those together in one
/// place, in one order, for every kind of item - which is why the same wording appears whether the
/// text came from the button, from a bound command's label, or from a command's description.
/// </para>
/// <para>
/// Nothing here touches the visual tree, so the composition can be read, tested and reasoned about
/// on its own. The button decides WHETHER to show a tooltip; the composer only decides what it
/// would say.
/// </para>
/// </remarks>
public static class ToolTipComposer
{
	/// <summary>The separator between a shortcut's parts, as desktop keyboard shortcuts are written.</summary>
	private const string ShortcutJoin = "+";

	/// <summary>
	/// Composes the visible tooltip: the item's text, the shortcut that invokes it, and the
	/// command's longer description underneath.
	/// </summary>
	/// <param name="text">The item's label - usually a verb, such as "Save".</param>
	/// <param name="shortcutText">The keystroke, already formatted (for example "Ctrl+S"), or null.</param>
	/// <param name="description">The bound command's description, or null.</param>
	/// <returns>
	/// The tooltip text, or null when there is nothing worth showing. A shortcut is appended to the
	/// text in parentheses; a description that says something the text does not follows on its own
	/// line.
	/// </returns>
	public static string? Compose(string? text, string? shortcutText, string? description)
	{
		var head = Trimmed(text);
		var shortcut = Trimmed(shortcutText);
		var detail = Trimmed(description);

		var builder = new StringBuilder();

		if (head is not null && shortcut is not null)
		{
			builder.Append(head).Append(" (").Append(shortcut).Append(')');
		}
		else if (head is not null)
		{
			builder.Append(head);
		}
		else if (shortcut is not null)
		{
			builder.Append(shortcut);
		}

		//A description that merely repeats the label is noise in a tooltip, not help.
		if (detail is not null && !string.Equals(detail, head, StringComparison.CurrentCulture))
		{
			if (builder.Length > 0)
			{
				builder.Append('\n');
			}

			builder.Append(detail);
		}

		return builder.Length == 0 ? null : builder.ToString();
	}

	/// <summary>
	/// Composes the name a screen reader announces for an item: the same wording as the tooltip,
	/// followed by the bar it belongs to.
	/// </summary>
	/// <param name="text">The item's label.</param>
	/// <param name="shortcutText">The formatted keystroke, or null.</param>
	/// <param name="description">The bound command's description, or null.</param>
	/// <param name="barTitle">The title of the tool bar the item sits in, or null.</param>
	/// <returns>
	/// The accessible name, or null when the item has nothing to announce. The bar title comes last
	/// because the action is what the listener is waiting for; the bar is context.
	/// </returns>
	/// <remarks>
	/// The bar title is deliberately absent from the VISIBLE tooltip: a sighted user can see which
	/// bar a button is in, and repeating it in every tooltip would be clutter. A screen-reader user
	/// cannot, so it is said once, at the end.
	/// </remarks>
	public static string? ComposeAccessibleName(string? text, string? shortcutText, string? description, string? barTitle)
	{
		var composed = Compose(text, shortcutText, description);
		var bar = Trimmed(barTitle);

		if (bar is null)
		{
			return composed;
		}

		//A newline reads as a pause; a comma is what a screen reader wants between two phrases.
		return composed is null ? bar : composed.Replace('\n', ' ') + ", " + bar;
	}

	/// <summary>
	/// Formats a keyboard accelerator the way a shortcut is written on a menu: modifiers first,
	/// then the key, joined by plus signs.
	/// </summary>
	/// <param name="accelerator">The accelerator to describe.</param>
	/// <returns>The shortcut text, for example "Ctrl+Shift+S", or null when there is no key.</returns>
	public static string? FormatShortcut(KeyboardAccelerator? accelerator)
		=> accelerator is null ? null : FormatShortcut(accelerator.Modifiers, accelerator.Key);

	/// <summary>
	/// Formats a modifier set and a key the way a shortcut is written on a menu.
	/// </summary>
	/// <param name="modifiers">The modifier keys held down.</param>
	/// <param name="key">The key pressed.</param>
	/// <returns>The shortcut text, for example "Ctrl+Shift+S", or null when there is no key.</returns>
	/// <remarks>
	/// The order - Control, Alt, Windows, Shift - is the framework's own order for the same job, so
	/// a shortcut composed here reads identically to one the framework composes elsewhere.
	/// </remarks>
	public static string? FormatShortcut(VirtualKeyModifiers modifiers, VirtualKey key)
	{
		if (key == VirtualKey.None)
		{
			return null;
		}

		var parts = new List<string>(5);

		if ((modifiers & VirtualKeyModifiers.Control) != 0)
		{
			parts.Add("Ctrl");
		}

		if ((modifiers & VirtualKeyModifiers.Menu) != 0)
		{
			parts.Add("Alt");
		}

		if ((modifiers & VirtualKeyModifiers.Windows) != 0)
		{
			parts.Add("Windows");
		}

		if ((modifiers & VirtualKeyModifiers.Shift) != 0)
		{
			parts.Add("Shift");
		}

		parts.Add(DescribeKey(key));

		return string.Join(ShortcutJoin, parts);
	}

	/// <summary>
	/// Names a single key as a shortcut writes it: "S", "7", "F5", "Delete".
	/// </summary>
	/// <param name="key">The key to name.</param>
	/// <returns>The key's shortcut spelling.</returns>
	internal static string DescribeKey(VirtualKey key)
	{
		var code = (int)key;

		//The letter and digit keys carry their character in the enum value itself, and a shortcut
		//is always written with the character rather than with the enum's name.
		if (key >= VirtualKey.A && key <= VirtualKey.Z)
		{
			return ((char)code).ToString(CultureInfo.InvariantCulture);
		}

		if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
		{
			return ((char)code).ToString(CultureInfo.InvariantCulture);
		}

		if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
		{
			return "Num " + (char)('0' + (code - (int)VirtualKey.NumberPad0));
		}

		return key.ToString();
	}

	/// <summary>Returns the trimmed value, or null when it holds nothing to say.</summary>
	/// <param name="value">The candidate text.</param>
	/// <returns>The trimmed text, or null.</returns>
	private static string? Trimmed(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
