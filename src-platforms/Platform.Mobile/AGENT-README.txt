================================================================================
AGENT-README: CodeBrix.Platform.Mobile
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.Mobile.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.Mobile.ApacheLicenseForever is the CodeBrix "Simple" MVVM
toolkit compiled for .NET MAUI applications on .NET 10 or later (Android, iOS,
Mac Catalyst, and Windows when built on Windows): SimpleViewModel
(INotifyPropertyChanged base with attribute-driven change cascades),
SimpleCommand (ICommand with sync/async handlers and MainThread marshalling),
SimpleDialog (Page.DisplayAlertAsync-backed dialogs), SimpleMessaging
(weak-reference publish/subscribe), SimpleServiceResolver (Generic-Host based
service resolution), SimpleEnum (rich enum metadata) and SimpleOsInfo (OS,
device and architecture facts). 25 public types in one namespace.

Provenance: this package is independent of the cross-platform CodeBrix.Platform
UI packages and shares no code with them at run time. The toolkit is original
CodeBrix code and is source-identical in the sibling packages
CodeBrix.Platform.WinUI.ApacheLicenseForever (see
src-platforms/Platform.WinUI/AGENT-README.txt) and
CodeBrix.Platform.WPF.ApacheLicenseForever (see
src-platforms/Platform.WPF/AGENT-README.txt): a view model written against
SimpleViewModel compiles unchanged for MAUI, WinUI and WPF. Only the
platform-facing edges differ, and this file documents the MAUI edges: the
Page-based IXamlRootGetter, DisplayAlertAsync dialogs, MainThread marshalling,
the MAUI Visibility enum and Android device information in SimpleOsInfo.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.Mobile.ApacheLicenseForever

Package id: CodeBrix.Platform.Mobile.ApacheLicenseForever
Assembly:   CodeBrix.Platform.Mobile
NuGet dependencies (by id): Microsoft.Extensions.DependencyInjection.Abstractions,
Microsoft.Extensions.Hosting.Abstractions, Microsoft.Maui.Controls,
Microsoft.Maui.Controls.Compatibility.

App-owned (NOT brought in by this package): Microsoft.Extensions.Hosting. The
package (and MAUI itself) references only Microsoft.Extensions.Hosting.Abstractions;
your app implements IHostBuilderProvider and returns Host.CreateDefaultBuilder(),
which lives in Microsoft.Extensions.Hosting. Add that package to the
application project.

License: Apache-2.0.

Requirements: a .NET MAUI application (UseMaui=true) targeting net10.0-android,
net10.0-ios, net10.0-maccatalyst and - when building on Windows -
net10.0-windows10.0.19041.0. The package's own minimum OS versions are
iOS 15.0, Mac Catalyst 15.0, Android API 21 and Windows 10.0.17763.0; the app's
SupportedOSPlatformVersion values must not be lower. A class library that
references this package must be a MAUI class library with the same target
frameworks.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.Simple;   // every type in this package

There is only one namespace. The MAUI types it uses (Page, MainThread,
DeviceInfo, Visibility) come from the Microsoft.Maui.* namespaces.

CORE API REFERENCE
==================

Startup contract (order matters)
--------------------------------
    1. SimpleServiceResolver.CreateInstance(...)  before the first Page (and
       its BindingContext) is created - the App constructor is the latest
       safe place, because CreateWindow() creates the Shell and its pages
       after it; MauiProgram.CreateMauiApp() before builder.Build() works too
    2. SimpleViewModel.SetIsDesignMode(false)     once, right after step 1
    3. every Page that hosts a view model hands it the Page:
       (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this)
       - BEFORE any dialog is shown

SimpleViewModel
---------------
    public abstract class SimpleViewModel
        : IXamlRootGetter, INotifyPropertyChanged, IDisposable

    // Design mode (MAUI cannot detect the designer: the value is whatever
    //   SetIsDesignMode stored, else your default)
    public static void SetIsDesignMode(bool isDesignMode);
    protected static bool IsDesignMode(bool defaultValueIfNotSet);

    // Page wiring - MAUI-specific, required by every dialog helper
    public void SetXamlRootGetter(Func<Page> getter);   // null -> ArgumentNullException
    protected Page GetXamlRoot();   // InvalidOperationException if never set

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

    // Dialogs - all marshalled to the main thread. Labels are English
    //   constants: "Information", "ERROR", "Are you sure?", "OK", "Cancel",
    //   "Yes", "No"
    protected virtual SimpleDialog CreateDialog(string message,
        string title = null, SimpleDialogButtons buttons = SimpleDialogButtons.OK);
        // = SimpleDialog.Create(<page getter>, message, title, buttons)
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

    // Threading - via Microsoft.Maui.ApplicationModel.MainThread
    protected virtual void InvokeOnMainThread(Action functionToExecute);
        // runs inline when already on the main thread, else
        // MainThread.BeginInvokeOnMainThread (does not wait)
    protected virtual Task<T> InvokeOnMainThreadAsync<T>(
        Func<Task<T>> functionToExecute);
        // awaits inline on the main thread, else MainThread.InvokeOnMainThreadAsync
    protected static Visibility GetVisibility(bool isVisible);
        // the MAUI Visibility enum: Visible / Hidden (never Collapsed)

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
    public virtual void Dispose();       // detaches every PropertyChanged handler
    protected virtual void Dispose(bool disposing);  // and drops the Page getter

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
    public async void RaiseCanExecuteChanged();  // async void on MAUI
    public event EventHandler CanExecuteChanged;
    public void Dispose();                       // clears delegates and handlers

