using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Composition.Skia;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// A laid-out block of text as the Skia text engine produces it: the platform-neutral queries of
/// <see cref="ITextLayout"/>, plus drawing onto a Skia painting session.
/// </summary>
internal interface IParsedText : ITextLayout
{
	void Draw(in PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret, // null to skip drawing a caret
		(int selectionStart, int selectionEnd, CompositionBrush selectedTextBackgroundBrush, Brush selectedTextForegroundBrush)? selection); // null to skip drawing a selection
}
