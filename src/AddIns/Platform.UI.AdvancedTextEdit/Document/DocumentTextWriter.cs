#nullable enable

using System;
using System.IO;
using System.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Document;

//was previously: ICSharpCode.AvalonEdit/Document/DocumentTextWriter.cs in the AvalonEdit repo (MIT).

/// <summary>
/// A TextWriter implementation that directly inserts into a document.
/// </summary>
public class DocumentTextWriter : TextWriter
{
	readonly IDocument document;
	int insertionOffset;

	/// <summary>
	/// Creates a new DocumentTextWriter that inserts into document, starting at insertionOffset.
	/// </summary>
	public DocumentTextWriter(IDocument document, int insertionOffset)
	{
		this.insertionOffset = insertionOffset;
		if (document == null)
			throw new ArgumentNullException(nameof(document));
		this.document = document;
		IDocumentLine? line = document.GetLineByOffset(insertionOffset);
		if (line.DelimiterLength == 0)
			line = line.PreviousLine;
		if (line != null)
			this.NewLine = document.GetText(line.EndOffset, line.DelimiterLength);
	}

	/// <summary>
	/// Gets/Sets the current insertion offset.
	/// </summary>
	public int InsertionOffset
	{
		get { return insertionOffset; }
		set { insertionOffset = value; }
	}

	/// <inheritdoc/>
	public override void Write(char value)
	{
		document.Insert(insertionOffset, value.ToString());
		insertionOffset++;
	}

	/// <inheritdoc/>
	public override void Write(char[] buffer, int index, int count)
	{
		document.Insert(insertionOffset, new string(buffer, index, count));
		insertionOffset += count;
	}

	/// <inheritdoc/>
	public override void Write(string? value)
	{
		//was previously: took non-nullable string (null flowed into IDocument.Insert, which threw
		//ArgumentNullException). The base override is nullable, so the same guard is explicit here.
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		document.Insert(insertionOffset, value);
		insertionOffset += value.Length;
	}

	/// <inheritdoc/>
	public override Encoding Encoding
	{
		get { return Encoding.UTF8; }
	}
}