Behaviour on MAUI: with ShouldExecuteOnMainThread the handler runs through
MainThread.InvokeOnMainThreadAsync - skipped when the call is already on the
main thread. RaiseCanExecuteChanged marshals through MainThread only when
ShouldRaiseCanExecuteOnMainThread is true AND the caller is not already on the
main thread. Execute awaits async handlers but is itself async void: an
exception escaping your handler is not observable by the caller - catch
inside.

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
    public static SimpleDialog Create(Func<Page> xamlRootGetter, string message,
        string title = null, SimpleDialogButtons buttons = SimpleDialogButtons.OK);
        // getter null -> ArgumentNullException
    public string Message { get; set; }   // trimmed; null becomes ""
    public string Title { get; set; }     // blank becomes null -> "" title
    public SimpleDialogButtons Buttons { get; set; }
    public Task<SimpleDialogResult> ShowAsync();  // ObjectDisposedException after Dispose
    public void Dispose();

    public interface IXamlRootGetter
        void SetXamlRootGetter(Func<Page> getter);

On MAUI ShowAsync calls xamlRootGetter() to get the Page and then, on the main
thread, page.DisplayAlertAsync(title ?? "", message, "OK") for a one-button
dialog, or page.DisplayAlertAsync(title ?? "", message, accept, cancel) for
OKCancel / YesNo, mapping the bool to OK/Cancel or Yes/No. The Page returned by
the getter must be the page currently on screen: DisplayAlertAsync is an
instance method of that Page. From a view model you normally never call
Create: CreateDialog / ShowInfo / ShowError / ConfirmDialog pass the view
model's Page getter for you.

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
registered; AutoRegisterServices on the CodeBrix.Platform.Mobile assembly ONLY;
AddSimpleMessaging(); } -> Build(). It does NOT start the host: call
await SimpleServiceResolver.Instance.StartupHost() yourself if you register
IHostedService implementations. This host is SEPARATE from the MAUI
MauiAppBuilder service collection: services registered through
builder.Services in MauiProgram are not visible to GetService<T>() and vice
versa, unless you register them in both places.
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
        public IdentifiedLinuxDistro LinuxDistro { get; }   // Android on Android
        public OsVersionInfo OsVersionInfo { get; }
        public string RunningAsUser { get; }                 // "mobile" on Android
        public bool? IsAdminUser { get; }                    // false on Android
        // computed:
        public bool IsWindows { get; }   public bool IsMacOs { get; }
        public bool IsLinux { get; }
        public bool IsAndroid { get; }   // DeviceInfo.Current.Platform == DevicePlatform.Android
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

On Android, Gather() uses DeviceInfo.Current: Version fills OsVersionInfo
(Major/Minor/Build/Revision), the API level and codename are looked up from an
internal table, ProductName becomes "Android <version> (<codename>) API level
<n>", and ProductNameDisplay appends " - <Model> (<Manufacturer>)" when the
device reports them. RunningAsUser is "mobile" and IsAdminUser is false. On
Windows (MAUI on Windows) Environment.OSVersion and WindowsIdentity are used;
macOS and Linux shell out through RunUnixShellCommand. There is no
iOS-specific branch: on iOS only the RuntimeInformation-derived properties
(OsDescription, PlatformArchitecture, DotNetVersion, IsX64 / IsArm64) carry
information.

COMPLETE EXAMPLES
=================
Files for a minimal MAUI app "MyApp" (Shell-based, as the MAUI template
generates). HostHelper is shown in the DI section.

