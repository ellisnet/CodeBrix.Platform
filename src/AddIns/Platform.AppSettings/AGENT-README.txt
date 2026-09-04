================================================================================
AGENT-README: CodeBrix.Platform.AppSettings
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.AppSettings.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
A persistent application-settings system for CodeBrix.Platform applications -
the ONLY add-in that is not a UI control. It stores every configurable value
as JSON text in one portable SQLite file, settings.sqlite, and provides NO
settings screen: the application builds its own, or has none and just saves in
the background. Target: .NET 10 or later.

Seven public types, all in the CodeBrix.Platform.AppSettings namespace:

  AppSettingsService          the static facade you normally use: Initialize
                              once, then Get/Set/HasValue anywhere, per-key
                              change handlers, typed handles via Wrap
  AppSettingsStore            the store behind the facade (reachable as
                              AppSettingsService.Store, or constructed directly
                              for a second, independent file): the same
                              Get/Set surface plus the global SettingChanged
                              event, the file lifecycle (auto-backup with
                              retention pruning on every start, quarantine of a
                              corrupt database and restore from the newest good
                              backup, silent first-run creation), ExportToFile
                              and StageIncomingFile
  AppSettingProperty<T>       a typed, in-memory-cached handle over one key,
                              with a Changed event and old-key migration
  AppSettingProperty          the static factory (Create<T>) for those handles
  AppSettingChangedEventArgs  Key / OldValue / NewValue of a change
  AppSettingLoggingService    the backend's logger: console + the framework's
                              ambient ILogger + your own sinks
  AppSettingLogLevel          Info / Warning / Error, the severity of a line

Provenance: the facade and typed handle descend from MonoDevelop's
PropertyService/ConfigurationProperty (MIT), rebuilt on a SQLite store; the
namespace is CodeBrix.Platform.AppSettings throughout - no upstream namespace
survives.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.AppSettings.ApacheLicenseForever

Reference it from the project that carries your other framework package
references (the application's .Core project in the standard layout). It draws
nothing and references no XAML type, so it can equally be referenced from a
plain class library that a CodeBrix.Platform application consumes.

Dependencies (flow in automatically):
  CodeBrix.Platform.ApacheLicenseForever      the core framework (carries the
                                              ambient logger the service
                                              forwards to)
  CodeBrix.Sqlite.ApacheLicenseForever        the SQLite engine behind
                                              settings.sqlite
  Microsoft.Extensions.Logging.Abstractions   ILogger/ILoggerFactory

License: Apache-2.0. Requirements: a writable per-user configuration folder
(or any folder you pass to Initialize). Works on all six heads; the store is a
plain file, so the same settings.sqlite can be copied between machines.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.AppSettings;

That is the whole surface. No XAML namespace is involved (nothing here is
declarable in markup).

CORE API REFERENCE
==================

Start-up and the file location
------------------------------
    static void   AppSettingsService.Initialize(string appName)
    static void   AppSettingsService.Initialize(string appName, string directoryPath)
    static void   AppSettingsService.Shutdown()
    static bool   AppSettingsService.IsInitialized
    static string AppSettingsService.DirectoryPath              // after Initialize
    static string AppSettingsService.GetDefaultDirectory(string appName)

Initialize once, before any UI renders (the App constructor or Program.Main),
with nothing but the application name. The store then lives at

    {per-user configuration folder}/CodeBrix/{appName}/settings/settings.sqlite

which on Linux is ~/.config/CodeBrix/{appName}/settings/settings.sqlite and
the equivalent per-user application-data location on Windows and macOS
(Environment.SpecialFolder.ApplicationData). GetDefaultDirectory(appName)
returns that folder without opening anything. Initialize(appName,
directoryPath) puts the store somewhere else (a test host's temp folder, a
portable install). The application name becomes a folder name: it must be
non-blank and contain no characters invalid in a file name, or
ArgumentException is thrown.

Initialize CONSTRUCTS the store, and construction runs the whole start-up
sequence: adopt a staged import if one is waiting, open with integrity check
and recovery, then take the automatic backup and prune old ones. A second
Initialize throws InvalidOperationException; Shutdown() closes the store and
permits a later Initialize (it is a no-op when never initialized, so it is safe
to call unconditionally at teardown).

Reading and writing through the facade
--------------------------------------
    static T    AppSettingsService.Get<T>(string key, T defaultValue)
    static T?   AppSettingsService.Get<T>(string key)
    static void AppSettingsService.Set(string key, object? value)
    static bool AppSettingsService.HasValue(string key)

There is no generic constraint on T and Set is NOT generic: it takes object?
and serializes the value by its RUNTIME type with System.Text.Json (default
options plus enums-as-strings). Every Set writes through to settings.sqlite
immediately and synchronously. A null value REMOVES the key.

Get<T>(key, defaultValue) returns defaultValue when the key is unset, when the
stored JSON deserializes to null, or when it cannot be read as T - in that
last case the mismatch is logged as a Warning and the default returned, never
thrown. Get<T>(key) is the same with default(T): null for a reference type, 0
for an int, false for a bool.

Keys are ordinal, case-sensitive strings. The one key the package uses for
itself is AppSettingsStore.AutoBackupRetentionKey
("CodeBrix.Platform.AppSettings.AutoBackupRetention"); prefix your own with the
application name ("MyApp.Window.Width") to keep them apart.

WHAT ROUND-TRIPS: anything System.Text.Json serializes with default options.
Verified by the package's own tests: string, int, bool, enums (stored as their
NAME, so DayOfWeek.Friday is the text "Friday"), and byte[] (stored as a base64
JSON string). Any other type follows the same rule - a value type or a plain
object with public get/set properties round-trips; an enum member that is
renamed between versions no longer parses and falls back to the default with a
Warning.

