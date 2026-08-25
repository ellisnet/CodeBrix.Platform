================================================================================
AGENT-README: CodeBrix.Platform.TextLayout
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.TextLayout.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.TextLayout is Pango-class text layout with NO XAML and NO
application host required. It shapes text with HarfBuzz, resolves
bidirectional runs (UAX #9), itemises across fallback fonts, and then
reports the geometry an editor or a renderer needs:
  - the measured size and per-line metrics (top, height, baseline)
  - caret rectangles and cluster rectangles for any text index
  - cluster-correct hit-testing, strict (-1 outside) or nearest
  - selection rectangles for a range (a list, because one logical range can
    be visually discontiguous across lines and bidi boundaries)
  - per-glyph or combined outline SKPaths, for stroked / outlined text -
    which a filled text blob cannot give you
It draws into ANY SKCanvas: an offscreen surface, a document layer, a
bitmap, a window.

This is a FACADE over the very same engine that lays out every TextBlock in
a CodeBrix.Platform application - the same shaping, the same itemisation
and font fallback, the same caret and cluster maths. There is deliberately
only one text implementation in the family: a bug fixed here is fixed for
TextBlock too, and vice versa.

Nothing in the public surface is a XAML type. The whole API is one static
entry point (TextLayoutEngine), one result type (TextLayoutResult), two
small input types (TextRunDescriptor, TextLayoutOptions), three geometry
records (TextLineInfo, TextLineMetrics, GlyphOutline) and five enums. It is
equally usable from a document model, a game, an image pipeline, a unit
test, or a XAML control's render pass (the family's TerminalView and
AdvancedTextEdit controls consume it exactly that way).

Target: .NET 10 or later.


INSTALLATION
============
Package id:   CodeBrix.Platform.TextLayout.ApacheLicenseForever

    dotnet add package CodeBrix.Platform.TextLayout.ApacheLicenseForever

NuGet dependencies (all flow in automatically):
  - CodeBrix.Platform.ApacheLicenseForever                 the framework (the
                                                           engine lives here)
  - SkiaSharp, SkiaSharp.HarfBuzz, HarfBuzzSharp           geometry types and
                                                           shaping
  - CodeBrix.Platform.Unicode.ApacheLicenseForever         ICU natives for
  - CodeBrix.Platform.UnicodeMacOs.ApacheLicenseForever    Windows and macOS

License: Apache-2.0.

Requirements:
  - ICU. Shaping, bidi and line breaking call native ICU. Windows and macOS
    get it from the two Unicode packages above. Linux loads the SYSTEM ICU
    from the dynamic-linker search path (nothing ships for Linux), so the
    distribution's libicu package must be installed - the failure mode
    otherwise is "Failed to load libicuuc." on the very first Layout call.
  - Native SkiaSharp and HarfBuzz. Inside a CodeBrix.Platform application
    the head's runtime package supplies them. A project WITHOUT an
    application head (a console tool, a test project) must reference the
    native-asset packages for its OS itself - SkiaSharp.NativeAssets.Linux
    plus HarfBuzzSharp.NativeAssets.Linux, or the .Win32 / .macOS pairs -
    exactly as this package's own test project does.
  - Inside a CodeBrix.Platform application: nothing further. Reference the
    package from the shared UI project (or any library project) and use it.

WHICH PROJECT: any. The package is a plain code API; it does not need to
sit in the UI project.


KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.UI.TextLayout;   // everything in this package
    using SkiaSharp;                         // SKCanvas, SKPaint, SKPoint,
                                             // SKRect, SKSize, SKPath, SKFont,
                                             // SKColor

There is exactly one public namespace. Nothing under it is a XAML type, and
no Microsoft.UI.Xaml using is needed to consume it.


CORE API REFERENCE
==================
Public surface, complete (12 types):

    static class     TextLayoutEngine
    sealed class     TextLayoutResult : IDisposable
    sealed class     TextLayoutOptions
    sealed class     TextRunDescriptor
    readonly record struct TextLineInfo
    readonly record struct TextLineMetrics
    sealed class     GlyphOutline : IDisposable
    enum             TextAlign, TextDirection, TextFontWeight, TextFontStyle,
                     TextFontStretch

