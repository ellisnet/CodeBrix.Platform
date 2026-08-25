================================================================================
AGENT-README: CodeBrix.Platform.Svg
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.Svg.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.Svg makes the core framework's SvgImageSource actually
render on the Skia desktop heads. The core (CodeBrix.Platform.ApacheLicenseForever)
already defines SvgImageSource, its UriSource / RasterizePixelWidth /
RasterizePixelHeight properties, its Opened / OpenFailed events and the Image
control's handling of it - but the core has no SVG parser. This package plugs
one in: an ISvgProvider implementation (SvgProvider) that parses the SVG bytes
with CodeBrix.SkiaSvg into a Skia picture and hands the Image control a canvas
element that draws that picture, as vectors, at whatever size the Image is
arranged to.

It is an INVISIBLE add-in. Application code never names a type from this
package: you reference it once, then use the core's SvgImageSource in XAML and
code exactly as on Windows. The package registers itself with the core's
ApiExtensibility registry at compile time (see INSTALLATION).

Target: .NET 10 or later. One Skia runtime assembly serves every Skia head.

Provenance: the SVG add-in of the upstream project, re-based on CodeBrix.SkiaSvg
(the CodeBrix port of the Svg.Skia library) instead of the upstream's Svg.Skia
dependency. Namespace CodeBrix.Platform.UI.Svg; do not use the upstream
namespace.

INSTALLATION
============
Package id:   CodeBrix.Platform.Svg.ApacheLicenseForever
License:      Apache-2.0
Assembly:     CodeBrix.Platform.UI.Svg.dll

    dotnet add package CodeBrix.Platform.Svg.ApacheLicenseForever

WHERE: reference it ONCE, in the application's .Core project, next to the core
framework package. Every head inherits it through the .Core project reference.
Never add it to a head project.

NuGet dependencies (pulled automatically):
  - CodeBrix.Platform.ApacheLicenseForever            the core framework
                                                      (SvgImageSource lives here)
  - CodeBrix.SkiaSvg.MitLicenseForever                the SVG parser/renderer
  - CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
  - CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever  the SKCanvasElement the
                                                      picture is drawn on

HOW IT ACTIVATES: the assembly carries
[assembly: ApiExtension(typeof(ISvgProvider), typeof(SvgProvider))]. The XAML
source generator scans every referenced assembly for that attribute while
compiling the application and emits the corresponding
ApiExtensibility.Register(...) call into the generated App code. Every
SvgImageSource constructed afterwards asks the registry for an ISvgProvider and
gets a SvgProvider. There is nothing to call and nothing to configure.

WITHOUT THE PACKAGE: SvgImageSource still exists (it is a core type), but every
instance logs an error - "To use SVG on this platform, make sure to install the
CodeBrix.Platform.WinUI.Svg package." - and the Image stays blank. That message
is the symptom of a missing or mis-placed reference.

Requirements: a Skia head (Windows Win32 or WPF host, Linux X11 / Wayland /
frame buffer, macOS). No system package to install.

KEY NAMESPACES / USINGS
=======================
    using Microsoft.UI.Xaml.Media.Imaging;   // SvgImageSource (core),
                                             // SvgImageSourceLoadStatus,
                                             // SvgImageSourceOpenedEventArgs,
                                             // SvgImageSourceFailedEventArgs
    using Microsoft.UI.Xaml.Controls;        // Image (core)
    using Windows.Storage.Streams;           // IRandomAccessStream,
                                             // InMemoryRandomAccessStream
                                             // (for SetSourceAsync)

XAML: SvgImageSource and Image are in the default XAML namespace; no prefix
and no xmlns for this package are needed.

Only for framework-level code (you will normally never write these):
    using CodeBrix.Platform.UI.Svg;                       // SvgProvider
    using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;    // ISvgProvider (core)

CORE API REFERENCE
==================

