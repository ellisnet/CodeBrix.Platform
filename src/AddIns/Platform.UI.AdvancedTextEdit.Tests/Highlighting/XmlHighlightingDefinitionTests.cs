#nullable enable

using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Highlighting;

//was previously: ICSharpCode.AvalonEdit.Tests/Highlighting/XmlHighlightingDefinitionTests.cs in the AvalonEdit repo (MIT).
//The color name is a const local so the non-nullable XshdReference constructor argument needs no
//null-forgiveness; the test data is unchanged.

/// <summary>
/// Exercises keyword matching in <see cref="XmlHighlightingDefinition"/>.
/// </summary>
public class XmlHighlightingDefinitionTests
{
	[Fact]
	public void longer_keywords_are_preferred_over_shorter_prefix_keywords() // LongerKeywordsArePreferred
	{
		//Arrange
		const string colorName = "Result";
		var color = new XshdColor { Name = colorName };
		var syntaxDefinition = new XshdSyntaxDefinition
		{
			Elements =
			{
				color,
				new XshdRuleSet
				{
					Elements = { new XshdKeywords
						{
							ColorReference = new XshdReference<XshdColor>(null, colorName),
							Words = { "foo", "foo.bar." }
						}
					}
				}
			}
		};

		var document = new TextDocument("This is a foo.bar. keyword");
		var highlighter = new DocumentHighlighter(document, new XmlHighlightingDefinition(syntaxDefinition, HighlightingManager.Instance));

		//Act
		var result = highlighter.HighlightLine(1);

		//Assert
		var highlightedText = document.GetText(result.Sections.Single());
		Assert.Equal("foo.bar.", highlightedText);
	}
}
