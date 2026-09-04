================================================================================
AGENT-README: CodeBrix.Platform.WebView
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.WebView.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.WebView.ApacheLicenseForever is the optional WebView add-on
for CodeBrix.Platform desktop apps (.NET 10 or later). Reference it once and
the XAML WebView2 control (Microsoft.UI.Xaml.Controls.WebView2, backed by
Microsoft.Web.WebView2.Core.CoreWebView2) works on ALL six Skia heads:
Windows (Win32), Skia-on-WPF, Linux X11, Linux native Wayland, Linux
FrameBuffer, and macOS.

What the package delivers differs by head:
  - Windows (Win32) and Skia-on-WPF: the package bundles the Microsoft Edge
    WebView2 SDK redistributable (the native loader plus the managed WebView2
    control assemblies) and copies it to the app output, backing the control
    with the Microsoft Edge WebView2 runtime. Only the SDK is shipped here -
    the Edge WebView2 runtime itself comes from the end user's Windows install.
    See THIRD-PARTY-NOTICES.txt (Microsoft Edge WebView2 SDK).
  - Linux (X11, Wayland, AND FrameBuffer): web content is rendered offscreen
    by the system-installed WPE WebKit engine and composited directly into
    the Skia scene - no native child windows, no airspace problems: clipping,
    transforms and z-order behave like any other XAML content. This Linux path
    is 100% Apache-2.0 managed code that P/Invokes the distro's WPE WebKit at
    run time; no WPE engine binaries ship in the package.
  - macOS: inert - WKWebView is built into the OS and the macOS head already
    uses it.

You program against the standard WebView2 contract, which lives in the core
framework package; this add-on supplies the per-head engine behind it. The
add-in's own types (WpeNativeWebViewProvider and the WPE interop declarations
in the CodeBrix.Platform.UI.WebView.Skia.Linux namespaces) are internal
framework seams instantiated through ApiExtension registrations - they are not
an API for app code and are not documented here.

INSTALLATION
============
PackageId:   CodeBrix.Platform.WebView.ApacheLicenseForever
License:     Apache-2.0

    dotnet add package CodeBrix.Platform.WebView.ApacheLicenseForever

NuGet dependencies (pulled in automatically):
  - CodeBrix.Platform.ApacheLicenseForever      the framework itself
  - SkiaSharp

WHERE TO REFERENCE IT
---------------------
Reference this package ONCE, in your app's .Core project, like the other
extension add-ons. Every head gets it transitively:
  - It activates the WPE path on the three Linux heads.
  - It delivers the Microsoft Edge WebView2 payload to the app output on the
    Windows (Win32) and Skia-on-WPF heads. The Windows-head runtime packages
    flag themselves so that the package's build logic applies only there;
    there is nothing for you to configure.
  - It is inert on macOS.
Never reference it from a head project and never look for a per-head variant.

SAME-GENERATION CORE REQUIRED
-----------------------------
The package always ships at the same version as the rest of the
CodeBrix.Platform family (the whole family is published together) and
requires a core of the same generation: the add-in implements internal
framework seams, so the core's InternalsVisibleTo grants must match. Keep
CodeBrix.Platform.ApacheLicenseForever and this package at the same version.

LINUX MACHINES MUST HAVE THE ENGINE INSTALLED
---------------------------------------------
    sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1

When any of the three libraries is missing, creating a WebView throws
PlatformNotSupportedException naming the missing library, its Debian package
and that exact apt command (see COMMON PITFALLS). Windows needs the Microsoft
Edge WebView2 runtime present on the end-user machine (it is part of current
Windows installs); macOS needs nothing.

