================================================================================
AGENT-README: CodeBrix.Platform.Lottie
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.Lottie.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.Lottie plays Lottie (Bodymovin JSON) vector animations inside
a CodeBrix.Platform XAML application on the Skia desktop heads. The core
framework already ships the AnimatedVisualPlayer control and the
IAnimatedVisualSource contract it plays; what this package adds is the SOURCE
side of that contract:

  - LottieVisualSource         loads a Lottie JSON file and drives playback,
  - ThemableLottieVisualSource the same, plus run-time recolouring of shapes
                               whose layer names carry "{ Color : var(Name) }"
                               bindings,
  - LottieVisualSourceProvider the registration that lets the core's
                               ProgressRing spin (it draws a Lottie ring).

Frames are decoded by SkiaSharp.Skottie (SkiaSharp.Skottie.Animation) and drawn
on a Skia canvas element that the source adds as the player's child, so the
animation is real vector output at any size. One Skia runtime assembly serves
every Skia head; there is nothing to call at startup - referencing the package
activates it (see INSTALLATION).

Target: .NET 10 or later.

Provenance: a port of the Windows Community Toolkit Lottie library
(Microsoft.Toolkit.Uwp.UI.Lottie) as carried by the upstream project, rebased on
CodeBrix.Platform. The public source types keep the CommunityToolkit.WinUI.Lottie
namespace so existing XAML and code-behind written against the toolkit compile
unchanged; the provider lives in CodeBrix.Platform.UI.Lottie. A small
System.Json (from dotnet/corefx) is vendored for the themable source's JSON
rewriting. Do NOT reference the Microsoft.Toolkit / CommunityToolkit Lottie
NuGet packages themselves - this package replaces them.

INSTALLATION
============
Package id:   CodeBrix.Platform.Lottie.ApacheLicenseForever
License:      Apache-2.0
Assembly:     CodeBrix.Platform.UI.Lottie.dll

    dotnet add package CodeBrix.Platform.Lottie.ApacheLicenseForever

WHERE: reference it ONCE, in the application's .Core project (the project that
holds the framework and extension packages). Every head inherits it through
the .Core project reference; never add it to a head project.

NuGet dependencies (pulled automatically):
  - CodeBrix.Platform.ApacheLicenseForever            the core framework
  - CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
  - CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever  the SKCanvasElement the
                                                      animation is drawn on
  - SkiaSharp.Skottie                                 the Lottie decoder

HOW IT ACTIVATES: the assembly carries an [assembly: ApiExtension(...)]
registration for ILottieVisualSourceProvider. The XAML source generator scans
every referenced assembly for that attribute while compiling the application
and emits the ApiExtensibility.Register(...) call into the generated App code,
so the mere reference wires the core's ProgressRing to this package. The
LottieVisualSource types themselves are ordinary classes you use directly.

BUILD-TIME CHECK: the package's MSBuild targets run in each head project and
fail the build if the SkiaSharp.Skottie assembly is not among the head's
references. The package's own dependency normally satisfies this; if the error
appears (for example after a package-reference cleanup), add SkiaSharp.Skottie
to .Core explicitly, or set the MSBuild property
CodeBrixDisableLottieSkiaVersionCheck=true to skip the check.

Requirements: a Skia head (Windows Win32 or WPF host, Linux X11 / Wayland /
frame buffer, macOS). No system package to install.

KEY NAMESPACES / USINGS
=======================
    using CommunityToolkit.WinUI.Lottie;   // LottieVisualSource,
                                           // ThemableLottieVisualSource,
                                           // LottieVisualSourceBase,
                                           // LottieVisualOptions
    using Microsoft.UI.Xaml.Controls;      // AnimatedVisualPlayer (core),
                                           // IAnimatedVisualSource,
                                           // IThemableAnimatedVisualSource,
                                           // ILottieVisualSourceProvider
    using CodeBrix.Platform.UI.Lottie;     // LottieVisualSourceProvider
                                           // (only if you call it yourself)

XAML prefix for the source types (AnimatedVisualPlayer is in the default XAML
namespace and needs no prefix):

    xmlns:lottie="using:CommunityToolkit.WinUI.Lottie"

