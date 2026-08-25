================================================================================
AGENT-README: CodeBrix.Platform.FlexPanel
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.FlexPanel.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
A CSS flexbox-style XAML layout panel for CodeBrix.Platform applications:
FlexPanel : Panel. Children are arranged in optionally wrapping rows or
columns with the familiar flexbox model - Direction (Row default, both axes
reversible), JustifyContent (Start / Center / End / SpaceBetween / SpaceAround
/ SpaceEvenly), AlignItems (+ per-child AlignSelf), Wrap (NoWrap / Wrap /
Reverse), and AlignContent for the wrapped lines - and per-child attached
properties Grow, Shrink, Basis, Order and AlignSelf. Target: .NET 10 or later.

Provenance: the layout engine is a managed port of the .NET MAUI FlexLayout
engine (MIT; see THIRD-PARTY-NOTICES.txt in the package), so layout semantics
match MAUI/CSS for the same tree of sizes. The public types are this package's
own (namespace CodeBrix.Platform.UI.FlexPanel; enum names carry a Flex prefix:
FlexDirection, FlexJustify, ...) - do not use Microsoft.Maui.* namespaces or
type names. Pure managed layout: no native dependency, live on all six heads
(Windows Win32-Skia, Windows WPF-Skia, Linux X11, Linux Wayland, Linux
FrameBuffer, macOS).

CONSUMPTION PATTERN: follows the Lottie/AudioPlayer pattern - application code
references the add-in's own public types directly:

    xmlns:flex="using:CodeBrix.Platform.UI.FlexPanel"
    <flex:FlexPanel Direction="Row" Wrap="Wrap" JustifyContent="Center">
      <Border flex:FlexPanel.Grow="1" ... />
      <Border flex:FlexPanel.Basis="25%" ... />
      <Border flex:FlexPanel.Order="-1" ... />
    </flex:FlexPanel>

INSTALLATION
============
    dotnet add package CodeBrix.Platform.FlexPanel.ApacheLicenseForever

