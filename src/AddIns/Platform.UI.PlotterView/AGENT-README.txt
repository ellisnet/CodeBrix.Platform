================================================================================
AGENT-README: CodeBrix.Platform.PlotterView
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.PlotterView.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.PlotterView is a chart view for CodeBrix.Platform
applications. Its one application-facing type, PlotterControl (a XAML Control
that implements the CodeBrix.Plotter IPlotView contract), hosts a
CodeBrix.Plotter PlotModel - the CodeBrix port of OxyPlot: 40+ series types,
linear / logarithmic / date-time / category / polar axes, annotations and
legends - on a Skia surface, on every head the framework has: Windows (Win32
and Skia-on-WPF), Linux (X11, Wayland, FrameBuffer) and macOS.

The library's FULL interaction model is wired in out of the box:
  - pan:      right-drag, or the arrow keys
  - zoom:     mouse wheel, the + / - keys, or a middle-drag zoom rectangle
  - tracker:  left-click shows a data-point tracker (Ctrl+left for free
              tracking)
  - reset:    double-middle-click, or the A / Home keys
  - touch:    single-finger pan, two-finger pinch zoom
Every one of those bindings can be rebound or removed through the Controller
property, which takes a CodeBrix.Plotter PlotController.

Every piece of chart text renders through the APPLICATION's fonts, never the
host system's - the framework-wide "no system font fallback" rule. Text is
shaped by the plot engine itself (SkiaSharp.HarfBuzz), so this package has no
TextLayout dependency.

Target: .NET 10 or later, inside a CodeBrix.Platform application (the control
is a XAML element and needs the visual tree of one of the heads).

Provenance: new code written against the CodeBrix.Plotter view contract
(IPlotView + IPlotController). The interaction semantics - which handler
feeds which controller method, what the tracker and zoom rectangle mean -
follow the view controls of the upstream project the plotting library was
ported from, but no upstream view code was ported.


INSTALLATION
============
Package id:   CodeBrix.Platform.PlotterView.ApacheLicenseForever

    dotnet add package CodeBrix.Platform.PlotterView.ApacheLicenseForever

Reference it from the shared UI project of your application - the project
that already references CodeBrix.Platform.ApacheLicenseForever - not from
the per-platform head projects.

NuGet dependencies (all flow in automatically):
  - CodeBrix.Platform.ApacheLicenseForever   the framework itself
  - CodeBrix.Plotter.MitLicenseForever       the plotting engine (PlotModel,
                                             series, axes, controller,
                                             exporters)
  - SkiaSharp                                declared directly by this package
                                             (and also carried by the framework
                                             and by CodeBrix.Plotter)
  - SkiaSharp.HarfBuzz                       (through CodeBrix.Plotter)
Deliberately NOT a dependency: CodeBrix.Platform.TextLayout - the plot engine
shapes its own text.

License: Apache-2.0 (the CodeBrix.Plotter dependency is MIT).

Requirements: a running CodeBrix.Platform application head. There is no
head-specific setup; the control works on all six heads the same way.


KEY NAMESPACES / USINGS
=======================
XAML - either form works (the demo uses the second):

    xmlns:plot="using:CodeBrix.Platform.UI.PlotterView"
    xmlns:plot="clr-namespace:CodeBrix.Platform.UI.PlotterView;assembly=CodeBrix.Platform.UI.PlotterView"

Code:

    using CodeBrix.Platform.UI.PlotterView;   // PlotterControl
    using CodeBrix.Plotter;                   // PlotModel, PlotController,
                                              // IPlotController, PlotCommands,
                                              // PlotterColor, PlotterColors,
                                              // PlotterRect, TrackerHitResult,
                                              // CursorType, PlotterMouseButton,
                                              // PlotterKey, DataPoint
    using CodeBrix.Plotter.Series;            // LineSeries, ScatterSeries, ...
    using CodeBrix.Plotter.Axes;              // LinearAxis, CategoryAxis, ...
    using CodeBrix.Plotter.Legends;           // Legend
    using CodeBrix.Plotter.Skia;              // PngExporter, SvgExporter, ...

PlotterColor and PlotterColors live in the ROOT CodeBrix.Plotter namespace
(as do PlotterRect, PlotterSize, ScreenPoint and the input enumerations).
There is no OxyPlot namespace and no Oxy* type anywhere in the family.

