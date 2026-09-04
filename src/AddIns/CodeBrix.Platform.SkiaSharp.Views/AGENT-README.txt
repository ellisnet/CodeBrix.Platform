================================================================================
AGENT-README: CodeBrix.Platform.SkiaSharp.Views
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.SkiaSharp.Views carries the SkiaSharp XAML view types for
CodeBrix.Platform applications (.NET 10 or later), in the same namespace
(SkiaSharp.Views.Windows) and with the same members as SkiaSharp's own WinUI
views, so code and control libraries written against those types compile
unchanged:

    SKXamlCanvas               a Canvas that raises PaintSurface with a CPU
                               SKSurface; the drawing is presented through a
                               WriteableBitmap. FULLY FUNCTIONAL.
    SKPaintSurfaceEventArgs    Surface / Info / RawInfo for that event.
    SKSwapChainPanel           the GPU swap-chain view. On the CodeBrix.Platform
                               heads it is a NON-FUNCTIONAL placeholder (its
                               constructor throws unless you opt out, and it
                               never paints) — present only so shared code
                               compiles. For GPU Skia use SkiaGLCanvasElement
                               from the Graphics3DGL package.
    SKPaintGLSurfaceEventArgs  the event-args type of SKSwapChainPanel.
    UWPExtensions              Point/Rect/Size/Color <-> SKPoint/SKRect/SKSize/
                               SKColor conversion extension methods.

Provenance: vendored from the SkiaSharp repository's views source (MIT;
portions copyright Microsoft Corporation), compiled against the CodeBrix
Skia runtime. Keep using the upstream namespace SkiaSharp.Views.Windows —
that is the point of the package. Do NOT also reference SkiaSharp's own
SkiaSharp.Views.WinUI (or any other SkiaSharp.Views.* XAML view package):
the type names would collide.

VERSIONING RULE (durable): this package is versioned to track the SkiaSharp
release it vendors, NOT the CodeBrix.Platform family version. Reference it
without a version attribute, like the other framework packages, and NuGet
resolves the release that matches the SkiaSharp the family is built against.

INSTALLATION
============
Package id:   CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
License:      MIT

    dotnet add package CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever

Reference it in the .Core (shared UI) project. NuGet dependencies (resolved
automatically): SkiaSharp and CodeBrix.Platform.ApacheLicenseForever (the
core framework). The Lottie and Svg add-ins depend on this package
themselves; reference it directly only when your OWN code uses SKXamlCanvas
or the conversion helpers.

Requirements: any CodeBrix.Platform head (all are Skia heads). A single
net10.0 assembly; no extra native libraries.

KEY NAMESPACES / USINGS
=======================
    using SkiaSharp.Views.Windows;        // SKXamlCanvas, SKPaintSurfaceEventArgs,
                                          // SKSwapChainPanel, SKPaintGLSurfaceEventArgs,
                                          // UWPExtensions
    using SkiaSharp;                      // SKCanvas, SKPaint, SKSurface, SKImageInfo, ...
    using CodeBrix.Platform.UI.Hosting;   // UseDirectSkiaCanvasMode() (core package,
                                          // head Program.cs only)
XAML:
    xmlns:skia="using:SkiaSharp.Views.Windows"
    <skia:SKXamlCanvas x:Name="Canvas" PaintSurface="OnPaintSurface" />

CORE API REFERENCE
==================

SKXamlCanvas
------------
    namespace SkiaSharp.Views.Windows;

    public partial class SKXamlCanvas : Canvas
    {
        public SKXamlCanvas();
        public event EventHandler<SKPaintSurfaceEventArgs> PaintSurface;
        protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e);
        public new void Invalidate();
        public SKSize CanvasSize { get; }        // size of the last painted surface
        public bool IgnorePixelScaling { get; set; }
        public double Dpi { get; }               // display scale factor (logical DPI / 96)
    }

