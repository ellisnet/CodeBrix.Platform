================================================================================
AGENT-README: CodeBrix.Platform.WinUI
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.WinUI, .WinUI.Skia and .WinUI.Lottie NuGet packages
================================================================================

OVERVIEW
========
CodeBrix.Platform.WinUI is a family of three NuGet packages for WinUI (Windows
App SDK) desktop applications on .NET 10 or later:

  * Core   - CodeBrix.Platform.WinUI.ApacheLicenseForever
             The CodeBrix "Simple" MVVM toolkit compiled for WinUI:
             SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
             SimpleServiceResolver (Generic-Host DI), SimpleEnum (rich enum
             metadata) and SimpleOsInfo. 25 public types, one namespace.
  * Skia   - CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever
             EmbeddedImage and EmbeddedImageButton controls that load images
             from embedded resources (embedded://) or application URIs and
             render SVG vector-direct through CodeBrix.SkiaSvg on a SkiaSharp
             canvas at full display resolution. 4 public types.
  * Lottie - CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever
             A Skia-rendered (SkiaSharp.Skottie) Lottie player:
             AnimatedVisualPlayer, LottieVisualSource and the run-time
             recolourable ThemableLottieVisualSource. 8 public types.

Provenance: these packages are independent of the cross-platform
CodeBrix.Platform UI packages and share no code with them at run time. The
Simple toolkit is original CodeBrix code and is source-identical in the sibling
packages CodeBrix.Platform.WPF.ApacheLicenseForever (see
src-platforms/Platform.WPF/AGENT-README.txt) and
CodeBrix.Platform.Mobile.ApacheLicenseForever (see
src-platforms/Platform.Mobile/AGENT-README.txt): a view model written against
SimpleViewModel compiles unchanged for WinUI, WPF and .NET MAUI. The SVG and
Lottie rendering code is ported from the CodeBrix.Platform Svg and Lottie
add-ins so that the same SVG file or Lottie JSON renders identically here and
on the CodeBrix.Platform Skia heads. There are no upstream namespaces to fall
back to; use only the namespaces listed below.

INSTALLATION
============
Package ids:
    CodeBrix.Platform.WinUI.ApacheLicenseForever          (Core)
    CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever     (Skia)
    CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever   (Lottie)

    dotnet add package CodeBrix.Platform.WinUI.ApacheLicenseForever
    dotnet add package CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever
    dotnet add package CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever

WHICH ONE DO I REFERENCE
------------------------
Dependency direction is strictly Lottie -> Skia -> Core; each package brings
the ones below it transitively.
  * View models, commands, dialogs, messaging, DI, OS info only
        -> reference Core.
  * Also EmbeddedImage / EmbeddedImageButton / SVG rendering
        -> reference Skia (brings Core).
  * Also the Lottie player
        -> reference Lottie (brings Skia and Core).
Referencing all three explicitly is harmless.

NuGet dependencies (by id):
    Core:   Microsoft.Extensions.DependencyInjection.Abstractions,
            Microsoft.Extensions.Hosting.Abstractions, Microsoft.WindowsAppSDK
    Skia:   Core + CodeBrix.SkiaSvg.MitLicenseForever + SkiaSharp.Views.WinUI
    Lottie: Core + Skia + SkiaSharp.Skottie

App-owned (NOT brought in by these packages): Microsoft.Extensions.Hosting.
The Core package references only the *.Abstractions packages; the app
implements IHostBuilderProvider and returns Host.CreateDefaultBuilder(), which
lives in Microsoft.Extensions.Hosting. Add that package to the application
project (see MINIMUM VIABLE PROJECT).

License: Apache-2.0 for all three packages. Provenance of the ported rendering
code is listed in the THIRD-PARTY-NOTICES.txt shipped inside each package.

Requirements: a Windows App SDK application targeting
net10.0-windows10.0.19041.0 (minimum platform 10.0.17763.0) with
UseWinUI=true. Windows only. A class library that references the Core package
must target the same net10.0-windows10.0.19041.0 framework (the toolkit's types
reference Microsoft.UI.Xaml). The Skia and Lottie packages need no separate
native install: their SkiaSharp dependencies carry the Windows binaries.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.Simple;          // Core: every Simple* type,
                                             //   IXamlRootGetter, IHostBuilderProvider,
                                             //   SimpleServiceExtensions
    using CodeBrix.Platform.WinUI.Controls;  // Skia: EmbeddedImage,
                                             //   EmbeddedImageButton, ImagePosition
    using CodeBrix.Platform.WinUI.Skia;      // Skia: ImageSizeHelper
    using CodeBrix.Platform.WinUI.Lottie;    // Lottie: AnimatedVisualPlayer,
                                             //   LottieVisualSource, Themable...,
                                             //   IAnimatedVisualSource...
XAML:
    xmlns:controls="using:CodeBrix.Platform.WinUI.Controls"
    xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie"

Assembly names: CodeBrix.Platform.WinUI, CodeBrix.Platform.WinUI.Skia,
CodeBrix.Platform.WinUI.Lottie.

CORE API REFERENCE - THE "SIMPLE" TOOLKIT (Core package)
========================================================
All types are in namespace CodeBrix.Platform.Simple.

Startup contract (order matters)
--------------------------------
    1. SimpleServiceResolver.CreateInstance(...)  in the App constructor,
       before InitializeComponent() and before any view model exists
    2. SimpleViewModel.SetIsDesignMode(false)     once, right after step 1
    3. construct view models ON THE UI THREAD; the hosting Page calls
       SetXamlRootGetter(() => XamlRoot) before any dialog is shown

SimpleViewModel
---------------
    public abstract class SimpleViewModel
        : IXamlRootGetter, INotifyPropertyChanged, IDisposable

    // Design mode (WinUI cannot detect the designer: the value is whatever
    //   SetIsDesignMode stored, else your default)
    public static void SetIsDesignMode(bool isDesignMode);
    protected static bool IsDesignMode(bool defaultValueIfNotSet);

    // XamlRoot wiring - WinUI-specific, required by every dialog helper
    public void SetXamlRootGetter(Func<XamlRoot> getter);  // null -> ArgumentNullException
    protected XamlRoot GetXamlRoot();  // InvalidOperationException if never set

    // Service resolution (wrap SimpleServiceResolver.Instance)
    protected static T GetService<T>() where T : class;   // throws when unregistered
    protected static IEnumerable<T> GetServices<T>() where T : class;

    // Messaging (wrap the ISimpleMessaging service)
    protected static void MessagingSend<TSender, TArgs>(TSender sender,
        string message, TArgs args) where TSender : class;
    protected static void MessagingSend<TSender>(TSender sender, string message)
        where TSender : class;
    protected static void MessagingSubscribe<TSender, TArgs>(object subscriber,
        string message, Action<TSender, TArgs> callback, TSender source)
        where TSender : class;
    protected static void MessagingSubscribe<TSender, TArgs>(object subscriber,
        string message, Func<TSender, TArgs, Task> callback, TSender source)
        where TSender : class;
    protected static void MessagingSubscribeFrom<TSender>(object subscriber,
        string message, Action<TSender> callback, TSender source)
        where TSender : class;
    protected static void MessagingSubscribeFrom<TSender>(object subscriber,
        string message, Func<TSender, Task> callback, TSender source)
        where TSender : class;
    protected static void MessagingSubscribe<TArgs>(object subscriber,
        string message, Action<TArgs> callback);
    protected static void MessagingSubscribe<TArgs>(object subscriber,
        string message, Func<TArgs, Task> callback);
    protected static void MessagingUnsubscribe<TSender, TArgs>(object subscriber,
        string message) where TSender : class;
    protected static void MessagingUnsubscribeFrom<TSender>(object subscriber,
        string message) where TSender : class;
    protected static void MessagingUnsubscribe<TArgs>(object subscriber,
        string message);

    // Dialogs - all marshalled to the UI thread. Labels are English constants:
    //   "Information", "ERROR", "Are you sure?", "OK", "Cancel", "Yes", "No"
    protected virtual SimpleDialog CreateDialog(string message,
        string title = null, SimpleDialogButtons buttons = SimpleDialogButtons.OK);
    protected virtual Task ShowInfo(string message);
        // title "Information"; does nothing for a blank message
    protected virtual Task ShowError(string message, string details = null);
        // title "ERROR"; body "An error occurred:\n   <message>" plus, when
        // given, "Details:\n<details>" (details over 200 chars are cut to
        // 195 + "[...]")
    protected virtual Task ShowError(Exception exception, string message = null);
        // message = exception.Message (prefixed "<message> - " when given);
        // details = exception.ToString()
    protected virtual Task<bool> ConfirmDialog(string message, string title = null,
        SimpleDialogButtons confirmButtons = SimpleDialogButtons.YesNo);
        // default title "Are you sure?"; true when the user chose Yes (YesNo)
        // or OK (OK / OKCancel)

    // Threading - the Microsoft.UI.Dispatching.DispatcherQueue of the thread
    //   that CONSTRUCTED the view model is captured and used here
    protected virtual void InvokeOnMainThread(Action functionToExecute);
        // TryEnqueue; returns immediately
    protected virtual Task<T> InvokeOnMainThreadAsync<T>(
        Func<Task<T>> functionToExecute);
    protected static Visibility GetVisibility(bool isVisible);
        // Microsoft.UI.Xaml.Visibility: Visible / Collapsed

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void NotifyPropertyChanged(string propertyName,
        bool notifyOnMainThread = false);
    protected virtual void ThisPropertyChanged(
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false);
    protected virtual void RaiseCanExecuteChanged(SimpleCommand command);
    protected virtual void CheckAffectedProperties(string propertyName);
    protected virtual void CheckAffectedCommands(string propertyName);

    // SetProperty family: assign and notify only when the value changed
    protected virtual void SetProperty<T>(ref T property, T newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
        where T : class;
    protected virtual void SetProperty(ref string property, string newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false);
    protected virtual void SetProperty(ref bool property, bool newValue, ...same...);
    protected virtual void SetProperty(ref int property, int newValue, ...same...);
    protected virtual void SetProperty(ref DateTime property, DateTime newValue, ...);
    protected virtual void SetProperty(ref DateTimeOffset property,
        DateTimeOffset newValue, ...same...);
    protected virtual void SetEnumProperty<TEnum>(ref TEnum property, TEnum newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
        where TEnum : Enum;
        // assigns only when BOTH old and new values are defined members

    // Disposal
    protected virtual void CheckDisposed(Func<bool> disposedChecker,
        [CallerMemberName] string caller = null);   // ObjectDisposedException
    public virtual void Dispose();       // detaches every PropertyChanged handler,
    protected virtual void Dispose(bool disposing);  // drops dispatcher + getter

Every NotifyPropertyChanged (so every SetProperty / ThisPropertyChanged) raises
PropertyChanged for propertyName and then looks up the PUBLIC INSTANCE property
of that name by reflection and applies the attributes it carries:

The Affects* attributes
-----------------------
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AffectsPropertiesAttribute : Attribute
        public AffectsPropertiesAttribute(params string[] propertyNames);
        public IList<string> AffectedProperties { get; }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AffectsCommandsAttribute : Attribute
        public AffectsCommandsAttribute(params string[] commandNames);
        public IList<string> AffectedCommands { get; }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AffectsAllCommandsAttribute : Attribute

    [AffectsProperties(nameof(FullName), nameof(Initials))]
        -> PropertyChanged is also raised for FullName and Initials
    [AffectsCommands(nameof(SaveCommand))]
        -> SaveCommand.RaiseCanExecuteChanged() is called (the property's
           VALUE must be a SimpleCommand; null values are skipped)
    [AffectsAllCommands]
        -> RaiseCanExecuteChanged() on every public property whose DECLARED
           type is exactly SimpleCommand

Names are matched case-insensitively. When a property carries both,
[AffectsAllCommands] wins and [AffectsCommands] is ignored.

SimpleCommand
-------------
    public class SimpleCommand : ICommand, IDisposable
    // 12 constructors: {no gate | Func<bool> | Func<object,bool>}
    //                x {Action | Action<object> | Func<Task> | Func<object,Task>}
    public SimpleCommand(Action executeFunction, bool executeOnMainThread = false);
    public SimpleCommand(Func<bool> canExecuteFunction, Action executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<object, bool> canExecuteFunction, Action executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Action<object> executeFunction, bool executeOnMainThread = false);
    public SimpleCommand(Func<bool> canExecuteFunction, Action<object> executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<object, bool> canExecuteFunction,
        Action<object> executeFunction, bool executeOnMainThread = false);
    public SimpleCommand(Func<Task> executeFunction, bool executeOnMainThread = false);
    public SimpleCommand(Func<bool> canExecuteFunction, Func<Task> executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<Task> executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<object, Task> executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<bool> canExecuteFunction, Func<object, Task> executeFunction,
        bool executeOnMainThread = false);
    public SimpleCommand(Func<object, bool> canExecuteFunction,
        Func<object, Task> executeFunction, bool executeOnMainThread = false);

    public bool ShouldExecuteOnMainThread { get; set; }         // from the ctor flag
    public bool ShouldRaiseCanExecuteOnMainThread { get; set; } // default true
    public bool CanExecute(object parameter);   // no gate supplied -> true
    public async void Execute(object parameter); // awaits Func<Task> handlers
    public void RaiseCanExecuteChanged();
    public event EventHandler CanExecuteChanged;
    public void Dispose();                       // clears delegates and handlers

Behaviour on WinUI: with ShouldExecuteOnMainThread the handler is enqueued
(TryEnqueue) on the DispatcherQueue captured when the command was constructed;
if none is available it runs inline. RaiseCanExecuteChanged is enqueued the
same way while ShouldRaiseCanExecuteOnMainThread is true. Execute awaits async
handlers but is itself async void: an exception escaping your handler is not
observable by the caller and becomes an unhandled exception - catch inside.

Lazy-property pattern (the one the samples use):
    private SimpleCommand _saveCommand;
    public SimpleCommand SaveCommand =>
        _saveCommand ??= new SimpleCommand(CanSave, DoSaveAsync);
    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
    private async Task DoSaveAsync() { ... }

SimpleDialog
------------
    public enum SimpleDialogButtons { OK = 0, OKCancel = 1, YesNo = 2 }
    public enum SimpleDialogResult  { None = 0, OK = 1, Cancel = 2, Yes = 3, No = 4 }

    public class SimpleDialog : IDisposable
    public static SimpleDialog Create(Func<XamlRoot> xamlRootGetter,
        DispatcherQueue dispatcher, string message, string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK);
        // both getter and dispatcher null -> ArgumentNullException
    public string Message { get; set; }   // trimmed; null becomes ""
    public string Title { get; set; }     // blank becomes null (no title shown)
    public SimpleDialogButtons Buttons { get; set; }
    public Task<SimpleDialogResult> ShowAsync();  // ObjectDisposedException after Dispose
    public void Dispose();

    public interface IXamlRootGetter
        void SetXamlRootGetter(Func<XamlRoot> getter);

On WinUI ShowAsync builds a Microsoft.UI.Xaml.Controls.ContentDialog on the
dispatcher - message wrapped at 74 characters per line in a TextBlock,
XamlRoot = xamlRootGetter(), ContentDialogPlacement.Popup, primary button =
OK/Yes, secondary button = Cancel/No - and maps ContentDialogResult back to
SimpleDialogResult (None when dismissed). From a view model you normally never
call Create: CreateDialog / ShowInfo / ShowError / ConfirmDialog pass the view
model's XamlRoot getter and dispatcher for you.

SimpleMessaging
---------------
    public interface ISimpleMessaging
        void Send<TSender, TArgs>(TSender sender, string message, TArgs args)
            where TSender : class;
        void Send<TSender>(TSender sender, string message) where TSender : class;
        void Subscribe<TSender, TArgs>(object subscriber, string message,
            Action<TSender, TArgs> callback, TSender source) where TSender : class;
        void Subscribe<TSender, TArgs>(object subscriber, string message,
            Func<TSender, TArgs, Task> callback, TSender source) where TSender : class;
        void SubscribeFrom<TSender>(object subscriber, string message,
            Action<TSender> callback, TSender source) where TSender : class;
        void SubscribeFrom<TSender>(object subscriber, string message,
            Func<TSender, Task> callback, TSender source) where TSender : class;
        void Subscribe<TArgs>(object subscriber, string message, Action<TArgs> callback);
        void Subscribe<TArgs>(object subscriber, string message,
            Func<TArgs, Task> callback);
        void Unsubscribe<TSender, TArgs>(object subscriber, string message)
            where TSender : class;
        void UnsubscribeFrom<TSender>(object subscriber, string message)
            where TSender : class;
        void Unsubscribe<TArgs>(object subscriber, string message);

    public class SimpleMessaging : ISimpleMessaging
        // interface members are implemented EXPLICITLY: always call through
        // the ISimpleMessaging-typed Instance (or the SimpleViewModel helpers)
        public static ISimpleMessaging Instance { get; }
        public static void ConfigureServices(IServiceCollection services);
            // registers Instance as a singleton if not already registered

Semantics (as implemented):
  * A subscription is keyed by (message, sender Type, args Type).
    Send<TSender, TArgs> reaches subscriptions made with exactly that TSender
    and TArgs, plus "generic" Subscribe<TArgs> subscriptions with that TArgs.
    Type matching is exact: subscribing with a base class does NOT receive
    messages sent with a derived TSender.
  * Send<TSender>(sender, message) (no args) pairs with SubscribeFrom<TSender>
    and UnsubscribeFrom<TSender>.
  * source: a specific instance restricts delivery to that sender; null means
    any sender of that type.
  * Generic Subscribe<TArgs> callbacks receive the args only.
  * Action callbacks run synchronously on the sending thread. Func<..., Task>
    callbacks are fire-and-forget on a thread-pool Task, serialised per
    subscription; touch UI from them only via InvokeOnMainThread.
  * Lifetime: the subscriber is held by a WeakReference. The callback target
    is held weakly ONLY when the callback is an instance method of the
    subscriber itself. A lambda that captures "this" has a compiler-generated
    closure as its target and is held STRONGLY until you Unsubscribe. Prefer
    method groups and unsubscribe in Dispose.
  * Dead subscriptions are purged when Unsubscribe runs for the same key.

SimpleServiceResolver and DI
----------------------------
    public interface ISimpleServiceResolver : IServiceProvider
        T GetService<T>() where T : class;
        IEnumerable<T> GetServices<T>() where T : class;
    public interface IHostBuilderProvider
        IHostBuilder CreateDefaultBuilder();
        IHostBuilder CreateDefaultBuilder(string[] args);
    public interface IAutoRegisterServices
        void RegisterServices(IServiceCollection services);
        // implementors need a public parameterless constructor

    public class SimpleServiceResolver : ISimpleServiceResolver
        public static SimpleServiceResolver Instance { get; }
            // InvalidOperationException before CreateInstance
        public static void CreateInstance(IHostBuilderProvider host,
            Action<IServiceCollection> configureServices, string[] args = null);
        public static void CreateInstance(IHost host);
        public Task StartupHost();     // IHost.StartAsync
        public Task ShutdownHost();    // IHost.StopAsync + Dispose
        public object GetService(Type serviceType);      // null when missing
        public T GetService<T>() where T : class;        // GetRequiredService: throws
        public IEnumerable<T> GetServices<T>() where T : class;

    public static class SimpleServiceExtensions
        public static bool IsRegistered(this IServiceCollection services,
            Type serviceType);   // any registered ServiceType assignable to it
        public static bool IsRegistered<TService>(this IServiceCollection services);
        public static IServiceCollection AutoRegisterServices(
            this IServiceCollection services, IList<Assembly> fromAssemblies);
        public static IServiceCollection AutoRegisterServices(
            this IServiceCollection services, IList<Type> fromAssembliesContainingTypes);
        public static IServiceCollection AddSimpleMessaging(
            this IServiceCollection services);

What CreateInstance(IHostBuilderProvider, ...) does, in order:
host.CreateDefaultBuilder([args]) -> ConfigureServices { your
configureServices(services); register ISimpleServiceResolver unless already
registered; AutoRegisterServices on the CodeBrix.Platform.WinUI assembly ONLY;
AddSimpleMessaging(); } -> Build(). It does NOT start the host: call
await SimpleServiceResolver.Instance.StartupHost() yourself if you register
IHostedService implementations.
What CreateInstance(IHost) does: wraps your prebuilt host and nothing else. You
must have called services.AddSimpleMessaging() yourself, or every Messaging*
helper throws (ISimpleMessaging is resolved with GetRequiredService).

The IHostBuilderProvider your app supplies (verbatim from the samples):

    using CodeBrix.Platform.Simple;
    using Microsoft.Extensions.Hosting;          // app-owned package

    namespace MyApp.Helpers;

    public static class HostHelper
    {
        private class HostBuilderProvider : IHostBuilderProvider
        {
            public IHostBuilder CreateDefaultBuilder() => Host.CreateDefaultBuilder();
            public IHostBuilder CreateDefaultBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args);
        }

        private static readonly HostBuilderProvider _hostBuilderProvider = new();

        public static IHostBuilderProvider GetHost() => _hostBuilderProvider;
    }

SimpleEnum (rich enum metadata)
-------------------------------
    public interface ISimpleEnumInfo
        string Description { get; }
        Type EnumType { get; }
    public interface ISimpleEnumInfoAttribute
        Type InfoType { get; }
        string InfoMemberName { get; }

    public abstract class SimpleEnumInfo<TEnum> : ISimpleEnumInfo where TEnum : Enum
        protected SimpleEnumInfo(TEnum member);   // undefined member ->
                                                  //   ArgumentOutOfRangeException
        public TEnum Member { get; }
        public string Description { get; protected set; }
        public Type EnumType { get; }             // typeof(TEnum)
        protected static TInfo FindInfo<TInfo>(TEnum member)
            where TInfo : class, ISimpleEnumInfo;
        protected static Dictionary<TEnum, TInfo> GetDictionary<TInfo>()
            where TInfo : class, ISimpleEnumInfo;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SimpleEnumAttribute<TInfo> : Attribute, ISimpleEnumInfoAttribute
        where TInfo : class, ISimpleEnumInfo
        public SimpleEnumAttribute(string infoMemberName);

    public static class SimpleEnumHelper
        public static TInfo FindMemberInfo<TInfo>(string memberName)
            where TInfo : class, ISimpleEnumInfo;             // case-insensitive
        public static TInfo FindMemberInfo<TEnum, TInfo>(TEnum member)
            where TEnum : Enum where TInfo : class, ISimpleEnumInfo;
        public static Dictionary<TEnum, TInfo> GetInfoDictionary<TEnum, TInfo>()
            where TEnum : Enum where TInfo : class, ISimpleEnumInfo;
            // members without an attribute map to null
        public static IList<TInfo> GetPossibleValues<TEnum, TInfo>()
            where TEnum : Enum where TInfo : class, ISimpleEnumInfo;
            // the non-null, distinct infos

Contract: the info class exposes one PUBLIC STATIC property OF ITS OWN TYPE per
enum member, and the attribute on the enum member names that property. Two
SimpleEnum attributes on one member throw TypeLoadException at first lookup.
Lookups are cached per enum/info type.

    public enum Shipping
    {
        [SimpleEnum<ShippingInfo>(nameof(ShippingInfo.Standard))] Standard = 0,
        [SimpleEnum<ShippingInfo>(nameof(ShippingInfo.Express))]  Express,
    }

    public sealed class ShippingInfo : SimpleEnumInfo<Shipping>
    {
        public int Days { get; }

        public ShippingInfo(Shipping member, string description, int days)
            : base(member)
        {
            Description = description;
            Days = days;
        }

        public static ShippingInfo Standard => new(Shipping.Standard, "Standard (5 days)", 5);
        public static ShippingInfo Express  => new(Shipping.Express,  "Express (1 day)",   1);

        public static Dictionary<Shipping, ShippingInfo> GetDictionary() =>
            GetDictionary<ShippingInfo>();
    }

    var info   = SimpleEnumHelper.FindMemberInfo<Shipping, ShippingInfo>(Shipping.Express);
    var days   = info.Days;                                                   // 1
    var all    = SimpleEnumHelper.GetPossibleValues<Shipping, ShippingInfo>(); // 2 infos
    var byName = SimpleEnumHelper.FindMemberInfo<ShippingInfo>("standard");   // case-insensitive
    var picker = ShippingInfo.GetDictionary().Values.Select(v => v.Description);

SimpleOsInfo
------------
    public enum IdentifiedLinuxDistro
        { Unknown = 0, Alpine, Debian, Ubuntu, Mint, LMDE, Android, NotLinux = 999 }

    public class OsVersionInfo
        public string VersionNumber { get; set; }
        public int MajorVersion { get; set; }
        public int? MinorVersion { get; set; }
        public int? BuildVersion { get; set; }
        public int? RevisionVersion { get; set; }
        public bool? IsLongTermSupported { get; set; }
        public string VersionCodename { get; set; }
        public string BasedOnVersion { get; set; }
        public string ProductName { get; set; }
        public string ProductNameDisplay { get; set; }
        public string FullVersion { get; }

    public class RunUnixShellCommandResult
        public RunUnixShellCommandResult(bool isComplete = false);
        public string Output { get; set; }
        public string Error { get; set; }
        public Exception Exception { get; set; }
        public bool IsComplete { get; }
        public bool IsError { get; }
        public bool IsEmptyOutput { get; }
        public string[] OutputLines { get; }
        public void SetComplete();

    public class SimpleOsInfo
        public static Task<SimpleOsInfo> GatherInfo(bool withConsoleOutput = false);
            // = new SimpleOsInfo().Gather(withConsoleOutput)
        public Task<SimpleOsInfo> Gather(bool withConsoleOutput = false); // returns this
        public static Task<RunUnixShellCommandResult> RunUnixShellCommand(
            string command, string args = null, bool ignoreWarnings = true,
            bool showOutput = true, int? postRunWaitSeconds = null);
        public IdentifiedLinuxDistro IdentifyLinuxDistro(string distroInfo);
        public bool IsUnixRootUser(string username);
        // populated by Gather():
        public IdentifiedLinuxDistro LinuxDistro { get; }
        public OsVersionInfo OsVersionInfo { get; }
        public string RunningAsUser { get; }
        public bool? IsAdminUser { get; }
        // computed:
        public bool IsWindows { get; }   public bool IsMacOs { get; }
        public bool IsLinux { get; }     public bool IsAndroid { get; }
        public bool IsX64 { get; }       public bool IsArm64 { get; }
        public string OsDescription { get; }        // RuntimeInformation.OSDescription
        public string OsVersion { get; }            // OsVersionInfo.FullVersion or ""
        public string ProductName { get; }
        public string ProductNameDisplay { get; }
        public string LinuxDistroName { get; }
        public string PlatformOsName { get; }       // "Microsoft Windows", "Apple macOS",
                                                    //   "Android", "Linux (<distro>)"
        public string DotNetVersion { get; }        // RuntimeEnvironment.GetSystemVersion()
        public string PlatformArchitecture { get; } // RuntimeInformation.OSArchitecture
        public const string LinuxListTextFileCommand = "cat";
        public const string LinuxIdentifyDistroArgs = "/etc/issue";
        public const string DebianVersionArgs = "/etc/debian_version";
        public const string LinuxOsReleaseDetailsArgs = "/etc/os-release";
        public const string MacOsInfoCommand = "system_profiler";
        public const string MacOsInfoCommandArgs = "SPSoftwareDataType";
        public const string UnixUsernameCommand = "whoami";
        public static char[] UnixLineSplitters { get; }

On Windows, Gather() reads Environment.OSVersion and the current WindowsIdentity
(IsAdminUser = membership of the Administrators role). The macOS and Linux
branches (Debian, Ubuntu, Mint, LMDE detailed; Alpine identified only) shell
out through RunUnixShellCommand; they are present because the same source ships
in the WPF and MAUI editions. IsAndroid on WinUI is derived from
AnalyticsInfo.VersionInfo.DeviceFamily.

CORE API REFERENCE - SKIA IMAGE CONTROLS (Skia package)
=======================================================
namespace CodeBrix.Platform.WinUI.Controls

EmbeddedImage
-------------
    public sealed class EmbeddedImage : ContentControl
        public static readonly DependencyProperty UriSourceProperty;
        public string UriSource { get; set; }        // default null
        public static readonly DependencyProperty StretchProperty;
        public Stretch Stretch { get; set; }         // Microsoft.UI.Xaml.Media;
                                                     //   default Stretch.Uniform

Loading rules - a UriSource change starts an async load; ANY failure is written
to Debug output ("[EmbeddedImage] Failed to load from '...'") and the control
shows nothing; no exception reaches you:
    embedded://AssemblyName/Resource.Name
        AssemblyName = simple name of an assembly ALREADY LOADED in the process
        (matched against AppDomain.CurrentDomain.GetAssemblies()).
        Resource.Name = the exact manifest resource name, i.e. the
        <EmbeddedResource> LogicalName (or the default
        RootNamespace.Folder.File.ext when no LogicalName is set).
    any other URI (ms-appx:///Assets/x.png, https://host/x.png)
        non-SVG -> BitmapImage { UriSource = uri }
        .svg    -> Windows.Storage.StorageFile.GetFileFromApplicationUriAsync,
                   so application URIs only; an https:// SVG fails.
A ".svg" extension (case-insensitive) selects the vector path: the bytes are
parsed by CodeBrix.SkiaSvg (SKSvg) off the UI thread and drawn on an
SKXamlCanvas at the final physical-pixel size, so it stays sharp at any scale
factor. Everything else is a plain BitmapImage inside an Image.
Sizing: natural size = the SVG's CullRect / the bitmap size, adjusted by
Stretch; set Width/Height to constrain.

EmbeddedImageButton
-------------------
    public enum ImagePosition { Left, Top, Right, Bottom }

    public sealed class EmbeddedImageButton : Button
        // every property below is a dependency property (XxxProperty static
        // field) and any change rebuilds the button content
        public string ImageUriSource { get; set; }   // same schemes as
                                                     //   EmbeddedImage.UriSource
        public string Text { get; set; }             // default null
        public ImagePosition ImagePosition { get; set; }  // default Left
                                                     //   (image relative to text)
        public double Spacing { get; set; }          // image/text gap; default 10
        public double ImageWidth { get; set; }       // default NaN = automatic
        public double ImageHeight { get; set; }      // default NaN = automatic
        public VerticalAlignment TextVerticalAlignment { get; set; }     // Center
        public HorizontalAlignment TextHorizontalAlignment { get; set; } // Center
        // static fields: ImageUriSourceProperty, TextProperty,
        //   ImagePositionProperty, SpacingProperty, ImageWidthProperty,
        //   ImageHeightProperty, TextVerticalAlignmentProperty,
        //   TextHorizontalAlignmentProperty

Behaviour: string element content
(<controls:EmbeddedImageButton>Save</controls:EmbeddedImageButton>) is
redirected into Text. The button builds its own Content: a StackPanel
(Horizontal for Left/Right, Vertical for Top/Bottom) holding an EmbeddedImage
and a TextBlock, or just one of them when only ImageUriSource or only Text is
set; no image and no text gives empty content. Default CornerRadius is 4.
Command, CommandParameter, Background, Width, Height and the rest are ordinary
Button members.

ImageSizeHelper
---------------
namespace CodeBrix.Platform.WinUI.Skia
    public static class ImageSizeHelper
        public static (double x, double y) BuildScale(Stretch stretch,
            Size destinationSize, Size sourceSize);
        public static Size AdjustSize(Stretch stretch, Size availableSize,
            Size measuredSize);
Stretch-aware scale math: None -> (1, 1); Uniform -> min of the two axis
scales; UniformToFill -> max; Fill -> per-axis. An infinite destination axis
falls back to the finite one (1.0 when both are infinite). AdjustSize never
exceeds a finite available axis. Public so your own Skia-drawn controls size
exactly like EmbeddedImage and the Lottie player.

CORE API REFERENCE - LOTTIE PLAYER (Lottie package)
===================================================
namespace CodeBrix.Platform.WinUI.Lottie

Interfaces and options
----------------------
    public interface IAnimatedVisualSource
        void Update(AnimatedVisualPlayer? player);  // attach (null = detach);
                                                    //   starts loading
        Size Measure(Size availableSize);
        void Play(double fromProgress, double toProgress, bool looped); // 0..1
        void Stop();
        void Pause();
        void Resume();
        void SetProgress(double progress);  // 0..1 clamped; stops and shows frame
        void Load();    // called by the player on Loaded
        void Unload();  // called by the player on Unloaded
    public interface IAnimatedVisualSourceWithUri : IAnimatedVisualSource
        Uri? UriSource { get; set; }
    public interface IThemableAnimatedVisualSource : IAnimatedVisualSource
        void SetColorThemeProperty(string propertyName, Windows.UI.Color? color);
        Windows.UI.Color? GetColorThemeProperty(string propertyName);
    public enum LottieVisualOptions { None = 0, Optimize = 1,
        IncludeDiagnostics = 2, All = 3 }   // accepted for API parity; no effect

AnimatedVisualPlayer
--------------------
    [ContentProperty(Name = "Source")]
    public partial class AnimatedVisualPlayer : Panel
        // dependency properties (XxxProperty static fields exist for each)
        public IAnimatedVisualSource? Source { get; set; } // XAML content property
        public bool AutoPlay { get; set; }       // default true: Play(0, 1, true)
                                                 //   as soon as the JSON loads
        public Stretch Stretch { get; set; }     // default Uniform
        public double PlaybackRate { get; set; } // default 1.0; read every frame
        public TimeSpan Duration { get; }        // set by the source; read-only
                                                 //   in practice (TimeSpan.Zero
                                                 //   until loaded)
        public bool IsAnimatedVisualLoaded { get; }  // set by the source
        public bool IsPlaying { get; }               // set by the source
        public Task PlayAsync(double fromProgress, double toProgress, bool looped);
            // completes when IsPlaying becomes false: at toProgress for a
            // one-shot, or on Stop() when looped; already complete when
            // Source is null
        public void Stop();
        public void Pause();
        public void Resume();
        public void SetProgress(double progress);

Background: a SolidColorBrush Background becomes the canvas clear colour
(alpha x Opacity); anything else clears to transparent. Layout: Source.Measure
is used - with Stretch.None the composition size; otherwise the player fills
the space it is given (aspect-preserving only when one axis is unconstrained),
so give it Width/Height inside a Grid cell. Before the JSON is loaded the
measured size is 0 x 0; the source invalidates layout when loading completes.

LottieVisualSourceBase / LottieVisualSource / ThemableLottieVisualSource
-----------------------------------------------------------------------
    public abstract partial class LottieVisualSourceBase
        : DependencyObject, IAnimatedVisualSource, IAnimatedVisualSourceWithUri
        public delegate void UpdatedAnimation(string animationJson, string cacheKey);
        public static DependencyProperty UriSourceProperty { get; }
        public Uri? UriSource { get; set; }            // changing it reloads
        public static DependencyProperty OptionsProperty { get; }
        public LottieVisualOptions Options { get; set; }   // no effect
        public Task SetSourceAsync(Uri sourceUri);     // sets UriSource; returns
                                                       //   BEFORE the load finishes
        public static LottieVisualSource CreateFromString(string uri);
                                                       // throws NotImplementedException
        public void Update(AnimatedVisualPlayer? player);
        public void Play(double fromProgress, double toProgress, bool looped);
            // before the JSON is loaded the request is remembered and started
            // once it is
        public void Stop();  public void Pause();  public void Resume();
        public void SetProgress(double progress);
        public void Load();  public void Unload();
        protected abstract bool IsPayloadNeedsToBeUpdated { get; }
        protected virtual IDisposable? LoadAndObserveAnimationData(
            IInputStream sourceJson, string sourceCacheKey,
            UpdatedAnimation updateCallback);

    [Bindable] public partial class LottieVisualSource : LottieVisualSourceBase
        // JSON is fed to the renderer unmodified

    [Bindable] public partial class ThemableLottieVisualSource
        : LottieVisualSourceBase, IThemableAnimatedVisualSource
        public void SetColorThemeProperty(string propertyName, Color? color);
            // before load: remembered and applied on load; after load: the
            // JSON is rewritten, re-emitted and the animation reloaded
        public Color? GetColorThemeProperty(string propertyName);
            // the pending value if any, else the applied one, else null

URI schemes (UriSource):
    embedded://AssemblyName/Resource.Name
        the host is passed to Assembly.Load(); host "." means the
        application's own assembly; the literal text "(assembly)" in the path
        is replaced by that assembly's name
    ms-appx:///Assets/animation.json
        a Content item in the app (CopyToOutputDirectory)
    ms-appdata:///local/... | ms-appdata:///roaming/... | ms-appdata:///temp/...
        the ApplicationData Local / Roaming / Temporary folders
    http:// and https://
        ThemableLottieVisualSource only (downloaded with HttpClient);
        LottieVisualSource logs "Failed to load animation" and stays blank

Playback engine: a DispatcherQueueTimer ticks at max(1/120 s, 1/fps) and
repaints an SKXamlCanvas hosted by the player; PlaybackRate is applied per
frame; a one-shot play freezes at toProgress and calls Stop(). Player Unloaded
pauses (remembering it was playing) and Loaded resumes.

Theming contract (ThemableLottieVisualSource): name the SHAPE in the Lottie
file with a CSS-var-style binding. The real assets in this repository use:

    { Color : var(Foreground) }
    { Color : var(Background) }

Parser rules, as implemented: a binding block is "{ name : var(Binding) }" and
several bindings may be separated by ";" ("{ Color: var(A); Color1: var(B) }");
whitespace is optional and identifiers start with a letter. ONLY the property
name "Color" is honoured and the comparison is ordinal, so "{color: var(x)}" is
silently ignored. Groups ("gr") are searched recursively, but only shapes under
the document's top-level "layers" are scanned (precomp assets are not). Only a
shape's static colour ("c": {"k": [r, g, b, a]}) is rewritten; keyframed
colours are left alone. Colour components are written as 0..1 floats from the
Windows.UI.Color you pass. Each SetColorThemeProperty after load restarts the
animation from the remembered play range.

COMPLETE EXAMPLES
=================
Files for a minimal WinUI app "MyApp" that uses all three packages. HostHelper
is shown in the DI section above.

App.xaml.cs
-----------
    using CodeBrix.Platform.Simple;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using MyApp.Helpers;
    using MyApp.Services;

    namespace MyApp;

    public partial class App : Application
    {
        private Window _window;

        public App()
        {
            SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
            {
                services.AddSingleton<IGreetingService, GreetingService>();
            });
            SimpleViewModel.SetIsDesignMode(false);

            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new Window();
            var frame = new Frame();
            _window.Content = frame;
            frame.Navigate(typeof(Views.MainPage));
            _window.Activate();
        }
    }

Services/GreetingService.cs
---------------------------
    using System.Threading.Tasks;

    namespace MyApp.Services;

    public interface IGreetingService
    {
        Task<string> ComposeAsync(string name);
    }

    public sealed class GreetingService : IGreetingService
    {
        public Task<string> ComposeAsync(string name) => Task.FromResult($"Hello, {name}!");
    }

ViewModels/MainViewModel.cs (compiles unchanged for WPF and MAUI too)
---------------------------------------------------------------------
    using CodeBrix.Platform.Simple;
    using MyApp.Services;
    using System;
    using System.Threading.Tasks;

    namespace MyApp.ViewModels;

    public sealed class MainViewModel : SimpleViewModel
    {
        private IGreetingService _greetings;

        public MainViewModel()
        {
            if (!IsDesignMode(true))   // skip service resolution in a designer
            {
                _greetings = GetService<IGreetingService>();
            }
        }

        private string _name = string.Empty;
        [AffectsProperties(nameof(Greeting))]
        [AffectsCommands(nameof(GreetCommand))]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        public string Greeting =>
            string.IsNullOrWhiteSpace(Name) ? string.Empty : $"Hello, {Name}!";

        private bool _isBusy;
        [AffectsAllCommands]
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, notifyOnMainThread: true);
        }

        private SimpleCommand _greetCommand;
        public SimpleCommand GreetCommand =>
            _greetCommand ??= new SimpleCommand(CanGreet, DoGreetAsync);

        private bool CanGreet() => !IsBusy && !string.IsNullOrWhiteSpace(Name);

        private async Task DoGreetAsync()
        {
            try
            {
                IsBusy = true;
                var text = await _greetings.ComposeAsync(Name.Trim());
                await ShowInfo(text);
                MessagingSend(this, "Greeted", Name);
            }
            catch (Exception ex)
            {
                await ShowError(ex, "Could not greet");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private SimpleCommand _resetCommand;
        public SimpleCommand ResetCommand =>
            _resetCommand ??= new SimpleCommand(DoResetAsync);

        private async Task DoResetAsync()
        {
            if (await ConfirmDialog("Clear the name?"))
            {
                Name = string.Empty;
            }
        }

        public override void Dispose()
        {
            _greetCommand?.Dispose();
            _greetCommand = null;
            _resetCommand?.Dispose();
            _resetCommand = null;
            _greetings = null;
            base.Dispose();
        }
    }

Views/MainPage.xaml
-------------------
    <Page
        x:Class="MyApp.Views.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        xmlns:controls="using:CodeBrix.Platform.WinUI.Controls"
        xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie">

        <Page.DataContext>
            <vm:MainViewModel />
        </Page.DataContext>

        <StackPanel Margin="20" Spacing="12">
            <TextBox Text="{Binding Name, Mode=TwoWay,
                                   UpdateSourceTrigger=PropertyChanged}" />
            <TextBlock Text="{Binding Greeting}" />

            <controls:EmbeddedImageButton
                Command="{Binding GreetCommand}"
                ImageUriSource="embedded://MyApp/MyApp.Assets.wave-icon.svg"
                Text="Greet" ImagePosition="Left"
                ImageWidth="24" ImageHeight="24" Spacing="6" />

            <controls:EmbeddedImageButton
                Command="{Binding ResetCommand}"
                ImageUriSource="ms-appx:///Assets/reset.png">Reset</controls:EmbeddedImageButton>

            <controls:EmbeddedImage
                UriSource="embedded://MyApp/MyApp.Assets.logo.svg"
                Width="120" Height="120" Stretch="Uniform" />

            <lottie:AnimatedVisualPlayer x:Name="Spinner" AutoPlay="True"
                                         Width="48" Height="48">
                <lottie:LottieVisualSource UriSource="ms-appx:///Assets/spinner.json" />
            </lottie:AnimatedVisualPlayer>
        </StackPanel>
    </Page>

Views/MainPage.xaml.cs
----------------------
    using CodeBrix.Platform.Simple;
    using Microsoft.UI.Xaml.Controls;

    namespace MyApp.Views;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            // Subscribe BEFORE InitializeComponent(): that call assigns the
            // DataContext declared in XAML.
            DataContextChanged += (_, _) =>
                (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

            InitializeComponent();
        }
    }

