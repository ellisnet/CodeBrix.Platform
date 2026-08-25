================================================================================
AGENT-README: CodeBrix.Platform.Graphics2DSK
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.Graphics2DSK is the immediate-mode 2D drawing add-in for
CodeBrix.Platform applications (.NET 10 or later). It contributes ONE type:

    SKCanvasElement   an abstract XAML FrameworkElement that you subclass.
                      Each time the element is painted, the framework hands
                      your RenderOverride method the SkiaSharp SKCanvas of the
                      frame being composed, already translated and clipped to
                      the element's own rectangle. You draw with the ordinary
                      SkiaSharp API and call Invalidate() whenever the picture
                      should change.

It is the lightest way to put custom SkiaSharp drawing into a page: there is
no intermediate bitmap, no per-frame pixel copy and no extra texture — the
element draws straight into the same Skia picture the rest of the UI is
rendered with. Hit-testing works (the element receives pointer events over
its whole area), and it is an ordinary element for layout, clipping, opacity
and transforms.

Provenance: a port of the upstream project's Graphics2DSK add-in. The
namespace is CodeBrix.Platform.WinUI.Graphics2DSK; do not use the upstream
namespace.

INSTALLATION
============
Package id:   CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever
License:      Apache-2.0

    dotnet add package CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever

Reference it ONCE, in the .Core (shared UI) project of the application —
never in a head project. NuGet dependency (resolved automatically): SkiaSharp.
The assembly also binds the core UI framework (the
CodeBrix.Platform.ApacheLicenseForever package), which every .Core project
already references.

Requirements: a CodeBrix.Platform application running on one of its Skia
heads. All of the framework's heads (Windows Win32-Skia and WPF-Skia, Linux
X11, Wayland and Frame Buffer, macOS) are Skia heads, so the element works on
every one of them with no native library beyond what the head itself needs.
The package ships a single net10.0 assembly.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.WinUI.Graphics2DSK;   // SKCanvasElement
    using SkiaSharp;                              // SKCanvas, SKPaint, SKColors, ...
    using Windows.Foundation;                     // Size (the RenderOverride argument)

There is nothing to declare in XAML for the package itself: because you
subclass SKCanvasElement, the XAML namespace is YOUR project's namespace:

    xmlns:local="using:MyApp.Views"
    <local:SignalScope x:Name="Scope" Height="160" />

CORE API REFERENCE
==================

SKCanvasElement
---------------
    namespace CodeBrix.Platform.WinUI.Graphics2DSK;

    public abstract partial class SKCanvasElement : FrameworkElement
    {
        protected SKCanvasElement();
        public static bool IsSupportedOnCurrentPlatform();
        public void Invalidate();
        protected abstract void RenderOverride(SKCanvas canvas, Size area);
    }

That is the entire public surface of the package.

protected SKCanvasElement()
    Throws PlatformNotSupportedException when IsSupportedOnCurrentPlatform()
    is false. On the CodeBrix.Platform heads it never throws.

public static bool IsSupportedOnCurrentPlatform()
    True when the running framework build has registered the Skia
    canvas-visual factory the element draws through. The core framework
    registers it during Application initialization on every Skia head, so on
    CodeBrix.Platform it is true once the app has started. It is false only
    in a build of the same XAML for a native WindowsAppSDK (non-Skia)
    target, where the element is a stub.

public void Invalidate()
    Marks the element's visual dirty so the compositor repaints it (calling
    RenderOverride again) on its next frame. Call it after every state change
    that should become visible. Note that the element is ALSO repainted
    whenever the compositor recomposes the frame for other reasons (layout,
    other dirty elements), so RenderOverride must always draw the CURRENT
    state from your fields — never accumulate onto the canvas.