The assembly-qualified form is equivalent and is what the JustBetweenUs sample
uses:

    xmlns:lottie="clr-namespace:CommunityToolkit.WinUI.Lottie;assembly=CodeBrix.Platform.UI.Lottie"

Also public, but NOT for application code: the vendored System.Json types
(JsonValue, JsonObject, JsonArray, JsonPrimitive, JsonType in namespace
System.Json). They are the themable source's internal document model and are
public only because the upstream sources were. Use System.Text.Json in your app.

CORE API REFERENCE
==================

AnimatedVisualPlayer (core; Microsoft.UI.Xaml.Controls) : FrameworkElement
-------------------------------------------------------------------------
The player you put in XAML. Its content property is Source, so a source can be
written as the element's child.

    public IAnimatedVisualSource Source { get; set; }    // the Lottie source
    public bool     AutoPlay        { get; set; }        // default true
    public Stretch  Stretch         { get; set; }        // default Uniform
    public double   PlaybackRate    { get; set; }        // default 1.0
    public DataTemplate FallbackContent { get; set; }    // stored, never shown
    public bool     IsPlaying              { get; }      // read-only, bindable
    public bool     IsAnimatedVisualLoaded { get; }      // read-only, bindable
    public TimeSpan Duration               { get; }      // read-only, set on load

    public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped);
    public void Pause();
    public void Resume();
    public void Stop();
    public void SetProgress(double progress);

Dependency properties exist for every property above (SourceProperty,
AutoPlayProperty, StretchProperty, PlaybackRateProperty, IsPlayingProperty,
IsAnimatedVisualLoadedProperty, DurationProperty, FallbackContentProperty).

Semantics (verified in the core source):
  - Every method forwards to Source; with no Source they are no-ops.
  - PlayAsync starts playback and RETURNS AN ALREADY-COMPLETED action. It does
    not wait for the animation to reach toProgress. Use IsPlaying (a
    dependency property - bind it or register a property-changed callback) to
    observe the end of a non-looped segment.
  - Loading is triggered when the player is loaded into the visual tree
    (Source.Update(player) + Source.Load()); unloading pauses playback and
    reloading resumes it. Setting Source, AutoPlay, Stretch or PlaybackRate on
    an already-loaded player calls Source.Update(player) again.
  - Not implemented on the Skia heads (marked [NotImplemented]): Diagnostics,
    AnimationOptimization and ProgressObject (the last throws
    NotImplementedException when read).

IAnimatedVisualSource / IThemableAnimatedVisualSource (core)
------------------------------------------------------------
    public interface IAnimatedVisualSource
    {
        void Update(AnimatedVisualPlayer player);
        void Load();
        void Unload();
        void Play(double fromProgress, double toProgress, bool looped);
        void Stop();
        void Pause();
        void Resume();
        void SetProgress(double progress);
        Size Measure(Size availableSize);
        IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor,
                                                out object diagnostics);
    }

    public interface IThemableAnimatedVisualSource : IAnimatedVisualSource
    {
        void   SetColorThemeProperty(string propertyName, Color? color);
        Color? GetColorThemeProperty(string propertyName);
    }

    public interface ILottieVisualSourceProvider
    {
        IAnimatedVisualSource         CreateFromLottieAsset(Uri sourceFile);
        IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile);
        bool TryCreateThemableFromAnimatedVisualSource(
            IAnimatedVisualSource animatedVisualSource,
            out IThemableAnimatedVisualSource themableAnimatedVisualSource);
    }