Naming clash to know about: CodeBrix.Plotter.HorizontalAlignment and
CodeBrix.Plotter.VerticalAlignment collide with the XAML layout enums of the
same names. Code-behind that imports both CodeBrix.Plotter and
Microsoft.UI.Xaml must alias one of them:

    using PlotterHorizontalAlignment = CodeBrix.Plotter.HorizontalAlignment;

The helper namespaces CodeBrix.Platform.UI.PlotterView.Input and
CodeBrix.Platform.UI.PlotterView.Rendering exist (see "Helper types" below)
but an application never needs to import them.


CORE API REFERENCE
==================

PlotterControl
--------------
    public sealed partial class PlotterControl : Control, IPlotView

    public PlotterControl()
        Creates the control with no model. IsTabStop is true so the key
        bindings can receive focus; a pointer press on the plot focuses it.

Properties:

    public static readonly DependencyProperty ModelProperty
    public PlotModel? Model { get; set; }
        The plot to show. A dependency property, so it can be bound. Setting
        it detaches the previous model (if any), attaches the new one to this
        view, clears any tracker / zoom rectangle, and schedules a full
        update. A PlotModel can be attached to ONE view at a time.

    public IPlotController? Controller { get; set; }
        The controller that maps input gestures onto plot commands. Null (the
        default) means a standard PlotController with the stock bindings.
        Assign a customized controller to change them. Read at every input
        event, so it can be swapped at any time.

    public string? PlotFontFamily { get; set; }
        The font that plot text renders in when the model names no loadable
        application font: an application font URI such as
        "ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/Roboto.ttf". Null
        (the default) means the application's default font. Changing it
        resets the typeface cache and repaints.

    public PlotterColor TrackerBackground { get; set; }
        Tracker box fill. Default PlotterColor.FromArgb(0xE6, 0x2D, 0x2D,
        0x30) - near-opaque dark gray.
    public PlotterColor TrackerForeground { get; set; }
        Tracker text and border color. Default PlotterColors.White.
    public double TrackerFontSize { get; set; }
        Tracker text size in DIPs. Default 12; values below 4 clamp to 4.
    public PlotterColor ZoomRectangleFill { get; set; }
        Default PlotterColor.FromArgb(0x40, 0xFF, 0xFF, 0x00) - translucent
        yellow.
    public PlotterColor ZoomRectangleStroke { get; set; }
        Default PlotterColors.Black.
    (Each of the five setters above repaints immediately.)

IPlotView members the control exposes (a controller, a manipulator, or your
own code can call them):

    public PlotModel? ActualModel { get; }
        The model in effect - the same object as Model.
    public IController ActualController { get; }
        Controller, or the lazily created default PlotController when
        Controller is null. This is what every input handler drives.
    public PlotterRect ClientArea { get; }
        (0, 0, width, height) of the drawing surface in DIPs.
    public void InvalidatePlot(bool updateData = true)
        Schedules a repaint, re-reading the model's data first when
        updateData is true. Safe from ANY thread. This is the method that
        PlotModel.InvalidatePlot reaches, so calling either is equivalent.
        Requests coalesce: any number of calls between two paints produce
        one Update (with updateData true if any caller asked for it) and one
        Render.
    public void ShowTracker(TrackerHitResult trackerHitResult)
    public void HideTracker()
        Show / hide the tracker box. The text comes from
        TrackerHitResult.Text; the box is centered above the tracked point
        with a small gap, flips below when there is no room above, and is
        clamped into the client area. It renders in the model's DefaultFont
        (resolved to an application font like all other text) at
        TrackerFontSize, normal weight.
    public void ShowZoomRectangle(PlotterRect rectangle)
    public void HideZoomRectangle()
        Show / hide the zoom rectangle overlay (ZoomRectangleFill /
        ZoomRectangleStroke, 1 DIP border).
    public void SetCursorType(CursorType cursorType)
        Maps the plot cursor onto the framework cursor: Pan -> SizeAll,
        ZoomRectangle -> Cross, ZoomHorizontal -> SizeWestEast,
        ZoomVertical -> SizeNorthSouth, Default -> no override (the ambient
        cursor).
    public void SetClipboardText(string text)
        Places text on the framework clipboard (SetContent + Flush, so it
        outlives the app). A transiently unavailable clipboard is swallowed;
        the copy simply does not take.