SvgImageSource (core; Microsoft.UI.Xaml.Media.Imaging) : ImageSource
--------------------------------------------------------------------
The type application code uses.

    public SvgImageSource();
    public SvgImageSource(Uri uriSource);

    public Uri    UriSource            { get; set; }   // UriSourceProperty
    public double RasterizePixelWidth  { get; set; }   // default NaN (logical px)
    public double RasterizePixelHeight { get; set; }   // default NaN (logical px)

    public IAsyncOperation<SvgImageSourceLoadStatus>
        SetSourceAsync(IRandomAccessStream streamSource);

    public event TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> Opened;
    public event TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> OpenFailed;

    public enum SvgImageSourceLoadStatus { Success, NetworkError, InvalidFormat, Other }
    public partial class SvgImageSourceFailedEventArgs
        { public SvgImageSourceLoadStatus Status { get; } }
    public partial class SvgImageSourceOpenedEventArgs { }   // no members

Semantics (verified in the core and in this package):
  - Setting UriSource unloads any previous SVG and starts loading the new
    one; setting it to null unloads. The bytes are fetched by the core (see
    SOURCE URI FORMS) and handed to SvgProvider.TryLoadSvgDataAsync.
  - Parsing runs on a thread-pool thread. Success raises Opened (and the
    provider's SourceLoaded, which the Image control listens to); a document
    CodeBrix.SkiaSvg cannot parse raises OpenFailed with
    Status = InvalidFormat. A URI that cannot be fetched at all produces no
    picture and the Image stays blank - OpenFailed is NOT raised for a fetch
    failure, only for a parse failure. Treat the Opened / OpenFailed handlers
    as possibly running off the UI thread and dispatch any UI work.
  - SetSourceAsync(stream): throws ArgumentException for a null stream;
    otherwise clones the stream, loads it and completes with the resulting
    status (Success, InvalidFormat, or Other). This is THE way to load an
    embedded resource or any in-memory SVG (example 3).
  - RasterizePixelWidth / RasterizePixelHeight: while BOTH are NaN (the
    default) the SVG is drawn as vectors at the arranged size - crisp at every
    size and DPI. When BOTH are set, the provider pre-renders the picture
    into a bitmap of that logical size (multiplied by the display scale) and
    the Image draws that bitmap stretched to its arranged size. Setting only
    one of the two changes nothing. Changing either value after load
    re-rasterizes immediately.
  - Implicit conversions: assigning a string or Uri whose path ends in ".svg"
    or ".svgz" to an ImageSource-typed property (for example Image.Source in
    XAML: Source="ms-appx:///Assets/logo.svg") creates a SvgImageSource
    automatically; any other extension creates a BitmapImage.

Image (core) with an SvgImageSource on the Skia heads
-----------------------------------------------------
  - The Image asks the source for its canvas (SvgProvider.GetCanvas()) and
    adds it as its child; when the SVG has parsed it measures itself from
    SvgImageSource's intrinsic size (the parsed picture's bounds) and
    arranges the canvas to the size Image.Stretch produces from that.
  - Image.ImageOpened fires when the SVG has parsed; Image.ImageFailed fires
    (message "Failed to load Svg source") when OpenFailed fires.
  - Other ImageSource consumers (an ImageBrush, for instance) do not get the
    vector canvas: they receive a bitmap rendered once at the SVG's intrinsic
    size. Use an Image element wherever sharpness at large sizes matters.

SvgProvider (this package; CodeBrix.Platform.UI.Svg) : ISvgProvider
-------------------------------------------------------------------
Framework-facing. Listed so an agent recognises it; application code does not
construct or call it.

    public SvgProvider(object owner);          // owner must be a SvgImageSource,
                                               // else InvalidOperationException
    public event EventHandler SourceLoaded;    // after a successful parse
    public bool IsParsed { get; }              // a picture is loaded
    public Size SourceSize { get; }            // picture bounds (width, height);
                                               // default(Size) before load
    public UIElement GetCanvas();              // the SKCanvasElement-based
                                               // drawing surface for one owner
    public object TryGetLoadedDataAsPictureAsync();   // the SKPicture, or null
    public Task<bool> TryLoadSvgDataAsync(byte[] svgBytes);   // parse; raises the
                                               // owner's Opened / OpenFailed
    public void Unload();                      // dispose the picture and any
                                               // rasterized bitmap

    ISvgProvider (core; CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg) declares
    exactly these members and is documented in the core as internal plumbing
    whose signature may change - do not implement or call it from an app.