How it paints: Invalidate() IS the paint. On the UI thread it synchronously
(re)creates a BGRA8888 premultiplied pixel buffer of
(ActualWidth x Dpi) by (ActualHeight x Dpi) physical pixels when the size
changed, wraps it in an SKSurface, resets the canvas state (RestoreToCount(1)
and ResetMatrix), raises PaintSurface, flushes, copies the pixels into a
WriteableBitmap and sets that bitmap as the element's Background (an
ImageBrush, top-left aligned, Stretch = Fill). Called from another thread,
Invalidate() marshals itself to the UI thread. The element invalidates
itself on Loaded, SizeChanged, display-DPI change and Visibility change.
While Visibility is Collapsed, or the element has no positive size, nothing
is painted (CanvasSize becomes empty).

Nothing else repaints it: after any state change call Invalidate(). The
surface is NOT cleared between paints (the same buffer is reused), so start
each handler with canvas.Clear(...) unless you intend to accumulate.

IgnorePixelScaling and the two sizes:

    IgnorePixelScaling == false (default)
        e.Info.Width/Height  = PHYSICAL pixels (ActualWidth x Dpi, ...)
        canvas matrix        = identity: you draw in physical pixels
        CanvasSize           = physical pixels
        -> a 100-DIP-wide element on a 200 % display yields a 200-px-wide
           canvas; multiply pointer positions (DIPs) by Dpi before use.

    IgnorePixelScaling == true
        e.Info.Width/Height  = DIPs (ActualWidth, ActualHeight, truncated)
        canvas matrix        = pre-scaled by Dpi: draw in DIPs, output stays
                               crisp at the physical resolution
        CanvasSize           = DIPs
        -> pointer positions can be used directly.

    e.RawInfo is ALWAYS the physical pixel buffer (its size, and
    Bgra8888 / Premul). Setting IgnorePixelScaling calls Invalidate() itself.

Because it derives from Canvas, ordinary XAML children may be placed inside
it and are drawn above the painted background.

SKPaintSurfaceEventArgs
-----------------------
    public class SKPaintSurfaceEventArgs : EventArgs
    {
        public SKPaintSurfaceEventArgs(SKSurface surface, SKImageInfo info);
        public SKPaintSurfaceEventArgs(SKSurface surface, SKImageInfo info, SKImageInfo rawInfo);
        public SKSurface Surface { get; }     // draw on Surface.Canvas
        public SKImageInfo Info { get; }      // user-visible size (see IgnorePixelScaling)
        public SKImageInfo RawInfo { get; }   // the actual pixel buffer
    }

SKSwapChainPanel — a placeholder on the CodeBrix.Platform heads
---------------------------------------------------------------
    public partial class SKSwapChainPanel : FrameworkElement
    {
        public static bool RaiseOnUnsupported { get; set; }   // default: true
        public SKSwapChainPanel();          // throws NotSupportedException when RaiseOnUnsupported
        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurface;   // never raised
        protected virtual void OnPaintSurface(SKPaintGLSurfaceEventArgs e);
        public new void Invalidate();       // no-op
        public SKSize CanvasSize { get; }   // throws when RaiseOnUnsupported; else empty
        public GRContext GRContext { get; } // throws when RaiseOnUnsupported; else null
        public double ContentsScale { get; }
        public bool EnableRenderLoop { get; set; }   // setter accepted; has no effect
        public bool DrawInBackground { get; set; }   // [NotImplemented]: getter and setter
                                                     // throw NotImplementedException
    }

On every CodeBrix.Platform head this type exists only so that shared code
compiles. It is not GPU-backed and it does not paint: with the default
RaiseOnUnsupported = true its constructor throws NotSupportedException
("SKSwapChainPanel is not supported for Skia based platforms"); setting
SKSwapChainPanel.RaiseOnUnsupported = false BEFORE constructing one turns it
into a silent empty element (CanvasSize empty, GRContext null, PaintSurface
never raised). Use SkiaGLCanvasElement
(CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever) for a real GPU-backed
SKSurface.