Input mapping facts (what the control does before it calls the controller):
  - Mouse buttons: left, middle, right, XButton1, XButton2 are reported;
    the click count for double-click gestures is computed by the control
    (presses within 500 ms and 4 DIPs of the first press of a run).
  - Modifier keys tracked: Shift, Control, Alt, Windows; they are delivered
    as PlotterModifierKeys on every event, never as key presses.
  - Keys forwarded: A-Z, 0-9, number pad 0-9, F1-F12, and the navigation /
    editing keys the PlotterKey enumeration has (Home included). Keys with
    no PlotterKey equivalent are ignored.
  - Touch: the first finger down starts a gesture (HandleTouchStarted), any
    finger movement reports a delta (HandleTouchDelta - one finger = pan,
    the first two fingers = pinch), the last finger up completes it. A
    cancelled touch or a lost capture completes the gesture cleanly rather
    than leaving a phantom contact.
  - Pointer capture is taken on press so drag manipulators (pan, zoom
    rectangle) keep receiving moves outside the control.
  - e.Handled follows the controller's args.Handled, so an unbound gesture
    bubbles to the parent element.

Rendering facts:
  - The control renders with a CodeBrix.Plotter.Skia SkiaRenderContext set
    to RenderTarget.Screen, UseTextShaping = true, MiterLimit = 10 and
    DpiScale = 1 (the surface is already pre-scaled so one unit is one DIP;
    the display scale is never applied twice).
  - Rendering happens under the model's SyncRoot, which is what makes the
    "mutate on any thread, then InvalidatePlot" pattern safe.
  - A model whose Background is visible clears the surface to it; otherwise
    the surface is transparent and the XAML background behind the control
    shows through.