TextLayoutEngine
----------------
    public static TextLayoutResult Layout(
        IReadOnlyList<TextRunDescriptor> runs,
        TextLayoutOptions? options = null)
        Lays out a sequence of styled runs. The runs are concatenated in
        order to form the layout's text. Throws ArgumentNullException when
        runs is null or contains a null run; ArgumentException when runs is
        EMPTY - to lay out empty text, pass one run whose Text is "".

    public static TextLayoutResult Layout(
        string text,
        string? fontFamily = null,
        float fontSize = 12f,
        TextLayoutOptions? options = null)
        Convenience for a single uniformly styled run: equivalent to
        Layout([new TextRunDescriptor(text, fontFamily, fontSize)], options).
        Throws ArgumentNullException when text is null.

Engine behaviour worth knowing:
  - The first call initialises the engine (ICU); inside an application that
    has already happened and costs nothing.
  - Base direction: options.BaseDirection, or - for Auto - detected from
    the concatenated text per UAX #9. A run whose Direction is Auto inherits
    the layout's base direction rather than being resolved per run.
  - Wrapping is ON only when options.MaxWidth has a value; otherwise lines
    exist only where the text breaks them ("\n" and friends).
  - With no MaxWidth there is no box to align within, so Alignment is
    ignored and every line starts at x = 0.
  - Line stacking uses the tallest run on each line (mixed sizes take the
    taller line height); LineHeight, when set, overrides that.
  - Font resolution goes through the engine's font cache. A family that
    resolves to a font still loading asynchronously (an application font
    URI, for example) is laid out IMMEDIATELY with a fallback face; layout
    never blocks on a font load. Re-layout later if exact metrics matter.

TextRunDescriptor
-----------------
One styled run. Runs are concatenated to form the layout text; text indices
address that concatenation, never an individual run. A run is not a line -
line breaks come from the text.

    public TextRunDescriptor(
        string text,                                       // may be "", not null
        string? fontFamily = null,                         // null = platform default
        float fontSize = 12f,                              // em size; > 0
        TextFontWeight weight = TextFontWeight.Normal,
        TextFontStyle style = TextFontStyle.Normal,
        TextFontStretch stretch = TextFontStretch.Normal,
        TextDirection direction = TextDirection.Auto)
        Throws ArgumentNullException (text null) or ArgumentOutOfRange-
        Exception (fontSize not greater than zero).

    public static TextRunDescriptor Create(
        string text, string? fontFamily = null, float fontSize = 12f,
        bool bold = false, bool italic = false)
        The bold / italic shorthand: bold -> TextFontWeight.Bold, italic ->
        TextFontStyle.Italic, otherwise Normal.

    public string Text { get; }
    public string? FontFamily { get; }
    public float FontSize { get; }
    public TextFontWeight Weight { get; }
    public TextFontStyle Style { get; }
    public TextFontStretch Stretch { get; }
    public TextDirection Direction { get; }
    public SKColor? Color { get; init; }
        The color to paint THIS run's glyphs with when the layout is drawn,
        or null to use the color of the SKPaint passed to Draw. Set with an
        object initializer. Color affects drawing only - never measurement,
        shaping or hit-testing - so mixing colored and uncolored runs is
        free.

    fontFamily is a family name such as "sans-serif", "monospace" or
    "Open Sans", or (inside a CodeBrix.Platform application) an application
    font URI such as
    "ms-appx:///CodeBrix.Platform.Fonts.RobotoMono/Fonts/RobotoMono.ttf".
    Resolution of a bare name is machine-dependent: the same name can land
    on different faces on different hosts, and a host that does not know
    the name substitutes its default face (Windows does so without
    honouring the requested weight).

TextLayoutOptions
-----------------
    public sealed class TextLayoutOptions
    {
        public float? MaxWidth { get; set; }            // null = no wrapping
        public int MaxLines { get; set; }               // 0 = unlimited
        public TextAlign Alignment { get; set; }        // Left
        public float LineHeight { get; set; }           // 0 = from font metrics
        public TextDirection BaseDirection { get; set; }// Auto
    }

    MaxWidth        The width to lay out within. Null means no wrapping and
                    no alignment: a consumer that models its own line breaks
                    (an editor holding a list of lines) wants null. Setting
                    it turns wrapping on and gives Alignment a box.
    MaxLines        The maximum number of lines to keep; 0 (or a negative
                    value, which is clamped to 0) means unlimited.
    Alignment       Horizontal alignment within MaxWidth; ignored when
                    MaxWidth is null.
    LineHeight      An explicit line height; 0 takes it from the font
                    metrics of the tallest run on each line.
    BaseDirection   The base writing direction of the layout as a whole.
                    Auto resolves it from the text content per UAX #9.