LottieVisualSourceBase (CommunityToolkit.WinUI.Lottie) : DependencyObject
-------------------------------------------------------------------------
Abstract base of both sources; implements IAnimatedVisualSource.

    public Uri UriSource { get; set; }                  // UriSourceProperty
    public LottieVisualOptions Options { get; set; }    // OptionsProperty;
                                                        // [NotImplemented]: stored, ignored
    public Task SetSourceAsync(Uri sourceUri);          // sets UriSource; the
                                                        // returned Task is already
                                                        // complete - it does NOT
                                                        // await the load
    public static LottieVisualSource CreateFromString(string uri);
                                                        // [NotImplemented]: throws
                                                        // NotImplementedException
    public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor,
                                                   out object diagnostics);
                                                        // [NotImplemented]: throws

    public void Update(AnimatedVisualPlayer player);   // (re)load for this player
    public void Load();                                 // resume if it was playing
                                                        // when Unload() ran
    public void Unload();                               // pause + remember
    public void Play(double fromProgress, double toProgress, bool looped);
    public void Stop();
    public void Pause();
    public void Resume();
    public void SetProgress(double progress);           // clamped to 0..1; stops
                                                        // playback and shows that frame

    public delegate void UpdatedAnimation(string animationJson, string cacheKey);
    protected abstract bool IsPayloadNeedsToBeUpdated { get; }
    protected virtual IDisposable LoadAndObserveAnimationData(
        IInputStream sourceJson, string sourceCacheKey, UpdatedAnimation updateCallback);

Changing UriSource on a source that is attached to a player reloads it (an
equal Uri is ignored). Play() before the JSON has arrived is remembered and
applied as soon as the animation is decoded.

LottieVisualSource (CommunityToolkit.WinUI.Lottie) : LottieVisualSourceBase
---------------------------------------------------------------------------
    [Bindable] public partial class LottieVisualSource : LottieVisualSourceBase

The plain source: no extra members. It loads embedded://, ms-appx:/// and
ms-appdata:// URIs (see SOURCE URI FORMS); it does NOT download http(s) URIs.

ThemableLottieVisualSource (CommunityToolkit.WinUI.Lottie)
----------------------------------------------------------
    [Bindable] public partial class ThemableLottieVisualSource
        : LottieVisualSourceBase, IThemableAnimatedVisualSource

    public void   SetColorThemeProperty(string propertyName, Color? color);
    public Color? GetColorThemeProperty(string propertyName);

Parses the JSON into a document, finds every shape whose "nm" (name) carries a
Color binding, and rewrites that shape's colour ("c"."k") whenever
SetColorThemeProperty is called; the rewritten JSON is re-decoded by Skottie
and playback continues with the current play state. Details in THEMING below.
This source DOES download http(s) URIs.

LottieVisualSourceProvider (CodeBrix.Platform.UI.Lottie) : ILottieVisualSourceProvider
--------------------------------------------------------------------------------
    public LottieVisualSourceProvider(object owner);
    public IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);
        // => new LottieVisualSource { UriSource = sourceFile }
    public IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile);
        // => new ThemableLottieVisualSource { UriSource = sourceFile }
    public bool TryCreateThemableFromAnimatedVisualSource(
        IAnimatedVisualSource animatedVisualSource,
        out IThemableAnimatedVisualSource themableAnimatedVisualSource);
        // true for a ThemableLottieVisualSource (returned as-is) or a
        // LottieVisualSource (a themable twin with the same UriSource is
        // created); false for anything else

Registered with the core as the ILottieVisualSourceProvider extension. Resolve
it the way the core does when you need a source without naming the concrete
type:

    if (ApiExtensibility.CreateInstance<ILottieVisualSourceProvider>(this, out var provider))
        player.Source = provider.CreateThemableFromLottieAsset(uri);
    // ApiExtensibility: namespace CodeBrix.Platform.Foundation.Extensibility

LottieVisualOptions (CommunityToolkit.WinUI.Lottie)
--------------------------------------------------
    public enum LottieVisualOptions { None = 0, Optimize = 1,
                                      IncludeDiagnostics = 2, All = 3 }

Present for source compatibility only; the whole enum is [NotImplemented] and
the Options property has no effect on the Skia heads.

