#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Indentation;

//was previously: ICSharpCode.AvalonEdit/Indentation/DefaultIndentationStrategy.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Handles indentation by copying the indentation from the previous line.
/// Does not support indenting multiple lines.
/// </summary>
public class DefaultIndentationStrategy : IIndentationStrategy
{
	/// <inheritdoc/>
	public virtual void IndentLine(TextDocument document, DocumentLine line)
	{
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}

		if (line == null)
		{
			throw new ArgumentNullException(nameof(line));
		}

		DocumentLine? previousLine = line.PreviousLine;
		if (previousLine != null)
		{
			ISegment indentationSegment = TextUtilities.GetWhitespaceAfter(document, previousLine.Offset);
			string indentation = document.GetText(indentationSegment);
			// copy indentation to line
			indentationSegment = TextUtilities.GetWhitespaceAfter(document, line.Offset);
			document.Replace(indentationSegment.Offset, indentationSegment.Length, indentation,
							 OffsetChangeMappingType.RemoveAndInsert);
			// OffsetChangeMappingType.RemoveAndInsert guarantees the caret moves behind the new indentation.
		}
	}

	/// <summary>
	/// Does nothing: indenting multiple lines is useless without a smart indentation strategy.
	/// </summary>
	public virtual void IndentLines(TextDocument document, int beginLine, int endLine)
	{
	}
}