Reference it from the project that carries your framework package references
(the application's .Core project in the standard layout); the XAML in the
shared .UI project then resolves the flex: namespace.

Dependencies (flow in automatically):
  CodeBrix.Platform.ApacheLicenseForever    the core framework (Panel,
                                            DependencyProperty, Measure/Arrange)

License: Apache-2.0. Requirements: none - the panel uses only public framework
API and no native code.

KEY NAMESPACES / USINGS
=======================
    xmlns:flex="using:CodeBrix.Platform.UI.FlexPanel"       (XAML)
    using CodeBrix.Platform.UI.FlexPanel;                   (C#)

Nine public types, all in that namespace:

    FlexPanel          the panel
    FlexDirection      Column, ColumnReverse, Row, RowReverse
    FlexJustify        Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly
    FlexAlignItems     Stretch, Center, Start, End
    FlexAlignSelf      Auto, Stretch, Center, Start, End
    FlexAlignContent   Stretch, Center, Start, End, SpaceBetween, SpaceAround,
                       SpaceEvenly
    FlexWrap           NoWrap, Wrap, Reverse
    FlexBasis          struct: Auto | absolute length | fraction of the main axis
    FlexPosition       Relative, Absolute (API parity only - see below)

CORE API REFERENCE
==================

FlexPanel  ([Bindable] partial class FlexPanel : Panel)
-------------------------------------------------------
Panel-level dependency properties (each has a matching static
<Name>Property field; changing any of them invalidates the panel's measure):

    FlexDirection    Direction        default Row
        The main axis and its direction. Row = children stacked horizontally,
        Column = vertically; the *Reverse values run the same axis backwards.
    FlexJustify      JustifyContent   default Start
        How free MAIN-axis space is distributed between and around children:
        Start / Center / End pack them; SpaceBetween puts the first child at
        the start and the last at the end; SpaceAround gives the first and last
        a half-size space; SpaceEvenly gives equal space everywhere.
    FlexAlignItems   AlignItems       default Stretch
        How children align on the CROSS axis of their line: Stretch (children
        without an explicit cross-axis size fill the line), Center, Start, End.
        Overridable per child with FlexPanel.AlignSelf.
    FlexAlignContent AlignContent     default Stretch
        How the LINES are distributed on the cross axis when the panel wraps:
        Stretch, Center, Start, End, SpaceBetween, SpaceAround, SpaceEvenly.
        Ignored while Wrap is NoWrap.
    FlexWrap         Wrap             default NoWrap
        NoWrap keeps every child on one line (they shrink to fit, per Shrink);
        Wrap starts new lines as needed; Reverse wraps with the lines stacked
        in reverse order.
    FlexPosition     Position         default Relative
        Present for API parity with the MAUI FlexLayout the panel is ported
        from. The engine always positions children by the flexbox rules, so
        Absolute has NO EFFECT on child arrangement.
    Thickness        Padding          default 0
        Space between the panel's edges and the area children are laid out in.

Per-child attached properties (XAML flex:FlexPanel.Xxx="..."; C# static
Get/Set pairs; changing one re-runs the OWNING panel's layout):

    int          Order      default 0
        Children are arranged by ascending Order; insertion order breaks ties.
        Negative values are fine (Order="-1" moves a child first).
    float        Grow       default 0     (must not be negative)
        The child's share of FREE main-axis space: two children with Grow=1
        split it equally; Grow=2 takes twice as much as Grow=1; 0 never grows.
    float        Shrink     default 1     (must not be negative)
        The child's share of OVERFLOW reclaimed when children exceed the main
        axis on one line. 0 means "never shrink below the basis".
    FlexBasis    Basis      default Auto
        The child's initial main-axis size before growing/shrinking:
        "Auto" (its measured size), an absolute length like "150", or a
        percentage of the PANEL's main axis like "25%".
    FlexAlignSelf AlignSelf default Auto
        Overrides the panel's AlignItems for this child: Auto (use the panel's
        value), Stretch, Center, Start, End.

    static int           GetOrder(DependencyObject element)
    static void          SetOrder(DependencyObject element, int value)
    static float         GetGrow(DependencyObject element)
    static void          SetGrow(DependencyObject element, float value)
    static float         GetShrink(DependencyObject element)
    static void          SetShrink(DependencyObject element, float value)
    static FlexAlignSelf GetAlignSelf(DependencyObject element)
    static void          SetAlignSelf(DependencyObject element, FlexAlignSelf value)
    static FlexBasis     GetBasis(DependencyObject element)
    static void          SetBasis(DependencyObject element, FlexBasis value)

    Attached DependencyProperty fields: OrderProperty, GrowProperty,
    ShrinkProperty, AlignSelfProperty, BasisProperty.

Setting Grow or Shrink to a negative value throws ArgumentException.

What the panel reads from each child: its Width/Height when explicitly set
(FrameworkElement), its Margin, its Visibility, and its DesiredSize from the
measure pass. Child Margin participates exactly as CSS margins do - it
occupies main-axis space between siblings and offsets cross-axis alignment.
A Collapsed child takes no space and no line position at all.

FlexBasis  (struct, IEquatable<FlexBasis>)
------------------------------------------
    static readonly FlexBasis Auto                 // the default (default(FlexBasis) == Auto)
    FlexBasis(float length, bool isRelative = false)
    float Length     { get; }    // pixels, or a fraction in [0, 1] when IsRelative; 0 when Auto
    bool  IsAuto     { get; }
    bool  IsRelative { get; }
    static implicit operator FlexBasis(float length)          // absolute
    static FlexBasis CreateFromString(string value)           // the XAML converter
    bool Equals(FlexBasis other);  ==  and  !=;  ToString()

Three kinds of basis:
    FlexBasis.Auto                          the child's measured main-axis size
    new FlexBasis(150)  or  (FlexBasis)150f an absolute length in device-
                                            independent pixels
    new FlexBasis(0.25f, isRelative: true)  a FRACTION of the panel's main axis
                                            (0.25 = 25%)

Constructor rules: length must not be negative; a relative length must be in
[0, 1] - both violations throw ArgumentException. Note new FlexBasis(0) is an
absolute zero basis, distinct from Auto (a Grow child with Basis 0 shares free
space from nothing, the classic "equal columns" recipe).

STRING FORMS (XAML attribute text, and CreateFromString):
    "Auto"    case-insensitive
    "150"     invariant-culture number - absolute pixels ("150.5" works)
    "25%"     number followed by % - a percentage of the panel's main axis
              (converted to the fraction 0.25; more than 100% throws)
Anything else throws FormatException. The XAML source generator and the
runtime XAML reader both honor this converter, so FlexPanel.Basis="30%" is
valid in markup and in a Style setter. ToString() round-trips: "Auto", "150",
"30%".

FlexPosition and the Position property
--------------------------------------
    enum FlexPosition { Relative, Absolute }

Mirrors the MAUI FlexLayout surface so markup ports without edits. The panel
property FlexPanel.Position accepts both values, but the engine always lays
children out by the flexbox rules: there are NO per-child Left/Top/Right/
Bottom attached properties, and Absolute changes nothing. Position an overlay
with a Canvas or a Grid instead.

LAYOUT SEMANTICS WORTH KNOWING
------------------------------
  - Main axis vs cross axis: Direction picks the main axis (Row -> x, Column
    -> y). JustifyContent, Grow, Shrink, Basis and Order act on the main axis;
    AlignItems, AlignSelf and AlignContent act on the cross axis.
  - A child with an explicit Width/Height cannot be stretched or grown past
    it - the framework clamps the child inside its (grown) layout slot, exactly
    as a Grid does. Leave the main-axis dimension unset on children that should
    Grow, and the cross-axis dimension unset on children that should stretch.
  - Unconstrained dimensions: when the panel is measured with infinite width or
    height (inside a StackPanel along its orientation, or a ScrollViewer), it
    measures to its natural size for that pass, treating every child as
    Shrink=0 and AlignSelf=Start. Shrink and cross-axis Stretch therefore only
    do anything when the panel has a bounded size on that axis - give it one
    (a Grid cell, an explicit Width/Height) when you rely on them.
  - The item tree is rebuilt on every layout pass from the current property
    values; there is no state to reset when children are added, removed or
    reordered - just change Order/Grow/... and the panel re-lays-out.

COMPLETE EXAMPLES
=================

1. A wrapping tag cloud, centered
---------------------------------
    <flex:FlexPanel Direction="Row" Wrap="Wrap" JustifyContent="Center"
                    AlignContent="Start" Padding="8">
      <Border Margin="4" Padding="8,4" CornerRadius="12" Background="LightGray">
        <TextBlock Text="alpha" />
      </Border>
      <Border Margin="4" Padding="8,4" CornerRadius="12" Background="LightGray">
        <TextBlock Text="beta" />
      </Border>
      <!-- ...as many as you like; they wrap onto new lines -->
    </flex:FlexPanel>

2. A navigation bar: logo left, actions right, a growing spacer between
----------------------------------------------------------------------
    <flex:FlexPanel Direction="Row" AlignItems="Center" Height="48" Padding="8,0">
      <Image Source="ms-appx:///Assets/logo.png" Height="32" />
      <Border flex:FlexPanel.Grow="1" />                 <!-- takes all free space -->
      <Button Content="Open"   flex:FlexPanel.Shrink="0" />
      <Button Content="Save"   flex:FlexPanel.Shrink="0" />
      <Button Content="Help"   flex:FlexPanel.Order="1"
              flex:FlexPanel.AlignSelf="End" />         <!-- last, bottom-aligned -->
    </flex:FlexPanel>

3. Equal columns that fill the width, one of them fixed
-------------------------------------------------------
    <flex:FlexPanel Direction="Row" AlignItems="Stretch">
      <Border flex:FlexPanel.Basis="0" flex:FlexPanel.Grow="1" Background="#20FF0000" />
      <Border flex:FlexPanel.Basis="0" flex:FlexPanel.Grow="1" Background="#2000FF00" />
      <Border flex:FlexPanel.Basis="200" flex:FlexPanel.Shrink="0" Background="#200000FF" />
      <Border flex:FlexPanel.Basis="25%" Background="#20FFFF00" />
    </flex:FlexPanel>

4. The same thing from code-behind
----------------------------------
    using CodeBrix.Platform.UI.FlexPanel;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;

    var panel = new FlexPanel
    {
        Direction = FlexDirection.Row,
        Wrap = FlexWrap.Wrap,
        JustifyContent = FlexJustify.SpaceBetween,
        AlignItems = FlexAlignItems.Center,
        AlignContent = FlexAlignContent.Start,
        Padding = new Thickness(8),
    };

    var logo   = new Border { Height = 32 };
    var spacer = new Border();
    var menu   = new Border { Height = 32 };

    FlexPanel.SetOrder(logo, -1);                                     // first
    FlexPanel.SetBasis(logo, 120f);                                   // implicit float -> absolute
    FlexPanel.SetGrow(spacer, 1f);                                    // absorbs free space
    FlexPanel.SetShrink(menu, 0f);                                    // never squeezed
    FlexPanel.SetBasis(menu, new FlexBasis(0.25f, isRelative: true)); // "25%"
    FlexPanel.SetAlignSelf(menu, FlexAlignSelf.End);

    panel.Children.Add(logo);
    panel.Children.Add(spacer);
    panel.Children.Add(menu);

    // Later - any change re-lays-out the owning panel:
    panel.Direction = FlexDirection.Column;
    FlexPanel.SetBasis(menu, FlexBasis.CreateFromString("40%"));
    FlexPanel.SetBasis(logo, FlexBasis.Auto);

5. Reading a child's settings
-----------------------------
    var basis = FlexPanel.GetBasis(menu);   // using System.Diagnostics; for Debug
    if (basis.IsRelative)  Debug.WriteLine($"{basis.Length * 100}% of the main axis");
    else if (basis.IsAuto) Debug.WriteLine("measured size");
    else                   Debug.WriteLine($"{basis.Length}px");   // basis.ToString() gives "150"

MINIMUM VIABLE PROJECT
======================
In the application's .Core project:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.FlexPanel.ApacheLicenseForever" />
      </ItemGroup>
    </Project>

and a page in the shared .UI project:

    <Page x:Class="MyApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:flex="using:CodeBrix.Platform.UI.FlexPanel">
      <flex:FlexPanel Direction="Row" Wrap="Wrap" JustifyContent="SpaceEvenly" Padding="16">
        <Border Width="120" Height="80" Margin="8" Background="CornflowerBlue" />
        <Border Width="120" Height="80" Margin="8" Background="Coral" flex:FlexPanel.Order="-1" />
        <Border Height="80" Margin="8" Background="SeaGreen" flex:FlexPanel.Grow="1" />
      </flex:FlexPanel>
    </Page>

No code-behind is needed.

PERFORMANCE TIPS
================
  - The flex item tree is rebuilt on every measure and arrange pass, and each
    child is measured once per measure pass. That is cheap for the dozens of
    children a toolbar, tag cloud or card row holds; for thousands of items
    use a virtualizing ItemsControl and put FlexPanel inside each item, not
    around them all.
  - Every attached-property change on a child invalidates the owning panel's
    measure. When configuring many children in code, set their Order/Grow/
    Basis BEFORE adding them to Children, so the panel lays out once.
  - Give the panel a bounded size on the axis where you rely on Shrink or
    Stretch (see LAYOUT SEMANTICS); an unconstrained panel takes the
    natural-size path on every pass.
  - Prefer Basis="0" + Grow for equal columns over percentages that must be
    kept in sum; the engine distributes free space in one pass either way, but
    the Grow form needs no arithmetic when a column is added.

COMMON PITFALLS TO AVOID
========================
  - A child with an explicit Width/Height cannot be stretched or grown past
    it - the framework clamps the child inside its (grown) layout slot, exactly
    as a Grid does. Leave the main-axis dimension unset on children that
    should Grow, and the cross-axis dimension unset on children that should
    stretch.
  - Basis percentages are of the PANEL's main axis, not of the remaining
    space, and the fraction must be within [0, 1]: "150%" throws
    ArgumentException when the value is created.
  - AlignContent only matters when Wrap is Wrap or Reverse; on a single line
    it is silently ignored - use AlignItems for that.
  - Position="Absolute" is accepted and does nothing (API parity only). There
    are no Left/Top/Right/Bottom attached properties.
  - Negative Grow or Shrink throws ArgumentException from the property
    setter - including when a binding delivers the value.
  - Inside a vertical StackPanel or a ScrollViewer the panel's cross axis (for
    a Row panel: height) is unconstrained; AlignItems="Stretch" then does
    nothing visible because the line is exactly as tall as its tallest child.
  - Order sorts within the whole panel, not within a line; a wrapped line is
    filled in Order sequence.
  - Margins count: two adjacent children with Margin="4" are 8 px apart, and a
    margin on the cross axis moves the child inside its line. There is no gap
    property - Margin is how spacing is expressed (see below).
  - The enum names carry a Flex prefix (FlexDirection.Row, FlexWrap.Wrap,
    FlexJustify.SpaceBetween); CSS keyword strings ("space-between") are not
    accepted anywhere. In XAML the plain member names are used ("Row", "Wrap",
    "SpaceBetween").

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No gap / row-gap / column-gap property: spacing between children is
    expressed with each child's Margin (which the engine honors as CSS margins).
  - No min-content / max-content / fit-content sizing keywords: a basis is
    Auto (the child's measured size), an absolute length, or a percentage.
  - No per-child absolute positioning: the engine's Left/Top/Right/Bottom
    are not exposed, and the panel-level Position property has no effect.
  - No right-to-left flow: the panel does not consult FlowDirection. Row
    always runs left to right; use RowReverse for the mirrored order.
  - No baseline alignment: AlignItems/AlignSelf offer Stretch, Center, Start
    and End only.
  - Child MinWidth/MaxWidth/MinHeight/MaxHeight are not inputs to the flex
    computation (only Width/Height, Margin, Visibility and DesiredSize are);
    they still apply when the framework arranges the child inside the slot the
    engine gave it, which can leave the slot partly empty.
  - No nested-layout awareness beyond ordinary measure/arrange: a FlexPanel
    inside a FlexPanel is just a child with a DesiredSize (nesting works; the
    inner panel does not share lines with the outer one).
  - No animation or transition of layout changes.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/FlexPanelDemo
      The reference application for this package (all six heads): an
      interactive playground - every panel property (Direction,
      JustifyContent, AlignItems, AlignContent, Wrap, Padding) switchable
      live, with children carrying FlexPanel.Order, Grow, AlignSelf and
      Basis="25%", a nested FlexPanel as one child, plus fixed Grow/Basis and
      navigation-bar examples. Start with FlexPanelDemo.UI/Views/MainPage.xaml.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.FlexPanel.Tests
      The test suite. FlexBasisTests.cs exercises the public FlexBasis struct
      (Auto vs zero, absolute, relative, string parsing). The other files
      (DirectionTests, JustifyContentTests, AlignItemsTests, AlignSelfTests,
      AlignContentTests, WrapTests, GrowTests, ShrinkTests, BasisTests,
      OrderTests, MarginTests, PaddingTests, ChildrenTests, SelfSizingTests,
      DefaultValuesTests, PositionTests) are the ported upstream engine suite
      and drive the INTERNAL engine directly with host-free item trees - read
      them for the exact frame every combination of settings produces; they
      are not consumer API.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.FlexPanel
      The package source: FlexPanel.cs, Models/FlexBasis.cs, Models/FlexEnums.cs
      - fully XML-documented.

QUICK REFERENCE CARD
====================
namespace CodeBrix.Platform.UI.FlexPanel
xmlns:flex="using:CodeBrix.Platform.UI.FlexPanel"

[Bindable] partial class FlexPanel : Panel
    FlexDirection    Direction        Row | RowReverse | Column | ColumnReverse   (Row)
    FlexJustify      JustifyContent   Start | Center | End | SpaceBetween |
                                      SpaceAround | SpaceEvenly                   (Start)
    FlexAlignItems   AlignItems       Stretch | Center | Start | End              (Stretch)
    FlexAlignContent AlignContent     Stretch | Center | Start | End |
                                      SpaceBetween | SpaceAround | SpaceEvenly    (Stretch)
    FlexWrap         Wrap             NoWrap | Wrap | Reverse                     (NoWrap)
    FlexPosition     Position         Relative | Absolute  (no effect)            (Relative)
    Thickness        Padding                                                      (0)

    attached (flex:FlexPanel.X="..." / FlexPanel.GetX(el) / FlexPanel.SetX(el, v)):
    int           Order       (0)        ascending; ties by insertion order
    float         Grow        (0)        share of free main-axis space; >= 0
    float         Shrink      (1)        share of overflow reclaimed;    >= 0
    FlexBasis     Basis       (Auto)     "Auto" | "150" | "25%"
    FlexAlignSelf AlignSelf   (Auto)     Auto | Stretch | Center | Start | End

struct FlexBasis : IEquatable<FlexBasis>
    static readonly FlexBasis Auto
    FlexBasis(float length, bool isRelative = false)     // length >= 0; relative in [0,1]
    float Length;  bool IsAuto;  bool IsRelative
    static implicit operator FlexBasis(float length)     // absolute
    static FlexBasis CreateFromString(string value)      // "Auto" | number | "n%"
    ToString() -> "Auto" | "150" | "30%"

enums: FlexDirection, FlexJustify, FlexAlignItems, FlexAlignSelf,
       FlexAlignContent, FlexWrap, FlexPosition (values as listed above)
