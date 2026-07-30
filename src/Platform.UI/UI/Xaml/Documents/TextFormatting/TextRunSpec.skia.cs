#nullable enable

using SkiaSharp;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// A host-free description of a single styled run of text.
/// </summary>
/// <remarks>
/// This is the non-XAML counterpart of an <see cref="Inline"/>. It carries exactly the
/// information the layout engine reads off an inline, minus the two
/// <see cref="DependencyObject"/>-typed members (the inline back-reference and the
/// foreground brush), so a layout can be built with no application host present.
/// <see cref="Color"/> is the host-free stand-in for the missing foreground brush: when set,
/// <see cref="UnicodeText.DrawToCanvas"/> paints this run's glyphs with it instead of the
/// caller's paint colour. The XAML inline path never sets it.
/// </remarks>
internal sealed record TextRunSpec(
	string Text,
	FontDetails FontDetails,
	FlowDirection FlowDirection,
	double FontSize,
	FontWeight FontWeight,
	FontStretch FontStretch,
	FontStyle FontStyle,
	SKColor? Color = null);
