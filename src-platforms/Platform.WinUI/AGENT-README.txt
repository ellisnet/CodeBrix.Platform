================================================================================
AGENT-README: CodeBrix.Platform.WinUI
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.WinUI is the WinUI (Windows App SDK) member of the
CodeBrix.Platform "native" package families (siblings: CodeBrix.Platform.WPF and
CodeBrix.Platform.Mobile). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for WinUI, plus
Skia-rendered SVG image controls and a Skia-rendered (Skottie) Lottie player.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them at build time. The shared/ported sources are: (a) the
linked CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple),
which live at src-platforms/Platform.Simple in the repo and are compiled into
each family via file links under a Simple/ folder in the Core project; and
(b) the SVG/Lottie rendering code, which is PORTED from the Uno-based
CodeBrix.Platform add-ins (src/AddIns/Platform.UI.Svg and
src/AddIns/Platform.UI.Lottie) so that SVG files and Lottie animations render
IDENTICALLY on the native WinUI packages and the CodeBrix.Platform heads. When
changing rendering behavior here, check the Uno-based originals first --
fidelity with them is a design requirement. See THIRD-PARTY-NOTICES.txt for
full provenance of the ported code.

COMPILATION CONSTANT
--------------------
WinUI defines the WIN_UI constant (Microsoft.UI.Xaml / Microsoft.UI.Dispatching
code path in the shared Simple sources). MAUI and HAS_CODEBRIX (the Uno path)
are intentionally NOT defined. The constant is set for both Debug AND Release.
Consult each .csproj for current target frameworks and dependency versions --
they are not repeated here so this document does not go stale.

PACKAGES
--------
CodeBrix.Platform.WinUI.ApacheLicenseForever         (project: Core/)
    The "Simple" MVVM toolkit for WinUI. Namespace: CodeBrix.Platform.Simple.
    DI/Hosting dependencies are Abstractions-only (the app owns the concrete
    Microsoft.Extensions.Hosting reference; see IHostBuilderProvider below).

CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever    (project: Skia/)
    EmbeddedImage + EmbeddedImageButton controls with embedded:// resource
    loading and vector-direct SVG rendering via CodeBrix.SkiaSvg +
    SkiaSharp.Views.WinUI. SVGs draw as vectors onto an SKXamlCanvas at full
    display resolution (no intermediate rasterization), which is why they match
    the CodeBrix.Platform Skia heads pixel-for-pixel.
    Namespaces: CodeBrix.Platform.WinUI.Controls (controls),
    CodeBrix.Platform.WinUI.Skia (rendering helpers, e.g. ImageSizeHelper).
    Depends on: Core.

CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever  (project: Lottie/)
    Lottie player: AnimatedVisualPlayer, LottieVisualSource,
    ThemableLottieVisualSource, rendered with SkiaSharp.Skottie. Functionally
    identical to the Uno-based CodeBrix.Platform.Lottie.ApacheLicenseForever
    package: same URI schemes (embedded://, ms-appx:///, ms-appdata:///), same
    playback API (Play/Stop/Pause/Resume/SetProgress), same color theming via
    {color: var(Name)} shape-name bindings. The native Windows App SDK
    AnimatedVisualPlayer requires Composition/Win2D sources, so this package
    ships its OWN AnimatedVisualPlayer (namespace CodeBrix.Platform.WinUI.Lottie)
    that hosts a Skia render surface and delegates playback to the source,
    mirroring the Uno player contract.
    Namespace: CodeBrix.Platform.WinUI.Lottie.
    Depends on: Core, Skia.

PROJECT LAYOUT RULE
-------------------
Anything to do with Skia graphics/rendering on WinUI that ISN'T Lottie belongs
in the Skia project; the Lottie project adds only Lottie parsing/rendering and
depends on Skia. Dependency direction is strictly Lottie -> Skia -> Core.

THE "SIMPLE" TOOLKIT (namespace CodeBrix.Platform.Simple)
---------------------------------------------------------
SimpleViewModel (abstract view-model base; INotifyPropertyChanged, IDisposable)
  * SetProperty(ref field, value) overloads (generic, string, bool, int,
    DateTime, DateTimeOffset; SetEnumProperty for enums). SetProperty inspects
    attributes on the calling property and automatically cascades:
      [AffectsProperties("A", "B")]  -> raises PropertyChanged for A and B
      [AffectsCommands(nameof(Cmd))] -> calls Cmd.RaiseCanExecuteChanged()
      [AffectsAllCommands]           -> refreshes every SimpleCommand property
  * NotifyPropertyChanged(name, notifyOnMainThread) for manual raises.
  * GetService<T>() / GetServices<T>() -- resolve via SimpleServiceResolver.
  * Messaging helpers (MessagingSend, MessagingSubscribe, MessagingSubscribeFrom,
    MessagingUnsubscribe...) -- wrap SimpleMessaging.Instance.
  * Dialog helpers: ShowInfo(msg), ShowError(msg|Exception), ConfirmDialog(...),
    and virtual CreateDialog(...) to customize.
  * Thread helpers: InvokeOnMainThread(Action), InvokeOnMainThreadAsync<T>()
    (Microsoft.UI.Dispatching on WinUI).
  * GetVisibility(bool) -- WinUI-correct Visibility (Visible / Collapsed).
  * Design mode: static SetIsDesignMode(bool) (call once at app start);
    protected IsDesignMode(defaultIfNotSet) guard in view-model constructors.
  WinUI specifics: implements IXamlRootGetter with a Func<XamlRoot> -- the page
  hosting the view model must call SetXamlRootGetter(() => XamlRoot) so dialogs
  (ContentDialog) can be rooted correctly. See TYPICAL APP WIRING below.

SimpleCommand : ICommand, IDisposable
  * Constructor matrix: optional canExecute (Func<bool> or Func<object,bool>)
    x execute as Action, Action<object>, Func<Task> or Func<object,Task>,
    each with an executeOnMainThread flag. Async handlers are awaited.
  * RaiseCanExecuteChanged(); ShouldExecuteOnMainThread and
    ShouldRaiseCanExecuteOnMainThread properties.
  * Typical pattern: lazy property
      public SimpleCommand SaveCommand => _save ??= new(CanSave, DoSave);
    with [AffectsCommands(nameof(SaveCommand))] on the properties CanSave reads.

SimpleDialog (+ SimpleDialogButtons {OK, OKCancel, YesNo}, SimpleDialogResult)
  * SimpleDialog.Create(message, title, buttons ...) then await ShowAsync().
  * On WinUI this is backed by ContentDialog, marshalled to the main thread,
    with the XamlRoot supplied by the IXamlRootGetter wiring described above.

SimpleMessaging : ISimpleMessaging
  * Weak-reference publish/subscribe by message name, optionally typed by
    sender and/or argument type. Static Instance for direct use;
    services.AddSimpleMessaging() to also expose it through DI.

SimpleServiceResolver : ISimpleServiceResolver (Generic Host wrapper)
  * App startup:  SimpleServiceResolver.CreateInstance(hostBuilderProvider,
    services => { /* register app services */ });  or CreateInstance(IHost).
  * IHostBuilderProvider is a tiny adapter the APP implements (returning
    Host.CreateDefaultBuilder()); this keeps the package's dependencies to
    *.Abstractions packages only.
  * Instance.GetService<T>() / GetServices<T>(); StartupHost() / ShutdownHost().
  * services.AutoRegisterServices(assemblies) scans for IAutoRegisterServices
    implementors; services.IsRegistered<T>() checks for existing registrations.

SimpleEnum (rich enum metadata)
  * Derive an info class from SimpleEnumInfo<TEnum>; decorate enum members with
    [SimpleEnum<TInfo>(nameof(TInfo.StaticMember))] pointing at static factory
    properties on the info class. Look up via SimpleEnumHelper.FindMemberInfo,
    GetInfoDictionary or GetPossibleValues, or a GetDictionary() helper on the
    derived info class.

SimpleOsInfo
  * var info = await SimpleOsInfo.GatherInfo(); exposes PlatformOsName,
    OsDescription, OsVersion, ProductName, ProductNameDisplay, RunningAsUser,
    IsAdminUser, DotNetVersion, PlatformArchitecture, and more. Includes Linux
    distro identification (IdentifiedLinuxDistro) and unix shell-command
    helpers (RunUnixShellCommandResult).

TYPICAL APP WIRING (WinUI)
--------------------------
In App.xaml.cs:

    public App()
    {
        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            services.AddMyAppServices();
        });
        SimpleViewModel.SetIsDesignMode(false);
        this.InitializeComponent();
    }

where HostHelper is the app-side IHostBuilderProvider implementation returning
Host.CreateDefaultBuilder(). In a page constructor, wire the XamlRoot getter
(needed for SimpleDialog/ContentDialog) before/around InitializeComponent:

    DataContextChanged += (s, e) =>
        (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

XAML USAGE (Skia + Lottie packages)
-----------------------------------
xmlns:controls="using:CodeBrix.Platform.WinUI.Controls"
xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie"

<controls:EmbeddedImageButton
    ImageUriSource="embedded://MyApp/MyApp.Assets.icon.svg"
    Text="Do it" ImagePosition="Top" ImageWidth="40" ImageHeight="40" />

<controls:EmbeddedImage UriSource="ms-appx:///Assets/picture.svg" />

<lottie:AnimatedVisualPlayer AutoPlay="True">
    <lottie:LottieVisualSource UriSource="ms-appx:///Assets/animation.json" />
</lottie:AnimatedVisualPlayer>

The embedded:// scheme is embedded://AssemblyName/Fully.Qualified.Resource.Name
(matching an <EmbeddedResource> LogicalName in the consuming app project).

SAMPLE
------
samples/Platforms/JustBetweenUs -- the WinUI head (JustBetweenUs.WinUi/) uses
all three packages and shares its view models with the WPF and MAUI heads via
samples/Platforms/JustBetweenUs/Shared.

BUILD / PACKAGING CONVENTIONS
-----------------------------
* Every .csproj in src-platforms produces a ready-to-upload NuGet package on
  build (GeneratePackageOnBuild), with a date-stamped auto-incrementing version
  derived from UTC build time (see the comment block in each .csproj).
* Each package packs the family README.md, AGENT-README.txt,
  THIRD-PARTY-NOTICES.txt and the CodeBrix icon.
* License: Apache-2.0 (the .ApacheLicenseForever package-ID suffix encodes it).
  Third-party provenance for ported code is in THIRD-PARTY-NOTICES.txt.
* src-platforms has a deliberately empty Directory.Build.props that isolates
  this tree from the Uno-based build machinery in the rest of the repo --
  do not add imports to it.
================================================================================