STORES TEXT ONLY: values are JSON. A byte[] does round-trip, because
System.Text.Json renders one as a base64 string, but it is stored as text and
pays the base64 cost - encode deliberately, and keep large binary out of
settings.

Typed handles - AppSettingProperty<T>
-------------------------------------
    abstract class AppSettingProperty<T>
        T    Value { get; set; }
        bool Set(T newValue)                      // true when it actually changed
        event EventHandler? Changed
        static implicit operator T(AppSettingProperty<T> property)

    abstract class AppSettingProperty                          // the factory
        static AppSettingProperty<T> Create<T>(string key, T defaultValue,
                                               string? oldKey = null)

    static AppSettingProperty<T> AppSettingsService.Wrap<T>(string key, T defaultValue)

There is no public constructor: obtain a handle through
AppSettingProperty.Create (with optional old-key migration) or
AppSettingsService.Wrap (the same handle, no migration). Creating one requires
the service to be initialized - it reads the current value immediately - so do
not build handles in static field initializers that run before Initialize.

A handle caches its value IN MEMORY: Value reads the field, and setting Value
(or calling Set) writes through to the store, raises the store's change
notifications, and raises the handle's own Changed - but only when the value
really changed (EqualityComparer<T>.Default). Consequently a handle does NOT
see a later AppSettingsService.Set of the same key from elsewhere: pick one
access path per key.

OLD-KEY MIGRATION happens in Create when oldKey is given: if a value is stored
under oldKey, it is copied to key (only when key has no value yet - an existing
value under the new key wins), and the old key is removed either way. That is
the whole rename story for a setting: change the key and pass the previous
name as oldKey; the next run migrates silently.

    using CodeBrix.Platform.AppSettings;

    static class Prefs
    {
        // Renamed from "MyApp.Editor.Font" - the old value carries over once.
        public static readonly AppSettingProperty<string> EditorFont =
            AppSettingProperty.Create("MyApp.Editor.FontFamily", "Roboto Mono",
                                      oldKey: "MyApp.Editor.Font");

        public static readonly AppSettingProperty<int> TabSize =
            AppSettingsService.Wrap("MyApp.Editor.TabSize", 4);

        public static readonly AppSettingProperty<DayOfWeek> WeekStart =
            AppSettingsService.Wrap("MyApp.Calendar.WeekStart", DayOfWeek.Monday);
    }

    // ...after AppSettingsService.Initialize("MyApp") has run:
    int tabs = Prefs.TabSize;                // implicit conversion to T
    Prefs.TabSize.Value = 2;                 // writes through, raises Changed
    if (Prefs.TabSize.Set(2)) { /* not reached: unchanged */ }
    Prefs.EditorFont.Changed += (_, _) => ApplyFont(Prefs.EditorFont.Value);

