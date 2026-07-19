#nullable enable

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// The knobs that control how a set of runs is laid out.
/// </summary>
public sealed class TextLayoutOptions
{
	/// <summary>
	/// The width to lay out within, or null for an unbounded single-line-per-paragraph layout.
	/// </summary>
	/// <remarks>
	/// Null means no wrapping: lines exist only where the text itself breaks them. Setting a width
	/// turns wrapping on and gives <see cref="Alignment"/> something to align within. Consumers that
	/// model their own line breaks - a text editor holding a list of lines, for instance - want null.
	/// </remarks>
	public float? MaxWidth { get; set; }

	/// <summary>The maximum number of lines to keep, or 0 for unlimited.</summary>
	public int MaxLines { get; set; }

	/// <summary>Horizontal alignment within <see cref="MaxWidth"/>. Ignored when that is null.</summary>
	public TextAlign Alignment { get; set; } = TextAlign.Left;

	/// <summary>An explicit line height, or 0 to take it from the font metrics.</summary>
	public float LineHeight { get; set; }

	/// <summary>
	/// The base writing direction of the layout as a whole.
	/// </summary>
	/// <remarks>
	/// <see cref="TextDirection.Auto"/> resolves the direction from the text content per UAX #9,
	/// which is what a general-purpose consumer usually wants.
	/// </remarks>
	public TextDirection BaseDirection { get; set; } = TextDirection.Auto;
}
