#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/SingleCharacterElementGenerator.cs in the
//AvalonEdit repo (MIT). Differences forced by the engine model:
//- Tabs are ALWAYS turned into a TabTextElement (upstream only did so when ShowTabs was on and let
//  the WPF formatter expand raw '\t' otherwise): the engine has no tab stops, so the element
//  expands every tab to spaces itself - a marker arrow plus no-break padding when ShowTabs is on.
//  A tab stays ONE visual column; its expansion is layout text only.
//- The tab marker glyph is now the arrow '→' (upstream used '»').
//- The control-character box is drawn via the element's BackgroundBrush (a flat rectangle; the
//  rounded corners and 3-pixel padding of the upstream custom TextRun are gone).

/// <summary>
/// Element generator that displays "·" for spaces, expands tabs (with a "→" marker when
/// enabled), and shows a box with the name for control characters.
/// </summary>
/// <remarks>
/// This element generator is present in every text view by default; the enabled features can be
/// configured using the <see cref="AdvancedTextEditOptions"/>.
/// </remarks>
internal sealed class SingleCharacterElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
{
	/// <summary>
	/// Gets/Sets whether to show "·" for spaces.
	/// </summary>
	public bool ShowSpaces { get; set; }

	/// <summary>
	/// Gets/Sets whether to show "→" for tabs.
	/// </summary>
	public bool ShowTabs { get; set; }

	/// <summary>
	/// Gets/Sets whether to show a box with the hex code for control characters.
	/// </summary>
	public bool ShowBoxForControlCharacters { get; set; }

	/// <summary>
	/// Creates a new SingleCharacterElementGenerator instance.
	/// </summary>
	public SingleCharacterElementGenerator()
	{
		this.ShowSpaces = true;
		this.ShowTabs = true;
		this.ShowBoxForControlCharacters = true;
	}

	void IBuiltinElementGenerator.FetchOptions(AdvancedTextEditOptions options)
	{
		this.ShowSpaces = options.ShowSpaces;
		this.ShowTabs = options.ShowTabs;
		this.ShowBoxForControlCharacters = options.ShowBoxForControlCharacters;
	}

	/// <inheritdoc/>
	public override int GetFirstInterestedOffset(int startOffset)
	{
		Debug.Assert(CurrentContext != null, "GetFirstInterestedOffset may only be called during a generation run");
		DocumentLine endLine = CurrentContext.VisualLine.LastDocumentLine;
		StringSegment relevantText = CurrentContext.GetText(startOffset, endLine.EndOffset - startOffset);

		for (int i = 0; i < relevantText.Count; i++)
		{
			char c = relevantText.Text[relevantText.Offset + i];
			switch (c)
			{
				case ' ':
					if (ShowSpaces)
						return startOffset + i;
					break;
				case '\t':
					// Tabs always need an element: the engine has no tab stops, so the element
					// performs the expansion regardless of the ShowTabs marker option.
					return startOffset + i;
				default:
					if (ShowBoxForControlCharacters && char.IsControl(c))
					{
						return startOffset + i;
					}
					break;
			}
		}
		return -1;
	}

	/// <inheritdoc/>
	public override VisualLineElement? ConstructElement(int offset)
	{
		Debug.Assert(CurrentContext != null, "ConstructElement may only be called during a generation run");
		char c = CurrentContext.Document.GetCharAt(offset);
		if (ShowSpaces && c == ' ')
		{
			return new SpaceTextElement();
		}
		else if (c == '\t')
		{
			return new TabTextElement(ShowTabs);
		}
		else if (ShowBoxForControlCharacters && char.IsControl(c))
		{
			return new SpecialCharacterBoxElement(TextUtilities.GetControlCharacterName(c));
		}
		else
		{
			return null;
		}
	}

	static SKColor GetNonPrintableColor(ITextRunConstructionContext context)
	{
		return VisualLineElementTextRunProperties.GetSolidColor(context.TextView.NonPrintableCharacterBrush)
			?? new SKColor(128, 128, 128, 200);
	}

	sealed class SpaceTextElement : FormattedTextElement
	{
		public SpaceTextElement() : base("·", 1)
		{
		}

		public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
		{
			if (layoutText == null)
				throw new ArgumentNullException(nameof(layoutText));
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			layoutText.Append(Text);
			return CreateTextRunDescriptor(Text, GetNonPrintableColor(context));
		}

		public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			if (mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint)
				return base.GetNextCaretPosition(visualColumn, direction, mode);
			else
				return -1;
		}

		public override bool IsWhitespace(int visualColumn)
		{
			return true;
		}
	}

	sealed class TabTextElement : VisualLineElement
	{
		readonly bool showTabMarker;

		public TabTextElement(bool showTabMarker) : base(1, 1)
		{
			//was previously: the upstream element had visual length 2 (a marker glyph run plus a
			//'\t' run the WPF formatter expanded); the port keeps a tab at ONE visual column and
			//expands it in layout text instead.
			this.showTabMarker = showTabMarker;
		}

		public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
		{
			if (layoutText == null)
				throw new ArgumentNullException(nameof(layoutText));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			// The tab stop grid is measured in layout characters; the builder's current length is
			// this element's layout column.
			int indentationSize = Math.Max(1, context.TextView.Options.IndentationSize);
			int n = indentationSize - (layoutText.Length % indentationSize);
			string text;
			SKColor? color = null;
			if (showTabMarker)
			{
				// The no-break padding keeps the engine from wrapping inside the expanded tab.
				text = "→" + new string('\u00A0', n - 1);
				color = GetNonPrintableColor(context);
			}
			else
			{
				text = new string(' ', n);
			}
			layoutText.Append(text);
			return CreateTextRunDescriptor(text, color);
		}

		public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			if (mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint)
				return base.GetNextCaretPosition(visualColumn, direction, mode);
			else
				return -1;
		}

		public override bool IsWhitespace(int visualColumn)
		{
			return true;
		}
	}

	sealed class SpecialCharacterBoxElement : FormattedTextElement
	{
		public SpecialCharacterBoxElement(string text) : base(text, 1)
		{
			// The box: a gray fill behind the white character name. Drawn by the visual line's
			// background pass from this brush.
			BackgroundBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(200, 128, 128, 128));
		}

		public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
		{
			if (layoutText == null)
				throw new ArgumentNullException(nameof(layoutText));
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			layoutText.Append(Text);
			return CreateTextRunDescriptor(Text, SKColors.White);
		}
	}
}