Font resolution (all chart text, tracker included):
  - A model font family that is an application font URI (ms-appx:///...)
    loads that font.
  - ANY bare family name - including the model default "Segoe UI" - becomes
    the control's plot font: PlotFontFamily if set, otherwise the
    application's default font. A bare name is never looked up on the host.
  - Weight-aware: bold titles resolve like XAML bold text.
  - A font still loading paints with the interim face and swaps on arrival,
    exactly as TextBlock does; the swap re-resolves typefaces and repaints
    layout only (InvalidatePlot(false)).

Controller rebinding
--------------------
PlotController (CodeBrix.Plotter) is a ControllerBase; the binding helpers
are extension methods on IController in the same namespace. Verified
signatures:

    void   ControllerBase.UnbindAll()
    void   ControllerBase.Unbind(PlotterInputGesture gesture)
    void   ControllerBase.Unbind(IViewCommand command)
    void   IController.BindMouseDown(PlotterMouseButton button,
                                     IViewCommand<PlotterMouseDownEventArgs> command)
    void   IController.BindMouseDown(PlotterMouseButton button,
                                     PlotterModifierKeys modifiers,
                                     IViewCommand<PlotterMouseDownEventArgs> command)
    void   IController.BindMouseDown(PlotterMouseButton button,
                                     PlotterModifierKeys modifiers, int clickCount,
                                     IViewCommand<PlotterMouseDownEventArgs> command)
    void   IController.BindMouseWheel(IViewCommand<PlotterMouseWheelEventArgs> command)
    void   IController.BindMouseWheel(PlotterModifierKeys modifiers,
                                      IViewCommand<PlotterMouseWheelEventArgs> command)
    void   IController.BindMouseEnter(IViewCommand<PlotterMouseEventArgs> command)
    void   IController.BindTouchDown(IViewCommand<PlotterTouchEventArgs> command)
    void   IController.BindKeyDown(PlotterKey key,
                                   IViewCommand<PlotterKeyEventArgs> command)
    void   IController.BindKeyDown(PlotterKey key, PlotterModifierKeys modifiers,
                                   IViewCommand<PlotterKeyEventArgs> command)
    void   IController.UnbindMouseDown(PlotterMouseButton button,
                                       PlotterModifierKeys modifiers, int clickCount)
    void   IController.UnbindMouseWheel()
    void   IController.UnbindMouseEnter()
    void   IController.UnbindTouchDown()
    void   IController.UnbindKeyDown(PlotterKey key, PlotterModifierKeys modifiers)

The stock commands are static properties of CodeBrix.Plotter.PlotCommands:
PanAt, PanLeft/Right/Up/Down (+ ...Fine variants), ZoomRectangle, ZoomWheel,
ZoomWheelFine, ZoomIn/ZoomOut (+ ...At and ...Fine variants), Track,
SnapTrack, PointsOnlyTrack, HoverTrack, HoverSnapTrack,
HoverPointsOnlyTrack, PanZoomByTouch, SnapTrackTouch, PointsOnlyTrackTouch,
Reset, ResetAt, CopyCode.

One-line rebinding - pan with the LEFT button and leave everything else
stock:

    var controller = new PlotController();
    controller.UnbindMouseDown(PlotterMouseButton.Left, PlotterModifierKeys.None, 1);
    controller.BindMouseDown(PlotterMouseButton.Left, PlotCommands.PanAt);
    Plotter.Controller = controller;

A read-only chart (no interaction at all):

    var frozen = new PlotController();
    frozen.UnbindAll();
    Plotter.Controller = frozen;

Keyboard-only reset on Home, keeping wheel zoom:

    var c = new PlotController();
    c.UnbindAll();
    c.BindKeyDown(PlotterKey.Home, PlotCommands.Reset);
    c.BindMouseWheel(PlotCommands.ZoomWheel);
    Plotter.Controller = c;

Helper types (public, internal-purpose)
---------------------------------------
These are public so they can be unit-tested in isolation; they are NOT
extension points. The control calls its own instances directly and never
consults a replacement, and their shape may change without notice.

    CodeBrix.Platform.UI.PlotterView.Input
        ClickCounter            int Register(long timestampMilliseconds,
                                             double x, double y); void Reset();
                                int MaximumIntervalMilliseconds (500);
                                double MaximumDistance (4.0)
        TouchGestureTracker     bool Down(uint pointerId, ScreenPoint position);
                                bool Move(uint pointerId, ScreenPoint position,
                                          out ScreenPoint[] current,
                                          out ScreenPoint[] previous);
                                bool Up(uint pointerId); void Clear();
                                ScreenPoint[] Snapshot(); int Count
        PointerButtonMapper     static PlotterMouseButton
                                    ToMouseButton(PointerUpdateKind kind)
        VirtualKeyMapper        static PlotterKey ToPlotterKey(VirtualKey key)
    CodeBrix.Platform.UI.PlotterView.Rendering
        CursorTypeMapper        static InputSystemCursorShape?
                                    ToCursorShape(CursorType cursorType)
        TrackerBoxLayout        static PlotterRect Calculate(ScreenPoint anchor,
                                    PlotterSize contentSize, double padding,
                                    double gap, PlotterRect clientArea)

Export
------
This control does not export. Export the SAME PlotModel with the SkiaSharp
exporters in CodeBrix.Plotter.Skia - PngExporter, JpegExporter, PdfExporter,
SvgExporter:

    using CodeBrix.Plotter.Skia;
    var exporter = new PngExporter { Width = 800, Height = 480, Dpi = 96 };
    using var stream = File.Create("plot.png");
    exporter.Export(model, stream);

The application-font routing described above belongs to THIS control's
render context; an exporter builds its own. If exported text must use the
same fonts, see the TypefaceResolver section of the CodeBrix.Plotter
AGENT-README:
    https://github.com/ellisnet/CodeBrix.Plotter/blob/main/AGENT-README.txt
That file is also the reference for everything model-side: series, axes,
annotations, legends, palettes, the controller and command model, and the
IRenderContext / SkiaRenderContext drawing abstraction.


COMPLETE EXAMPLES
=================

1. Show a plot and update it from a timer (the demo's streaming chart)
-----------------------------------------------------------------------
XAML (a bounded star cell is the recommended host):

    <Page ...
          xmlns:plot="using:CodeBrix.Platform.UI.PlotterView">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <Button Grid.Row="0" Content="Reset view" Click="ResetView_Click" />
            <plot:PlotterControl x:Name="Plotter" Grid.Row="1" Margin="8" />
        </Grid>
    </Page>

Code-behind:

    using CodeBrix.Plotter;
    using CodeBrix.Plotter.Axes;
    using CodeBrix.Plotter.Series;
    using Microsoft.UI.Xaml;

    private readonly PlotModel _model = new() { Title = "Live Signal" };
    private readonly LineSeries _channel = new() { Title = "Channel A" };
    private readonly DispatcherTimer _timer = new()
        { Interval = TimeSpan.FromMilliseconds(35) };
    private double _t;

    public MainPage()
    {
        InitializeComponent();

        _model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Seconds" });
        _model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Volts" });
        _model.Series.Add(_channel);

        Plotter.Model = _model;                  // attaches the model to this view

        _timer.Tick += (_, _) =>
        {
            _t += 0.035;
            _channel.Points.Add(new DataPoint(_t, Math.Sin(2 * Math.PI * _t)));
            _channel.Points.RemoveAll(p => p.X < _t - 10);   // keep a 10 s window
            _model.InvalidatePlot(true);         // data changed: re-read and repaint
        };

        Loaded += (_, _) => { _timer.Start(); Plotter.Focus(FocusState.Programmatic); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        // The same effect as the controller's reset bindings (double-middle-click, A, Home)
        Plotter.ActualModel?.ResetAllAxes();
        Plotter.InvalidatePlot(false);           // view changed, data did not
        Plotter.Focus(FocusState.Programmatic);
    }

2. Update from a background thread
----------------------------------
Mutate under the model's SyncRoot (the control renders under the same
lock), then invalidate from wherever you are:

    Task.Run(() =>
    {
        while (running)
        {
            var sample = ReadSensor();
            lock (_model.SyncRoot)
            {
                _channel.Points.Add(sample);
            }
            _model.InvalidatePlot(true);         // thread-safe; coalesces
        }
    });

3. Switch between several cached models
---------------------------------------
The Model setter detaches the previous model, so a set of prepared models
can be swapped freely:

    Plotter.Model = _models[index];             // never assign a model that is
                                                // still attached to ANOTHER view

4. Bind the model from a view model
-----------------------------------
    <plot:PlotterControl Model="{x:Bind ViewModel.Chart, Mode=OneWay}" />


MINIMUM VIABLE PROJECT
======================
In an existing CodeBrix.Platform application, add to the shared UI
project's csproj (alongside the framework reference it already has):

    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.PlotterView.ApacheLicenseForever" />
    </ItemGroup>

Then a page:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:plot="using:CodeBrix.Platform.UI.PlotterView">
        <Grid>
            <plot:PlotterControl x:Name="Plotter" />
        </Grid>
    </Page>

    // MainPage.xaml.cs
    using CodeBrix.Plotter;
    using CodeBrix.Plotter.Series;

    public MainPage()
    {
        InitializeComponent();
        var model = new PlotModel { Title = "Hello" };
        model.Series.Add(new FunctionSeries(Math.Sin, -10, 10, 0.05, "sin(x)"));
        Plotter.Model = model;
    }

No head project changes are needed; the package works on all six heads.


PERFORMANCE TIPS
================
  - InvalidatePlot coalesces. Calling it many times between two frames costs
    one Update and one Render, so do not throttle it yourself - but do batch
    the DATA mutation (add a block of points under one lock, then
    invalidate once), because each lock hand-off competes with the paint.
  - Use InvalidatePlot(false) when only the view changed (axes reset, a
    zoom, a color) and the series data did not: it skips the data re-read.
  - The paint pass renders the WHOLE model every time; there is no partial
    redraw. Cost scales with what the model draws, so keep streaming series
    trimmed (the demo removes points that fell out of its 10-second window)
    rather than accumulating history you no longer show.
  - The tracker and zoom-rectangle setters, and the five color / font-size
    properties, each schedule a repaint. Set them once, not per frame.
  - Changing PlotFontFamily resets the typeface cache. Set it at
    construction time, not in response to input.
  - A font that is still loading renders with an interim face and repaints
    once when it arrives; that one extra paint is expected, not a leak.
  - Input handling runs on the UI thread. Long work belongs on a worker
    thread with the SyncRoot pattern above, never inside a command.


COMMON PITFALLS TO AVOID
========================
  - A PlotModel attaches to ONE view at a time. Assigning a model that is
    still attached to another PlotterControl throws; set that control's
    Model to null first.
  - Mutate a model only under its SyncRoot, or from one thread followed by
    InvalidatePlot. Mutating while the control renders (it renders under
    SyncRoot) corrupts the render pass.
  - Give the control a bounded size - a Grid star cell, not a StackPanel or
    an Auto-sized cell. The client area follows the control size.
  - The keyboard bindings need focus. Clicking the plot focuses it; from
    code call Focus(FocusState.Programmatic) (there is no GrabFocus helper
    on this control).
  - "Segoe UI" (the model default) is NOT missing: every bare font name
    resolves to the application font by design. Only an ms-appx:/// URI
    selects a different application font, and there is no system-font path
    at all.
  - PlotterColor is a struct with sentinel values; test with IsUndefined()
    / IsAutomatic() rather than comparing against null.
  - HorizontalAlignment / VerticalAlignment clash between CodeBrix.Plotter
    and the XAML namespaces - alias one side (see KEY NAMESPACES).
  - Controller is IPlotController, not PlotController. To rebind, create a
    PlotController, adjust it, then assign; reading Controller back when it
    was never set returns null, not the default - use ActualController.
  - The legacy input EVENTS of the upstream plotting library (PlotModel
    .MouseDown and friends) do not exist in CodeBrix.Plotter. All input goes
    through the controller and PlotCommands.
  - Exporting is not the control's job; the control is for the screen. Use
    the CodeBrix.Plotter.Skia exporters on the model.


WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does not export images or documents (use the CodeBrix.Plotter.Skia
    exporters on the same PlotModel).
  - It does not lay out text through the framework's TextLayout engine; the
    plot engine shapes its own text via SkiaSharp.HarfBuzz.
  - It does not host more than one model per control, and does not
    synchronise axes between controls.
  - It does not look up fonts on the host system - ever.
  - It does not provide a XAML-declarative model (series and axes are built
    in code or in a view model and bound through Model).
  - It does not raise input events of its own; interaction is the
    controller's.
  - It has no mobile (iOS / Android) or browser head, like the rest of the
    family.


WORKING EXAMPLES ON GITHUB
==========================
  - PlotterViewDemo - six heads; a chart gallery (streaming line, function
    series, scatter clusters, bar chart on a CategoryAxis, pie, heat map
    through the Viridis palette with a LinearColorAxis) plus a "Reset view"
    button that shows the ActualModel / InvalidatePlot(false) pattern. No
    hardware required.
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/PlotterViewDemo
  - The control's source (PlotterControl.cs is the whole application-facing
    surface; Input/ and Rendering/ hold the helper types):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.PlotterView
  - The plotting engine's own AGENT-README (series, axes, annotations,
    palettes, controller, exporters):
    https://github.com/ellisnet/CodeBrix.Plotter/blob/main/AGENT-README.txt