KEY NAMESPACES / USINGS
=======================
    using Microsoft.UI.Xaml.Controls;     // WebView2 (the control; default XAML xmlns)
    using Microsoft.Web.WebView2.Core;    // CoreWebView2, CoreWebView2Settings,
                                          // CoreWebView2NavigationStartingEventArgs,
                                          // CoreWebView2NavigationCompletedEventArgs,
                                          // CoreWebView2WebMessageReceivedEventArgs,
                                          // CoreWebView2NewWindowRequestedEventArgs,
                                          // CoreWebView2DownloadStartingEventArgs,
                                          // CoreWebView2DownloadOperation,
                                          // CoreWebView2DownloadState,
                                          // CoreWebView2HostResourceAccessKind,
                                          // CoreWebView2WebErrorStatus
Nothing from the add-in's own CodeBrix.Platform.UI.WebView.Skia.* namespaces
is referenced by app code. Package ids carry the license suffix; namespaces
do not.

CORE API REFERENCE
==================

WebView2 (Microsoft.UI.Xaml.Controls) - the XAML control
--------------------------------------------------------
    public Uri Source { get; set; }              // DP: SET it to navigate; it
                                                 // mirrors the engine only after
                                                 // navigation completes
    public string? SourceFromCore { get; }       // live current URL straight
                                                 // from CoreWebView2.Source
    public CoreWebView2 CoreWebView2 { get; }    // never null; created in the
                                                 // control's constructor
    public bool CanGoBack { get; }               // DPs, kept in sync from
    public bool CanGoForward { get; }            // HistoryChanged
    public bool IsScrollEnabled { get; set; }    // default true
    public bool NavigateToGoddessUrlOnLaunch { get; set; }
                                                 // opt-in: when true and Source is
                                                 // unset, navigates to the built-in
                                                 // default page on launch
    public IAsyncAction EnsureCoreWebView2Async()
                                                 // completes once the native engine
                                                 // view exists (after the control
                                                 // template is applied)
    public IAsyncOperation<string?> ExecuteScriptAsync(string javascriptCode)
    public void NavigateToString(string htmlContent)
    public void NavigateToGoddessUrl()           // = Source = the default page
    public void Reload()
    public void GoBack()
    public void GoForward()

    public event TypedEventHandler<WebView2, CoreWebView2InitializedEventArgs>
        CoreWebView2Initialized;
    public event TypedEventHandler<WebView2, CoreWebView2NavigationStartingEventArgs>
        NavigationStarting;
    public event TypedEventHandler<WebView2, CoreWebView2NavigationCompletedEventArgs>
        NavigationCompleted;
    public event TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>
        WebMessageReceived;
    public event TypedEventHandler<WebView2, CoreWebView2ProcessFailedEventArgs>
        CoreProcessFailed;                       // declared for contract parity;
                                                 // never raised on the Skia heads

The control's NavigationStarting/NavigationCompleted/WebMessageReceived simply
forward the CoreWebView2 events of the same name, so subscribe on whichever is
handier.

CoreWebView2 (Microsoft.Web.WebView2.Core) - the engine facade
--------------------------------------------------------------
    public CoreWebView2Settings Settings { get; }
    public string Source { get; }                // authoritative current top-level
                                                 // document URL
    public string DocumentTitle { get; }
    public bool CanGoBack { get; }
    public bool CanGoForward { get; }
    public void Navigate(string uri)             // must be an ABSOLUTE uri, else
                                                 // ArgumentException
    public void NavigateToString(string htmlContent)
    public IAsyncOperation<string?> ExecuteScriptAsync(string javaScript)
                                                 // null when no native view yet
    public void GoBack()
    public void GoForward()
    public void Stop()
    public void Reload()
    public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath,
                                                  CoreWebView2HostResourceAccessKind accessKind)
    public void ClearVirtualHostNameToFolderMapping(string hostName)

    public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationStartingEventArgs>
        NavigationStarting;
    public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationCompletedEventArgs>
        NavigationCompleted;
    public event TypedEventHandler<CoreWebView2, CoreWebView2NewWindowRequestedEventArgs>
        NewWindowRequested;
    public event TypedEventHandler<CoreWebView2, object> DocumentTitleChanged;
    public event TypedEventHandler<CoreWebView2, object> HistoryChanged;
    public event TypedEventHandler<CoreWebView2, CoreWebView2SourceChangedEventArgs>
        SourceChanged;
    public event TypedEventHandler<CoreWebView2, CoreWebView2WebMessageReceivedEventArgs>
        WebMessageReceived;
    public event TypedEventHandler<CoreWebView2, CoreWebView2DownloadStartingEventArgs>
        DownloadStarting;

