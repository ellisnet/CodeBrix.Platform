================================================================================
AGENT-README: CodeBrix.Platform.WPF
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.WPF.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.WPF.ApacheLicenseForever is the CodeBrix "Simple" MVVM
toolkit compiled for WPF applications on .NET 10 or later: SimpleViewModel
(INotifyPropertyChanged base with attribute-driven change cascades),
SimpleCommand (ICommand with sync/async handlers and main-thread marshalling),
SimpleDialog (MessageBox-backed dialogs), SimpleMessaging (weak-reference
publish/subscribe), SimpleServiceResolver (Generic-Host based service
resolution), SimpleEnum (rich enum metadata) and SimpleOsInfo (OS, user and
architecture facts). 24 public types in one namespace (the WinUI and .NET MAUI
siblings have 25 - they also expose IXamlRootGetter, which WPF does not need).

Provenance: this package is independent of the cross-platform CodeBrix.Platform
UI packages and shares no code with them at run time. The toolkit is original
CodeBrix code and is source-identical in the sibling packages
CodeBrix.Platform.WinUI.ApacheLicenseForever (see
src-platforms/Platform.WinUI/AGENT-README.txt) and
CodeBrix.Platform.Mobile.ApacheLicenseForever (see
src-platforms/Platform.Mobile/AGENT-README.txt): a view model written against
SimpleViewModel compiles unchanged for WPF, WinUI and .NET MAUI. Only the
platform-facing edges differ, and this file documents the WPF edges:
MessageBox dialogs, Application.Current.Dispatcher marshalling, WPF designer
detection and System.Windows.Visibility.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.WPF.ApacheLicenseForever

Package id: CodeBrix.Platform.WPF.ApacheLicenseForever
Assembly:   CodeBrix.Platform.WPF
NuGet dependencies (by id): Microsoft.Extensions.DependencyInjection.Abstractions,
Microsoft.Extensions.Hosting.Abstractions.

App-owned (NOT brought in by this package): Microsoft.Extensions.Hosting. The
package references only the *.Abstractions packages; your app implements
IHostBuilderProvider and returns Host.CreateDefaultBuilder(), which lives in
Microsoft.Extensions.Hosting. Add that package to the application project.

License: Apache-2.0.

Requirements: a WPF application targeting net10.0-windows with UseWPF=true.
Windows only. Any class library that references this package - and any test
project that instantiates view models derived from SimpleViewModel - must also
target net10.0-windows with UseWPF=true, because the toolkit's types reference
System.Windows.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.Simple;   // every type in this package

There is only one namespace. The WPF types it uses (Visibility, MessageBox,
Application.Current.Dispatcher, DesignerProperties) come from System.Windows.

CORE API REFERENCE
==================

Startup contract (order matters)
--------------------------------
    1. SimpleServiceResolver.CreateInstance(...)  in the App constructor,
       before the StartupUri window (and any view model) is created
    2. SimpleViewModel.SetIsDesignMode(false)     once, right after step 1
    3. bind view models as DataContext; no further wiring is needed on WPF
       (dialogs are MessageBoxes and need no window reference)

SimpleViewModel
---------------
    public abstract class SimpleViewModel : INotifyPropertyChanged, IDisposable
    // (no IXamlRootGetter on WPF: that interface does not exist in this package)

    // Design mode
    public static void SetIsDesignMode(bool isDesignMode);
    protected static bool IsDesignMode(bool? defaultValueIfNotSet = null);
        // if SetIsDesignMode was called: that value
        // else if a default is passed: the default
        // else: DesignerProperties.GetIsInDesignMode(new DependencyObject()),
        //   cached; works in the Visual Studio designer, not reliably in
        //   JetBrains Rider

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

    // Dialogs. The TITLES are English constants: "Information", "ERROR",
    //   "Are you sure?". The BUTTON words come from the system MessageBox
    //   (MessageBoxButton.OK / OKCancel / YesNo), so they follow the OS
    //   language, not this package.
    protected virtual SimpleDialog CreateDialog(string message,
        string title = null, SimpleDialogButtons buttons = SimpleDialogButtons.OK);
        // = SimpleDialog.Create(message, title, buttons)
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

    // Threading - via Application.Current.Dispatcher
    protected virtual void InvokeOnMainThread(Action functionToExecute);
        // Dispatcher.Invoke: SYNCHRONOUS - blocks until the action has run
    protected virtual Task<T> InvokeOnMainThreadAsync<T>(
        Func<Task<T>> functionToExecute);
        // Dispatcher.Invoke of an async lambda; the returned Task completes
        // (or faults) when your function does
    protected bool _isUnderTest;
        // set true in a test subclass: when Application.Current?.Dispatcher is
        // null both methods run the function directly instead of throwing
    protected static Visibility GetVisibility(bool isVisible);
        // System.Windows.Visibility: Visible / Hidden (never Collapsed)

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
    protected virtual void Dispose(bool disposing);

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

