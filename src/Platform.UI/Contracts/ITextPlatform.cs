#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The text engine: lays out the inlines of a <see cref="TextBlock"/> (measure and arrange), and creates the
/// platform side of each <see cref="TextBox"/>. The inline model (<c>Inline</c>, <c>Run</c>, <c>Span</c>,
/// <c>TextElement</c>) and the controls' own logic stay platform-neutral; shaping, fonts, glyph runs, line breaking
/// and drawing belong to the implementation.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once into <see cref="PlatformServices.Text"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Skia.TextSkiaPlatform</c> (the <c>UnicodeText</c> engine over HarfBuzz, ICU and Skia),
/// registered by <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>. It draws a text block from the platform
/// state of the block's composition visual, so drawing is not part of this contract.
/// </para>
/// </remarks>
internal interface ITextPlatform
{
	/// <summary>
	/// Gets the layout of a block that has not been measured yet: no lines, every query answers for empty text.
	/// </summary>
	ITextLayout EmptyLayout { get; }

	/// <summary>
	/// Lays out the inlines of <paramref name="textBlock"/> within <paramref name="availableSize"/>, using the block's
	/// font, wrapping, trimming, alignment, line height and line count properties.
	/// </summary>
	/// <param name="textBlock">The text block (its padding is already removed from <paramref name="availableSize"/>).</param>
	/// <param name="availableSize">The size available to the text.</param>
	/// <param name="desiredSize">The size the text needs.</param>
	/// <returns>The layout, which the block keeps until its next measure or arrange.</returns>
	ITextLayout CreateLayout(TextBlock textBlock, Size availableSize, out Size desiredSize);

	/// <summary>
	/// Creates the platform side of <paramref name="textBox"/>. Called once per text box, on first use, and kept in a
	/// field.
	/// </summary>
	/// <param name="textBox">The text box.</param>
	/// <returns>The platform side of the text box.</returns>
	ITextBoxPlatform CreateTextBoxPlatform(TextBox textBox);
}
