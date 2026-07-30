#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLinesInvalidException.cs in the AvalonEdit
//repo (MIT). [Serializable] and the (SerializationInfo, StreamingContext) constructor (already
//compiled out on .NET 6+) were dropped; binary serialization is dead on modern .NET.

/// <summary>
/// A VisualLinesInvalidException indicates that you accessed the <see cref="TextView.VisualLines"/> property
/// of the <see cref="TextView"/> while the visual lines were invalid.
/// </summary>
public class VisualLinesInvalidException : Exception
{
	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException() : base()
	{
	}

	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException(string message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException(string message, Exception? innerException) : base(message, innerException)
	{
	}
}