SOURCE URI FORMS
================
UriSource is resolved in this order (LottieVisualSourceBase):

  1. embedded://<AssemblyName>/<Manifest.Resource.Name>
     An embedded resource. The host part is the simple assembly name (loaded
     with Assembly.Load; "." means the assembly that contains your App class);
     the path part, minus its leading "/", is the manifest resource name, and
     the literal token "(assembly)" inside it is replaced by that assembly's
     name. The manifest name follows the normal MSBuild rule
     <RootNamespace>.<folder>.<file> with path separators turned into dots,
     so this .Core csproj line

         <EmbeddedResource Include="..\Shared\Assets\star_icon.json"
                           Link="Assets\star_icon.json" />

     in a project whose RootNamespace is "JustBetweenUs" (assembly
     JustBetweenUs.Core) is addressed as

         embedded://JustBetweenUs.Core/JustBetweenUs.Assets.star_icon.json

     A missing resource logs a warning ("Unable to find embedded resource
     named '...'") and the player stays empty. Use exactly this form in XAML;
     the XAML editor may flag the URI, but it is correct.

  2. ms-appx:///Assets/<file>.json
     A file in the application's install folder (the folder of the entry
     assembly). Ship it as content copied to the output, e.g. in .Core:

         <Content Include="Assets\anim.json" CopyToOutputDirectory="PreserveNewest" />

     Assets that a library assembly ships in its own folder use the
     ms-appx:///<AssemblyName>/Assets/<file> form.

  3. ms-appdata://local/... (and the other ms-appdata roots)
     A file under the application's data folders, opened from disk.

  4. http:// and https://
     Downloaded with HttpClient - but ONLY by ThemableLottieVisualSource. A
     plain LottieVisualSource treats a web URI as unloadable (a
     NotSupportedException "Failed to load animation" is logged; nothing is
     thrown to your code).

Not supported by either source: file:// URIs and relative URIs. Read the file
yourself and embed it, copy it under ms-appdata, or use one of the forms above.

PLAYBACK MODEL
==============
Verified from the Skottie implementation:

  - After the JSON is decoded, Player.Duration is set from the animation and
    Player.IsAnimatedVisualLoaded becomes true (when Duration > 0). Then a
    pending Play() (issued before the load finished) is applied; otherwise,
    if Player.AutoPlay is true, Play(0, 1, looped: true) starts.
  - Play(from, to, looped) creates a DispatcherQueue timer on the CURRENT
    thread with interval max(1/120 s, 1/Fps of the animation) and restarts a
    stopwatch. Each tick invalidates the canvas; the frame shown is
    (elapsed + from * Duration) * Player.PlaybackRate. PlaybackRate is read
    every frame, so changing it takes effect immediately.
  - Reaching to * Duration: looped -> the stopwatch restarts (seamless loop);
    not looped -> the source calls Stop() and the frame at "to" stays visible.
  - Pause() halts timer and stopwatch (IsPlaying = false); Resume() continues
    from the same frame. Stop() clears the play state; the natural end of a
    segment is the only Stop() that leaves the last frame showing.
  - SetProgress(p) clamps p to 0..1, stops playback and repaints that frame:
    the scrubber primitive.
  - Layout: Measure honours Player.Stretch - Stretch.None reports the
    animation's own size; with one infinite dimension the other is derived
    from the aspect ratio; both infinite returns the natural size. Rendering
    scales by the Stretch mode and centres the animation in the player.
  - Unloading the player from the tree pauses a playing animation and
    reloading resumes it (Unload/Load).

Threading: Play() must run on the UI thread (it creates the timer for the
calling thread). Stop() marshals itself to the dispatcher when called from
another thread; Pause/Resume/SetProgress do not - call them on the UI thread.

THEMING (ThemableLottieVisualSource)
====================================
Bindings live in the shape NAMES inside the Lottie file (the "nm" property of
a shape), using the toolkit's CSS-like syntax:

    { Color : var(Foreground) }
    { Color : var(Foreground); Color : var(Accent) }

Rules verified in the parser and the source:
  - Only the property name "Color" is honoured; other property names parse
    but are ignored.
  - The walk covers the document's top-level "layers", each layer's "shapes"
    and nested "gr" (group) items recursively. Shapes inside precomposition
    assets ("assets") are NOT visited, so put bound shapes on ordinary layers.
  - SetColorThemeProperty("Foreground", color) rewrites the "c"."k" colour
    array of every bound shape to [r, g, b, a] in 0..1 floats, rebuilds the
    JSON and re-decodes the animation; the current play state (segment,
    looping) is re-applied. Passing null clears the pending value; the shapes
    keep their last applied colour.
  - It may be called BEFORE the JSON has loaded: the value is stored and
    applied on load.
  - GetColorThemeProperty returns the pending value if one is set, else the
    last applied value, else null.
  - Colours flow through Windows.UI.Color (alpha included).