protected abstract void RenderOverride(SKCanvas canvas, Size area)
    Your drawing code. When it is called:
      - canvas is the compositor's frame canvas. Its origin (0,0) is the
        top-left corner of THIS element; the framework has already applied
        the element's position, its transforms and the window's display
        scale.
      - area is the element's arranged size (RenderSize) in DIPs. Anything
        drawn outside the (0, 0, area.Width, area.Height) rectangle is
        clipped away (the clip is anti-aliased).
      - The canvas state is saved before the call and restored after it, so
        you may Translate/Scale/ClipRect freely without balancing
        Save/Restore yourself (balancing them is still good practice).
      - The canvas is NOT cleared for you. Draw a background (canvas.Clear
        or DrawRect) if you need an opaque one; otherwise whatever is behind
        the element shows through.
    It runs on the UI thread as part of frame composition. Keep it fast and
    allocation-free (see PERFORMANCE TIPS); never block or await inside it.

DIPs versus pixels
------------------
The element works entirely in device-independent pixels (DIPs) — the same
units as ActualWidth/ActualHeight and pointer positions. The frame canvas
already carries the window's rasterization scale (on a 200 % display one DIP
is two physical pixels), so:
  - draw in DIPs and the output is crisp at any display scale — do NOT
    multiply your coordinates by a DPI factor;
  - a 1-DIP stroke is one DIP wide on screen (two physical pixels at 200 %);
    read canvas.TotalMatrix.ScaleX if you need the scale, for example to draw
    hairlines exactly one physical pixel wide;
  - pointer positions from e.GetCurrentPoint(this).Position are in the same
    DIP space as your drawing; no conversion is needed.
This is the opposite of SKXamlCanvas (SkiaSharp.Views package), which by
default reports PHYSICAL pixel sizes in its PaintSurface event unless its
IgnorePixelScaling property is set.

Relationship to the other Skia surfaces
---------------------------------------
    SKCanvasElement (this package)
        CPU Skia drawn directly into the frame; subclass + RenderOverride;
        DIPs; zero copies. The best default for custom-drawn controls,
        charts, gauges and editors.
    SKXamlCanvas (CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever)
        CPU Skia into an offscreen WriteableBitmap that is then presented as
        the element's background; used from XAML without subclassing via a
        PaintSurface event; pixel-unit semantics compatible with SkiaSharp's
        own views. One extra full-frame copy per paint unless the app opts
        into the experimental UseDirectSkiaCanvasMode() host-builder flag
        (that flag affects SKXamlCanvas ONLY — SKCanvasElement never had the
        copy). Choose it when porting code written against SkiaSharp's
        SKXamlCanvas or when a control library expects that type.
    SkiaGLCanvasElement / GLCanvasElement
    (CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever)
        A GPU-backed SKSurface, or raw OpenGL, rendered offscreen and read
        back. Choose them only when the drawing itself needs the GPU
        (shaders, 3D, very large GPU-Skia scenes); the read-back costs a
        copy per frame.

COMPLETE EXAMPLES
=================

1. A live signal scope (subclass, XAML, animation)
--------------------------------------------------
SignalScope.cs (in the .Core project):

    using System;
    using CodeBrix.Platform.WinUI.Graphics2DSK;
    using SkiaSharp;
    using Windows.Foundation;

    namespace MyApp.Views;

    public sealed partial class SignalScope : SKCanvasElement
    {
        // Paints are reused across frames; never allocate them in RenderOverride.
        private readonly SKPaint _background = new() { Color = new SKColor(0x10, 0x14, 0x1C) };
        private readonly SKPaint _grid = new()
        {
            Color = new SKColor(0x30, 0x38, 0x48), StrokeWidth = 1,
            Style = SKPaintStyle.Stroke, IsAntialias = false
        };
        private readonly SKPaint _trace = new()
        {
            Color = SKColors.LimeGreen, StrokeWidth = 2,
            Style = SKPaintStyle.Stroke, IsAntialias = true
        };
        private readonly SKPath _path = new();
        private float[] _samples = Array.Empty<float>();   // values in -1..1

        public void SetSamples(float[] samples)
        {
            _samples = samples;
            Invalidate();                 // repaint with the new data
        }

        protected override void RenderOverride(SKCanvas canvas, Size area)
        {
            var w = (float)area.Width;
            var h = (float)area.Height;

            canvas.DrawRect(0, 0, w, h, _background);
            for (var x = 0f; x < w; x += 20f) canvas.DrawLine(x, 0, x, h, _grid);
            for (var y = 0f; y < h; y += 20f) canvas.DrawLine(0, y, w, y, _grid);

            if (_samples.Length < 2) return;

            _path.Reset();
            var step = w / (_samples.Length - 1);
            for (var i = 0; i < _samples.Length; i++)
            {
                var x = i * step;
                var y = h / 2 - _samples[i] * (h / 2 - 4);
                if (i == 0) _path.MoveTo(x, y); else _path.LineTo(x, y);
            }
            canvas.DrawPath(_path, _trace);
        }
    }

