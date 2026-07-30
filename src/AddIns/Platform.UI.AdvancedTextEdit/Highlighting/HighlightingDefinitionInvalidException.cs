#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingDefinitionInvalidException.cs in
//the AvalonEdit repo (MIT). [Serializable] and the (SerializationInfo, StreamingContext)
//constructor (already compiled out on .NET 6+) were dropped; binary serialization is dead on
//modern .NET.

/// <summary>
/// Indicates that the highlighting definition that was tried to load was invalid.
/// </summary>
public class HighlightingDefinitionInvalidException : Exception
{
	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException() : base()
	{
	}

	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException(string message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException(string message, Exception? innerException) : base(message, innerException)
	{
	}
}