Virtual host mapping: on the Windows (Win32) and WPF heads the mapping is
passed to the Edge engine. On the Linux heads the add-in resolves navigations
to a mapped host itself: hostName/path becomes file://<app install folder>/
<folderPath>/<path> (the accessKind is accepted but not enforced there).

CoreWebView2Settings
--------------------
    public bool IsWebMessageEnabled { get; set; }   // default true
    public string UserAgent { get; set; }           // "" (default) = engine default

CUSTOM USER-AGENT: on every head, app code can set the User-Agent string the
WebView sends; an empty string restores the engine's default:
    myWebView.CoreWebView2.Settings.UserAgent = "MyApp/1.0";
It may be set before or after the control loads and applies to the next
request. Backed natively on all six heads (WPE WebKit on Linux, Edge WebView2
on Windows/WPF, WKWebView customUserAgent on macOS). With no value set, each
engine sends its own desktop User-Agent.

Event argument types
--------------------
    CoreWebView2NavigationStartingEventArgs
        public ulong NavigationId { get; }
        public string? Uri { get; }          // for NavigateToString this is a
                                             // data:text/html;charset=utf-8;base64 URI
        public bool Cancel { get; set; }     // set true to refuse the navigation
        public bool IsRedirected { get; }
        public bool IsUserInitiated { get; }
    CoreWebView2NavigationCompletedEventArgs
        public ulong NavigationId { get; }
        public bool IsSuccess { get; }
        public int HttpStatusCode { get; }
        public CoreWebView2WebErrorStatus WebErrorStatus { get; }
    CoreWebView2WebMessageReceivedEventArgs
        public string WebMessageAsJson { get; }
        public string TryGetWebMessageAsString()   // ArgumentException when the
                                                   // posted value is not a string
    CoreWebView2NewWindowRequestedEventArgs
        public string Uri { get; }
        public bool Handled { get; set; }

PAGE-TO-HOST MESSAGING
----------------------
Page-to-host messaging supports both the WebView2 idiom and the WebKit idiom;
use either from your page script, on any head:
    window.chrome.webview.postMessage("hello");                         // WebView2 idiom
    window.webkit.messageHandlers.codebrixWebView.postMessage("hello"); // WebKit idiom
The host receives it in WebMessageReceived (always on the UI thread):
    Browser.WebMessageReceived += (WebView2 sender,
                                   CoreWebView2WebMessageReceivedEventArgs args) =>
    {
        var text = args.TryGetWebMessageAsString();   // or args.WebMessageAsJson
    };
Settings.IsWebMessageEnabled must be true (the default).

HOST-TO-PAGE: CoreWebView2.PostWebMessageAsString / PostWebMessageAsJson are
NOT implemented on the Skia heads (they log a not-implemented message, or
throw NotImplementedException when FeatureConfiguration.ApiInformation
.IsFailWhenNotImplemented is true). Push data into the page with
ExecuteScriptAsync instead:
    await Browser.ExecuteScriptAsync("window.receiveFromHost('hello')");

DOWNLOADS
---------
File downloads work on every head through the standard WebView2 contract. A
response the engine cannot display, one whose Content-Disposition is
"attachment", or an anchor with the HTML5 download attribute becomes a
download instead of a dead-ended navigation.

CoreWebView2.DownloadStarting is raised on the UI thread with:
    CoreWebView2DownloadStartingEventArgs
        public CoreWebView2DownloadOperation DownloadOperation { get; }
        public bool Cancel { get; set; }            // refuse the download
        public string ResultFilePath { get; set; }  // change the target file
        public bool Handled { get; set; }
        public Deferral GetDeferral()               // decide asynchronously
                                                    // (e.g. after a save-file
                                                    // picker); the download is
                                                    // parked until it completes