Asset items in MyApp.csproj
---------------------------
    <ItemGroup>
      <EmbeddedResource Include="Assets\wave-icon.svg">
        <LogicalName>MyApp.Assets.wave-icon.svg</LogicalName>
      </EmbeddedResource>
      <EmbeddedResource Include="Assets\logo.svg">
        <LogicalName>MyApp.Assets.logo.svg</LogicalName>
      </EmbeddedResource>
      <Content Include="Assets\reset.png" />
      <Content Include="Assets\spinner.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </Content>
    </ItemGroup>

Driving the player and re-colouring a Lottie from code-behind
-------------------------------------------------------------
    using CodeBrix.Platform.WinUI.Lottie;

    // one-shot: play the first half and wait for it to finish
    await Spinner.PlayAsync(0, 0.5, looped: false);
    Spinner.SetProgress(1.0);       // jump to the last frame, stopped
    Spinner.Pause();  Spinner.Resume();  Spinner.Stop();

    // run-time re-colouring: shapes named "{ Color : var(Foreground) }"
    var themed = new ThemableLottieVisualSource
    {
        UriSource = new Uri("ms-appx:///Assets/spinner.json")
    };
    themed.SetColorThemeProperty("Foreground",
        Windows.UI.Color.FromArgb(255, 0, 120, 215));
    Spinner.Source = themed;        // may also be set before the colours
    Windows.UI.Color? current = themed.GetColorThemeProperty("Foreground");