QUICK REFERENCE CARD
====================
    Package:      CodeBrix.Platform.PlotterView.ApacheLicenseForever
                  (+ CodeBrix.Plotter.MitLicenseForever, automatic)
    XAML:         xmlns:plot="using:CodeBrix.Platform.UI.PlotterView"
                  <plot:PlotterControl x:Name="Plotter" />
    Code:         using CodeBrix.Platform.UI.PlotterView;
                  using CodeBrix.Plotter;   // + .Series, .Axes, .Legends, .Skia

    PlotterControl : Control, IPlotView
      Model (DP)               PlotModel?         one view per model
      Controller               IPlotController?   null = stock PlotController
      PlotFontFamily           string?            ms-appx:/// URI or null
      TrackerBackground        PlotterColor       0xE6 2D 2D 30
      TrackerForeground        PlotterColor       White
      TrackerFontSize          double             12 (min 4)
      ZoomRectangleFill        PlotterColor       0x40 FF FF 00
      ZoomRectangleStroke      PlotterColor       Black
      ActualModel              PlotModel?         == Model
      ActualController         IController        Controller ?? default
      ClientArea               PlotterRect        (0,0,w,h) DIPs
      InvalidatePlot(bool updateData = true)      any thread; coalesces
      ShowTracker(TrackerHitResult) / HideTracker()
      ShowZoomRectangle(PlotterRect) / HideZoomRectangle()
      SetCursorType(CursorType)
      SetClipboardText(string)

    Stock gestures:  right-drag pan | wheel zoom | middle-drag zoom box |
                     left-click track | double-middle / A / Home reset |
                     arrows pan | + and - zoom | touch pan + pinch
    Rebind:          var c = new PlotController(); c.UnbindAll();
                     c.BindMouseDown(PlotterMouseButton.Left, PlotCommands.PanAt);
                     Plotter.Controller = c;
    Update pattern:  lock (model.SyncRoot) { mutate } model.InvalidatePlot(true);
    Export:          new PngExporter { Width, Height, Dpi }.Export(model, stream)
                     (CodeBrix.Plotter.Skia; also Jpeg / Pdf / Svg)
    Rules:           bounded size | focus for keys | one view per model |
                     no system fonts, ever