SKPaintGLSurfaceEventArgs
-------------------------
    public class SKPaintGLSurfaceEventArgs : EventArgs
    {
        public SKPaintGLSurfaceEventArgs(SKSurface surface, GRBackendRenderTarget renderTarget);
        public SKPaintGLSurfaceEventArgs(SKSurface surface, GRBackendRenderTarget renderTarget,
                                         GRSurfaceOrigin origin, SKColorType colorType);
        public SKPaintGLSurfaceEventArgs(SKSurface surface, GRBackendRenderTarget renderTarget,
                                         GRSurfaceOrigin origin, SKImageInfo info);
        public SKPaintGLSurfaceEventArgs(SKSurface surface, GRBackendRenderTarget renderTarget,
                                         GRSurfaceOrigin origin, SKImageInfo info, SKImageInfo rawInfo);
        public SKSurface Surface { get; }
        public GRBackendRenderTarget BackendRenderTarget { get; }
        public SKColorType ColorType { get; }
        public GRSurfaceOrigin Origin { get; }
        public SKImageInfo Info { get; }
        public SKImageInfo RawInfo { get; }
    }
Provided for source compatibility; no type in this package raises it on the
CodeBrix.Platform heads.

UWPExtensions (conversion helpers)
----------------------------------
    namespace SkiaSharp.Views.Windows;

    public static class UWPExtensions
    {
        public static SKPoint ToSKPoint(this Windows.Foundation.Point point);
        public static Windows.Foundation.Point ToPoint(this SKPoint point);
        public static SKRect ToSKRect(this Windows.Foundation.Rect rect);
        public static Windows.Foundation.Rect ToRect(this SKRect rect);
        public static SKSize ToSKSize(this Windows.Foundation.Size size);
        public static Windows.Foundation.Size ToSize(this SKSize size);
        public static SKColor ToSKColor(this Windows.UI.Color color);
        public static Windows.UI.Color ToColor(this SKColor color);
    }

The class is named UWPExtensions (not WindowsExtensions) in this build. The
upstream WriteableBitmap <-> SKBitmap / SKImage / SKPixmap helpers
(ToWriteableBitmap, ToSKBitmap, ToSKImage, ToSKPixmap) are NOT compiled into
this package — see WHAT THIS PACKAGE DOES NOT DO.

The direct-present mode (a core host-builder flag)
--------------------------------------------------
    namespace CodeBrix.Platform.UI.Hosting;   // core package

    public static ICodeBrixPlatformHostBuilder UseDirectSkiaCanvasMode(
        this ICodeBrixPlatformHostBuilder builder);
    public static class DirectSkiaCanvasMode { public static bool IsEnabled { get; } }

EXPERIMENTAL. Chained once onto the host builder in a head's Program.cs
(its position relative to the .Use...() head call does not matter), it makes
every SKXamlCanvas draw straight into its on-screen WriteableBitmap buffer
instead of into a staging buffer that is then copied — one fewer full-frame
copy per paint. It is app-wide and one-way (it cannot be turned off and
there is no per-canvas override) and changes nothing when omitted. It
affects SKXamlCanvas only. Enable it to test performance/stability; it may
change or be removed.

WHEN TO USE WHICH SURFACE
=========================
    SKXamlCanvas (this package)
        Drop-in from XAML, event-based, compatible with SkiaSharp's own view
        API; CPU Skia into a bitmap (one copy per paint by default). Pick it
        for ported SkiaSharp code and for control libraries that expect the
        SkiaSharp view types.
    SKCanvasElement (CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever)
        Subclass + RenderOverride(SKCanvas, Size); CPU Skia drawn directly
        into the compositor's frame — no bitmap, no copy, always in DIPs.
        Pick it for new custom-drawn controls.
    SkiaGLCanvasElement (CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever)
        A GPU-backed SKSurface + GRContext on an offscreen OpenGL context,
        read back per frame. Pick it when the drawing itself needs the GPU
        (shaders, heavy filters, very large scenes). It is the working
        replacement for SKSwapChainPanel / SKGLView.
    GLCanvasElement (same Graphics3DGL package)
        Raw OpenGL 3.0+.