Because every change re-parses and re-decodes the animation, treat colour
changes as theme-level events, not per-frame animation.

THE CORE'S PROGRESSRING
=======================
ProgressRing (core) resolves ILottieVisualSourceProvider through
ApiExtensibility in its constructor and plays two Lottie files through an
AnimatedVisualPlayer named "LottiePlayer" in its template. Facts a consumer
needs:

  - WITHOUT this package the ring renders nothing and an error is logged
    saying the ProgressRing control needs an additional package. Referencing
    CodeBrix.Platform.Lottie.ApacheLicenseForever in .Core is the fix.
  - The ring's colours come from the control's Foreground and Background
    brushes, pushed into the themable source as the bindings "Foreground" and
    "Background" (solid-colour brushes only).
  - The animations are swappable: ProgressRing.IndeterminateSource and
    ProgressRing.DeterminateSource (both IAnimatedVisualSource dependency
    properties) accept your own LottieVisualSource / ThemableLottieVisualSource;
    a plain LottieVisualSource is upgraded to a themable one automatically so
    the Foreground/Background bindings still apply if your file has them.
  - The default files are embedded in the core and addressed by
    FeatureConfiguration.ProgressRing.ProgressRingAsset and
    .DeterminateProgressRingAsset (both Uri, settable at startup; namespace
    CodeBrix.Platform.UI). Point them at your own embedded:// or ms-appx:///
    JSON to re-skin every ring in the app.

COMPLETE EXAMPLES
=================

1. Autoplaying looped animation from an embedded resource (XAML)
----------------------------------------------------------------
.Core csproj (RootNamespace "MyApp", assembly "MyApp.Core"):

    <ItemGroup>
      <EmbeddedResource Include="Assets\star_icon.json" />
    </ItemGroup>

Page XAML:

    <Page ...
          xmlns:lottie="using:CommunityToolkit.WinUI.Lottie">
        <AnimatedVisualPlayer x:Name="Player"
                              AutoPlay="True"
                              Stretch="Uniform"
                              Width="120" Height="120">
            <lottie:LottieVisualSource
                UriSource="embedded://MyApp.Core/MyApp.Assets.star_icon.json" />
        </AnimatedVisualPlayer>
    </Page>

2. Play one segment once, from code-behind, and react to the end
----------------------------------------------------------------
    using CommunityToolkit.WinUI.Lottie;
    using Microsoft.UI.Xaml.Controls;

    var player = new AnimatedVisualPlayer
    {
        AutoPlay = false,
        Source = new LottieVisualSource
        {
            UriSource = new Uri("ms-appx:///Assets/checkmark.json")
        }
    };
    root.Children.Add(player);

    // IsPlaying is a dependency property: watch it to learn when the
    // non-looped segment has finished (PlayAsync completes immediately).
    player.RegisterPropertyChangedCallback(AnimatedVisualPlayer.IsPlayingProperty,
        (s, dp) =>
        {
            if (!player.IsPlaying && player.IsAnimatedVisualLoaded)
                StatusText.Text = "done";
        });

    // Safe to call before the JSON has arrived: the segment is applied on load.
    _ = player.PlayAsync(0.0, 0.5, looped: false);

3. Scrubber: a Slider that drives the frame
-------------------------------------------
    <AnimatedVisualPlayer x:Name="Player" AutoPlay="False" Height="200">
        <lottie:LottieVisualSource UriSource="ms-appx:///Assets/intro.json" />
    </AnimatedVisualPlayer>
    <Slider Minimum="0" Maximum="1" StepFrequency="0.01"
            ValueChanged="Scrub_ValueChanged" />

    private void Scrub_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        => Player.SetProgress(e.NewValue);   // stops playback, shows that frame