SOURCE URI FORMS
================
SvgImageSource.UriSource is fetched by the core's image pipeline, which
understands these schemes:

  - ms-appx:///Assets/<file>.svg
      A file in the application's install folder (the folder of the entry
      assembly). Ship it as content copied to the output, e.g. in .Core:

          <Content Include="Assets\logo.svg" CopyToOutputDirectory="PreserveNewest" />

      Assets that a library assembly ships in its own folder use the
      ms-appx:///<AssemblyName>/Assets/<file>.svg form (the runtime tests load
      ms-appx:///CodeBrix.Platform.UI.RuntimeTests/Assets/help.svg this way).
  - ms-appdata://local/<file>.svg   (and the other ms-appdata roots)
      A file under the application's data folders.
  - http:// and https://
      Downloaded with HttpClient.
  - file:///absolute/path.svg
      Read from disk.
  - A relative or scheme-less string set from CODE ("Assets/logo.svg",
      "/Assets/logo.svg") is normalised to ms-appx:///Assets/logo.svg. In
      XAML, a value with a leading "/" is likewise prefixed with ms-appx:///,
      but a bare relative value is resolved against the XAML file's own
      location; write the absolute ms-appx:/// form in XAML to avoid surprises.

NOT a URI form for SvgImageSource: embedded://. Unlike the Lottie and
AudioPlayer add-ins, SvgImageSource has no embedded-resource resolver. For an
SVG embedded in an assembly, open the manifest stream yourself and call
SetSourceAsync (example 3) - this is what the JustBetweenUs sample's
EmbeddedImage control does.

COMPLETE EXAMPLES
=================

1. Vector image from an application asset (XAML)
------------------------------------------------
    <Image Width="96" Height="96" Stretch="Uniform">
        <Image.Source>
            <SvgImageSource UriSource="ms-appx:///Assets/logo.svg" />
        </Image.Source>
    </Image>

The short form works too, because a ".svg" path converts to a SvgImageSource:

    <Image Width="96" Height="96" Source="ms-appx:///Assets/logo.svg" />

.Core csproj:

    <ItemGroup>
      <Content Include="Assets\logo.svg" CopyToOutputDirectory="PreserveNewest" />
    </ItemGroup>

2. From code, with success/failure handling
-------------------------------------------
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media.Imaging;

    var svg = new SvgImageSource(new Uri("ms-appx:///Assets/diagram.svg"));
    svg.Opened += (s, e) => Status.Text = "loaded";
    svg.OpenFailed += (s, e) => Status.Text = $"failed: {e.Status}";   // InvalidFormat, ...

    var image = new Image { Source = svg, Width = 400, Stretch = Stretch.Uniform };
    root.Children.Add(image);

3. From an embedded resource (SetSourceAsync)
---------------------------------------------
.Core csproj (RootNamespace "MyApp", assembly "MyApp.Core"):

    <ItemGroup>
      <EmbeddedResource Include="Assets\padlock-icon.svg" />
    </ItemGroup>

Code (the manifest name is <RootNamespace>.<folder>.<file>, dots for
separators):

    using System.Reflection;
    using Windows.Storage.Streams;
    using Microsoft.UI.Xaml.Media.Imaging;

    static async Task<SvgImageSource> LoadEmbeddedSvgAsync(string resourceName)
    {
        var assembly = typeof(App).Assembly;    // or Assembly.Load("MyApp.Core")
        await using var resource = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        var ras = new InMemoryRandomAccessStream();
        var writer = ras.AsStreamForWrite();
        await resource.CopyToAsync(writer);
        await writer.FlushAsync();
        ras.Seek(0);

        var svg = new SvgImageSource();
        var status = await svg.SetSourceAsync(ras);      // SvgImageSourceLoadStatus
        if (status != SvgImageSourceLoadStatus.Success)
            throw new InvalidOperationException($"SVG load failed: {status}");
        return svg;
    }

    // usage
    Logo.Source = await LoadEmbeddedSvgAsync("MyApp.Assets.padlock-icon.svg");