TextLayoutResult
----------------
Every index parameter and return value is a TEXT index into Text - never a
glyph index. That distinction matters wherever shaping is not one-to-one
(ligatures, combining marks, anything that forms a cluster).

    public string Text { get; }                  // every run's text, concatenated
    public SKSize Size { get; }                  // measured size
    public int LineCount { get; }
    public float LineHeight { get; }             // height of the first line, or
                                                 // the default font's line height
                                                 // when there is no text
    public bool IsBaseDirectionRightToLeft { get; }

    public SKRect GetCaretRect(int textIndex, float caretThickness = 1f)
        The caret rectangle for a text index from 0 to Text.Length
        INCLUSIVE, caretThickness wide, in layout coordinates. Throws
        ArgumentOutOfRangeException outside that range. Advances leftwards
        in right-to-left text.

    public SKRect GetRectForIndex(int textIndex)
        The rectangle covering the CLUSTER at a text index (0..Length
        inclusive). A combining mark shares its base character's cluster.

    public int GetIndexAt(SKPoint point)
        The text index at a point in layout coordinates, or -1 when the
        point falls outside the text.

    public int GetNearestIndexAt(SKPoint point)
        The nearest text index, clamped into the text instead of -1. This
        is what a drag-selection wants: a point above, below or beside the
        text still resolves to the closest caret position.

    public TextLineInfo GetLineAt(int textIndex)
        Which line a text index (0..Length inclusive) falls on.

    public TextLineMetrics GetLineMetrics(int lineIndex)
        The geometry of a line by zero-based index (0..LineCount - 1;
        ArgumentOutOfRangeException otherwise): top, height, baseline, and
        the slice of Text it covers. Combine with GetLineAt to walk a layout
        line by line.

    public IReadOnlyList<SKRect> GetSelectionRects(int start, int length)
        One rectangle per contiguous visual segment covering the range;
        empty for an empty range. A range that runs past the end is
        clamped. Never a single bounding box: a range spans lines, and
        within a line a bidi boundary splits it.

    public SKPath GetOutlinePath()
        One path combining every glyph outline, already positioned in
        layout coordinates. THE CALLER OWNS IT - dispose it. Fill it, stroke
        it, or both (outlined text). Empty for empty text.

    public IReadOnlyList<GlyphOutline> GetGlyphOutlines()
        Every positioned glyph, in visual order, each with its own outline.
        THE CALLER OWNS EACH GlyphOutline - dispose them. For plain outlined
        text GetOutlinePath is cheaper; use this for per-glyph effects,
        animation, or hit-testing an outline.

    public void Draw(SKCanvas canvas, SKPoint origin, SKPaint paint)
        Paints filled glyphs via text blobs (the fast path) with the
        layout's top-left corner at origin. paint's color is used for runs
        that set no Color of their own. Throws ArgumentNullException for a
        null canvas or paint. For stroked / outlined text use
        GetOutlinePath and draw the path yourself.

    public void Draw(SKCanvas canvas, SKPaint paint)
        Draw at the canvas origin (SKPoint.Empty).

    public void Dispose()
        Currently a no-op: the layout holds no unmanaged resources of its
        own, and the fonts it references belong to the engine's shared
        cache. IDisposable is implemented so callers can adopt `using` now
        and stay correct if that ever changes. Paths and outlines handed
        out by GetOutlinePath / GetGlyphOutlines are NOT covered by it.

TextLineInfo
------------
    public readonly record struct TextLineInfo(
        int Start,          // index of the line's first character in Text
        int Length,         // characters on the line, INCLUDING a trailing line break
        int LineIndex,      // zero-based line number
        bool IsFirstLine,
        bool IsLastLine);

TextLineMetrics
---------------
    public readonly record struct TextLineMetrics(
        int Start,            // index of the line's first character in Text
        int Length,           // characters on the line, incl. trailing line break
        float Top,            // vertical offset of the line's top edge
        float Height,         // line height
        float BaselineOffset);// distance from Top down to the baseline
    Underlines sit relative to Top + BaselineOffset; selection or highlight
    backgrounds fill Top to Top + Height.

