================================================================================
AGENT-README: CodeBrix.Platform.Mobile
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.Mobile is the .NET MAUI member of the CodeBrix.Platform
"native" package families (siblings: CodeBrix.Platform.WinUI and
CodeBrix.Platform.WPF). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for .NET MAUI.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them. The only shared source is the linked
CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple), which
live at src-platforms/Platform.Simple in the repo and are compiled into each
family via file links under a Simple/ folder in the Core project.

COMPILATION CONSTANT
--------------------
Mobile defines the MAUI constant, which selects the Microsoft.Maui.* code path
in the shared Simple sources. WIN_UI (native WinUI) and HAS_CODEBRIX (the Uno
path) are intentionally NOT defined. The constant is set for both Debug AND
Release. Consult the .csproj for the current target frameworks and dependency
versions -- they are not repeated here so this document does not go stale.

PACKAGES
--------
CodeBrix.Platform.Mobile.ApacheLicenseForever   (project: Core/) -- the "Simple"
    MVVM toolkit for .NET MAUI. Namespace: CodeBrix.Platform.Simple.
    DI/Hosting dependencies are Abstractions-only (the app owns the concrete
    Microsoft.Extensions.Hosting reference; see IHostBuilderProvider below).

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
  * Thread helpers: InvokeOnMainThread(Action), InvokeOnMainThreadAsync<T>().
  * GetVisibility(bool) -- platform-correct XAML Visibility conversion.
  * Design mode: static SetIsDesignMode(bool) (call once at app start);
    protected IsDesignMode(defaultIfNotSet) guard in view-model constructors.
  MAUI specifics: implements IXamlRootGetter with a Func<Page> -- the page
  hosting the view model should call SetXamlRootGetter(() => this) so dialogs
  can call DisplayAlert on the right Page.

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
  * On MAUI this is backed by Page.DisplayAlert, marshalled to the main thread;
    the Page comes from the IXamlRootGetter wiring described above.

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
    helpers (RunUnixShellCommandResult). On MAUI, device model/manufacturer
    info is also gathered where the platform provides it.

TYPICAL APP WIRING (MAUI)
-------------------------
In MauiProgram / App startup:

    SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
    {
        services.AddMyAppServices();
    });
    SimpleViewModel.SetIsDesignMode(false);

where HostHelper is the app-side IHostBuilderProvider implementation returning
Host.CreateDefaultBuilder(). In a page constructor, give the view model the
Page for dialogs:

    (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);

SAMPLE
------
samples/Platforms/JustBetweenUs -- the Mobile head (Mobile/) shares its view
models with the WinUI and WPF heads via samples/Platforms/JustBetweenUs/Shared,
demonstrating the same SimpleViewModel-based MVVM across all three families.

BUILD / PACKAGING CONVENTIONS
-----------------------------
* Every .csproj in src-platforms produces a ready-to-upload NuGet package on
  build (GeneratePackageOnBuild), with a date-stamped auto-incrementing version
  derived from UTC build time (see the comment block in the .csproj).
* Each package packs the family README.md, AGENT-README.txt,
  THIRD-PARTY-NOTICES.txt and the CodeBrix icon.
* License: Apache-2.0 (the .ApacheLicenseForever package-ID suffix encodes it).
* src-platforms has a deliberately empty Directory.Build.props that isolates
  this tree from the Uno-based build machinery in the rest of the repo --
  do not add imports to it.
================================================================================