Do not dispose the InMemoryRandomAccessStream right after SetSourceAsync; it is
managed memory and is collected once the source releases it.

4. Rasterize once at a fixed size
---------------------------------
For an icon that is drawn many times at one size, pre-rasterizing trades
vector crispness for a cheaper bitmap blit:

    <Image Width="32" Height="32">
        <Image.Source>
            <SvgImageSource UriSource="ms-appx:///Assets/icon.svg"
                            RasterizePixelWidth="32"
                            RasterizePixelHeight="32" />
        </Image.Source>
    </Image>

Both properties must be set; the display scale is applied for you, so 32
logical pixels becomes a 64-pixel bitmap on a 200 % display.

5. Swapping the image at run time
---------------------------------
    var svg = (SvgImageSource)Logo.Source;
    svg.UriSource = new Uri("ms-appx:///Assets/logo-dark.svg");   // reloads
    svg.UriSource = null;                                           // unloads

MINIMUM VIABLE PROJECT
======================
.Core project fragment (heads reference .Core and add nothing SVG-related):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <AssemblyName>MyApp.Core</AssemblyName>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.Svg.ApacheLicenseForever" />
      </ItemGroup>
      <ItemGroup>
        <Content Include="Assets\logo.svg" CopyToOutputDirectory="PreserveNewest" />
      </ItemGroup>
    </Project>

Page (in the .UI shared project) - no extra xmlns:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        <Image Width="200" Height="200" Stretch="Uniform">
            <Image.Source>
                <SvgImageSource UriSource="ms-appx:///Assets/logo.svg" />
            </Image.Source>
        </Image>
    </Page>

(Use the default XAML namespace your app template already declares; the
JustBetweenUs sample declares it as
clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI.)

PERFORMANCE TIPS
================
  - Vector mode (the default) re-draws the picture every time the Image's
    canvas repaints; the picture itself is parsed once per load. For a large,
    complex SVG shown at a constant size, set RasterizePixelWidth/Height so
    each repaint is a bitmap blit.
  - Parsing happens off the UI thread (Task.Run), so a large document does
    not stall input. Creating many SvgImageSource instances at once is fine;
    each one parses independently (no shared cache), so reuse one source for
    repeated images of the same file.
  - Changing RasterizePixelWidth/Height re-renders the bitmap synchronously
    on the thread that sets the property; do not animate those values.
  - Keep SVGs simple: filters, masks and text are the expensive constructs
    in any Skia-based SVG renderer; see the CodeBrix.SkiaSvg guide for what
    is and is not supported.