Messaging between view models
-----------------------------
    // Receiver: method group => weakly held; dies with the subscriber.
    public sealed class HistoryViewModel : SimpleViewModel
    {
        public ObservableCollection<string> Names { get; } = new();

        public HistoryViewModel()
        {
            MessagingSubscribe<MainViewModel, string>(this, "Greeted", OnGreeted, null);
        }

        private void OnGreeted(MainViewModel sender, string name) =>
            InvokeOnMainThread(() => Names.Add(name));

        public override void Dispose()
        {
            MessagingUnsubscribe<MainViewModel, string>(this, "Greeted");
            base.Dispose();
        }
    }

Showing OS information
----------------------
    var os = await SimpleOsInfo.GatherInfo();
    await ShowInfo($"{os.PlatformOsName} {os.OsVersion} ({os.PlatformArchitecture})"
        + $"\nuser {os.RunningAsUser}{(os.IsAdminUser is true ? " (admin)" : "")}"
        + $"\n.NET {os.DotNetVersion}");

MINIMUM VIABLE PROJECT
======================
MyApp.csproj (a Windows App SDK app; App.xaml, app.manifest and the Assets
folder are whatever the WinUI app template generated):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
        <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
        <UseWinUI>true</UseWinUI>
        <RootNamespace>MyApp</RootNamespace>
        <Platforms>x86;x64;ARM64</Platforms>
        <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <EnableMsixTooling>true</EnableMsixTooling>
      </PropertyGroup>

      <ItemGroup>
        <!-- Version attributes omitted here on purpose; `dotnet add package`
             writes the current ones. -->
        <PackageReference Include="CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever" />
        <!-- (Lottie brings Skia and Core. Reference only Core or only Skia if
             that is all you use.) -->
        <PackageReference Include="Microsoft.Extensions.Hosting" />
        <!-- app-owned: Host.CreateDefaultBuilder() for IHostBuilderProvider -->
        <PackageReference Include="Microsoft.WindowsAppSDK" />
        <PackageReference Include="Microsoft.Windows.SDK.BuildTools" />
        <!-- the two above are what the WinUI app template already adds -->
        <Manifest Include="$(ApplicationManifest)" />
      </ItemGroup>
    </Project>