UNHANDLED DEFAULT: the file is saved silently to the user's Downloads folder
(the XDG download dir on Linux, ~/Downloads elsewhere) under a collision-free
name ("name (1).ext" auto-rename, the WebView2 scheme).

    CoreWebView2DownloadOperation
        public string Uri { get; }
        public string MimeType { get; }
        public string ContentDisposition { get; }
        public long TotalBytesToReceive { get; }
        public long BytesReceived { get; }
        public string EstimatedEndTime { get; }
        public string ResultFilePath { get; }
        public CoreWebView2DownloadState State { get; }   // InProgress,
                                                          // Interrupted, Completed
        public CoreWebView2DownloadInterruptReason InterruptReason { get; }
        public bool CanResume { get; }                    // always false
        public void Cancel()
        public event TypedEventHandler<CoreWebView2DownloadOperation, object>?
            BytesReceivedChanged;
        public event TypedEventHandler<CoreWebView2DownloadOperation, object>?
            EstimatedEndTimeChanged;
        public event TypedEventHandler<CoreWebView2DownloadOperation, object>?
            StateChanged;

Head notes: the Windows (Win32/WPF) heads pass the native Edge WebView2
download straight through. The Linux heads use WebKit's asynchronous
decide-destination, which needs a WPE WebKit build at least as new as 2.40
(current Debian packages qualify). macOS uses WKDownload (macOS 11.3 or later;
on older macOS downloads never start) and requires the current
libCodeBrixNativeMac.dylib from the macOS head package - with an older dylib
the WebView still works and a one-time warning says downloads are disabled.

NOT IMPLEMENTED - downloads: CoreWebView2DownloadOperation.Pause() and
.Resume() (no engine on any head exposes pause/resume; CanResume is always
false), and the DefaultDownloadDialog APIs (the Skia heads draw no built-in
download UI). These remain NotImplemented stubs by design.

FeatureConfiguration
--------------------
FeatureConfiguration.WebView and FeatureConfiguration.WebView2 exist but carry
no settable members on the Skia heads (their upstream flags are compiled out),
so there is nothing WebView-specific to configure. The one relevant global
switch is FeatureConfiguration.ApiInformation.IsFailWhenNotImplemented, which
turns not-implemented members (PostWebMessageAsString, CoreWebView2Environment
.*, ...) from a logged message into a thrown NotImplementedException.

Windows-head specifics
----------------------
  - The Win32 head creates the Edge environment with the user data folder
    ApplicationData.Current.LocalFolder\WebView2; the WPF head hosts the Edge
    WebView2 WPF control with that control's own defaults. The user data
    folder is not configurable through the contract:
    CoreWebView2Environment.UserDataFolder, CreateAsync() and
    GetAvailableBrowserVersionString(...) are NotImplemented stubs, so do not
    use them for runtime detection; if the Edge WebView2 runtime is absent the
    environment creation fails and that exception surfaces when the WebView's
    native view is created (first layout of the control).
  - The Win32 head REQUIRES an STA UI thread. Use a synchronous
    "[STAThread] static void Main" that calls host.Run(); an "async Task Main"
    silently drops [STAThread] and the WebView fails with an
    InvalidOperationException explaining exactly this (RPC_E_CHANGED_MODE
    underneath).

UI-THREAD RULES
---------------
  - WebView2 is a XAML Control: create it, set Source, and call its methods
    on the UI thread, like any other control.
  - Every event - NavigationStarting/Completed, WebMessageReceived,
    DocumentTitleChanged, HistoryChanged, SourceChanged, NewWindowRequested,
    DownloadStarting and the download-operation progress events - is raised on
    the UI thread on every head (the Linux engine runs on its own thread and
    the add-in marshals everything back). You may touch other controls from
    those handlers directly.
  - await Browser.EnsureCoreWebView2Async() from Loaded (or later) before
    subscribing to CoreWebView2 events that must catch the very first
    navigation, such as DownloadStarting.

