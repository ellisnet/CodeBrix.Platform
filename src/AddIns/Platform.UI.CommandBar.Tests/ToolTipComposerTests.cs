using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml.Input;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The wording of a tool bar item's tooltip and of the name a screen reader announces.
/// </summary>
public class ToolTipComposerTests
{
	[Fact]
	public void Compose_puts_the_shortcut_in_parentheses_after_the_text()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose("Save", "Ctrl+S", null);

		//Assert
		composed.Should().Be("Save (Ctrl+S)");
	}

	[Fact]
	public void Compose_returns_the_text_alone_when_there_is_no_shortcut()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose("Save", null, null);

		//Assert
		composed.Should().Be("Save");
	}

	[Fact]
	public void Compose_returns_the_shortcut_alone_when_there_is_no_text()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose(null, "Ctrl+S", null);

		//Assert
		composed.Should().Be("Ctrl+S");
	}

	[Fact]
	public void Compose_puts_a_description_on_its_own_line()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose("Save", "Ctrl+S", "Write the score to disk");

		//Assert
		composed.Should().Be("Save (Ctrl+S)\nWrite the score to disk");
	}

	[Fact]
	public void Compose_drops_a_description_that_only_repeats_the_text()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose("Save", null, "Save");

		//Assert
		//A tooltip reading "Save / Save" tells the reader nothing twice.
		composed.Should().Be("Save");
	}

	[Fact]
	public void Compose_returns_null_when_there_is_nothing_to_say()
	{
		//Arrange
		//Act
		var composed = ToolTipComposer.Compose("   ", null, null);

		//Assert
		composed.Should().BeNull();
	}

	[Fact]
	public void ComposeAccessibleName_ends_with_the_bar_title()
	{
		//Arrange
		//Act
		var name = ToolTipComposer.ComposeAccessibleName("Save", "Ctrl+S", null, "Main");

		//Assert
		name.Should().Be("Save (Ctrl+S), Main");
	}

	[Fact]
	public void ComposeAccessibleName_says_a_description_on_one_line()
	{
		//Arrange
		//Act
		var name = ToolTipComposer.ComposeAccessibleName("Save", null, "Write the score to disk", "Main");

		//Assert
		//A newline is a pause to a screen reader; the whole announcement is one phrase.
		name.Should().Be("Save Write the score to disk, Main");
	}

	[Fact]
	public void ComposeAccessibleName_falls_back_to_the_tooltip_when_there_is_no_bar_title()
	{
		//Arrange
		//Act
		var name = ToolTipComposer.ComposeAccessibleName("Save", "Ctrl+S", null, null);

		//Assert
		name.Should().Be("Save (Ctrl+S)");
	}

	[Fact]
	public void FormatShortcut_writes_the_modifiers_before_the_key()
	{
		//Arrange
		//Act
		var text = ToolTipComposer.FormatShortcut(
			VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
			VirtualKey.S);

		//Assert
		text.Should().Be("Ctrl+Shift+S");
	}

	[Fact]
	public void FormatShortcut_writes_the_modifiers_in_the_frameworks_own_order()
	{
		//Arrange
		//Act
		var text = ToolTipComposer.FormatShortcut(
			VirtualKeyModifiers.Shift | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Control,
			VirtualKey.F5);

		//Assert
		//Control, Alt, Windows, Shift - the same order the framework uses when it writes a shortcut
		//itself, so the two never disagree on the same key combination.
		text.Should().Be("Ctrl+Alt+Shift+F5");
	}

	[Fact]
	public void FormatShortcut_names_digits_and_letters_by_their_character()
	{
		//Arrange
		//Act
		var digit = ToolTipComposer.FormatShortcut(VirtualKeyModifiers.Control, VirtualKey.Number7);
		var letter = ToolTipComposer.FormatShortcut(VirtualKeyModifiers.None, VirtualKey.Z);

		//Assert
		digit.Should().Be("Ctrl+7");
		letter.Should().Be("Z");
	}

	[Fact]
	public void FormatShortcut_returns_null_without_a_key()
	{
		//Arrange
		//Act
		var text = ToolTipComposer.FormatShortcut(VirtualKeyModifiers.Control, VirtualKey.None);

		//Assert
		//Modifiers alone are not a shortcut, and "Ctrl+" is not something to show a user.
		text.Should().BeNull();
	}

	[Fact]
	public void FormatShortcut_reads_an_accelerator_object()
	{
		//Arrange
		var accelerator = new KeyboardAccelerator
		{
			Key = VirtualKey.P,
			Modifiers = VirtualKeyModifiers.Control,
		};

		//Act
		var text = ToolTipComposer.FormatShortcut(accelerator);

		//Assert
		text.Should().Be("Ctrl+P");
	}
}