Source files: App.xaml + App.xaml.cs, Helpers/HostHelper.cs,
Services/GreetingService.cs, ViewModels/MainViewModel.cs,
Views/MainPage.xaml + .xaml.cs - all shown in COMPLETE EXAMPLES.

Placing view models in a separate class library: that library must also target
net10.0-windows10.0.19041.0 with UseWinUI=true and reference the Core package;
so must any test project that instantiates them.

PERFORMANCE TIPS
================
  * EmbeddedImage parses an SVG once per UriSource change (off the UI thread)
    and keeps the SKPicture; each paint is one DrawPicture at the current
    scale. Bind UriSource once; do not re-assign it per frame.
  * Every EmbeddedImageButton property change rebuilds its content tree. Set
    the properties in XAML (one pass) rather than one at a time from code in a
    hot path.
  * A playing Lottie repaints a software canvas at its fps (capped at 120 Hz).
    Keep players small, avoid many simultaneous players, and call Stop() (or
    let Unloaded pause them) when they leave the screen.
  * ThemableLottieVisualSource re-serialises the whole JSON and rebuilds the
    Skottie animation on every SetColorThemeProperty after load. Set all
    colours before assigning Source; never animate a colour through it.
  * The Affects* cascade reflects over the view model's properties on every
    notification. That is fine for form-sized view models; for a value that
    changes thousands of times per second, assign the field and notify once.
  * Async SimpleMessaging callbacks are serialised per subscription: a slow
    handler delays later messages to that subscriber only.
  * SimpleOsInfo.GatherInfo() does I/O; gather once and cache the instance
    (the samples use  _osInfo ??= await SimpleOsInfo.GatherInfo()).