COMPLETE EXAMPLES
=================

1. Paint on demand (XAML + code-behind)
---------------------------------------
MainPage.xaml:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:skia="using:SkiaSharp.Views.Windows">
        <Grid RowSpacing="8" Padding="12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <Button Content="Shuffle" Click="OnShuffle" />
            <skia:SKXamlCanvas x:Name="Chart" Grid.Row="1" MinHeight="160"
                               PaintSurface="OnChartPaintSurface" />
        </Grid>
    </Page>

MainPage.xaml.cs:

    using System;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using SkiaSharp;
    using SkiaSharp.Views.Windows;

    namespace MyApp.Views;

    public sealed partial class MainPage : Page
    {
        private readonly float[] _values = { 0.3f, 0.8f, 0.5f, 0.9f, 0.2f, 0.6f };
        private readonly SKPaint _bar = new() { Color = SKColors.SteelBlue, IsAntialias = true };
        private readonly Random _random = new();

        public MainPage() => InitializeComponent();

        private void OnChartPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var w = e.Info.Width;             // physical pixels (IgnorePixelScaling is false)
            var h = e.Info.Height;

            canvas.Clear(SKColors.White);     // the buffer is reused: always clear
            var slot = w / (float)_values.Length;
            for (var i = 0; i < _values.Length; i++)
            {
                var barHeight = _values[i] * (h - 10);
                canvas.DrawRect(i * slot + slot * 0.15f, h - barHeight,
                                slot * 0.7f, barHeight, _bar);
            }
        }

        private void OnShuffle(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < _values.Length; i++)
                _values[i] = (float)_random.NextDouble();
            Chart.Invalidate();               // nothing repaints until you ask
        }
    }

2. Drawing in DIPs with pointer input (IgnorePixelScaling = true)
-----------------------------------------------------------------
    <skia:SKXamlCanvas x:Name="Sketch" IgnorePixelScaling="True"
                       PaintSurface="OnSketchPaintSurface"
                       PointerPressed="OnSketchPointerPressed"
                       PointerMoved="OnSketchPointerMoved"
                       PointerReleased="OnSketchPointerReleased" />

    private readonly SKPath _stroke = new();
    private readonly SKPaint _ink = new()
    {
        Color = SKColors.Black, StrokeWidth = 3, Style = SKPaintStyle.Stroke,
        IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
    };
    private bool _drawing;

    private void OnSketchPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;        // already scaled by Dpi: DIP coordinates
        canvas.Clear(SKColors.White);
        canvas.DrawPath(_stroke, _ink);
    }

    private void OnSketchPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(Sketch).Position;   // DIPs — usable as-is
        _stroke.MoveTo((float)p.X, (float)p.Y);
        _drawing = true;
        Sketch.CapturePointer(e.Pointer);
        Sketch.Invalidate();
        e.Handled = true;
    }

    private void OnSketchPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_drawing) return;
        var p = e.GetCurrentPoint(Sketch).Position;
        _stroke.LineTo((float)p.X, (float)p.Y);
        Sketch.Invalidate();
    }

    private void OnSketchPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _drawing = false;
        Sketch.ReleasePointerCapture(e.Pointer);
    }

3. Physical-pixel drawing (default) with converted pointer positions
--------------------------------------------------------------------
    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(Canvas).Position;          // DIPs
        var scale = (float)Canvas.Dpi;                        // e.g. 2.0 at 200 %
        _hit = new SKPoint((float)p.X * scale, (float)p.Y * scale);   // now pixels
        Canvas.Invalidate();
    }
    // In PaintSurface, e.Info is in pixels, e.RawInfo == e.Info, canvas matrix identity.

