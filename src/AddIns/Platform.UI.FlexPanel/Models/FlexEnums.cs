#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;

namespace CodeBrix.Platform.UI.FlexPanel;

//was previously: src/Core/src/Layouts/FlexEnums.cs (namespace Microsoft.Maui.Layouts) in dotnet/maui;
//the MAUI [TypeConverter] attributes are dropped because the CodeBrix.Platform XAML engine parses
//enum attribute values natively. Each member is pinned to its internal engine value so the panel
//converts with a plain cast.

/// <summary>
/// Direction and main axis of a <see cref="FlexPanel"/>'s children.
/// </summary>
public enum FlexDirection
{
	/// <summary>Children are stacked vertically; the main axis is the y-axis.</summary>
	Column = Direction.Column,
	/// <summary>Like <see cref="Column"/>, but in reverse order.</summary>
	ColumnReverse = Direction.ColumnReverse,
	/// <summary>Children are stacked horizontally; the main axis is the x-axis.</summary>
	Row = Direction.Row,
	/// <summary>Like <see cref="Row"/>, but in reverse order.</summary>
	RowReverse = Direction.RowReverse,
}

/// <summary>
/// How a <see cref="FlexPanel"/> distributes free space between and around children along the main axis.
/// </summary>
public enum FlexJustify
{
	/// <summary>Children are packed around the center.</summary>
	Center = Justify.Center,
	/// <summary>Children are packed at the start of the main axis.</summary>
	Start = Justify.Start,
	/// <summary>Children are packed at the end of the main axis.</summary>
	End = Justify.End,
	/// <summary>Children are distributed evenly; the first child at the start, the last child at the end.</summary>
	SpaceBetween = Justify.SpaceBetween,
	/// <summary>Children are distributed evenly; the first and last children get a half-size space.</summary>
	SpaceAround = Justify.SpaceAround,
	/// <summary>Children are distributed evenly, all with equal space around them.</summary>
	SpaceEvenly = Justify.SpaceEvenly,
}

/// <summary>
/// How a <see cref="FlexPanel"/> aligns children along the cross axis of their line.
/// </summary>
public enum FlexAlignItems
{
	/// <summary>Children without an explicit cross-axis size are stretched to the line.</summary>
	Stretch = AlignItems.Stretch,
	/// <summary>Children are centered on the cross axis.</summary>
	Center = AlignItems.Center,
	/// <summary>Children are packed at the cross-axis start.</summary>
	Start = AlignItems.Start,
	/// <summary>Children are packed at the cross-axis end.</summary>
	End = AlignItems.End,
}

/// <summary>
/// A per-child override of the parent's <see cref="FlexPanel.AlignItems"/> value, applied with the
/// <see cref="FlexPanel.AlignSelfProperty"/> attached property.
/// </summary>
public enum FlexAlignSelf
{
	/// <summary>The parent's <see cref="FlexPanel.AlignItems"/> value is used.</summary>
	Auto = AlignSelf.Auto,
	/// <summary>The child is stretched to the line when it has no explicit cross-axis size.</summary>
	Stretch = AlignSelf.Stretch,
	/// <summary>The child is centered on the cross axis.</summary>
	Center = AlignSelf.Center,
	/// <summary>The child is packed at the cross-axis start.</summary>
	Start = AlignSelf.Start,
	/// <summary>The child is packed at the cross-axis end.</summary>
	End = AlignSelf.End,
}

/// <summary>
/// How a wrapping <see cref="FlexPanel"/> distributes space between and around its lines. Ignored
/// unless <see cref="FlexPanel.Wrap"/> is <see cref="FlexWrap.Wrap"/> or <see cref="FlexWrap.Reverse"/>.
/// </summary>
public enum FlexAlignContent
{
	/// <summary>Lines are stretched to fill the cross axis.</summary>
	Stretch = AlignContent.Stretch,
	/// <summary>Lines are packed around the center.</summary>
	Center = AlignContent.Center,
	/// <summary>Lines are packed at the cross-axis start.</summary>
	Start = AlignContent.Start,
	/// <summary>Lines are packed at the cross-axis end.</summary>
	End = AlignContent.End,
	/// <summary>Lines are distributed evenly; the first line at the start, the last line at the end.</summary>
	SpaceBetween = AlignContent.SpaceBetween,
	/// <summary>Lines are distributed evenly; the first and last lines get a half-size space.</summary>
	SpaceAround = AlignContent.SpaceAround,
	/// <summary>Lines are distributed evenly, all with equal space around them.</summary>
	SpaceEvenly = AlignContent.SpaceEvenly,
}

/// <summary>
/// Whether a <see cref="FlexPanel"/> arranges children on a single line or lets them wrap onto
/// multiple lines.
/// </summary>
public enum FlexWrap
{
	/// <summary>Children are arranged on a single line.</summary>
	NoWrap = Internal.Wrap.NoWrap,
	/// <summary>Children wrap onto multiple lines when needed.</summary>
	Wrap = Internal.Wrap.Wrap,
	/// <summary>Like <see cref="Wrap"/>, but lines stack in reverse order.</summary>
	Reverse = Internal.Wrap.WrapReverse,
}

/// <summary>
/// Whether children are positioned by the flexbox rules of the layout engine or by fixed
/// coordinates. Mirrors the .NET MAUI FlexLayout API surface.
/// </summary>
public enum FlexPosition
{
	/// <summary>Children are positioned by the flexbox rules of the layout engine.</summary>
	Relative = Position.Relative,
	/// <summary>Children are positioned by fixed coordinate values.</summary>
	Absolute = Position.Absolute,
}