COMMON PITFALLS TO AVOID
========================
  1. Using SimpleServiceResolver.Instance, GetService<T>() or any Messaging*
     helper before CreateInstance -> InvalidOperationException ("...
     CreateInstance() static method must be called at application start").
     Call CreateInstance in the App constructor BEFORE InitializeComponent();
     XAML can construct view models during initialisation.
  2. Forgetting SimpleViewModel.SetIsDesignMode(false) at startup: WinUI has no
     designer detection, so IsDesignMode(true) returns TRUE at run time and a
     view model that guards its constructor with it silently does nothing.
  3. Constructing a SimpleViewModel off the UI thread: the DispatcherQueue is
     captured in the constructor (DispatcherQueue.GetForCurrentThread()). Off
     the UI thread it is null, and InvokeOnMainThread, InvokeOnMainThreadAsync
     and every dialog helper then throw ArgumentNullException. Let XAML
     (<Page.DataContext>) or UI-thread code create view models.
  4. Dialogs before SetXamlRootGetter: ShowInfo / ShowError / ConfirmDialog
     throw InvalidOperationException ("Unable to perform the requested UI
     operation before SetXamlRootGetter() has been called"). Subscribe to
     DataContextChanged BEFORE InitializeComponent() as in MainPage above, so
     the getter is wired the moment the DataContext is assigned. A view model
     holds ONE getter; the last page to bind it wins.
  5. SetProperty<T> is constrained to reference types. Value types other than
     bool, int, DateTime, DateTimeOffset and enums (SetEnumProperty) have NO
     overload: for double, long, decimal, Guid, ... compare and assign
     yourself, then call ThisPropertyChanged() (or NotifyPropertyChanged).
  6. SetEnumProperty silently ignores the assignment when either value is not
     a defined member (flags combinations; a field defaulted to 0 when 0 is not
     a member).
  7. The Affects* cascade runs only from NotifyPropertyChanged; raising the
     PropertyChanged event yourself bypasses it. It also finds PUBLIC INSTANCE
     properties only: [AffectsCommands] on a private/internal property does
     nothing, and [AffectsAllCommands] refreshes only properties whose declared
     type is exactly SimpleCommand (not ICommand).
  8. SimpleCommand.Execute is async void: an exception thrown by your handler
     is an unhandled exception. Wrap the body in try/catch and call ShowError.
  9. CreateInstance(IHost) registers nothing extra: without your own
     services.AddSimpleMessaging() the Messaging* helpers throw on first use.
 10. AutoRegisterServices during CreateInstance scans only the
     CodeBrix.Platform.WinUI assembly. Register your own IAutoRegisterServices
     classes with services.AutoRegisterServices([typeof(App)]) inside
     configureServices.
 11. Messaging is type-exact (Send<Derived,...> never reaches
     Subscribe<Base,...>), and a lambda callback that captures "this" is held
     strongly. Use method groups and Unsubscribe in Dispose.
 12. embedded:// naming (Skia): the assembly must already be loaded (the app's
     own assembly always is; a class library's only once one of its types has
     been touched), and the resource name must be the exact manifest name. Set
     <LogicalName> on the <EmbeddedResource> and use that string. A wrong name
     renders nothing and only writes to Debug output.
 13. embedded:// naming (Lottie) is resolved differently: the host is passed
     to Assembly.Load (host "." = the app assembly) and "(assembly)" in the
     path expands to the assembly name.
 14. SVG over https:// is not supported by EmbeddedImage (an SVG needs an
     application URI); bitmaps over https:// are fine. LottieVisualSource does
     not download http(s) JSON at all; use ThemableLottieVisualSource for a
     remote file.
 15. Do not assign EmbeddedImageButton.Content yourself: the button rebuilds
     its Content whenever one of its own properties changes. Only string
     content is captured (into Text).
 16. AnimatedVisualPlayer fills the available space when Stretch is not None:
     inside a Grid cell without Width/Height it takes the whole cell.
 17. Lottie theming: the shape name must read "{ Color : var(Name) }" with a
     capital C; "{color: var(Name)}" is silently ignored. Only static colours
     ("c":{"k":[...]}) under top-level layers are rewritten.
 18. Duration, IsAnimatedVisualLoaded and IsPlaying are written BY the source;
     do not set them, and do not read Duration before IsAnimatedVisualLoaded
     is true. SetSourceAsync returns before the animation is ready.
 19. PlayAsync on a looped animation completes only when Stop() is called;
     awaiting it on the UI thread without a Stop() waits forever.
 20. A class library or test project that references the Core package must
     target net10.0-windows10.0.19041.0; a plain net10.0 project cannot
     reference it.