COMPLETE EXAMPLES
=================

1. XAML: a browser page
-----------------------
    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBox x:Name="AddressBox" Width="400" Text="https://example.com" />
                <Button Content="Go" Click="Go_Click" />
                <Button Content="Back" Click="Back_Click" />
            </StackPanel>
            <WebView2 x:Name="Browser" Grid.Row="1"
                      Source="https://example.com" />
        </Grid>
    </Page>

2. Code-behind: navigation, title, back, script, messages
---------------------------------------------------------
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.Web.WebView2.Core;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Browser.NavigationStarting += Browser_NavigationStarting;
            Browser.NavigationCompleted += Browser_NavigationCompleted;
            Browser.WebMessageReceived += Browser_WebMessageReceived;
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Settings.UserAgent = "MyApp/1.0";
            Browser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        }

        private void Go_Click(object sender, RoutedEventArgs e)
            => Browser.Source = new Uri(AddressBox.Text);   // absolute URIs only

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack) Browser.GoBack();
        }

        private void Browser_NavigationStarting(WebView2 sender,
            CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri is { } uri && uri.StartsWith("http://", StringComparison.Ordinal))
            {
                args.Cancel = true;   // example policy: refuse plain http
            }
        }

        private async void Browser_NavigationCompleted(WebView2 sender,
            CoreWebView2NavigationCompletedEventArgs args)
        {
            // Read the live URL from the engine, not the Source DP.
            AddressBox.Text = sender.CoreWebView2.Source;
            if (args.IsSuccess)
            {
                var title = await Browser.ExecuteScriptAsync("document.title");
                // same value as sender.CoreWebView2.DocumentTitle
            }
            else
            {
                // args.WebErrorStatus / args.HttpStatusCode explain the failure
            }
        }

        private void Browser_WebMessageReceived(WebView2 sender,
            CoreWebView2WebMessageReceivedEventArgs args)
        {
            var text = args.TryGetWebMessageAsString();
            // page called window.chrome.webview.postMessage(text)
        }

        private void CoreWebView2_DownloadStarting(CoreWebView2 sender,
            CoreWebView2DownloadStartingEventArgs args)
        {
            var op = args.DownloadOperation;
            op.StateChanged += (o, _) =>
            {
                if (o.State == CoreWebView2DownloadState.Completed)
                {
                    // o.ResultFilePath now exists on disk
                }
            };
            // leave args untouched to accept the default Downloads-folder path,
            // or set args.ResultFilePath / args.Cancel.
        }
    }

3. Loading your own HTML and talking both ways
----------------------------------------------
    Browser.NavigateToString("""
        <html><body>
          <button onclick="window.chrome.webview.postMessage('clicked')">Click</button>
          <div id="out"></div>
          <script>
            window.receiveFromHost = function (s) {
              document.getElementById('out').textContent = s;
            };
          </script>
        </body></html>
        """);

    Browser.WebMessageReceived += async (s, args) =>
    {
        if (args.TryGetWebMessageAsString() == "clicked")
        {
            await Browser.ExecuteScriptAsync("window.receiveFromHost('host says hi')");
        }
    };

4. Deferred download decision (pick a file first)
-------------------------------------------------
    private async void CoreWebView2_DownloadStarting(CoreWebView2 sender,
        CoreWebView2DownloadStartingEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var path = await ChooseSavePathAsync();   // your picker code
            if (path is null)
            {
                args.Cancel = true;
            }
            else
            {
                args.ResultFilePath = path;
            }
        }
        finally
        {
            deferral.Complete();   // the parked download proceeds (or is refused)
        }
    }

5. Serving app-local content through a virtual host
---------------------------------------------------
    await Browser.EnsureCoreWebView2Async();
    Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
        "app.local", "WebContent", CoreWebView2HostResourceAccessKind.Allow);
    Browser.Source = new Uri("https://app.local/index.html");
    // "WebContent" is a folder of Content files copied to the app output.

