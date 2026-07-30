#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Search;

//was previously: ICSharpCode.AvalonEdit/Search/ISearchStrategy.cs in the AvalonEdit repo (MIT).
//SearchPatternException drops the binary-serialization constructor (guarded upstream for
//frameworks older than this library targets) and the redundant ISerializable interface listing.
//FindNext is annotated nullable: it returns null when there is no match in the given range.

/// <summary>
/// Basic interface for search algorithms.
/// </summary>
public interface ISearchStrategy : IEquatable<ISearchStrategy>
{
	/// <summary>
	/// Finds all matches in the given ITextSource and the given range.
	/// </summary>
	/// <remarks>
	/// This method must be implemented thread-safe.
	/// All segments in the result must be within the given range, and they must be returned in order
	/// (e.g. if two results are returned, EndOffset of first result must be less than or equal StartOffset of second result).
	/// </remarks>
	IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length);

	/// <summary>
	/// Finds the next match in the given ITextSource and the given range.
	/// Returns null when there is no match.
	/// </summary>
	/// <remarks>This method must be implemented thread-safe.</remarks>
	ISearchResult? FindNext(ITextSource document, int offset, int length);
}

/// <summary>
/// Represents a search result.
/// </summary>
public interface ISearchResult : ISegment
{
	/// <summary>
	/// Replaces parts of the replacement string with parts from the match. (e.g. $1)
	/// </summary>
	string ReplaceWith(string replacement);
}

/// <summary>
/// Defines supported search modes.
/// </summary>
public enum SearchMode
{
	/// <summary>
	/// Standard search
	/// </summary>
	Normal,
	/// <summary>
	/// RegEx search
	/// </summary>
	RegEx,
	/// <summary>
	/// Wildcard search
	/// </summary>
	Wildcard
}

/// <summary>
/// The exception thrown when a search pattern is invalid.
/// </summary>
public class SearchPatternException : Exception
{
	/// <summary>
	/// Creates a new SearchPatternException.
	/// </summary>
	public SearchPatternException()
	{
	}

	/// <summary>
	/// Creates a new SearchPatternException with the specified message.
	/// </summary>
	public SearchPatternException(string? message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new SearchPatternException with the specified message and inner exception.
	/// </summary>
	public SearchPatternException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}