WHAT THIS PACKAGE DOES NOT DO
=============================
  * No navigation framework, no view-model locator, no Frame/Window helpers.
  * No designer detection on WinUI: IsDesignMode only reports what
    SetIsDesignMode stored, or your default.
  * No localisation: dialog titles and button labels are English constants.
  * No custom dialog UI: SimpleDialog shows text with one or two buttons (OK,
    OKCancel, YesNo). No input dialogs, no three-button dialogs.
  * SimpleMessaging has no request/response, no awaitable Send and no ordering
    guarantees between subscribers.
  * SimpleServiceResolver is not a general container: it wraps one Generic
    Host, has no scopes or child containers, and does not start the host.
  * SimpleOsInfo reports OS, user and architecture only; nothing about
    windows, displays or hardware.
  * Skia package: only the two controls and ImageSizeHelper. Bitmaps are
    decoded by BitmapImage, not Skia; no image processing, no SVG animation or
    scripting, no drop-in replacement for SvgImageSource.
  * Lottie package: does not use or accept the Windows App SDK
    AnimatedVisualPlayer or Composition/Win2D IAnimatedVisual sources - this
    player is its own Panel. LottieVisualOptions (Optimize,
    IncludeDiagnostics) are accepted and ignored; CreateFromString throws
    NotImplementedException; there is no Diagnostics output; only the "Color"
    binding can be themed.

