#nullable enable

using System;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Utils;

//was previously: ICSharpCode.AvalonEdit.Tests/Utils/IndentationStringTests.cs in the AvalonEdit repo (MIT).
//Upstream TextEditorOptions is AdvancedTextEditOptions in the port.

/// <summary>
/// Exercises <see cref="AdvancedTextEditOptions.IndentationString"/> and
/// <see cref="AdvancedTextEditOptions.GetIndentationString"/> for tab and space indentation.
/// </summary>
public class IndentationStringTests
{
	[Fact]
	public void indenting_with_tabs_always_yields_a_single_tab() // IndentWithSingleTab
	{
		//Arrange
		var options = new AdvancedTextEditOptions { IndentationSize = 4, ConvertTabsToSpaces = false };

		//Act + Assert
		Assert.Equal("\t", options.IndentationString);
		Assert.Equal("\t", options.GetIndentationString(2));
		Assert.Equal("\t", options.GetIndentationString(3));
		Assert.Equal("\t", options.GetIndentationString(4));
		Assert.Equal("\t", options.GetIndentationString(5));
		Assert.Equal("\t", options.GetIndentationString(6));
	}

	[Fact]
	public void indenting_with_spaces_fills_up_to_the_next_indentation_column() // IndentWith4Spaces
	{
		//Arrange
		var options = new AdvancedTextEditOptions { IndentationSize = 4, ConvertTabsToSpaces = true };

		//Act + Assert
		Assert.Equal("    ", options.IndentationString);
		Assert.Equal("   ", options.GetIndentationString(2));
		Assert.Equal("  ", options.GetIndentationString(3));
		Assert.Equal(" ", options.GetIndentationString(4));
		Assert.Equal("    ", options.GetIndentationString(5));
		Assert.Equal("   ", options.GetIndentationString(6));
	}
}
