#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Document;

//was previously: the WPF System.Windows.Documents.LogicalDirection enum, which this framework
//does not provide. Re-declared with the same member order so ported code and serialized values
//keep their meaning.

/// <summary>
/// A direction through the document's text: toward its start or toward its end.
/// </summary>
public enum LogicalDirection
{
	/// <summary>Toward the start of the document.</summary>
	Backward = 0,

	/// <summary>Toward the end of the document.</summary>
	Forward = 1,
}