COMMON PITFALLS TO AVOID
========================
  - BLANK IMAGE + "make sure to install the CodeBrix.Platform.WinUI.Svg
    package" in the log = the add-in is not referenced by the project chain
    that compiles the app (put it in .Core), or the head is not a Skia head.
  - embedded:// is NOT understood by SvgImageSource. Use SetSourceAsync with
    the manifest stream (example 3).
  - Loading errors do not throw. A bad path or unreachable URI leaves the
    Image blank and raises NOTHING; only a malformed document raises
    OpenFailed (InvalidFormat) and Image.ImageFailed. Verify the path first
    when an image simply never appears.
  - RasterizePixelWidth/Height only take effect when BOTH are non-NaN.
    Also, in the current provider the two values are applied crosswise when
    the bitmap is allocated (the height value sizes the bitmap's width and
    vice versa); use equal values for a square raster unless you have
    verified the result at your aspect ratio.
  - Rasterized output is stretched to the arranged size: a 32 px raster shown
    at 128 px is blurry. Rasterize at the largest size you will show, or stay
    in vector mode.
  - The intrinsic size comes from the parsed picture's bounds. Give the Image
    an explicit Width/Height (or rely on Stretch inside a sized container)
    when the SVG's own width/height/viewBox is not what you want on screen.
  - SetSourceAsync(null) throws ArgumentException.
  - Do not implement ISvgProvider or construct SvgProvider yourself; the core
    documents the interface as internal and subject to change.
  - Never add the package to a head project; .Core only.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It is not an SVG API. It exposes no document model, no element access,
    no hit testing and no export; it renders a whole SVG file into an Image.
    For programmatic SVG work use CodeBrix.SkiaSvg.MitLicenseForever directly
    (it is already a dependency) - see its guide:
        https://github.com/ellisnet/CodeBrix.SkiaSvg/blob/main/AGENT-README.txt
  - It supports the SVG feature subset that CodeBrix.SkiaSvg supports, no
    more. Unsupported elements, filters and CSS constructs are listed in that
    package's "COMMON PITFALLS TO AVOID" and "WHAT THIS LIBRARY DOES NOT DO"
    sections rather than restated here.
  - It does not animate SVGs (SMIL / CSS animation); use the Lottie add-in for
    animation.
  - It does not resolve embedded:// URIs (see SOURCE URI FORMS).
  - It does not cache parsed pictures across SvgImageSource instances.
  - It does not add a WinUI/Windows App SDK head implementation; the
    src-platforms heads use their own SVG assembly and package.

WORKING EXAMPLES ON GITHUB
==========================
  - JustBetweenUs sample (all six Skia heads): an EmbeddedImage control that
    loads .svg embedded resources through SvgImageSource.SetSourceAsync and
    ms-appx / https .svg URIs through UriSource, used by the page's buttons.
      https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/JustBetweenUs
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/samples/CodeBrixPlatform/JustBetweenUs/JustBetweenUs.Core/Controls/EmbeddedImage.cs
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/samples/CodeBrixPlatform/JustBetweenUs/JustBetweenUs.UI/Views/MainPage.xaml
  - Runtime tests loading SvgImageSource from ms-appx:///Assets/... and
    ms-appx:///<AssemblyName>/Assets/... URIs, plus null-URI behaviour
    (When_SVGImageSource, When_SVGImageSource_Uri_Is_Null,
    When_SVGImageSource_Uri_Is_Set_Null):
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/src/Platform.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_Image.cs

QUICK REFERENCE CARD
====================
Package:    CodeBrix.Platform.Svg.ApacheLicenseForever   (reference in .Core)
Assembly:   CodeBrix.Platform.UI.Svg.dll  (invisible: app code uses core types)
Companion:  CodeBrix.SkiaSvg.MitLicenseForever (dependency; the parser)
            https://github.com/ellisnet/CodeBrix.SkiaSvg/blob/main/AGENT-README.txt
See also:   Lottie animation add-in - src/AddIns/Platform.UI.Lottie/AGENT-README.txt

    <Image Width="96" Height="96">
        <Image.Source>
            <SvgImageSource UriSource="ms-appx:///Assets/logo.svg" />
        </Image.Source>
    </Image>

SvgImageSource (core, Microsoft.UI.Xaml.Media.Imaging):
    new SvgImageSource()  new SvgImageSource(Uri)
    UriSource (Uri)   RasterizePixelWidth / RasterizePixelHeight (double, NaN = vector)
    SetSourceAsync(IRandomAccessStream) -> SvgImageSourceLoadStatus
    Opened / OpenFailed (args.Status: Success | NetworkError | InvalidFormat | Other)

URI forms:      ms-appx:///Assets/x.svg   ms-appx:///<Assembly>/Assets/x.svg
                ms-appdata://local/x.svg  http(s)://...   file:///...
                relative -> ms-appx:///   embedded:// -> NOT supported (use SetSourceAsync)

Symptom card:   blank + "install the CodeBrix.Platform.WinUI.Svg package" -> add-in missing
                OpenFailed InvalidFormat -> the parser rejected the document
                blurry -> rasterized smaller than displayed; drop Rasterize* or enlarge