MINIMUM VIABLE PROJECT
======================
The app follows the standard CodeBrix.Platform layout (.Core class library +
.UI shared project + one head project per OS; see the framework's root
AGENT-README for the head packages and each head's Program.cs). The parts
specific to this add-on:

MyApp.Core/MyApp.Core.csproj (the ONLY place the package is referenced):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever"
                          Version="<current family version>" />
        <PackageReference Include="CodeBrix.Platform.WebView.ApacheLicenseForever"
                          Version="<same family version>" />
      </ItemGroup>
    </Project>

Head projects: nothing extra. Linux machines need the engine:

    sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1

MyApp.Win32Skia/Program.cs (the STA requirement matters on this head):

    using CodeBrix.Platform.UI.Hosting;

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var host = CodeBrixPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWindowsWin32()
                .Build();
            host.Run();          // synchronous - NOT 'await host.RunAsync()'
        }
    }

MyApp.UI/Views/MainPage.xaml:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        <WebView2 x:Name="Browser" Source="https://example.com" />
    </Page>

That is a working browser on all six heads.

PERFORMANCE TIPS
================
  - Linux: each rendered web frame is copied into a Skia image and composited;
    cost scales with the WebView's pixel size. Keep very large WebViews to
    what the page needs, and collapse hidden ones rather than leaving them
    rendering behind other content.
  - ExecuteScriptAsync round-trips to the engine thread on Linux (and to the
    Edge process on Windows); batch work into one script call rather than many
    small ones inside tight loops.
  - Keep one WebView2 alive and re-navigate it instead of creating a fresh
    control per page: engine initialization is the expensive step.
  - CoreWebView2.Source and DocumentTitle are plain property reads; they need
    no script call.