Change notification
-------------------
    class AppSettingChangedEventArgs : EventArgs
        string  Key       { get; }
        object? OldValue  { get; }   // the previous value AS STORED JSON TEXT,
                                     // or null when the key was not set before
        object? NewValue  { get; }   // the object passed to Set, or null when
                                     // the key was removed

    // per key - facade and store:
    static void AppSettingsService.AddSettingHandler(string key,
                    EventHandler<AppSettingChangedEventArgs> handler)
    static void AppSettingsService.RemoveSettingHandler(string key,
                    EventHandler<AppSettingChangedEventArgs> handler)
    void AppSettingsStore.AddSettingHandler(string key,
                    EventHandler<AppSettingChangedEventArgs> handler)
    void AppSettingsStore.RemoveSettingHandler(string key,
                    EventHandler<AppSettingChangedEventArgs> handler)

    // global - store only:
    event EventHandler<AppSettingChangedEventArgs>? AppSettingsStore.SettingChanged

    // per handle:
    event EventHandler? AppSettingProperty<T>.Changed

Set raises, in order, the store's SettingChanged (every key) and then the
handler(s) registered for that key - and only when the stored JSON actually
changed (writing the same value again raises nothing). Both are raised
synchronously on the thread that called Set, so a handler that updates UI from
a Set made on a background thread must marshal to the UI thread itself. The
facade exposes the per-key pair; the global event is reached through
AppSettingsService.Store.

OldValue is the raw JSON text of the previous value (the store writes through
Set(string, object) and never learns the type to read the old value back as);
NewValue is the object you passed. Read the typed current value with Get<T>
inside the handler rather than casting NewValue.

    using CodeBrix.Platform.AppSettings;

    // One key:
    AppSettingsService.AddSettingHandler("MyApp.Theme", OnThemeChanged);

    void OnThemeChanged(object? sender, AppSettingChangedEventArgs e)
    {
        var theme = AppSettingsService.Get("MyApp.Theme", "Light");
        DispatcherQueue.TryEnqueue(() => ApplyTheme(theme));
    }

    // Every key (for a "settings changed" indicator, or syncing to a server):
    AppSettingsService.Store.SettingChanged += (_, e) =>
        Log($"{e.Key} -> {e.NewValue ?? "(removed)"}");

    // Unsubscribe with the same delegate instance:
    AppSettingsService.RemoveSettingHandler("MyApp.Theme", OnThemeChanged);

The store - AppSettingsStore
----------------------------
    sealed class AppSettingsStore : IDisposable
        AppSettingsStore(string appName)
        AppSettingsStore(string appName, string directoryPath)
        static string GetDefaultDirectory(string appName)

        string AppName            { get; }
        string DirectoryPath      { get; }     // the settings folder
        string DatabaseFilePath   { get; }     // .../settings.sqlite
        bool   WasCreatedFresh       { get; }  // no usable file existed this start
        bool   WasRestoredFromBackup { get; }  // corrupt file replaced by newest backup
        bool   WasReplacedByImport   { get; }  // a staged import was adopted this start
        int    AutoBackupRetention   { get; set; }   // 0..10, default 5

        bool HasValue(string key)
        T?   Get<T>(string key)
        T    Get<T>(string key, T defaultValue)
        bool Set(string key, object? value)          // true when it changed
        void AddSettingHandler(...)  /  void RemoveSettingHandler(...)
        event EventHandler<AppSettingChangedEventArgs>? SettingChanged

        void ExportToFile(string destinationFilePath)
        void StageIncomingFile(string sourceFilePath)
        void Dispose()

    Constants: SettingsFileName = "settings.sqlite",
               AutoBackupFilePrefix = "settings_auto_backup_",
               CorruptFilePrefix = "settings_corrupt_",
               IncomingFileName = "settings_incoming.sqlite",
               OldFilePrefix = "settings_old_",
               TimestampFormat = "yyyy-MM-dd_HH-mm-ss",
               AutoBackupRetentionKey, DefaultAutoBackupRetention = 5,
               MaxAutoBackupRetention = 10, FamilyFolderName = "CodeBrix"

Reach the application's store as AppSettingsService.Store (throws
InvalidOperationException before Initialize; check IsInitialized first when
that is possible). The store's Set returns a bool the facade's does not; the
three Was* flags tell a start-up routine whether to show a "settings were
restored/imported/reset" notice.

Constructing an AppSettingsStore yourself gives a SECOND, independent settings
file (per-document or per-profile settings); it has the same surface, and the
facade knows nothing about it. Dispose it when done. After Dispose, writes
throw ObjectDisposedException.