GlyphOutline
------------
    public sealed class GlyphOutline : IDisposable
    {
        public ushort GlyphId { get; }   // glyph id within Font
        public SKPath? Path { get; }     // outline at the ORIGIN, not at Origin;
                                         // translate by Origin to place it.
                                         // Empty for glyphs with nothing to draw
                                         // (a space, a bitmap or color emoji)
        public SKPoint Origin { get; }   // baseline-left, layout coordinates
        public float Advance { get; }    // horizontal advance
        public SKFont Font { get; }      // engine-owned - do NOT dispose. Can
                                         // differ glyph to glyph after fallback
        public void Dispose();           // disposes Path
    }

Enums
-----
    TextDirection    Auto = 0, LeftToRight = 1, RightToLeft = 2
    TextAlign        Left = 0, Center = 1, Right = 2
                     (no Justify; alignment needs a MaxWidth)
    TextFontWeight   Thin = 100, ExtraLight = 200, Light = 300, Normal = 400,
                     Medium = 500, SemiBold = 600, Bold = 700,
                     ExtraBold = 800, Black = 900   (the OpenType scale)
    TextFontStyle    Normal = 0, Oblique = 1, Italic = 2
    TextFontStretch  Undefined = 0 (treated as Normal), UltraCondensed = 1,
                     ExtraCondensed = 2, Condensed = 3, SemiCondensed = 4,
                     Normal = 5, SemiExpanded = 6, Expanded = 7,
                     ExtraExpanded = 8, UltraExpanded = 9


COMPLETE EXAMPLES
=================

1. Caret, hit-test, selection, draw (an editor's four questions)
-----------------------------------------------------------------
    using CodeBrix.Platform.UI.TextLayout;
    using SkiaSharp;

    using var layout = TextLayoutEngine.Layout("Hello, world", "sans-serif", 24f);

    SKRect caret = layout.GetCaretRect(3);                      // before 'l'
    int index    = layout.GetIndexAt(new SKPoint(x, y));        // -1 outside
    int nearest  = layout.GetNearestIndexAt(new SKPoint(x, y)); // never -1
    IReadOnlyList<SKRect> selection = layout.GetSelectionRects(0, 5);

    using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
    using var selPaint  = new SKPaint { Color = new SKColor(0x33, 0x66, 0xcc, 0x59) };

    canvas.Save();
    canvas.Translate(10, 10);                       // layout origin
    foreach (var r in selection) canvas.DrawRect(r, selPaint);
    layout.Draw(canvas, SKPoint.Empty, textPaint);
    canvas.DrawRect(caret, textPaint);
    canvas.Restore();

2. Mixed runs, wrapped and centered, with per-run colors
--------------------------------------------------------
    var runs = new[]
    {
        new TextRunDescriptor("if ", "monospace", 13f) { Color = new SKColor(0x56, 0x9C, 0xD6) },
        TextRunDescriptor.Create("(ready)", "monospace", 13f, bold: true),
        new TextRunDescriptor(" // launch", "monospace", 13f,
                              TextFontWeight.Normal, TextFontStyle.Italic)
            { Color = new SKColor(0x6A, 0x99, 0x55) },
    };
    var options = new TextLayoutOptions
    {
        MaxWidth = 240f,                 // wrapping ON, and a box to align in
        Alignment = TextAlign.Center,
        MaxLines = 3,
    };
    using var layout = TextLayoutEngine.Layout(runs, options);

    // layout.Text == "if (ready) // launch"; indices address that string
    using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
    layout.Draw(canvas, new SKPoint(20, 20), paint);   // uncolored runs use White

3. Outlined text (fill + stroke) and per-glyph work
---------------------------------------------------
    using var layout = TextLayoutEngine.Layout("OUTLINE", "sans-serif", 72f);

    using var fill   = new SKPaint { Color = SKColors.White, IsAntialias = true };
    using var stroke = new SKPaint
    {
        Color = SKColors.Black, IsAntialias = true,
        Style = SKPaintStyle.Stroke, StrokeWidth = 3f, StrokeJoin = SKStrokeJoin.Round,
    };

    // One combined, already-positioned path: the cheap way to outline text
    using (var path = layout.GetOutlinePath())       // caller owns it
    {
        canvas.Save();
        canvas.Translate(40, 40);
        canvas.DrawPath(path, stroke);
        canvas.DrawPath(path, fill);
        canvas.Restore();
    }

    // Per glyph: each Path sits at the origin; translate by Origin to place it
    var glyphs = layout.GetGlyphOutlines();          // caller owns each one
    try
    {
        foreach (var g in glyphs)
        {
            if (g.Path is null || g.Path.IsEmpty) continue;   // space, color emoji
            canvas.Save();
            canvas.Translate(40 + g.Origin.X, 140 + g.Origin.Y);
            canvas.DrawPath(g.Path, fill);
            canvas.Restore();
            // g.Advance, g.GlyphId and g.Font (engine-owned) are available too
        }
    }
    finally
    {
        foreach (var g in glyphs) g.Dispose();
    }

4. Walk the lines (per-line decorations)
----------------------------------------
    using var layout = TextLayoutEngine.Layout(longText, "sans-serif", 14f,
        new TextLayoutOptions { MaxWidth = 300f });

    using var rule = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1f };
    for (var i = 0; i < layout.LineCount; i++)
    {
        TextLineMetrics m = layout.GetLineMetrics(i);
        var baseline = m.Top + m.BaselineOffset;
        canvas.DrawLine(0, baseline + 2, layout.Size.Width, baseline + 2, rule);
        string lineText = layout.Text.Substring(m.Start, m.Length);
    }

    TextLineInfo where = layout.GetLineAt(caretIndex);   // which line is the caret on?