4. Opting into the direct-present mode (each head's Program.cs)
---------------------------------------------------------------
    using CodeBrix.Platform.UI.Hosting;

    var host = CodeBrixPlatformHostBuilder.Create()
        .App(() => new App())
        .UseLinuxX11()                 // the head's own .Use...() call
        .UseDirectSkiaCanvasMode()     // EXPERIMENTAL; app-wide; SKXamlCanvas only
        .Build();
    host.Run();

5. Conversions
--------------
    using SkiaSharp.Views.Windows;    // brings the extension methods into scope

    SKRect r = new Windows.Foundation.Rect(10, 20, 100, 50).ToSKRect();
    SKColor accent = ((SolidColorBrush)Resources["AccentBrush"]).Color.ToSKColor();
    Windows.Foundation.Point p = new SKPoint(3, 4).ToPoint();

MINIMUM VIABLE PROJECT
======================
.Core csproj additions:

    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever" />
    </ItemGroup>

Then the MainPage.xaml/.cs from example 1. No head project changes are
needed (example 4 is optional). See the core package's AGENT-README for the
head/bootstrap scaffold.

PERFORMANCE TIPS
================
  - Each Invalidate() paints synchronously on the UI thread and then copies
    the whole buffer into the WriteableBitmap; cost scales with element area
    times Dpi squared. Keep canvases as small as the design allows.
  - The surface and event args are cached between paints; do not dispose
    e.Surface or keep references to it after the handler returns.
  - Cache SKPaint / SKPath / SKFont / SKTextBlob / SKImage in fields; never
    allocate them inside PaintSurface.
  - Invalidate() only when state changed; for animation drive it from a
    DispatcherQueueTimer or at the end of your handler (each call paints
    one frame).
  - Ported code that expects SKGLView performance will not get it here:
    SKXamlCanvas is CPU Skia. Move GPU-bound drawing to SkiaGLCanvasElement.
  - Consider the experimental UseDirectSkiaCanvasMode() to drop one
    full-frame copy per paint (measure; it is app-wide and one-way).
  - For zero-copy CPU drawing in new code, SKCanvasElement (Graphics2DSK)
    avoids the bitmap and the copy entirely.

COMMON PITFALLS TO AVOID
========================
  - Constructing SKSwapChainPanel: it throws NotSupportedException on every
    CodeBrix.Platform head. Ported pages that declare
    <skia:SKSwapChainPanel/> must switch to SkiaGLCanvasElement
    (Graphics3DGL) or SKXamlCanvas; setting RaiseOnUnsupported = false only
    silences the exception, it does not make the panel paint.
  - Forgetting canvas.Clear(): the pixel buffer is reused, so old frames
    remain under the new drawing.
  - Mixing DIPs and pixels: by default e.Info is PHYSICAL pixels while
    pointer positions are DIPs. Either set IgnorePixelScaling = true
    (example 2) or multiply by Dpi (example 3). Do both and everything is
    double-scaled.
  - Assuming e.Info == e.RawInfo: with IgnorePixelScaling they differ (Info
    in DIPs, RawInfo in pixels). Use RawInfo when you need the buffer size.
  - Expecting automatic repaints: apart from load/resize/DPI/visibility
    changes, only Invalidate() paints.
  - Zero-size element: with no bounded size the canvas never paints and
    CanvasSize stays empty. Give it a star row or Height/MinHeight.
  - Calling Invalidate() in a tight loop from a background thread: each call
    is marshalled to the UI thread and paints a full frame; coalesce them.
  - Also referencing SkiaSharp.Views.WinUI (or another SkiaSharp views
    package): duplicate SkiaSharp.Views.Windows types, CS0433.
  - Looking for WindowsExtensions: the helper class here is UWPExtensions
    (same methods for Point/Rect/Size/Color; no bitmap helpers).
  - Using SKPaintGLSurfaceEventArgs in the hope of GPU access: nothing in
    this package raises it; the GPU path is SkiaGLPaintSurfaceEventArgs
    from Graphics3DGL.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does not provide a working SKSwapChainPanel, SKGLView or any GPU
    surface; SKSwapChainPanel is a throwing/no-op placeholder here.
  - It does not include the upstream WriteableBitmap conversion helpers
    (ToWriteableBitmap, ToSKBitmap, ToSKImage, ToSKPixmap). To show Skia
    output in an Image, paint it in an SKXamlCanvas, or read the SKImage's
    pixels into a WriteableBitmap's PixelBuffer yourself.
  - It does not contain SkiaSharp itself (SKCanvas, SKPaint, ...); that is
    the SkiaSharp package, pulled in as a dependency.
  - It does not contain SKCanvasElement (Graphics2DSK) or the GL elements
    (Graphics3DGL), and the Graphics2DSK package does not depend on this
    one.
  - It does not offer a render loop, vsync, or background-thread painting
    (DrawInBackground throws NotImplementedException; EnableRenderLoop is
    inert).
  - It does not bundle Lottie or SVG rendering — those are the separate
    Lottie and Svg add-ins, which depend on this package.

HOW THIS PACKAGE IS VERIFIED
===========================
  src/AddIns/CodeBrix.Platform.SkiaSharp.Views.Tests is this add-in's unit-test
  suite, registered in the Tests folder of all three solutions. It runs with no
  application head: it constructs an SKXamlCanvas, lays it out, invalidates it,
  and reads the presented pixels back out of the WriteableBitmap the control
  paints through, so the whole paint-and-present path is measured, not just the
  API shape. It also pins the managed/native SkiaSharp agreement, the tie
  between this add-in's version and the SkiaSharp it vendors, the conversions in
  UWPExtensions, SKSwapChainPanel's unsupported behaviour, the control at a
  scaled display, and the opt-in direct present path.

  Run it after any change to the vendored sources or to the SkiaSharp pin:

      dotnet test src/AddIns/CodeBrix.Platform.SkiaSharp.Views.Tests/CodeBrix.Platform.SkiaSharp.Views.Skia.Unit.Tests.csproj -c Release

  What it cannot cover without a head: Loaded/Unloaded (never raised outside a
  visual tree), a live DPI change from the system, and the on-screen present of
  the ImageBrush through the compositor. Those stay a head-level check.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/EmulateFrameBufferDemo
      src/EmulateFrameBufferDemo.UI/Views/MainPage.xaml declares
      <skia:SKXamlCanvas x:Name="Sketch" PaintSurface="OnSketchPaintSurface"
      PointerPressed=... /> as a free-hand sketch pane; MainPage.xaml.cs
      shows the PaintSurface handler (drawing.Render(e.Surface, e.Info)),
      pointer capture and Sketch.Invalidate() after each stroke change —
      next to a GLCanvasElement pane from the Graphics3DGL package.

QUICK REFERENCE CARD
====================
    Package:    CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever  (.Core)
                version follows the vendored SkiaSharp release (reference unversioned)
    Namespace:  SkiaSharp.Views.Windows        XAML: xmlns:skia="using:SkiaSharp.Views.Windows"

    SKXamlCanvas : Canvas                       <skia:SKXamlCanvas PaintSurface="..."/>
      PaintSurface (SKPaintSurfaceEventArgs)    e.Surface.Canvas, e.Info, e.RawInfo
      Invalidate()                              paints NOW on the UI thread (marshals if needed)
      IgnorePixelScaling                        false: pixels (default) / true: DIPs, pre-scaled
      Dpi                                       scale factor;  CanvasSize: last painted size
      Always canvas.Clear() first; buffer is reused.
    SKSwapChainPanel : FrameworkElement         PLACEHOLDER: ctor throws NotSupportedException
      RaiseOnUnsupported (static)               false -> silent, still never paints
      -> use SkiaGLCanvasElement (Graphics3DGL) for GPU Skia
    SKPaintGLSurfaceEventArgs                   compile-compat only; never raised here
    UWPExtensions                               ToSKPoint/ToPoint, ToSKRect/ToRect,
                                                ToSKSize/ToSize, ToSKColor/ToColor
    Host flag (core):  .UseDirectSkiaCanvasMode()   EXPERIMENTAL, app-wide, one-way,
                                                    SKXamlCanvas only
    Siblings:   SKCanvasElement     -> CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever
                SkiaGLCanvasElement -> CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