THE FILE LIFECYCLE (runs inside the constructor, i.e. inside Initialize):
  1. If settings_incoming.sqlite is present (staged by StageIncomingFile on a
     previous run), the current settings.sqlite is renamed to
     settings_old_<timestamp>.sqlite (never pruned) and the incoming file takes
     its place; WasReplacedByImport = true.
  2. settings.sqlite is opened and integrity-checked. A missing file is created
     silently (WasCreatedFresh). A file that fails to open or fails
     PRAGMA integrity_check is moved aside as settings_corrupt_<timestamp>.sqlite
     and the newest settings_auto_backup_*.sqlite is copied into place
     (WasRestoredFromBackup); with no usable backup a fresh store is created.
  3. If AutoBackupRetention > 0, a settings_auto_backup_<timestamp>.sqlite is
     written (a checkpointed, self-contained copy) and all but the newest N
     are deleted. Recency comes from the timestamp in the file name (local
     time, TimestampFormat), never from file-system dates; files that do not
     match the naming scheme exactly - a manual copy, a settings_old_ file -
     are never deleted.
  Every step logs through AppSettingLoggingService; a failed backup or
  adoption is logged and never prevents the application from starting.

AutoBackupRetention is a setting itself (AutoBackupRetentionKey), clamped to
0..10 on read and write. Because the backup-and-prune pass runs during
construction, a new value takes effect on the NEXT start - lowering it does
not delete existing backups on the spot; 0 disables automatic backups.

ExportToFile(destinationFilePath) writes a safe, complete, self-contained copy
(quiesce, WAL checkpoint, SQLite online backup) - the single file is the whole
database and needs no -wal/-shm companions. It REFUSES a destination inside
the settings folder (InvalidOperationException), which holds only the live
store and its own backups.

