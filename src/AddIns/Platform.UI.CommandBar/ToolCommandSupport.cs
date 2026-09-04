using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The command-binding rules every tool bar button follows, in one place.
/// </summary>
/// <remarks>
/// <para>
/// A command object can supply a button's label, icon, description, shortcut and access key, but
/// only where the button did not say those things for itself - the button always wins. Gathering
/// that precedence here means the three button types share one answer instead of three, and that
/// the rules can be read and tested without a visual tree.
/// </para>
/// <para>
/// The modifier probe lives here too, because "which keys were down at the click" is part of the
/// same story: what the application learns about an invocation.
/// </para>
/// </remarks>
internal static class ToolCommandSupport
{
	/// <summary>
	/// Reads the modifier keys currently held down. Replaceable so a test can state the answer
	/// instead of pressing keys.
	/// </summary>
	/// <remarks>
	/// This is process-wide state, which is why the suite runs one test at a time and every test
	/// that replaces it restores it in a finally.
	/// </remarks>
	internal static Func<VirtualKeyModifiers> ModifierProbe { get; set; } = ReadModifiersFromInput;

	/// <summary>Reads the live modifier state from the platform's keyboard input source.</summary>
	/// <returns>The modifier keys held down on the calling thread.</returns>
	internal static VirtualKeyModifiers ReadModifiersFromInput()
	{
		var modifiers = VirtualKeyModifiers.None;

		if (IsDown(VirtualKey.Shift))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (IsDown(VirtualKey.Control))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		if (IsDown(VirtualKey.Menu))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;

		static bool IsDown(VirtualKey key)
			=> (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
	}

	/// <summary>
	/// Picks the label a button shows: its own text first, then the bound command's label, then a
	/// string that was handed to the button as content.
	/// </summary>
	/// <param name="ownText">The text set on the button, if any.</param>
	/// <param name="command">The bound command, if any.</param>
	/// <param name="content">The button's content, which is a string when the label arrived that way.</param>
	/// <returns>The label to show, or null when the button has none.</returns>
	/// <remarks>
	/// The content fallback matters because the framework binds a <see cref="XamlUICommand"/>'s
	/// label onto a button's Content property; honouring it means a command's label reaches a tool
	/// bar button by either route.
	/// </remarks>
	internal static string? ResolveText(string? ownText, ICommand? command, object? content)
	{
		if (!string.IsNullOrEmpty(ownText))
		{
			return ownText;
		}

		if (command is XamlUICommand uiCommand && !string.IsNullOrEmpty(uiCommand.Label))
		{
			return uiCommand.Label;
		}

		return content as string;
	}

	/// <summary>
	/// Picks the icon source a button shows: its own icon first, then the bound command's.
	/// </summary>
	/// <param name="ownIcon">The icon set on the button, if any.</param>
	/// <param name="command">The bound command, if any.</param>
	/// <returns>The icon source to render, or null.</returns>
	/// <remarks>
	/// The result is an <see cref="IconSource"/> rather than a <see cref="ToolIconSource"/>: a
	/// command written for the rest of the framework carries a symbol or font icon, and a tool bar
	/// button shows it rather than nothing.
	/// </remarks>
	internal static IconSource? ResolveIconSource(ToolIconSource? ownIcon, ICommand? command)
	{
		if (ownIcon is not null)
		{
			return ownIcon;
		}

		return (command as XamlUICommand)?.IconSource;
	}

	/// <summary>Reads the description a bound command supplies for the tooltip.</summary>
	/// <param name="command">The bound command, if any.</param>
	/// <returns>The description, or null.</returns>
	internal static string? ResolveDescription(ICommand? command)
	{
		var description = (command as XamlUICommand)?.Description;
		return string.IsNullOrEmpty(description) ? null : description;
	}

	/// <summary>
	/// Picks the shortcut text a tooltip shows: the button's own shortcut string, then its own
	/// registered accelerator, then the bound command's first accelerator.
	/// </summary>
	/// <param name="ownShortcut">The shortcut text set on the button, if any.</param>
	/// <param name="ownAccelerators">The accelerators registered on the button.</param>
	/// <param name="command">The bound command, if any.</param>
	/// <returns>The formatted shortcut, or null when the item has none.</returns>
	internal static string? ResolveShortcutText(
		string? ownShortcut,
		IList<KeyboardAccelerator>? ownAccelerators,
		ICommand? command)
	{
		if (!string.IsNullOrWhiteSpace(ownShortcut))
		{
			return ownShortcut.Trim();
		}

		if (ownAccelerators is { Count: > 0 })
		{
			var fromOwn = ToolTipComposer.FormatShortcut(ownAccelerators[0]);
			if (fromOwn is not null)
			{
				return fromOwn;
			}
		}

		if (command is XamlUICommand uiCommand && uiCommand.KeyboardAccelerators.Count > 0)
		{
			return ToolTipComposer.FormatShortcut(uiCommand.KeyboardAccelerators[0]);
		}

		return null;
	}

	/// <summary>
	/// Gives the element the command's access key, unless it already has one of its own.
	/// </summary>
	/// <param name="target">The button to set the access key on.</param>
	/// <param name="command">The bound command, if any.</param>
	internal static void SyncAccessKey(UIElement target, ICommand? command)
	{
		if (!string.IsNullOrEmpty(target.AccessKey))
		{
			return;
		}

		var accessKey = (command as XamlUICommand)?.AccessKey;

		if (!string.IsNullOrEmpty(accessKey))
		{
			target.AccessKey = accessKey;
		}
	}

	/// <summary>
	/// Finds the title of the tool bar an item belongs to, for the name a screen reader announces.
	/// </summary>
	/// <param name="element">The item to look upwards from.</param>
	/// <returns>The bar's title, or null when the item is not in a titled bar.</returns>
	/// <remarks>
	/// A bar states its title either as an automation name or as a Title property; both are read,
	/// nearest ancestor first, so an item announces the bar it is in however the bar was written.
	/// The property is looked up through the framework's own dependency-property registry rather
	/// than by reflection, so nothing here depends on a type being kept by the trimmer.
	/// </remarks>
	internal static string? FindBarTitle(FrameworkElement element)
	{
		for (DependencyObject? current = element.Parent; current is not null;)
		{
			if (current is FrameworkElement ancestor)
			{
				var automationName = AutomationProperties.GetName(ancestor);
				if (!string.IsNullOrWhiteSpace(automationName))
				{
					return automationName.Trim();
				}

				var titleProperty = DependencyProperty.GetProperty(ancestor.GetType(), "Title");
				if (titleProperty is not null && ancestor.GetValue(titleProperty) is string title
					&& !string.IsNullOrWhiteSpace(title))
				{
					return title.Trim();
				}

				current = ancestor.Parent;
			}
			else
			{
				current = null;
			}
		}

		return null;
	}
}