COMMON PITFALLS TO AVOID
========================
  - Missing engine on Linux: creating a WebView throws
    PlatformNotSupportedException such as
    "WebView on Linux requires the system WPE WebKit engine, and the library
     'libWPEWebKit-2.0.so.1' (Debian package 'libwpewebkit-2.0-1') was not
     found. To install everything needed on Debian-based distros, run:
     sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1"
    The library named is the first missing one of the three.
  - Mismatched generations: this package and CodeBrix.Platform
    .ApacheLicenseForever must be the same version (internal seams).
  - Reading WebView2.Source to learn where you are: it is the SET/bind target
    and lags redirects and link navigations. Read CoreWebView2.Source (or the
    SourceFromCore shortcut) inside navigation callbacks.
  - CoreWebView2.Navigate(string) requires an ABSOLUTE URI and throws
    ArgumentException otherwise; the same goes for new Uri(...) into Source.
  - PostWebMessageAsString / PostWebMessageAsJson do nothing on the Skia heads
    (see PAGE-TO-HOST MESSAGING); use ExecuteScriptAsync for host-to-page.
  - Subscribing to CoreWebView2.DownloadStarting (or other CoreWebView2
    events) only after the first navigation already finished misses that
    navigation; subscribe in the constructor or right after
    EnsureCoreWebView2Async() in Loaded.
  - Win32 head with "async Task Main": the WebView cannot initialize (MTA
    thread). Use [STAThread] + synchronous Main + host.Run().
  - CoreWebView2Environment.* (UserDataFolder, CreateAsync,
    GetAvailableBrowserVersionString) are NotImplemented stubs; there is no
    contract-level Edge runtime detection - handle the initialization
    exception instead.
  - Linux limitations by design: no IME (composed CJK/dead-key) text input;
    popup/new-window requests navigate the current view (NewWindowRequested is
    the hook if you want to intercept them); the mouse cursor does not change
    shape over links.
  - Do not reference the package from a head project or try to construct
    WpeNativeWebViewProvider yourself; the framework wires it.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - Does not ship a browser engine: Linux uses the distro's WPE WebKit (apt),
    Windows uses the end user's Microsoft Edge WebView2 runtime (only the SDK
    loader and managed assemblies are bundled), macOS uses the OS WKWebView.
  - Does nothing on macOS (inert; the head's own WKWebView support is used).
  - No host-to-page PostWebMessageAsString/AsJson (use ExecuteScriptAsync).
  - No download Pause()/Resume() (CanResume is always false), no built-in
    download UI (DefaultDownloadDialog APIs are stubs).
  - No CoreWebView2Environment configuration (user data folder, browser
    executable folder, runtime version query) through the contract.
  - No CoreProcessFailed reporting (declared, never raised).
  - Linux: no IME input, no separate popup windows, no link-hover cursor.
  - Does not change the WebView2 API surface: everything you use is the
    standard WinUI/WebView2 contract from the core package.

WORKING EXAMPLES ON GITHUB
==========================
  - WebViewDemo (all six heads: address box, Go/Back/Forward, a modal
    ContentDialog over the WebView, NavigationCompleted reading
    CoreWebView2.Source and DocumentTitle, and DownloadStarting wired into a
    status line with BytesReceivedChanged / StateChanged):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/WebViewDemo
    The shared page is WebViewDemo.UI/Views/MainPage.xaml(.cs).
  - The core WebView2 contract sources (control + CoreWebView2 facade):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI/UI/Xaml/Controls/WebView
  - The add-in source (internal seams; Linux WPE engine binding):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.WebView.Skia

QUICK REFERENCE CARD
====================
Package        CodeBrix.Platform.WebView.ApacheLicenseForever   Apache-2.0
Reference in   .Core ONLY (once); heads inherit it; same version as the core
Heads          Win32 + WPF: Edge WebView2 SDK bundled, Edge runtime from Windows
               X11 + Wayland + FrameBuffer: system WPE WebKit, offscreen -> Skia
               macOS: inert (WKWebView built in)
Linux setup    sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1
Missing engine PlatformNotSupportedException naming the .so, the package, the apt line
Win32 head     [STAThread] + synchronous Main + host.Run()

XAML           <WebView2 x:Name="Browser" Source="https://example.com" />
Navigate       Browser.Source = new Uri(absolute);  Browser.CoreWebView2.Navigate(string)
               Browser.NavigateToString(html);  Reload() / GoBack() / GoForward()
Where am I     Browser.CoreWebView2.Source (live)  - not Browser.Source (lags)
Title          Browser.CoreWebView2.DocumentTitle (+ DocumentTitleChanged)
Ready          await Browser.EnsureCoreWebView2Async();
Script         string? r = await Browser.ExecuteScriptAsync("document.title");
Page -> host   window.chrome.webview.postMessage(x)  or
               window.webkit.messageHandlers.codebrixWebView.postMessage(x)
               -> WebMessageReceived(args.TryGetWebMessageAsString() / WebMessageAsJson)
Host -> page   ExecuteScriptAsync(...)   (PostWebMessageAs* NOT implemented)
User-Agent     Browser.CoreWebView2.Settings.UserAgent = "MyApp/1.0";  "" = default
Nav events     NavigationStarting(args.Uri, args.Cancel) / NavigationCompleted
               (args.IsSuccess, HttpStatusCode, WebErrorStatus); NewWindowRequested
Virtual host   CoreWebView2.SetVirtualHostNameToFolderMapping(host, folder, kind)
Downloads      CoreWebView2.DownloadStarting: args.Cancel | args.ResultFilePath |
               args.GetDeferral(); args.DownloadOperation: Uri, MimeType,
               BytesReceived/TotalBytesToReceive, State, Cancel(), StateChanged,
               BytesReceivedChanged, EstimatedEndTimeChanged
               default: silent save to Downloads with "name (1).ext" renaming
Threads        all events on the UI thread; touch the control on the UI thread
Not on Skia    PostWebMessageAs*, CoreWebView2Environment.*, download Pause/Resume,
               DefaultDownloadDialog, CoreProcessFailed; Linux: IME, popups, cursor