5. Right-to-left and bidi
-------------------------
    using var rtl = TextLayoutEngine.Layout("שלום", "sans-serif", 20f);
    bool isRtl = rtl.IsBaseDirectionRightToLeft;            // true (detected)

    var forcedLtr = new TextLayoutOptions { BaseDirection = TextDirection.LeftToRight };
    using var mixed = TextLayoutEngine.Layout("abc שלום def", "sans-serif", 20f, forcedLtr);
    var rects = mixed.GetSelectionRects(2, 6);   // can be several rectangles on ONE
                                                 // line: the RTL stretch is visually
                                                 // discontiguous from its LTR neighbours

6. Render to a PNG with no application at all
---------------------------------------------
    using var layout = TextLayoutEngine.Layout("Headless", "sans-serif", 48f);
    var info = new SKImageInfo((int)Math.Ceiling(layout.Size.Width) + 20,
                               (int)Math.Ceiling(layout.Size.Height) + 20);
    using var surface = SKSurface.Create(info);
    surface.Canvas.Clear(SKColors.White);
    using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
    layout.Draw(surface.Canvas, new SKPoint(10, 10), paint);
    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var file = File.Create("headless.png");
    data.SaveTo(file);


MINIMUM VIABLE PROJECT
======================
A console application with no head. The package-set mirrors what this
package's own test project references (native SkiaSharp and HarfBuzz for
the OS). CodeBrixRuntimeIdentifier selects the framework's Skia runtime
assemblies for a project that has no head to select them - every head sets
it to "Skia" through its own build props.

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <CodeBrixRuntimeIdentifier>Skia</CodeBrixRuntimeIdentifier>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.TextLayout.ApacheLicenseForever" />
        <!-- native assets for the OS you run on; use the .Win32 / .macOS pairs elsewhere -->
        <PackageReference Include="SkiaSharp.NativeAssets.Linux" />
        <PackageReference Include="HarfBuzzSharp.NativeAssets.Linux" />
      </ItemGroup>
    </Project>

