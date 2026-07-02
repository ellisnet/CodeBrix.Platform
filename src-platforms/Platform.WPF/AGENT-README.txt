================================================================================
AGENT-README: CodeBrix.Platform.WPF
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.WPF is the WPF member of the CodeBrix.Platform "native"
package families (siblings: CodeBrix.Platform.WinUI and
CodeBrix.Platform.Mobile). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for WPF.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them. The only shared source is the linked
CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple), which
live at src-platforms/Platform.Simple in the repo and are compiled into each
family via file links under a Simple/ folder in the Core project.

COMPILATION CONSTANT
--------------------
WPF uses the shared Simple sources' "#else" branch (System.Windows). It
therefore defines NONE of the platform constants -- WIN_UI, MAUI and
HAS_CODEBRIX are all left undefined, which selects the WPF code path. UseWPF +
the -windows target framework supply System.Windows, in Debug AND Release.
Consult the .csproj for the current target framework and dependency versions --
they are not repeated here so this document does not go stale.

PACKAGES
--------
CodeBrix.Platform.WPF.ApacheLicenseForever   (project: Core/) -- the "Simple"
    MVVM toolkit for WPF. Namespace: CodeBrix.Platform.Simple.
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
  * Thread helpers: InvokeOnMainThread(Action) and InvokeOnMainThreadAsync<T>()
    -- on WPF these dispatch via Application.Current.Dispatcher, with a
    unit-test fallback that invokes directly when no Dispatcher exists.
  * GetVisibility(bool) -- WPF-correct Visibility (Visible / Hidden).
  * Design mode: IsDesignMode() detects the WPF designer via
    DesignerProperties.GetIsInDesignMode, and can be forced with the static
    SetIsDesignMode(bool); use IsDesignMode(defaultIfNotSet) as a guard in
    view-model constructors.
  WPF specifics: no XamlRoot/Page wiring is needed -- dialogs use MessageBox
  (see SimpleDialog below), so there is no IXamlRootGetter on this platform.

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
  * On WPF this is backed by System.Windows.MessageBox, mapping
    SimpleDialogButtons to MessageBoxButton and MessageBoxResult back to
    SimpleDialogResult.

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

TYPICAL APP WIRING (WPF)
------------------------
In App.xaml.cs:

    public App()
    {
        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            services.AddMyAppServices();
        });
        SimpleViewModel.SetIsDesignMode(false);
    }

where HostHelper is the app-side IHostBuilderProvider implementation returning
Host.CreateDefaultBuilder(). View models bind as DataContext of windows/pages;
no additional dialog wiring is required on WPF.

SAMPLE
------
samples/Platforms/JustBetweenUs -- the WPF head (JustBetweenUs.Wpf/) shares its
view models with the WinUI and MAUI heads via
samples/Platforms/JustBetweenUs/Shared, demonstrating the same
SimpleViewModel-based MVVM across all three families.

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