4. Recolour a themed animation at run time
------------------------------------------
Lottie file: the shapes to recolour are named "{ Color : var(Foreground) }".

    <AnimatedVisualPlayer x:Name="Player" AutoPlay="True">
        <lottie:ThemableLottieVisualSource x:Name="Themed"
            UriSource="embedded://./(assembly).Assets.spinner.json" />
    </AnimatedVisualPlayer>

    // code-behind; works before or after the file has loaded
    Themed.SetColorThemeProperty("Foreground", Windows.UI.Color.FromArgb(255, 0, 120, 215));
    var current = Themed.GetColorThemeProperty("Foreground");   // Color?

5. Speed and pause/resume
-------------------------
    Player.PlaybackRate = 2.0;   // applied on the next frame, mid-playback
    Player.Pause();
    Player.Resume();
    Player.Stop();

MINIMUM VIABLE PROJECT
======================
.Core project fragment (the framework package plus this add-in; heads reference
.Core and add nothing Lottie-related):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <AssemblyName>MyApp.Core</AssemblyName>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.Lottie.ApacheLicenseForever" />
      </ItemGroup>
      <ItemGroup>
        <EmbeddedResource Include="Assets\anim.json" />
      </ItemGroup>
    </Project>

Page (in the .UI shared project):

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:lottie="using:CommunityToolkit.WinUI.Lottie">
        <AnimatedVisualPlayer AutoPlay="True" Width="160" Height="160">
            <lottie:LottieVisualSource
                UriSource="embedded://MyApp.Core/MyApp.Assets.anim.json" />
        </AnimatedVisualPlayer>
    </Page>

(Use the default XAML namespace your app template already declares; the
JustBetweenUs sample declares it as
clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI.)

