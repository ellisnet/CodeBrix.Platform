#nullable enable

using System;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Foundation;
using CodeBrix.Platform.UI.Composition.Skia;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="ITextPlatform"/>: lays out text blocks with the <c>UnicodeText</c> engine
/// (HarfBuzz shaping, ICU bidi and line breaking, Skia fonts), draws them, and creates the managed text box
/// (<see cref="TextBoxSkiaPlatform"/>).
/// </summary>
/// <remarks>The layout and drawing are the code that lived in <c>TextBlock.skia.cs</c>, moved verbatim.</remarks>
internal sealed class TextSkiaPlatform : ITextPlatform
{
	// ParsedText is an immutable struct, so one boxed empty layout serves every text block.
	private static readonly ITextLayout _emptyLayout = ParsedText.Empty;

	/// <inheritdoc />
	public ITextLayout EmptyLayout => _emptyLayout;

	/// <inheritdoc />
	public ITextLayout CreateLayout(TextBlock textBlock, Size availableSize, out Size desiredSize) =>
		new UnicodeText(
			availableSize,
			textBlock.Inlines.TraversedTree.leafTree,
			GetDefaultFontDetails(textBlock),
			textBlock.MaxLines,
			(float)textBlock.LineHeight,
			textBlock.LineStackingStrategy,
			textBlock.FlowDirection,
			(textBlock.OwningTextBox as IDependencyObjectStoreProvider)?.Store
				.GetCurrentHighestValuePrecedence(TextBox.TextAlignmentProperty) is DependencyPropertyValuePrecedences.DefaultValue
					? null
					: textBlock.TextAlignment,
			textBlock.TextWrapping,
			out desiredSize);

	/// <inheritdoc />
	public ITextBoxPlatform CreateTextBoxPlatform(TextBox textBox) => new TextBoxSkiaPlatform(textBox);

	/// <summary>
	/// Draws the text of <paramref name="textBlock"/>, with its selection and caret, into <paramref name="session"/>.
	/// Called by the platform state of the block's <see cref="TextVisual"/> each time the visual is painted.
	/// </summary>
	/// <param name="textBlock">The text block.</param>
	/// <param name="session">The painting session.</param>
	internal static void Draw(TextBlock textBlock, in PaintingSession session)
	{
		session.Canvas.Save();
		session.Canvas.Translate((float)textBlock.Padding.Left, (float)textBlock.Padding.Top);
		var selection = (Math.Min(textBlock.Selection.start, textBlock.Selection.end), Math.Max(textBlock.Selection.start, textBlock.Selection.end), textBlock.SelectionHighlightColor.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()), DefaultBrushes.SelectedTextForegroundColor);
		((IParsedText)textBlock.ParsedText).Draw(
			session,
			textBlock.RenderCaret is { } c ? (c.index, c.brush, TextBlock.CaretThickness) : null,
			textBlock.RenderSelection ? selection : null);
		session.Canvas.Restore();
		textBlock.RaiseDrawingFinished();
	}

	/// <summary>
	/// Gets the line height of the TextBlock either
	/// based on the LineHeight property or the default
	/// font line height.
	/// </summary>
	/// <returns>Computed line height</returns>
	private static FontDetails GetDefaultFontDetails(TextBlock textBlock)
	{
		var (details, task) = FontDetailsCache.GetFont(textBlock.FontFamily?.Source, (float)textBlock.FontSize, textBlock.FontWeight, textBlock.FontStretch, textBlock.FontStyle);
		if (task.IsCompletedSuccessfully)
		{
			return task.Result;
		}
		else
		{
			task.ContinueWith(_ =>
			{
				NativeDispatcher.Main.Enqueue(textBlock.OnFontLoaded);
			});
			return details;
		}
	}
}