StageIncomingFile(sourceFilePath) is the import half: it copies the chosen
file to a private temp location (the user's file is never opened in place),
checks that it is a SQLite database passing integrity_check and that it holds
a readable Setting table, and stages a clean copy as settings_incoming.sqlite
to be adopted on the NEXT start. It throws FileNotFoundException for a missing
file and InvalidDataException for anything that fails validation; nothing
about the running store changes until the application restarts.

Logging - AppSettingLoggingService and AppSettingLogLevel
---------------------------------------------------------
    enum AppSettingLogLevel { Info, Warning, Error }

    static class AppSettingLoggingService
        const  string LogCategory = "CodeBrix.Platform.AppSettings"
        static bool   ConsoleOutput { get; set; }        // default TRUE
        static void   AddSink(Action<string> sink)                          // replayed
        static void   AddSink(Action<AppSettingLogLevel, string> sink)      // not replayed
        static bool   RemoveSink(Action<string> sink)
        static bool   RemoveSink(Action<AppSettingLogLevel, string> sink)
        static void   LogInfo(string message)
        static void   LogWarning(string message)
        static void   LogError(string message)
        static void   LogError(string message, Exception ex)

Every line the backend logs (backup created/pruned, restore, import adoption,
a type-mismatch read, ...) goes to THREE places:

  1. The console, while ConsoleOutput is true - which it is BY DEFAULT, because
     an application that has not configured framework logging would otherwise
     see nothing at all. An application that has configured logging and does
     not want the duplicate sets AppSettingLoggingService.ConsoleOutput = false
     during start-up. Console lines look like
     "[HH:mm:ss.fff] INFO : message" (labels INFO / WARN  / ERROR).
  2. The framework's ambient logger, as an ILogger created for the category
     "CodeBrix.Platform.AppSettings", at Information / Warning / Error for
     Info / Warning / Error. NOTE the category begins with "CodeBrix.Platform",
     so the usual AddFilter("CodeBrix.Platform", LogLevel.Warning) line hides
     the informational lines unless a more specific filter for
     AppSettingLoggingService.LogCategory is added. If forwarding ever fails
     (no logging assembly in the process), it is disabled for the rest of the
     process after one console warning - logging never stops the app.
  3. Your sinks. AddSink(Action<string>) receives the formatted line, and is
     first REPLAYED every line logged before it registered, so a diagnostics
     page opened late still shows the start-up sequence.
     AddSink(Action<AppSettingLogLevel, string>) receives severity + bare
     message so it can filter, and is NOT replayed. Sinks may be called from
     any thread; marshal to the UI thread inside the sink if needed. RemoveSink
     returns true when a sink was removed.

The four Log* methods are public so an application's own settings screen can
log into the same stream (an export the user made, say).

COMPLETE EXAMPLES
=================

1. Initialize at start-up, read and write anywhere
--------------------------------------------------
    using CodeBrix.Platform.AppSettings;

    // App constructor or Program.Main - once, before any UI renders:
    AppSettingLoggingService.ConsoleOutput = false;   // we have framework logging
    AppSettingsService.Initialize("MyApp");
    // -> {per-user config}/CodeBrix/MyApp/settings/settings.sqlite

    if (AppSettingsService.Store.WasRestoredFromBackup)
        ShowNotice("Your settings were restored from a backup.");

    // Anywhere, on any thread:
    AppSettingsService.Set("MyApp.Window.Width", 1280);
    var width = AppSettingsService.Get("MyApp.Window.Width", 1024);
    if (AppSettingsService.HasValue("MyApp.AssetsFolder")) { ... }
    AppSettingsService.Set("MyApp.AssetsFolder", null);     // removes the key

2. Remembering window size without writing on every resize tick
---------------------------------------------------------------
    // Set writes to disk synchronously; coalesce a burst of changes.
    private DispatcherQueueTimer? _saveTimer;

    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        _saveTimer ??= DispatcherQueue.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(500);
        _saveTimer.IsRepeating = false;
        _saveTimer.Tick -= SaveSize;
        _saveTimer.Tick += SaveSize;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveSize(DispatcherQueueTimer sender, object args)
    {
        AppSettingsService.Set("MyApp.Window.Width", (int)Bounds.Width);
        AppSettingsService.Set("MyApp.Window.Height", (int)Bounds.Height);
    }

3. A settings object as one value
---------------------------------
    // Any plain object with public get/set properties is one JSON value.
    public sealed class EditorOptions
    {
        public string FontFamily { get; set; } = "Roboto Mono";
        public int    FontSize   { get; set; } = 13;
        public bool   WordWrap   { get; set; }
    }

    var options = AppSettingsService.Get("MyApp.Editor", new EditorOptions());
    options.WordWrap = true;
    AppSettingsService.Set("MyApp.Editor", options);   // whole object rewritten

4. Export / import from an application's own settings page
----------------------------------------------------------
    // Export: the user picked a destination with the file-save picker.
    try
    {
        AppSettingsService.Store.ExportToFile(chosenPath);
    }
    catch (InvalidOperationException ex)   // destination inside the settings folder
    {
        ShowError(ex.Message);
    }

    // Import: validated now, adopted on the next start.
    try
    {
        AppSettingsService.Store.StageIncomingFile(pickedFile);
        ShowNotice("Settings will be applied the next time the app starts.");
    }
    catch (InvalidDataException ex)
    {
        ShowError(ex.Message);   // not a SQLite file / no Setting table / corrupt
    }

5. A diagnostics page showing the backend's log
-----------------------------------------------
    // Replayed: the start-up lines appear even though the page opened late.
    AppSettingLoggingService.AddSink(line =>
        DispatcherQueue.TryEnqueue(() => LogLines.Add(line)));

    // Severity-aware, not replayed:
    AppSettingLoggingService.AddSink((level, message) =>
    {
        if (level == AppSettingLogLevel.Error)
            DispatcherQueue.TryEnqueue(() => ErrorBanner.Text = message);
    });

6. Test host: fresh store per test
----------------------------------
    var folder = Path.Combine(Path.GetTempPath(), "MyApp.Tests", Guid.NewGuid().ToString("N"));
    AppSettingsService.Initialize("MyApp", folder);
    try
    {
        AppSettingsService.Set("MyApp.Test", 1);
        Assert.Equal(1, AppSettingsService.Get("MyApp.Test", 0));
    }
    finally
    {
        AppSettingsService.Shutdown();      // permits the next Initialize
        Directory.Delete(folder, recursive: true);
    }

MINIMUM VIABLE PROJECT
======================
No XAML is involved; the add-in works in any project a CodeBrix.Platform
application references. In the application's .Core project:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.AppSettings.ApacheLicenseForever" />
      </ItemGroup>
    </Project>

and in App.xaml.cs (the .UI project), before the first window is created:

    using CodeBrix.Platform.AppSettings;

    public App()
    {
        AppSettingsService.Initialize("MyApp");
        InitializeComponent();
    }

Then AppSettingsService.Get/Set from any view model. Nothing else is required:
no schema, no migration step, no file management.

PERFORMANCE TIPS
================
  - The whole Setting table is loaded into memory when the store opens; Get
    never touches the disk, but it DOES deserialize the JSON on every call.
    For a value read on a hot path (every frame, every key press) hold an
    AppSettingProperty<T>, which caches the typed value, or read once into a
    field and refresh from a change handler.
  - Every Set is a synchronous SQLite write. Debounce settings that change in
    bursts (window bounds, splitter positions, scroll offsets) as in example 2,
    and skip Set when the value is unchanged (the store already returns false
    and raises nothing for an identical value, but it still serializes to find
    that out).
  - Start-up cost is one file open, an integrity check, and (with retention
    above 0) one backup copy plus a directory listing. Small for any realistic
    settings file; set AutoBackupRetention = 0 only if you have measured a
    problem.
  - Values are JSON text, and byte[] is base64: keep large binary (thumbnails,
    document contents) in your own files and store the PATH in settings.
  - The console sink is on by default; set ConsoleOutput = false in release
    builds that already route framework logging somewhere.

COMMON PITFALLS TO AVOID
========================
  - A second Initialize throws InvalidOperationException; call Shutdown first.
    Get/Set/Store/Wrap/Create before Initialize also throw - the facade is not
    lazily initialized.
  - AutoBackupRetention is clamped to 0..10 and takes effect on the NEXT
    start - the backup/prune pass runs during construction, so lowering it does
    not delete existing backups on the spot.
  - AppSettingLoggingService writes to the console by DEFAULT (set
    ConsoleOutput = false to stop it) as well as forwarding to the framework's
    ambient logger under the category "CodeBrix.Platform.AppSettings" - the
    usual AddFilter("CodeBrix.Platform", Warning) line hides its informational
    lines unless a more specific filter is added.
  - ExportToFile REFUSES a destination inside the settings folder, which holds
    only the live store and its own backups.
  - StageIncomingFile changes nothing until the next start; do not expect
    Get to return imported values in the same session.
  - AppSettingProperty<T> handles cache in memory: a direct
    AppSettingsService.Set on the same key is not reflected in an existing
    handle, and does not raise its Changed. Use one access path per key.
  - Get<T>(key, default) swallows a type mismatch (Warning + default). If a
    setting "keeps resetting itself", look for that warning: the stored JSON is
    probably a different shape than T (a renamed enum member, a property that
    changed type).
  - Enums are stored by NAME, not number: renaming a member orphans stored
    values (they fall back to the default). Keep old member names, or migrate
    with a one-off Get<string>/Set.
  - Set takes object?: passing a value through a variable typed as object is
    fine (the runtime type is serialized), but passing null REMOVES the key -
    a nullable property that is null does not "store null", it deletes.
  - AppSettingChangedEventArgs.OldValue is JSON TEXT, not the previous typed
    value; NewValue is the object passed to Set. Re-read with Get<T> for a
    typed current value.
  - Change handlers run synchronously on the thread that called Set: UI work
    in a handler needs DispatcherQueue.TryEnqueue when Set can come from a
    background thread.
  - Keys are case-sensitive ordinal strings: "MyApp.theme" and "MyApp.Theme"
    are two settings.
  - The application name must be a valid folder name; "My/App" throws.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No settings screen, no XAML controls, no bindable settings view model: it
    is the storage layer an application's own settings page (or an application
    with no settings page at all) writes through.
  - No encryption and no secure storage: settings.sqlite is a plain, portable
    SQLite file. Do not put secrets in it.
  - No binary values: everything is JSON text; byte[] round-trips as base64 at
    the base64 cost.
  - No cross-process synchronization: one process owns the file; a second
    process opening the same folder gets its own in-memory view and its own
    start-up backup pass.
  - No schema versioning or typed migrations beyond the per-key old-key rename
    in AppSettingProperty.Create; a change of shape is handled by reading the
    old form once and writing the new one.
  - No roaming or cloud sync: the file lives in the per-user configuration
    folder (or where you point Initialize); ExportToFile/StageIncomingFile are
    the manual transfer path.
  - Import is never applied live - always on the next start.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.AppSettings.Tests
      The package's test suite, and the best worked examples of every API:
        AppSettingsServiceTests.cs      Initialize/Shutdown rules, facade
                                        Get/Set, per-key handlers add/remove,
                                        GetDefaultDirectory
        AppSettingPropertyTests.cs      typed handles: defaults, write-through,
                                        Set's bool, Changed once per real
                                        change, implicit conversion, enum
                                        round-trip, old-key migration (both the
                                        carry-over and the "new key wins" case),
                                        Wrap
        AppSettingsStoreTests.cs        the file lifecycle end to end: fresh
                                        creation, round-trips (string/int/bool/
                                        enum/byte[]), persistence across reopen,
                                        null removes, change events, auto-backup
                                        naming and pruning, retention clamping
                                        and next-start semantics, corrupt-file
                                        quarantine + restore, type-mismatch
                                        reads, export rules, import validation
                                        and adoption, app-name validation,
                                        use-after-dispose
        AppSettingLoggingServiceTests.cs  text sinks (replayed), level sinks
                                        (not replayed), line format, RemoveSink,
                                        ConsoleOutput default
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.AppSettings
      The package source: AppSettingsService.cs, AppSettingsStore.cs,
      AppSettingProperty.cs, AppSettingChangedEventArgs.cs,
      AppSettingLoggingService.cs - fully XML-documented.