WORKING EXAMPLES ON GITHUB
==========================
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/Platforms/JustBetweenUs
        JustBetweenUs.WinUI/ - the WinUI head, which uses all three packages:
          App.xaml.cs             CreateInstance + SetIsDesignMode in the App
                                  constructor, Frame navigation in OnLaunched
          Views/MainPage.xaml     EmbeddedImageButton with embedded:// SVGs
                                  (ImagePosition="Top", ImageWidth/Height,
                                  Spacing) and string content; an
                                  AnimatedVisualPlayer + LottieVisualSource
                                  loaded from ms-appx:///Assets/star_icon.json
          Views/MainPage.xaml.cs  SetXamlRootGetter inside DataContextChanged,
                                  subscribed before InitializeComponent()
          JustBetweenUs.WinUI.csproj  <EmbeddedResource> + <LogicalName> for
                                  the SVGs, <Content> for the Lottie JSON
        Shared/Helpers/HostHelper.cs - the IHostBuilderProvider implementation,
          Compile-linked into the head as Helpers/HostHelper.cs.
        Shared/ViewModels/MainViewModel.cs - a SimpleViewModel with lazy
          SimpleCommand properties, [AffectsCommands], SetProperty, ShowInfo /
          ShowError, InvokeOnMainThread and SimpleOsInfo.GatherInfo, shared
          unchanged with the WPF and .NET MAUI heads beside it.
        Shared/ViewModels/EncryptionMode.cs - a SimpleEnumInfo<TEnum> class
          with [SimpleEnum<TInfo>(...)] members and a GetDictionary() helper.