(Pin the versions your package management requires; the native-asset
packages must match the SkiaSharp / HarfBuzzSharp versions the package
brings in. On Linux, install the distribution's libicu package first.)

    // Program.cs
    using CodeBrix.Platform.UI.TextLayout;
    using SkiaSharp;

    using var layout = TextLayoutEngine.Layout("Hello, layout", "sans-serif", 24f);
    Console.WriteLine($"{layout.Size.Width:F1} x {layout.Size.Height:F1}, " +
                      $"{layout.LineCount} line(s), caret 5 at {layout.GetCaretRect(5).Left:F1}");

Inside an existing CodeBrix.Platform application none of the extra
properties or native packages are needed: add the one PackageReference and
use the API from any project.


PERFORMANCE TIPS
================
  - Layout() is the expensive call (shaping, bidi, itemisation, line
    breaking). Every query on the result - carets, hit-tests, selection
    rectangles, line metrics - is cheap. Keep the TextLayoutResult for as
    long as the text and style are unchanged; re-layout only when they
    change. An editor keeps one result per line (with MaxWidth = null) and
    re-lays out only the edited line.
  - Draw() uses text blobs - the fast path. Outlines build paths: cache the
    SKPath from GetOutlinePath for text that is drawn repeatedly, and
    prefer it over GetGlyphOutlines (one path versus one object and one
    path per glyph) unless glyphs really are handled individually.
  - Color on a run costs nothing at layout time; use it instead of
    splitting a layout into several layouts just to change colors.
  - The first Layout call in a process initialises ICU; measure after a
    warm-up call, not on the first one.
  - Fonts are cached by the engine per (family, size, weight, stretch,
    style). Many distinct sizes or families mean many cache entries; a
    handful of styles reused across runs is the cheap shape.
  - A layout made while an application font is still loading uses an
    interim face; if exact metrics matter (cursor placement in a font that
    just arrived), lay out again after the load.
  - Dispose paths and GlyphOutlines promptly - they hold native Skia
    memory. Disposing the TextLayoutResult itself is free (a no-op today).


COMMON PITFALLS TO AVOID
========================
  - Indices are TEXT indices into TextLayoutResult.Text (the concatenation
    of all runs), never glyph indices and never per-run offsets. Add the
    preceding runs' lengths when mapping from a run-local position.
  - Wrapping is OFF unless TextLayoutOptions.MaxWidth is set - a long
    single-line string comes back as one line, however wide. Alignment is
    ALSO ignored without a MaxWidth; Center / Right silently behave as Left.
  - Layout(runs) with an EMPTY list throws; empty text is one run with "".
    Empty text still has a caret (index 0) and a LineHeight.
  - GetCaretRect / GetRectForIndex / GetLineAt accept 0..Text.Length
    INCLUSIVE and throw outside it; GetLineMetrics takes a LINE index
    (0..LineCount - 1), not a text index.
  - GetIndexAt returns -1 outside the text. For drag-selection use
    GetNearestIndexAt, which clamps instead.
  - Selection always comes back as a LIST of rectangles (lines, bidi
    boundaries); never assume one.
  - TextLineInfo.Length and TextLineMetrics.Length INCLUDE a trailing line
    break; trim it before showing the slice.
  - GetOutlinePath and GetGlyphOutlines return objects YOU own - dispose
    them. GlyphOutline.Font is engine-owned - do NOT dispose it.
  - GlyphOutline.Path is positioned at the origin, not at Origin: translate
    by Origin (GetOutlinePath is already positioned). Spaces and bitmap /
    color emoji yield an empty path but still carry an Advance.
  - A run's Color overrides the Draw paint's color for that run only; it
    has no effect on GetOutlinePath (a path has no color - paint it).
  - TextDirection.Auto on a RUN means "inherit the layout's base
    direction", not "detect this run". Set BaseDirection explicitly when a
    mixed-direction UI must not flip with its content.
  - MaxLines below 0 is treated as 0 (unlimited), not as an error.
  - Font names resolve per machine. Tests in this repository use
    "sans-serif" and deliberately never assert what it resolves to; do the
    same, or ship an application font and name it by URI.
  - Headless on Linux without libicu installed fails on the first Layout
    call; headless anywhere without the native SkiaSharp / HarfBuzz assets
    fails on load.
  - `using var layout = ...` is the recommended idiom even though Dispose
    is a no-op today; it costs nothing and stays correct if that changes.


WHAT THIS PACKAGE DOES NOT DO
=============================
  - No vertical text, no ruby / furigana, no text-on-a-path.
  - No IME / preedit handling - an input concern, not a layout one.
  - No justification (TextAlign is Left / Center / Right) and no
    letter-spacing, word-spacing or kerning switches.
  - No decorations: underline, strikethrough, highlight and caret painting
    are the caller's, using the metrics this package reports.
  - No rich-text document model - no paragraphs, indents, tabs or margins;
    a layout is one paragraph of runs plus the line breaks in its text.
  - No font enumeration or font loading API; families are named and the
    engine resolves them.
  - No XAML types, events, or controls - and no application host
    requirement, which is the point.
  - No colored-emoji outlines (bitmap / color glyphs yield an empty path;
    Draw still paints them).


WORKING EXAMPLES ON GITHUB
==========================
The package's test project is the verified hostless consumer - a plain xunit
project with no application head:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.TextLayout.Tests
  - TextLayoutEngineTests.cs     Layout with no host; caret at start /
                                 monotonic across LTR text; hit-test round
                                 trips for every index; -1 outside; nearest
                                 index clamps; empty text has a caret and a
                                 line height; explicit newlines make lines;
                                 no MaxWidth means no wrap; empty run list
                                 and out-of-range caret index throw
  - TextLayoutOptionsTests.cs    runs concatenate into Text; bold measures
                                 wider; larger size measures taller; mixed
                                 sizes take the taller line; MaxWidth wraps;
                                 MaxLines clamps; Center indents a narrow
                                 line; alignment ignored without MaxWidth;
                                 explicit LineHeight; per-line metrics;
                                 argument validation; Create shorthand
  - TextLayoutClusterTests.cs    combining marks share a cluster / do not
                                 advance the caret / measure as one advance;
                                 RTL and LTR detection; explicit
                                 BaseDirection wins; RTL caret advances
                                 leftwards; bidi visual order; hit-testing
                                 inside an RTL run
  - TextLayoutSelectionTests.cs  GetSelectionRects: empty range, single
                                 line, matches character rects, one rect per
                                 line, whole text spans the width, clamps a
                                 range past the end, empty text, bidi range
                                 is discontiguous
  - TextLayoutOutlineTests.cs    GetGlyphOutlines: one per glyph, non-empty
                                 paths with advances, left-to-right
                                 positions, a space advances but draws
                                 nothing; GetOutlinePath combines every
                                 glyph, bounds within the measured size,
                                 empty for empty text; Draw puts ink on a
                                 canvas, at different origins, and leaves it
                                 alone for empty text
  - TestFonts.cs                 how the suite picks a family with distinct
                                 regular and bold faces on the machine
                                 ("sans-serif" first, then the installed
                                 families)

In-family consumers, for the render-pass pattern:
  - The TerminalView control measures its cell from one "x" layout and
    draws every attribute run through TextLayoutEngine.Layout + Draw:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.TerminalView
  - The package source itself (TextLayoutEngine.cs plus Models/):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.TextLayout


QUICK REFERENCE CARD
====================
    Package:   CodeBrix.Platform.TextLayout.ApacheLicenseForever
    Using:     using CodeBrix.Platform.UI.TextLayout;  using SkiaSharp;
    Headless:  + SkiaSharp/HarfBuzzSharp NativeAssets for the OS,
               <CodeBrixRuntimeIdentifier>Skia</CodeBrixRuntimeIdentifier>,
               libicu on Linux

    TextLayoutEngine.Layout(string text, string? fontFamily = null,
                            float fontSize = 12f, TextLayoutOptions? options = null)
    TextLayoutEngine.Layout(IReadOnlyList<TextRunDescriptor> runs,
                            TextLayoutOptions? options = null)      // never empty

    TextRunDescriptor(text, fontFamily = null, fontSize = 12f,
                      weight = Normal, style = Normal, stretch = Normal,
                      direction = Auto) { Color = SKColor? }
    TextRunDescriptor.Create(text, fontFamily, fontSize, bold, italic)

    TextLayoutOptions { float? MaxWidth; int MaxLines; TextAlign Alignment;
                        float LineHeight; TextDirection BaseDirection }

    TextLayoutResult (using var ...)
      Text, Size, LineCount, LineHeight, IsBaseDirectionRightToLeft
      GetCaretRect(int, float = 1f) -> SKRect        0..Length inclusive
      GetRectForIndex(int) -> SKRect                 cluster rect
      GetIndexAt(SKPoint) -> int                     -1 outside
      GetNearestIndexAt(SKPoint) -> int              clamped
      GetLineAt(int) -> TextLineInfo                 (Start, Length, LineIndex,
                                                      IsFirstLine, IsLastLine)
      GetLineMetrics(int line) -> TextLineMetrics    (Start, Length, Top,
                                                      Height, BaselineOffset)
      GetSelectionRects(int start, int length) -> IReadOnlyList<SKRect>
      GetOutlinePath() -> SKPath                     caller disposes
      GetGlyphOutlines() -> IReadOnlyList<GlyphOutline>   caller disposes each
      Draw(SKCanvas, SKPoint origin, SKPaint) / Draw(SKCanvas, SKPaint)

    GlyphOutline { GlyphId, Path (at origin), Origin, Advance, Font (not yours) }

    Enums: TextAlign Left/Center/Right | TextDirection Auto/LeftToRight/
           RightToLeft | TextFontWeight Thin..Black (100..900) |
           TextFontStyle Normal/Oblique/Italic | TextFontStretch
           Undefined, UltraCondensed..UltraExpanded (Normal = 5)

    Rules: text indices, not glyph indices | MaxWidth for wrap AND align |
           selection is a list | dispose paths, never Font