Behaviour on WPF: with ShouldExecuteOnMainThread the handler runs through
Application.Current.Dispatcher.Invoke (synchronous). RaiseCanExecuteChanged
ALSO goes through Application.Current.Dispatcher.Invoke while
ShouldRaiseCanExecuteOnMainThread is true (its default) - with no
Application.Current (unit tests) that is a NullReferenceException, so set
ShouldRaiseCanExecuteOnMainThread = false on commands under test. Execute
awaits async handlers but is itself async void: an exception escaping your
handler is not observable by the caller - catch inside.

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
    public static SimpleDialog Create(string message, string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK);
    public string Message { get; set; }   // trimmed; null becomes ""
    public string Title { get; set; }     // blank becomes null -> "" caption
    public SimpleDialogButtons Buttons { get; set; }
    public Task<SimpleDialogResult> ShowAsync();  // ObjectDisposedException after Dispose
    public void Dispose();

On WPF ShowAsync calls System.Windows.MessageBox.Show(message, title ?? "",
MessageBoxButton.{OK|OKCancel|YesNo}) - a modal, BLOCKING call on the calling
thread with no owner window - and maps MessageBoxResult to SimpleDialogResult
(None for anything else). The Task is already complete when it returns. There
is no XamlRoot/window wiring on WPF and no IXamlRootGetter type.

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
registered; AutoRegisterServices on the CodeBrix.Platform.WPF assembly ONLY;
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
        public bool IsLinux { get; }     public bool IsAndroid { get; }  // always false on WPF
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

On Windows (the only OS a WPF app runs on) Gather() reads Environment.OSVersion
and the current WindowsIdentity (IsAdminUser = membership of the Administrators
role; RunningAsUser from the identity or the USERNAME variable). The macOS and
Linux branches (Debian, Ubuntu, Mint, LMDE detailed; Alpine identified only)
shell out through RunUnixShellCommand and exist because the same source ships
in the WinUI and MAUI editions.

COMPLETE EXAMPLES
=================
Files for a minimal WPF app "MyApp". HostHelper is shown in the DI section.

App.xaml
--------
    <Application x:Class="MyApp.App"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 StartupUri="Views/MainWindow.xaml">
        <Application.Resources />
    </Application>

App.xaml.cs
-----------
    using CodeBrix.Platform.Simple;
    using Microsoft.Extensions.DependencyInjection;
    using MyApp.Helpers;
    using MyApp.Services;
    using System.Windows;

    namespace MyApp;

    public partial class App : Application
    {
        public App()
        {
            // Runs before the StartupUri window - and its DataContext - exist.
            SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
            {
                services.AddSingleton<IGreetingService, GreetingService>();
            });
            SimpleViewModel.SetIsDesignMode(false);
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

ViewModels/MainViewModel.cs (compiles unchanged for WinUI and MAUI too)
----------------------------------------------------------------------
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
            if (!IsDesignMode(true))   // skip service resolution in the designer
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

Views/MainWindow.xaml
---------------------
    <Window x:Class="MyApp.Views.MainWindow"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:vm="clr-namespace:MyApp.ViewModels"
            Title="MyApp" Width="420" Height="240">
        <Window.DataContext>
            <vm:MainViewModel />
        </Window.DataContext>
        <StackPanel Margin="20">
            <TextBox Margin="0,0,0,8"
                     Text="{Binding Name, Mode=TwoWay,
                                    UpdateSourceTrigger=PropertyChanged}" />
            <TextBlock Margin="0,0,0,8" Text="{Binding Greeting}" />
            <StackPanel Orientation="Horizontal">
                <Button Width="100" Margin="0,0,8,0"
                        Command="{Binding GreetCommand}">Greet</Button>
                <Button Width="100" Command="{Binding ResetCommand}">Reset</Button>
            </StackPanel>
        </StackPanel>
    </Window>

Views/MainWindow.xaml.cs
------------------------
    using System.Windows;

    namespace MyApp.Views;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();   // nothing else: WPF dialogs need no wiring
        }
    }

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