MainPage.xaml:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:MyApp.Views">
        <Grid RowSpacing="8" Padding="12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <TextBlock Text="Signal" FontSize="20" />
            <local:SignalScope x:Name="Scope" Grid.Row="1" MinHeight="120" />
        </Grid>
    </Page>

MainPage.xaml.cs — feed new data 30 times a second from a UI-thread timer:

    using System;
    using Microsoft.UI.Dispatching;
    using Microsoft.UI.Xaml.Controls;

    namespace MyApp.Views;

    public sealed partial class MainPage : Page
    {
        private readonly float[] _buffer = new float[256];
        private double _phase;
        private DispatcherQueueTimer _timer;

        public MainPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _timer = DispatcherQueue.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(33);
                _timer.IsRepeating = true;
                _timer.Tick += (_, _) => Advance();
                _timer.Start();
            };
            Unloaded += (_, _) => _timer?.Stop();
        }

        private void Advance()
        {
            _phase += 0.15;
            for (var i = 0; i < _buffer.Length; i++)
                _buffer[i] = (float)Math.Sin(_phase + i * 0.08);
            Scope.SetSamples(_buffer);        // -> Invalidate() -> RenderOverride
        }
    }

2. Pointer interaction in the same coordinate space
---------------------------------------------------
    // inside an SKCanvasElement subclass
    private readonly SKPaint _dot = new() { Color = SKColors.OrangeRed, IsAntialias = true };
    private SKPoint _marker = new(-100, -100);

    public MarkerCanvas()
    {
        PointerPressed += (s, e) =>
        {
            var p = e.GetCurrentPoint(this).Position;   // DIPs: same space as the drawing
            _marker = new SKPoint((float)p.X, (float)p.Y);
            Invalidate();
        };
    }

    protected override void RenderOverride(SKCanvas canvas, Size area)
    {
        canvas.DrawCircle(_marker, 6, _dot);
    }

3. Guarding a view that is also built for a non-Skia target
-----------------------------------------------------------
    if (SKCanvasElement.IsSupportedOnCurrentPlatform())
        Host.Children.Add(new SignalScope());
    else
        Host.Children.Add(new TextBlock { Text = "Custom drawing is not available here." });

MINIMUM VIABLE PROJECT
======================
Add to the .Core project's csproj (alongside the core framework package):

    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever" />
    </ItemGroup>

Then add the SignalScope class and the MainPage from example 1. No head
project changes and no bootstrap changes are needed: the element works as
soon as the app's host builder has started a Skia head. (See the core
package's AGENT-README for the head/bootstrap scaffold.)