QUICK REFERENCE CARD
====================
    // startup (App constructor, before InitializeComponent)
    SimpleServiceResolver.CreateInstance(HostHelper.GetHost(),
        s => s.AddSingleton<IFoo, Foo>());
    SimpleViewModel.SetIsDesignMode(false);
    // page constructor, before InitializeComponent
    DataContextChanged += (_, _) =>
        (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

    // view model
    public sealed class Vm : SimpleViewModel
    set => SetProperty(ref _field, value);   // class/string/bool/int/DateTime/
                                             //   DateTimeOffset
    SetEnumProperty(ref _mode, value);       // enums
    [AffectsProperties("A", "B")]  [AffectsCommands(nameof(Cmd))]  [AffectsAllCommands]
    public SimpleCommand Cmd => _cmd ??= new SimpleCommand(CanDo, DoAsync);
    await ShowInfo(text);   await ShowError(ex);   await ShowError("msg", details);
    if (await ConfirmDialog("Sure?")) { ... }
    InvokeOnMainThread(() => ...);
    var r = await InvokeOnMainThreadAsync(async () => await WorkAsync());
    var svc = GetService<IFoo>();
    MessagingSend(this, "Msg", args);
    MessagingSubscribe<SenderVm, ArgsType>(this, "Msg", OnMsg, null);
    MessagingUnsubscribe<SenderVm, ArgsType>(this, "Msg");
    var os = await SimpleOsInfo.GatherInfo();

    // enum metadata
    [SimpleEnum<FooInfo>(nameof(FooInfo.Bar))] Bar,
    var info = SimpleEnumHelper.FindMemberInfo<Foo, FooInfo>(Foo.Bar);
    var all  = SimpleEnumHelper.GetPossibleValues<Foo, FooInfo>();

    // XAML (xmlns:controls="using:CodeBrix.Platform.WinUI.Controls",
    //       xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie")
    <controls:EmbeddedImage UriSource="embedded://MyApp/MyApp.Assets.logo.svg"
                            Width="64" Height="64" Stretch="Uniform" />
    <controls:EmbeddedImageButton ImageUriSource="ms-appx:///Assets/x.png"
        Text="Save" ImagePosition="Left" ImageWidth="24" ImageHeight="24"
        Spacing="6" Command="{Binding SaveCommand}" />
    <lottie:AnimatedVisualPlayer AutoPlay="True" Width="48" Height="48"
                                 PlaybackRate="1.0" Stretch="Uniform">
        <lottie:LottieVisualSource UriSource="ms-appx:///Assets/anim.json" />
    </lottie:AnimatedVisualPlayer>

    // Lottie from code
    await player.PlayAsync(0, 1, looped: false);
    player.Stop();  player.Pause();  player.Resume();  player.SetProgress(0.5);
    var themed = new ThemableLottieVisualSource { UriSource = new Uri("ms-appx:///Assets/anim.json") };
    themed.SetColorThemeProperty("Foreground", Windows.UI.Color.FromArgb(255, r, g, b));
    //   (shape named "{ Color : var(Foreground) }" in the JSON)
    player.Source = themed;
================================================================================