MauiProgram.cs (unchanged from the template)
--------------------------------------------
    using Microsoft.Extensions.Logging;
    using Microsoft.Maui.Controls.Hosting;
    using Microsoft.Maui.Hosting;

    namespace MyApp;

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });
    #if DEBUG
            builder.Logging.AddDebug();
    #endif
            return builder.Build();
        }
    }

App.xaml.cs
-----------
    using CodeBrix.Platform.Simple;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Maui;
    using Microsoft.Maui.Controls;
    using MyApp.Helpers;
    using MyApp.Services;

    namespace MyApp;

    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Before CreateWindow(): that is where the Shell and its pages -
            // and therefore the first view model - are created.
            SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
            {
                services.AddSingleton<IGreetingService, GreetingService>();
            });
            SimpleViewModel.SetIsDesignMode(false);
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new AppShell());
        }
    }

AppShell.xaml
-------------
    <?xml version="1.0" encoding="UTF-8" ?>
    <Shell x:Class="MyApp.AppShell"
           xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
           xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
           xmlns:views="clr-namespace:MyApp.Views"
           Title="MyApp">
        <ShellContent Title="Home"
                      ContentTemplate="{DataTemplate views:MainPage}"
                      Route="MainPage" />
    </Shell>

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

ViewModels/MainViewModel.cs (compiles unchanged for WinUI and WPF too)
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
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                 xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                 xmlns:vm="clr-namespace:MyApp.ViewModels"
                 x:Class="MyApp.Views.MainPage">

        <ContentPage.BindingContext>
            <vm:MainViewModel />
        </ContentPage.BindingContext>

        <VerticalStackLayout Padding="20" Spacing="12">
            <Entry Placeholder="Your name" Text="{Binding Name}" />
            <Label Text="{Binding Greeting}" />
            <HorizontalStackLayout Spacing="8">
                <Button Text="Greet" WidthRequest="120"
                        Command="{Binding GreetCommand}" />
                <Button Text="Reset" WidthRequest="120"
                        Command="{Binding ResetCommand}" />
            </HorizontalStackLayout>
        </VerticalStackLayout>
    </ContentPage>

Views/MainPage.xaml.cs
----------------------
    using CodeBrix.Platform.Simple;
    using Microsoft.Maui.Controls;

    namespace MyApp.Views;

    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            // Subscribe BEFORE InitializeComponent(): that call assigns the
            // BindingContext declared in XAML. Every dialog needs this Page.
            BindingContextChanged += (_, _) =>
                (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);

            InitializeComponent();
        }
    }

    // Equivalent alternative: override the virtual instead of subscribing.
    //   protected override void OnBindingContextChanged()
    //   {
    //       base.OnBindingContextChanged();
    //       (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);
    //   }

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

Showing device / OS information
-------------------------------
    var os = await SimpleOsInfo.GatherInfo();
    await ShowInfo($"{os.PlatformOsName} {os.OsVersion} ({os.PlatformArchitecture})"
        + $"\n{os.ProductNameDisplay}"           // "Android 14 (...) - Pixel 8 (Google)"
        + $"\n.NET {os.DotNetVersion}");

MINIMUM VIABLE PROJECT
======================
MyApp.csproj (a single-project MAUI app; Resources/, Platforms/ and the
MauiIcon / MauiSplashScreen / MauiFont items are whatever the MAUI template
generated):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
        <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
        <OutputType>Exe</OutputType>
        <RootNamespace>MyApp</RootNamespace>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
        <ApplicationTitle>MyApp</ApplicationTitle>
        <ApplicationId>com.example.myapp</ApplicationId>
        <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
        <ApplicationVersion>1</ApplicationVersion>
        <WindowsPackageType>None</WindowsPackageType>

        <!-- not lower than the package's own minimums -->
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">15.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">15.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
        <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
      </PropertyGroup>

      <ItemGroup>
        <!-- Version attributes omitted here on purpose; `dotnet add package`
             writes the current ones. -->
        <PackageReference Include="CodeBrix.Platform.Mobile.ApacheLicenseForever" />
        <PackageReference Include="Microsoft.Extensions.Hosting" />
        <!-- app-owned: Host.CreateDefaultBuilder() for IHostBuilderProvider -->
        <PackageReference Include="Microsoft.Maui.Controls" />
        <PackageReference Include="Microsoft.Extensions.Logging.Debug" />
        <!-- the two above are what the MAUI app template already adds -->
      </ItemGroup>
    </Project>

Source files: MauiProgram.cs, App.xaml + App.xaml.cs, AppShell.xaml (+ its
empty code-behind), Helpers/HostHelper.cs, Services/GreetingService.cs,
ViewModels/MainViewModel.cs, Views/MainPage.xaml + .xaml.cs - all shown in
COMPLETE EXAMPLES.