QUICK REFERENCE CARD
====================
using CodeBrix.Platform.AppSettings;

static class AppSettingsService
    static void   Initialize(string appName)
    static void   Initialize(string appName, string directoryPath)
    static void   Shutdown()
    static bool   IsInitialized
    static AppSettingsStore Store                       // throws before Initialize
    static string DirectoryPath
    static string GetDefaultDirectory(string appName)
    static bool   HasValue(string key)
    static T      Get<T>(string key, T defaultValue)
    static T?     Get<T>(string key)
    static void   Set(string key, object? value)         // null removes
    static AppSettingProperty<T> Wrap<T>(string key, T defaultValue)
    static void   AddSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)
    static void   RemoveSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)

sealed class AppSettingsStore : IDisposable
    AppSettingsStore(string appName) / (string appName, string directoryPath)
    static string GetDefaultDirectory(string appName)
    string AppName, DirectoryPath, DatabaseFilePath
    bool   WasCreatedFresh, WasRestoredFromBackup, WasReplacedByImport
    int    AutoBackupRetention { get; set; }            // 0..10, default 5, next start
    bool   HasValue(string key);  T? Get<T>(string key);  T Get<T>(string key, T defaultValue)
    bool   Set(string key, object? value)
    void   AddSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)
    void   RemoveSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)
    event  EventHandler<AppSettingChangedEventArgs>? SettingChanged
    void   ExportToFile(string destinationFilePath)     // refuses the settings folder
    void   StageIncomingFile(string sourceFilePath)     // validated; adopted next start
    void   Dispose()
    const  SettingsFileName, AutoBackupFilePrefix, CorruptFilePrefix, IncomingFileName,
           OldFilePrefix, TimestampFormat, AutoBackupRetentionKey,
           DefaultAutoBackupRetention (5), MaxAutoBackupRetention (10), FamilyFolderName

