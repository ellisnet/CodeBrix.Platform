#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// A host-free description of a single styled run of text.
/// </summary>
/// <remarks>
/// This is the non-XAML counterpart of an inline (a Run or LineBreak). It carries exactly the information the layout
/// engine reads off an inline, minus the two XAML-typed members (the inline back-reference and the foreground brush),
/// so a layout can be built with no application host present. <see cref="Color"/> is the host-free stand-in for the
/// missing foreground brush: when set, <see cref="UnicodeText.DrawToCanvas"/> paints this run's glyphs with it instead
/// of the caller's paint colour. The XAML inline path never sets it.
/// <para>
/// Part of the shared text engine (compiled into the framework's Skia assembly and into
/// CodeBrix.Platform.UI.TextLayout.Core): it names no XAML or WinRT type.
/// </para>
/// </remarks>
internal sealed record TextRunSpec(
	string Text,
	FontDetails FontDetails,
	EngineFlowDirection FlowDirection,
	double FontSize,
	ushort FontWeight,
	EngineFontStretch FontStretch,
	EngineFontStyle FontStyle,
	SKColor? Color = null);