Unit-testing a view model without a WPF Application
---------------------------------------------------
    // Test double: mark the view model as under test so InvokeOnMainThread
    // runs inline when Application.Current is null.
    public sealed class TestableMainViewModel : MainViewModel
    {
        public TestableMainViewModel() { _isUnderTest = true; }
    }
    // Commands still marshal RaiseCanExecuteChanged through the dispatcher
    // by default; switch that off per command in tests:
    vm.GreetCommand.ShouldRaiseCanExecuteOnMainThread = false;
    // The test project must target net10.0-windows with UseWPF=true and call
    // SimpleServiceResolver.CreateInstance(...) once before the first view
    // model is created.

(MainViewModel above is sealed; drop "sealed" if you use the subclass trick,
or set _isUnderTest from a constructor overload instead.)

MINIMUM VIABLE PROJECT
======================
MyApp.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0-windows</TargetFramework>
        <UseWPF>true</UseWPF>
        <RootNamespace>MyApp</RootNamespace>
      </PropertyGroup>

      <ItemGroup>
        <!-- Version attributes omitted here on purpose; `dotnet add package`
             writes the current ones. -->
        <PackageReference Include="CodeBrix.Platform.WPF.ApacheLicenseForever" />
        <PackageReference Include="Microsoft.Extensions.Hosting" />
        <!-- app-owned: Host.CreateDefaultBuilder() for IHostBuilderProvider -->
      </ItemGroup>
    </Project>

Source files: App.xaml + App.xaml.cs, Helpers/HostHelper.cs,
Services/GreetingService.cs, ViewModels/MainViewModel.cs,
Views/MainWindow.xaml + .xaml.cs - all shown in COMPLETE EXAMPLES.

A class library holding the view models needs the same two lines
(<TargetFramework>net10.0-windows</TargetFramework> and <UseWPF>true</UseWPF>)
plus the package reference; so does every project that references that
library, including test projects.

PERFORMANCE TIPS
================
  * InvokeOnMainThread is a synchronous Dispatcher.Invoke on WPF: a background
    thread that calls it repeatedly is blocked for each round trip. Batch the
    UI updates into one call.
  * The Affects* cascade reflects over the view model's properties on every
    notification. That is fine for form-sized view models; for a value that
    changes thousands of times per second, assign the field and notify once.
  * Async SimpleMessaging callbacks are serialised per subscription: a slow
    handler delays later messages to that subscriber only.
  * SimpleDialog blocks the UI thread for the lifetime of the MessageBox;
    nothing else in the app (including async continuations queued to the
    dispatcher) runs until the user closes it.
  * SimpleOsInfo.GatherInfo() does I/O; gather once and cache the instance
    (the samples use  _osInfo ??= await SimpleOsInfo.GatherInfo()).