PERFORMANCE TIPS
================
  - The frame timer runs at min(120 Hz, the file's own frame rate). A 30 fps
    file costs 30 repaints per second per player; pause players that are not
    visible (Unload/Load do this automatically when the player leaves and
    re-enters the tree, but a hidden-but-loaded player keeps ticking).
  - There is no cross-instance cache: each source instance reads and decodes
    its JSON on every (re)load, and every UriSource change re-decodes. Keep a
    source instance alive and re-attach it rather than re-creating it per
    show.
  - ThemableLottieVisualSource re-parses the whole document, rewrites it and
    re-decodes the animation on every SetColorThemeProperty. Batch colour
    changes; do not animate colours through it.
  - Prefer embedded:// or ms-appx:/// for bundled assets; both are read from
    local storage. Web URIs (themable source only) go through a shared
    HttpClient with no caching, on every load.

COMMON PITFALLS TO AVOID
========================
  - LOAD FAILURES ARE SILENT. A wrong resource name, an unreadable file or
    JSON that Skottie rejects logs an error ("Failed to update lottie player
    for [uri]" / "Unable to find embedded resource named ...") and leaves the
    player blank; nothing is thrown to application code. Check the log first.
  - FallbackContent is stored but NEVER displayed on the Skia heads.
  - PlayAsync completes immediately. Awaiting it does not wait for the
    segment; watch IsPlaying instead (example 2).
  - SetSourceAsync(Uri) also completes immediately - it only sets UriSource.
  - The manifest resource name is <RootNamespace>.<Link path with dots>,
    NOT the assembly name plus the file name. Check the RootNamespace of the
    project that embeds the file; the assembly name goes in the host part.
  - A plain LottieVisualSource will not fetch http(s) URIs; use
    ThemableLottieVisualSource for a downloaded animation.
  - Play() off the UI thread fails (it creates a DispatcherQueue timer for the
    calling thread). Marshal to the dispatcher first.
  - JSON only. The loader reads the payload as UTF-8 text, so a zipped
    ".lottie" bundle or a JSON that references external image files cannot
    work; export plain Bodymovin JSON with embedded assets.
  - Colour bindings inside precomps are not found (only top-level layers'
    shapes are scanned), and only the "Color" property name is honoured.
  - Options / LottieVisualOptions, CreateFromString and TryCreateAnimatedVisual
    exist for source compatibility and do nothing or throw
    NotImplementedException. Do not port code that relies on them.
  - AutoPlay defaults to TRUE: a player with a source starts looping as soon
    as it loads unless you set AutoPlay="False".
  - Never add the package to a head project; .Core only.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does not render Lottie files through the Windows composition pipeline
    (no IAnimatedVisual, no ProgressObject, no Diagnostics); everything is
    drawn by Skottie on a Skia canvas.
  - It does not support dotLottie (.lottie) archives, external image assets,
    or any non-JSON input.
  - It does not offer per-frame events, frame counts, markers, or a
    "completed" event; the observable state is IsPlaying,
    IsAnimatedVisualLoaded and Duration.
  - It does not cache decoded animations across source instances.
  - It does not add a WinUI/Windows App SDK head implementation; the
    src-platforms heads use their own Lottie assembly and package.
  - It does not bring a Lottie AUTHORING or conversion tool; export JSON from
    After Effects (Bodymovin) or a Lottie editor.

WORKING EXAMPLES ON GITHUB
==========================
  - JustBetweenUs sample (all six Skia heads): an AnimatedVisualPlayer with a
    LottieVisualSource loaded from an embedded resource, inside a Button, and
    the .Core csproj that embeds the JSON.
      https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/JustBetweenUs
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/samples/CodeBrixPlatform/JustBetweenUs/JustBetweenUs.UI/Views/MainPage.xaml
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/samples/CodeBrixPlatform/JustBetweenUs/JustBetweenUs.Core/JustBetweenUs.Core.csproj
  - Unit tests for the themable source (SetColorThemeProperty before and
    after loading; the test file "animation.json" shows the
    "{ Color : var(Foreground) }" naming):
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/src/Platform.UI.Tests/Lottie/Given_DynamicReloadedLottieAnimatedVisualSource.cs
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/src/Platform.UI.Tests/Lottie/animation.json
  - Runtime tests that drive the core ProgressRing's Lottie player (IsPlaying
    toggling with IsIndeterminate):
      https://github.com/ellisnet/CodeBrix.Platform/blob/main/src/Platform.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_ProgressRing.cs

QUICK REFERENCE CARD
====================
Package:    CodeBrix.Platform.Lottie.ApacheLicenseForever   (reference in .Core)
Assembly:   CodeBrix.Platform.UI.Lottie.dll
XAML:       xmlns:lottie="using:CommunityToolkit.WinUI.Lottie"
Companion:  CodeBrix.SkiaSvg / CodeBrix.Platform.Svg for static SVG images
            (see src/AddIns/Platform.UI.Svg/AGENT-README.txt in this repo)

    <AnimatedVisualPlayer AutoPlay="True" Stretch="Uniform" Width="120" Height="120">
        <lottie:LottieVisualSource UriSource="embedded://MyApp.Core/MyApp.Assets.anim.json" />
    </AnimatedVisualPlayer>

AnimatedVisualPlayer (core):
    Source (IAnimatedVisualSource)  AutoPlay (true)  Stretch (Uniform)
    PlaybackRate (1.0)  IsPlaying  IsAnimatedVisualLoaded  Duration
    PlayAsync(from, to, looped)  Pause()  Resume()  Stop()  SetProgress(0..1)

LottieVisualSource / ThemableLottieVisualSource (CommunityToolkit.WinUI.Lottie):
    UriSource (Uri)   SetSourceAsync(Uri)   Play/Stop/Pause/Resume/SetProgress
    Themable only:    SetColorThemeProperty(name, Color?)  GetColorThemeProperty(name)
                      shape name syntax  { Color : var(Name) }
    Not implemented:  Options, CreateFromString, TryCreateAnimatedVisual

URI forms:
    embedded://<Assembly>/<RootNamespace>.<Folder>.<file>.json   ("." = app assembly,
                                                                  "(assembly)" = its name)
    ms-appx:///Assets/<file>.json          (Content copied to output)
    ms-appdata://local/<file>.json
    http(s)://...                          (ThemableLottieVisualSource only)

ProgressRing (core) needs this package; recolours via Foreground/Background;
swap files with IndeterminateSource / DeterminateSource or
FeatureConfiguration.ProgressRing.ProgressRingAsset / DeterminateProgressRingAsset.