PERFORMANCE TIPS
================
  - RenderOverride runs inside frame composition on the UI thread.
    Precompute geometry and keep SKPaint / SKPath / SKFont / SKImage objects
    in fields; creating them per frame causes GC churn and stalls at
    animation rates.
  - Do not call Invalidate() from inside RenderOverride unless you want a
    continuous animation; every call schedules another frame.
  - Invalidate() only when state actually changed. For high-rate data, batch
    updates and invalidate once per timer tick (as in example 1), not once
    per sample.
  - Draw only what lies inside `area`. The clip protects correctness, not
    cost: Skia still processes commands that end up fully clipped.
  - Large static backdrops: render them once into an SKImage (an SKSurface
    plus Snapshot()) and DrawImage it each frame instead of re-issuing
    hundreds of primitives.
  - Text: shape once and keep the SKTextBlob; re-shaping every frame is the
    usual hidden cost in drawn controls.

COMMON PITFALLS TO AVOID
========================
  - Forgetting Invalidate(): changing a field does nothing visible until you
    call Invalidate(). Conversely, RenderOverride may run whenever the frame
    is recomposed, so it must be a pure function of the current state.
  - Multiplying coordinates by a DPI factor. The canvas already carries the
    display scale; scaling again makes drawings twice as big on HiDPI. (Only
    SKXamlCanvas has pixel-vs-DIP switching — see its own AGENT-README.)
  - Drawing outside `area` and expecting it to show — it is clipped.
  - Not drawing a background and being surprised by "transparency": the
    frame canvas is not cleared for you.
  - Blocking in RenderOverride (file I/O, network, awaiting) freezes the
    whole UI; prepare data elsewhere and only draw in RenderOverride.
  - Constructing the element on a non-Skia target: the constructor throws
    PlatformNotSupportedException. Check IsSupportedOnCurrentPlatform() first
    if the XAML is shared with such a target.
  - Zero-size element: with no Height/MinHeight and no star row, an element
    may arrange at 0 x 0 and RenderOverride draws nothing. Give it a bounded
    size (a star Grid cell, or Height/MinHeight).
  - Expecting a GPU: SKCanvasElement is CPU Skia. For GPU rendering use the
    Graphics3DGL package.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does not contain SKXamlCanvas or SKSwapChainPanel — those live in
    CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever (this package does
    not depend on that one).
  - It does not provide GPU rendering, OpenGL or a GRContext — see
    CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever.
  - It does not render to an image or file; use SkiaSharp directly
    (SKSurface, SKBitmap) for offscreen output, or RenderTargetBitmap for a
    XAML snapshot.
  - It does not support a native WindowsAppSDK (non-Skia) target: there the
    element is a stub whose constructor throws.
  - It provides no built-in gestures, scrolling, zooming or hit-testing of
    drawn shapes — handle pointer events yourself (example 2).
  - It is not affected by UseDirectSkiaCanvasMode(); that flag concerns
    SKXamlCanvas only.

WORKING EXAMPLES ON GITHUB
==========================
No sample in this repository subclasses SKCanvasElement yet. The closest
related sample on the same heads:
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/EmulateFrameBufferDemo
      Uses SKXamlCanvas (SkiaSharp.Views package) for a Skia sketch pane and
      GLCanvasElement (Graphics3DGL package) for an OpenGL pane on the same
      page — the pointer-handling and Invalidate patterns are the same ones
      an SKCanvasElement subclass uses.

QUICK REFERENCE CARD
====================
    Package:    CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever  (.Core, once)
    Namespace:  CodeBrix.Platform.WinUI.Graphics2DSK
    Type:       public abstract partial class SKCanvasElement : FrameworkElement
      protected SKCanvasElement()            throws PlatformNotSupportedException off-Skia
      public static bool IsSupportedOnCurrentPlatform()
      public void Invalidate()               schedule a repaint (UI thread)
      protected abstract void RenderOverride(SKCanvas canvas, Size area)
                                             origin top-left, DIPs, clipped to area,
                                             Save/Restore done for you, NOT cleared
    Pattern:    subclass -> cache SKPaints -> draw current state in RenderOverride
                -> Invalidate() after every state change
    Units:      DIPs everywhere (drawing == layout == pointer positions)
    Siblings:   SKXamlCanvas       -> CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
                GLCanvasElement /
                SkiaGLCanvasElement -> CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
