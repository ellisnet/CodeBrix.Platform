#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Highlighting;

//was previously: ICSharpCode.AvalonEdit.Tests/Highlighting/HighlightingManagerTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises <see cref="HighlightingManager"/>.<c>RegisterHighlighting</c>: registering under an
/// existing name replaces the old definition in place.
/// </summary>
public class HighlightingManagerTests
{
	[Fact]
	public void registering_a_definition_with_an_existing_name_overwrites_it() // OverwriteHighlightingDefinitionWithSameName
	{
		//Arrange
		var highlightingManager = new HighlightingManager();

		var definitionA = CreateDefinition("TestDefinition");
		var definitionB = CreateDefinition("TestDefinition");
		var definitionC = CreateDefinition("DifferentName");

		Assert.Empty(highlightingManager.HighlightingDefinitions);

		//Act + Assert
		highlightingManager.RegisterHighlighting(definitionA.Name, Array.Empty<string>(), definitionA);
		Assert.Equal(new IHighlightingDefinition[] { definitionA }, highlightingManager.HighlightingDefinitions);

		highlightingManager.RegisterHighlighting(definitionB.Name, Array.Empty<string>(), definitionB);
		Assert.Equal(new IHighlightingDefinition[] { definitionB }, highlightingManager.HighlightingDefinitions);

		highlightingManager.RegisterHighlighting(definitionC.Name, Array.Empty<string>(), definitionC);
		Assert.Equal(new IHighlightingDefinition[] { definitionB, definitionC }, highlightingManager.HighlightingDefinitions);

		XmlHighlightingDefinition CreateDefinition(string name)
		{
			return new XmlHighlightingDefinition(new XshdSyntaxDefinition { Name = name, Elements = { new XshdRuleSet() } }, highlightingManager);
		}
	}
}