abstract class AppSettingProperty<T>
    T Value { get; set; };  bool Set(T newValue);  event EventHandler? Changed
    static implicit operator T(AppSettingProperty<T> property)
abstract class AppSettingProperty
    static AppSettingProperty<T> Create<T>(string key, T defaultValue, string? oldKey = null)

class AppSettingChangedEventArgs : EventArgs
    string Key;  object? OldValue (stored JSON text);  object? NewValue (object passed to Set)

enum AppSettingLogLevel { Info, Warning, Error }
static class AppSettingLoggingService
    const string LogCategory = "CodeBrix.Platform.AppSettings"
    static bool ConsoleOutput { get; set; }               // default true
    static void AddSink(Action<string> sink)              // replayed
    static void AddSink(Action<AppSettingLogLevel, string> sink)
    static bool RemoveSink(Action<string> sink)
    static bool RemoveSink(Action<AppSettingLogLevel, string> sink)
    static void LogInfo(string) / LogWarning(string)
    static void LogError(string) / LogError(string, Exception)

Files in the settings folder:
    settings.sqlite                          the live store
    settings_auto_backup_<stamp>.sqlite      newest N kept (N = AutoBackupRetention)
    settings_corrupt_<stamp>.sqlite          quarantined; never pruned
    settings_old_<stamp>.sqlite              replaced by an import; never pruned
    settings_incoming.sqlite                 staged import; adopted on next start
    <stamp> = local time, yyyy-MM-dd_HH-mm-ss
