#nullable enable

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/RichTextWriter.cs in the AvalonEdit repo (MIT).
//The WPF media types on the span methods are re-mapped for this framework: Color is
//Windows.UI.Color, FontWeight is Windows.UI.Text.FontWeight, FontStyle is
//Windows.UI.Text.FontStyle and FontFamily is Microsoft.UI.Xaml.Media.FontFamily. Member names
//and the abstract surface are unchanged.

// TODO: This class (and derived classes) is currently unused; decide whether to keep it.
// (until this is decided, keep the class internal)

/// <summary>
/// A text writer that supports creating spans of highlighted text.
/// </summary>
abstract class RichTextWriter : TextWriter
{
	/// <summary>
	/// Gets called by the RichTextWriter base class when a BeginSpan() method
	/// that is not overwritten gets called.
	/// </summary>
	protected abstract void BeginUnhandledSpan();

	/// <summary>
	/// Writes the RichText instance.
	/// </summary>
	public void Write(RichText richText)
	{
		Write(richText, 0, richText.Length);
	}

	/// <summary>
	/// Writes the RichText instance.
	/// </summary>
	public virtual void Write(RichText richText, int offset, int length)
	{
		// We have to use a TextWriter reference to invoke the virtual Write(string) method.
		// If we just call Write(richText.Text.Substring(...)) below, then the C# compiler invokes
		// the non-virtual Write(RichText) method due to RichText's implicit conversion from string.
		// That leads to an immediate, unconditional StackOverflowException!
		foreach (var section in richText.GetHighlightedSections(offset, length))
		{
			Debug.Assert(section.Color != null); // RichText.GetHighlightedSections always sets a color on every section
			BeginSpan(section.Color);
			((TextWriter)this).Write(richText.Text.Substring(section.Offset, section.Length));
			EndSpan();
		}
	}

	/// <summary>
	/// Begin a colored span.
	/// </summary>
	public virtual void BeginSpan(Color foregroundColor)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font weight.
	/// </summary>
	public virtual void BeginSpan(FontWeight fontWeight)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font style.
	/// </summary>
	public virtual void BeginSpan(FontStyle fontStyle)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font family.
	/// </summary>
	public virtual void BeginSpan(FontFamily fontFamily)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a highlighted span.
	/// </summary>
	public virtual void BeginSpan(HighlightingColor highlightingColor)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span that links to the specified URI.
	/// </summary>
	public virtual void BeginHyperlinkSpan(Uri uri)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Marks the end of the current span.
	/// </summary>
	public abstract void EndSpan();

	/// <summary>
	/// Increases the indentation level.
	/// </summary>
	public abstract void Indent();

	/// <summary>
	/// Decreases the indentation level.
	/// </summary>
	public abstract void Unindent();
}
