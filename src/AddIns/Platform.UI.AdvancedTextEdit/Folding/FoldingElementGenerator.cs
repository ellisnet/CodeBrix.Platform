#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Folding;

//was previously: ICSharpCode.AvalonEdit/Folding/FoldingElementGenerator.cs in the AvalonEdit repo
//(MIT). The folding-lookup logic is transliterated. Re-expressions forced by the render design:
//- The WPF text-formatter plumbing (TextFormatterFactory/PrepareText producing a TextLine) is
//  gone: FoldingLineElement carries the title string and contributes it to the visual line's
//  single engine layout via BuildLayoutText, coloring the run with the static TextBrush.
//- FoldingLineTextRun (which drew a 1-pixel border rectangle around the collapsed-section title)
//  has no counterpart: the port has no per-element draw hook, so the title renders in the text
//  brush without the surrounding box.
//- OnMouseDown expanded the section on a left double-click; pointer events carry no click count
//  at the element seam, so OnPointerPressed expands on a single left-button press.
//- GetFirstInterestedOffset's upstream loop over GetFoldingsContaining was dead code (its body
//  was commented out upstream) and was not carried over.

/// <summary>
/// A <see cref="VisualLineElementGenerator"/> that produces line elements for folded <see cref="FoldingSection"/>s.
/// </summary>
public sealed class FoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
{
	readonly List<TextView> textViews = new List<TextView>();
	FoldingManager? foldingManager;

	#region FoldingManager property / connecting with TextView
	/// <summary>
	/// Gets/Sets the folding manager from which the foldings should be shown.
	/// </summary>
	public FoldingManager? FoldingManager {
		get {
			return foldingManager;
		}
		set {
			if (foldingManager != value)
			{
				if (foldingManager != null)
				{
					foreach (TextView v in textViews)
						foldingManager.RemoveFromTextView(v);
				}
				foldingManager = value;
				if (foldingManager != null)
				{
					foreach (TextView v in textViews)
						foldingManager.AddToTextView(v);
				}
			}
		}
	}

	void ITextViewConnect.AddToTextView(TextView textView)
	{
		textViews.Add(textView);
		if (foldingManager != null)
			foldingManager.AddToTextView(textView);
	}

	void ITextViewConnect.RemoveFromTextView(TextView textView)
	{
		textViews.Remove(textView);
		if (foldingManager != null)
			foldingManager.RemoveFromTextView(textView);
	}
	#endregion

	/// <inheritdoc/>
	public override void StartGeneration(ITextRunConstructionContext context)
	{
		base.StartGeneration(context);
		if (foldingManager != null)
		{
			if (!foldingManager.textViews.Contains(context.TextView))
				throw new ArgumentException("Invalid TextView");
			if (context.Document != foldingManager.document)
				throw new ArgumentException("Invalid document");
		}
	}

	/// <inheritdoc/>
	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (foldingManager != null)
		{
			return foldingManager.GetNextFoldedFoldingStart(startOffset);
		}
		else
		{
			return -1;
		}
	}

	/// <inheritdoc/>
	public override VisualLineElement? ConstructElement(int offset)
	{
		if (foldingManager == null)
			return null;
		int foldedUntil = -1;
		FoldingSection? foldingSection = null;
		foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(offset))
		{
			if (fs.IsFolded)
			{
				if (fs.EndOffset > foldedUntil)
				{
					foldedUntil = fs.EndOffset;
					foldingSection = fs;
				}
			}
		}
		if (foldedUntil > offset && foldingSection != null)
		{
			// Handle overlapping foldings: if there's another folded folding
			// (starting within the foldingSection) that continues after the end of the folded section,
			// then we'll extend our fold element to cover that overlapping folding.
			bool foundOverlappingFolding;
			do
			{
				foundOverlappingFolding = false;
				foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(foldedUntil))
				{
					if (fs.IsFolded && fs.EndOffset > foldedUntil)
					{
						foldedUntil = fs.EndOffset;
						foundOverlappingFolding = true;
					}
				}
			} while (foundOverlappingFolding);

			string? title = foldingSection.Title;
			if (string.IsNullOrEmpty(title))
				title = "...";
			return new FoldingLineElement(foldingSection, title, foldedUntil - offset, textBrush);
		}
		else
		{
			return null;
		}
	}

	sealed class FoldingLineElement : FormattedTextElement
	{
		readonly FoldingSection fs;
		readonly Brush titleBrush;

		public FoldingLineElement(FoldingSection fs, string text, int documentLength, Brush titleBrush) : base(text, documentLength)
		{
			this.fs = fs;
			this.titleBrush = titleBrush;
		}

		public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
		{
			if (layoutText == null)
				throw new ArgumentNullException(nameof(layoutText));
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			//was previously: a VisualLineElementTextRunProperties clone got SetForegroundBrush
			//before the WPF text line was prepared; here the color rides on the run descriptor.
			layoutText.Append(Text);
			return CreateTextRunDescriptor(Text, VisualLineElementTextRunProperties.GetSolidColor(titleBrush));
		}

		//was previously: OnMouseDown, expanding only on a double-click (e.ClickCount == 2); see
		//the file header note on the single-click divergence.
		protected internal override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			if (e.Handled)
				return;
			if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
				return;
			fs.IsFolded = false;
			e.Handled = true;
		}
	}

	/// <summary>
	/// Default brush for folding element text: gray.
	/// </summary>
	public static readonly Brush DefaultTextBrush =
		new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 128, 128, 128));

	static Brush textBrush = DefaultTextBrush;

	/// <summary>
	/// Gets/sets the brush used for folding element text.
	/// </summary>
	public static Brush TextBrush {
		get { return textBrush; }
		set { textBrush = value; }
	}
}