Where CreateInstance goes: it is a static call with no MAUI dependency, so it
may sit in App's constructor (as above and in the samples) or in
MauiProgram.CreateMauiApp() before builder.Build(); it must simply run before
the first Page's BindingContext is constructed.

PERFORMANCE TIPS
================
  * InvokeOnMainThread runs inline when already on the main thread and
    otherwise posts without waiting; InvokeOnMainThreadAsync awaits. Prefer
    the sync form for fire-and-forget UI updates from background work.
  * The Affects* cascade reflects over the view model's properties on every
    notification. That is fine for form-sized view models; for a value that
    changes thousands of times per second, assign the field and notify once.
  * SimpleCommand.RaiseCanExecuteChanged is async void on MAUI and hops to the
    main thread when needed; [AffectsAllCommands] on a rapidly changing
    property produces one hop per command per change - use [AffectsCommands]
    with the commands that actually depend on it.
  * Async SimpleMessaging callbacks are serialised per subscription: a slow
    handler delays later messages to that subscriber only.
  * SimpleOsInfo.GatherInfo() reads DeviceInfo and, on desktop OSes, does I/O;
    gather once and cache the instance (the samples use
    _osInfo ??= await SimpleOsInfo.GatherInfo()).

COMMON PITFALLS TO AVOID
========================
  1. Using SimpleServiceResolver.Instance, GetService<T>() or any Messaging*
     helper before CreateInstance -> InvalidOperationException ("...
     CreateInstance() static method must be called at application start").
     With Shell, pages (and their XAML-declared BindingContext view models)
     are created inside CreateWindow(); call CreateInstance in App's
     constructor or in CreateMauiApp().
  2. Forgetting SimpleViewModel.SetIsDesignMode(false) at startup: MAUI has no
     designer detection, so IsDesignMode(true) returns TRUE at run time and a
     view model that guards its constructor with it silently does nothing.
  3. Dialogs before SetXamlRootGetter: ShowInfo / ShowError / ConfirmDialog
     throw InvalidOperationException ("Unable to perform the requested UI
     operation before SetXamlRootGetter() has been called"). Subscribe to
     BindingContextChanged BEFORE InitializeComponent() (or override
     OnBindingContextChanged) so the getter is wired the moment the
     BindingContext is assigned. A view model created by DI or navigation
     parameters before its page exists cannot show dialogs until a page wires
     it.
  4. The getter must return the page that is ON SCREEN: DisplayAlertAsync is
     an instance method of that Page. A view model shared by several pages
     holds ONE getter; the last page to bind it wins, and a dialog requested
     while another page is displayed is a MAUI DisplayAlert on a page that is
     not visible. With Shell ContentTemplate pages are created lazily on first
     navigation - the constructor wiring above covers that.
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
  8. SimpleCommand.Execute and (on MAUI) RaiseCanExecuteChanged are async
     void: an exception thrown by your handler is an unhandled exception. Wrap
     the body in try/catch and call ShowError.
  9. Two service containers: MauiAppBuilder.Services (MAUI DI, used by Shell
     and handlers) and the Generic Host behind SimpleServiceResolver. Register
     a service in both if both sides need it; GetService<T>() only sees the
     latter.
 10. CreateInstance(IHost) registers nothing extra: without your own
     services.AddSimpleMessaging() the Messaging* helpers throw on first use.
 11. AutoRegisterServices during CreateInstance scans only the
     CodeBrix.Platform.Mobile assembly. Register your own IAutoRegisterServices
     classes with services.AutoRegisterServices([typeof(App)]) inside
     configureServices.
 12. Messaging is type-exact (Send<Derived,...> never reaches
     Subscribe<Base,...>), and a lambda callback that captures "this" is held
     strongly. Use method groups and Unsubscribe in Dispose.
 13. GetVisibility(false) returns Hidden, not Collapsed. If you bind it to
     VisualElement.IsVisible you need a converter; the enum is meant for
     properties typed Visibility.
 14. Referencing this package from a plain net10.0 class library fails; a
     view-model library must be a MAUI class library with the same target
     frameworks as the app.

WHAT THIS PACKAGE DOES NOT DO
=============================
  * No navigation helpers: no Shell routing wrappers, no NavigationPage
    helpers, no view-model-first navigation, no page/view-model locator.
  * No designer detection on MAUI: IsDesignMode only reports what
    SetIsDesignMode stored, or your default.
  * No custom dialog UI: SimpleDialog is Page.DisplayAlertAsync with one or
    two buttons (OK, OKCancel, YesNo). No action sheets, no prompts
    (DisplayPromptAsync), no toasts.
  * No localisation: dialog titles and button labels are English constants.
  * No integration with MauiAppBuilder.Services: SimpleServiceResolver hosts
    its own Generic Host.
  * No UI controls of any kind: this package is view-model-side only. The
    WinUI sibling family adds image and Lottie controls; there is no MAUI
    equivalent in this package.
  * SimpleMessaging has no request/response, no awaitable Send and no ordering
    guarantees between subscribers.
  * SimpleOsInfo has no iOS-specific branch and reports no battery, network,
    display or permission state; on Android it reports version, codename, API
    level, model and manufacturer only.

WORKING EXAMPLES ON GITHUB
==========================
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/Platforms/JustBetweenUs
        Mobile/ (JustBetweenUs.Mobile.csproj) - the .NET MAUI head:
          MauiProgram.cs           the unmodified MAUI builder
          App.xaml.cs              CreateInstance + SetIsDesignMode in the App
                                   constructor, then CreateWindow(new AppShell())
          AppShell.xaml            Shell with a ContentTemplate ShellContent
          Views/MainPage.xaml      <ContentPage.BindingContext> view model,
                                   Entry / Editor / Picker bindings, Button
                                   Command bindings
          Views/MainPage.xaml.cs   SetXamlRootGetter(() => this) inside
                                   BindingContextChanged, subscribed before
                                   InitializeComponent()
          JustBetweenUs.Mobile.csproj  the MAUI target frameworks,
                                   SupportedOSPlatformVersion floors and the
                                   shared view-model files linked in
        Shared/Helpers/HostHelper.cs - the IHostBuilderProvider implementation,
          Compile-linked into the head as Helpers/HostHelper.cs.
        Shared/ViewModels/MainViewModel.cs - a SimpleViewModel with lazy
          SimpleCommand properties, [AffectsCommands], SetProperty, ShowInfo /
          ShowError, InvokeOnMainThread and SimpleOsInfo.GatherInfo, shared
          unchanged with the WinUI and WPF heads beside it.
        Shared/ViewModels/EncryptionMode.cs - a SimpleEnumInfo<TEnum> class
          with [SimpleEnum<TInfo>(...)] members and a GetDictionary() helper.

QUICK REFERENCE CARD
====================
    // startup (App constructor, before CreateWindow runs)
    SimpleServiceResolver.CreateInstance(HostHelper.GetHost(),
        s => s.AddSingleton<IFoo, Foo>());
    SimpleViewModel.SetIsDesignMode(false);
    // page constructor, before InitializeComponent
    BindingContextChanged += (_, _) =>
        (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);

    // view model
    public sealed class Vm : SimpleViewModel
    set => SetProperty(ref _field, value);   // class/string/bool/int/DateTime/
                                             //   DateTimeOffset
    SetEnumProperty(ref _mode, value);       // enums
    [AffectsProperties("A", "B")]  [AffectsCommands(nameof(Cmd))]  [AffectsAllCommands]
    public SimpleCommand Cmd => _cmd ??= new SimpleCommand(CanDo, DoAsync);
    await ShowInfo(text);   await ShowError(ex);   await ShowError("msg", details);
    if (await ConfirmDialog("Sure?")) { ... }          // DisplayAlertAsync Yes/No
    InvokeOnMainThread(() => ...);                     // MainThread, no wait
    var r = await InvokeOnMainThreadAsync(async () => await WorkAsync());
    var vis = GetVisibility(flag);                     // Visible / Hidden
    var svc = GetService<IFoo>();
    MessagingSend(this, "Msg", args);
    MessagingSubscribe<SenderVm, ArgsType>(this, "Msg", OnMsg, null);
    MessagingUnsubscribe<SenderVm, ArgsType>(this, "Msg");
    var os = await SimpleOsInfo.GatherInfo();          // os.ProductNameDisplay on Android

    // direct dialog (outside a view model)
    var dlg = SimpleDialog.Create(() => currentPage, "msg", "title",
        SimpleDialogButtons.OKCancel);
    var result = await dlg.ShowAsync();                // SimpleDialogResult

    // enum metadata
    [SimpleEnum<FooInfo>(nameof(FooInfo.Bar))] Bar,
    var info = SimpleEnumHelper.FindMemberInfo<Foo, FooInfo>(Foo.Bar);
    var all  = SimpleEnumHelper.GetPossibleValues<Foo, FooInfo>();

    // csproj: MAUI TFMs (android/ios/maccatalyst [+ windows on Windows]),
    //         UseMaui, SupportedOSPlatformVersion >= 15.0/15.0/21.0/10.0.17763.0,
    //         app also references Microsoft.Extensions.Hosting
================================================================================
