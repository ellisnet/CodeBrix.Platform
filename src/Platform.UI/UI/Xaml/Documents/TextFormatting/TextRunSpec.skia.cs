#nullable enable

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
/// </remarks>
internal sealed record TextRunSpec(
	string Text,
	FontDetails FontDetails,
	FlowDirection FlowDirection,
	double FontSize,
	FontWeight FontWeight,
	FontStretch FontStretch,
	FontStyle FontStyle);