COMMON PITFALLS TO AVOID
========================
  1. Using SimpleServiceResolver.Instance, GetService<T>() or any Messaging*
     helper before CreateInstance -> InvalidOperationException ("...
     CreateInstance() static method must be called at application start").
     Put CreateInstance in the App constructor: the StartupUri window (and the
     view model declared as its DataContext) is created after it.
  2. Forgetting SimpleViewModel.SetIsDesignMode(false) at startup: with no
     stored value, IsDesignMode(true) returns TRUE at run time and a view model
     that guards its constructor with it silently does nothing. (IsDesignMode()
     with no default falls back to WPF designer detection, which is fine in
     Visual Studio but not reliable in Rider.)
  3. Unit tests: SimpleCommand.RaiseCanExecuteChanged uses
     Application.Current.Dispatcher.Invoke while ShouldRaiseCanExecuteOnMainThread
     is true (its default). Without a WPF Application that is a
     NullReferenceException. Set the flag to false on commands under test, and
     set _isUnderTest = true so the view model's InvokeOnMainThread runs inline.
  4. Dialogs are MessageBoxes: they block the calling thread and have no owner
     window (they are not centred on your window and can appear behind it).
     Call them from the UI thread through the view-model helpers; do not call
     ShowAsync from a thread-pool thread.
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
     CodeBrix.Platform.WPF assembly. Register your own IAutoRegisterServices
     classes with services.AutoRegisterServices([typeof(App)]) inside
     configureServices.
 11. Messaging is type-exact (Send<Derived,...> never reaches
     Subscribe<Base,...>), and a lambda callback that captures "this" is held
     strongly. Use method groups and Unsubscribe in Dispose.
 12. GetVisibility(false) returns Hidden, not Collapsed: the element keeps its
     layout space. Return Visibility.Collapsed yourself if you need the space
     reclaimed.
 13. Referencing this package from a plain net10.0 class library fails; the
     library (and everything that references it) must target net10.0-windows
     with UseWPF=true.

WHAT THIS PACKAGE DOES NOT DO
=============================
  * No navigation framework, no view-model locator, no window management.
  * No custom dialog UI: SimpleDialog is a System.Windows.MessageBox with OK,
    OKCancel or YesNo buttons - no owner window, no input dialogs, no icons,
    no non-blocking dialogs.
  * No localisation: dialog titles and button labels are English constants
    (the MessageBox buttons themselves follow the OS language).
  * No UI controls of any kind: this package is view-model-side only. The
    WinUI sibling family adds image and Lottie controls; there is no WPF
    equivalent in this package.
  * SimpleMessaging has no request/response, no awaitable Send and no ordering
    guarantees between subscribers.
  * SimpleServiceResolver is not a general container: it wraps one Generic
    Host, has no scopes or child containers, and does not start the host.
  * SimpleOsInfo reports OS, user and architecture only; nothing about
    windows, displays or hardware; IsAndroid is always false on WPF.

WORKING EXAMPLES ON GITHUB
==========================
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/Platforms/JustBetweenUs
        JustBetweenUs.Wpf/ - the WPF head:
          App.xaml / App.xaml.cs   StartupUri window plus CreateInstance +
                                   SetIsDesignMode in the App constructor
          Views/MainWindow.xaml    <Window.DataContext> view model, TextBox
                                   bindings with UpdateSourceTrigger=
                                   PropertyChanged, Button Command bindings
          Views/MainWindow.xaml.cs DataContextChanged used only to hand the
                                   view model a clipboard delegate; no dialog
                                   wiring is needed on WPF
          JustBetweenUs.Wpf.csproj net10.0-windows + UseWPF with the shared
                                   view-model files linked in
        Shared/Helpers/HostHelper.cs - the IHostBuilderProvider implementation,
          Compile-linked into the head as Helpers/HostHelper.cs.
        Shared/ViewModels/MainViewModel.cs - a SimpleViewModel with lazy
          SimpleCommand properties, [AffectsCommands], SetProperty, ShowInfo /
          ShowError, InvokeOnMainThread and SimpleOsInfo.GatherInfo, shared
          unchanged with the WinUI and .NET MAUI heads beside it.
        Shared/ViewModels/EncryptionMode.cs - a SimpleEnumInfo<TEnum> class
          with [SimpleEnum<TInfo>(...)] members and a GetDictionary() helper.

QUICK REFERENCE CARD
====================
    // startup (App constructor)
    SimpleServiceResolver.CreateInstance(HostHelper.GetHost(),
        s => s.AddSingleton<IFoo, Foo>());
    SimpleViewModel.SetIsDesignMode(false);
    // window: <Window.DataContext><vm:MainViewModel/></Window.DataContext>
    //   - no XamlRoot/window wiring exists or is needed on WPF

    // view model
    public sealed class Vm : SimpleViewModel
    set => SetProperty(ref _field, value);   // class/string/bool/int/DateTime/
                                             //   DateTimeOffset
    SetEnumProperty(ref _mode, value);       // enums
    [AffectsProperties("A", "B")]  [AffectsCommands(nameof(Cmd))]  [AffectsAllCommands]
    public SimpleCommand Cmd => _cmd ??= new SimpleCommand(CanDo, DoAsync);
    await ShowInfo(text);   await ShowError(ex);   await ShowError("msg", details);
    if (await ConfirmDialog("Sure?")) { ... }          // MessageBox YesNo
    var dlg = SimpleDialog.Create("msg", "title", SimpleDialogButtons.OKCancel);
    var result = await dlg.ShowAsync();                // SimpleDialogResult
    InvokeOnMainThread(() => ...);                     // Dispatcher.Invoke (blocking)
    var r = await InvokeOnMainThreadAsync(async () => await WorkAsync());
    var vis = GetVisibility(flag);                     // Visible / Hidden
    var svc = GetService<IFoo>();
    MessagingSend(this, "Msg", args);
    MessagingSubscribe<SenderVm, ArgsType>(this, "Msg", OnMsg, null);
    MessagingUnsubscribe<SenderVm, ArgsType>(this, "Msg");
    var os = await SimpleOsInfo.GatherInfo();

    // enum metadata
    [SimpleEnum<FooInfo>(nameof(FooInfo.Bar))] Bar,
    var info = SimpleEnumHelper.FindMemberInfo<Foo, FooInfo>(Foo.Bar);
    var all  = SimpleEnumHelper.GetPossibleValues<Foo, FooInfo>();

    // tests
    _isUnderTest = true;  cmd.ShouldRaiseCanExecuteOnMainThread = false;

    // csproj: net10.0-windows + UseWPF=true (app, view-model libs, tests);
    //         app also references Microsoft.Extensions.Hosting
================================================================================
