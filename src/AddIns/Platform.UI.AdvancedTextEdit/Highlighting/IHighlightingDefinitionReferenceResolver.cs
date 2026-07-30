#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/IHighlightingDefinitionReferenceResolver.cs
//in the AvalonEdit repo (MIT).

/// <summary>
/// Interface for resolvers that can solve cross-definition references.
/// </summary>
public interface IHighlightingDefinitionReferenceResolver
{
	/// <summary>
	/// Gets the highlighting definition by name, or null if it is not found.
	/// </summary>
	IHighlightingDefinition? GetDefinition(string name);
}
