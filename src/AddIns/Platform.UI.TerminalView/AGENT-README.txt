================================================================================
AGENT-README: CodeBrix.Platform.TerminalView
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.TerminalView.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.TerminalView is a terminal emulator view for
CodeBrix.Platform applications. Its one application-facing type,
TerminalControl (a XAML Control), renders a CodeBrix.Terminal engine - the
CodeBrix fork of XtermSharp, VT100 / VT220 / xterm-compatible - as a fixed
monospace cell grid on a Skia surface, and turns keyboard input into VT byte
sequences. It runs on every head the framework has: Windows (Win32 and
Skia-on-WPF), Linux (X11, Wayland, FrameBuffer) and macOS.

The control is TRANSPORT-AGNOSTIC. It is the screen-and-keyboard half of a
terminal, the way a pty master would see it: feed it the bytes or text that
arrive from any source (an SSH ShellStream read loop, a PTY, a local
process), and wire the VT-encoded input it emits back to that source. The
whole transport contract is three wires: InputEmitted, GridResized, Feed.

What it has:
  - ANSI / SGR attributes (bold, italic, underline, inverse, dim, invisible,
    crossed-out) and the 16 + 256-color palettes
  - scrollback with a scrollbar, the mouse wheel, and Shift+PageUp /
    PageDown; typing snaps back to live output
  - text selection by drag, word / expression selection by double-click,
    auto-scroll while dragging past the top or bottom edge
  - clipboard copy AND paste, owned by the control: right-click context
    menu, Ctrl+Shift+C / Ctrl+Shift+V; pasted line endings normalized to CR
  - live grid resize following the control size, with (columns, rows)
    reported for PTY window-change requests
  - keyboard encoding through the engine's TerminalKeyEncoder:
    application-cursor mode, Ctrl chords (C0 codes), Alt-as-meta (ESC
    prefix), Shift+Tab back-tab, and layout-composed printables
  - OSC 0 / 2 window titles reported through an event
  - a blinking block cursor when focused, a steady hollow one when not
  - the software keyboard on heads that have one (the FrameBuffer head) is
    summoned when the terminal takes focus
  - wide (two-cell) CJK characters and emoji stay grid-aligned

Text is laid out by the framework's TextLayout engine; the default font is
the bundled Roboto Mono.

Target: .NET 10 or later, inside a CodeBrix.Platform application (the control
is a XAML element and needs the visual tree of one of the heads).

Provenance: TerminalControl and its Rendering helpers are the author's own
code, first written for a separate tool and relicensed to Apache-2.0 for
this package; they were reworked onto the family's add-in pattern (Control
plus an internal render canvas, a code-built scrollbar template, the
engine's own scrolling and key-encoding APIs).


INSTALLATION
============
Package id:   CodeBrix.Platform.TerminalView.ApacheLicenseForever

    dotnet add package CodeBrix.Platform.TerminalView.ApacheLicenseForever

Reference it from the shared UI project of your application - the project
that already references CodeBrix.Platform.ApacheLicenseForever - not from
the per-platform head projects.

NuGet dependencies (all flow in automatically):
  - CodeBrix.Platform.ApacheLicenseForever              the framework
  - CodeBrix.Platform.TextLayout.ApacheLicenseForever   text shaping and
                                                        cell metrics
  - CodeBrix.Terminal.MitLicenseForever                 the terminal engine
                                                        (parser, buffer,
                                                        key encoder,
                                                        selection service,
                                                        PTY helpers on
                                                        Unix / macOS)
  - CodeBrix.Platform.Fonts.RobotoMono.OflLicenseForever  the default font
  - SkiaSharp                                           declared directly by
                                                        this package (and also
                                                        carried by the
                                                        framework)

License: Apache-2.0 (dependencies: MIT for the engine, OFL-1.1 for the
font, Apache-2.0 for the rest).

Requirements: a running CodeBrix.Platform application head, and a transport
of your own - the package ships none. For SSH the family offers
CodeBrix.SSH; for a local shell on Unix / macOS the engine's Pty class
(see COMPLETE EXAMPLES).


KEY NAMESPACES / USINGS
=======================
XAML - either form works (the demo uses the second):

    xmlns:term="using:CodeBrix.Platform.UI.TerminalView"
    xmlns:term="clr-namespace:CodeBrix.Platform.UI.TerminalView;assembly=CodeBrix.Platform.UI.TerminalView"

Code:

    using CodeBrix.Platform.UI.TerminalView;   // TerminalControl
    using SkiaSharp;                           // SKColor for the color properties
    using CodeBrix.Terminal.Engine;            // only for Pty / UnixWindowSize
                                               // when hosting a local shell

The helper namespace CodeBrix.Platform.UI.TerminalView.Rendering exists
(see "Helper types" below) but an application never needs to import it.

Name clash to know about: inside the engine's namespace the simple name
"Terminal" is both the CodeBrix.Terminal namespace and the engine's Terminal
class. Application code that imports CodeBrix.Terminal.Engine and also
names its control x:Name="Terminal" should refer to the engine type by its
full name if it ever needs it (the control never requires you to touch the
engine type).


CORE API REFERENCE
==================

TerminalControl
---------------
    public sealed partial class TerminalControl : Control

    public TerminalControl()
        Creates the control with an 80x25 engine that resizes to fit, with
        ConvertEol = false and the engine's default scrollback.

Events (all raised on the UI thread):

    public event Action<string>? InputEmitted
        VT-encoded keyboard input - and pasted text - to send to the host.
        The string is what a pty master would receive; send it to the
        transport as UTF-8 bytes. Raising it also snaps the view back to
        the live tail and repaints.

    public event Action<int, int>? GridResized
        (columns, rows) whenever the grid dimensions change with the
        control size or font. Forward it to the transport's window-size
        channel - for SSH, ShellStream.ChangeWindowSize(cols, rows, 0, 0);
        for a PTY, Pty.SetWinSize. Fires only when the numbers actually
        change; a resize that keeps the same grid is silent.

    public event Action<string>? TitleChanged
        The window title set by OSC 0 / OSC 2. (OSC 1 icon titles are
        accepted and ignored.)

    public event Action<string>? CopyRequested
        The selected text, whenever it is copied (context menu or
        Ctrl+Shift+C). Observational: by the time it fires the control has
        already placed the text on the clipboard. Use it for a status line,
        a copy history, or a host-side clipboard mirror. It does not fire
        for an empty selection.

Properties:

    public int Columns { get; }
    public int Rows { get; }
        The engine's current grid. Read-only; the grid follows the control
        size (minimum 4 columns x 2 rows).

    public SKColor ForegroundColor { get; set; }     default white (ff,ff,ff)
    public SKColor BackgroundColor { get; set; }     default black (00,00,00)
    public SKColor SelectionColor { get; set; }      default (4d,8b,d8) alpha 66
        The three theme knobs. Each setter repaints, so a scheme switch
        restyles the whole grid - scrollback included - instantly. Only
        DEFAULT-attributed cells follow ForegroundColor / BackgroundColor;
        text that names a palette color keeps its palette value on either
        ground. SelectionColor is a translucent overlay painted over
        selected cells, so give it an alpha that reads on your background.

    public bool ConvertEol { get; set; }              default false
        Whether a bare LF in fed data is treated as CR+LF. False suits
        transports that emit explicit CR+LF (remote shells over SSH, most
        PTYs); set true for a host that emits bare LF (a pipe-connected
        process, a log file), or every line break lands one column in.

    public int Scrollback { get; set; }               default 1000
        Lines kept beyond the visible rows. Set it BEFORE the control is
        loaded; a change afterwards takes effect on the next grid resize.
        Negative values clamp to 0.

    public string TerminalFontFamily { get; set; }
        A font URI or family name understood by TextLayout. Default
        "ms-appx:///CodeBrix.Platform.Fonts.RobotoMono/Fonts/RobotoMono.ttf"
        (the RobotoMono fonts package, a declared dependency). Null or
        blank restores the default. Must be MONOSPACED: the cell advance is
        measured from the glyph "x", so a proportional face misaligns the
        grid. Setting it re-measures the cell, re-fits the grid (GridResized
        fires if the numbers change) and repaints.

    public float TerminalFontSize { get; set; }       default 14 (DIPs)
        Values below 4 clamp to 4. Same re-measure / re-fit behaviour.

Methods:

    public void Feed(string data)
    public void Feed(byte[] data, int length)
        Feed VT output into the terminal. Both are safe from ANY thread:
        each call enqueues one work item on the control's dispatcher queue,
        which feeds the engine, updates the scrollbar and schedules a
        repaint. Differences:
          - Feed(string) hands already-decoded text to the engine. Use it
            for text you produce yourself (a local echo, a banner, a
            status message).
          - Feed(byte[], int) copies the first `length` bytes and hands the
            COPY to the engine's byte path, so the caller may reuse its
            read buffer immediately. This is the natural shape for a
            transport read loop (a stream's Read into a byte[]), and it
            keeps decoding inside the engine: decoding to a string yourself
            before Feed(string) risks splitting a multi-byte UTF-8 sequence
            across two reads.
        Both return without doing anything when the data is empty - AND
        when the control has no dispatcher queue yet, i.e. it is not in the
        visual tree. Data fed before the control is loaded is dropped.

    public void Reset()
        A full terminal reset (RIS): engine reset, selection cleared,
        scrollbar updated, repaint. A host clearing the screen between
        sessions calls this. It does not touch the color properties, the
        font, or the event subscriptions.

    public void GrabFocus()
        Gives the control keyboard focus (FocusState.Programmatic). Call it
        after connecting a session so typing goes straight to the host.

Keyboard behaviour (what happens before InputEmitted fires):
  - Ctrl+Shift+C copies, Ctrl+Shift+V pastes; neither reaches the host.
  - Shift+PageUp / PageDown page through scrollback by (rows - 1) lines;
    neither reaches the host.
  - Ctrl and Alt chords go through TerminalKeyEncoder.Encode (Ctrl -> C0
    control codes, Alt -> ESC prefix), honouring the engine's
    application-cursor mode.
  - Shift+Tab becomes back-tab.
  - Cursor / function / editing keys use the encoder's special-key path.
  - Printable characters prefer the platform's layout-composed character
    (so shifted symbols on non-US layouts come out right); the raw-key
    fallback assumes US-QWERTY.
  - Caps Lock is tracked and passed to the encoder as a modifier.

Mouse behaviour:
  - Left press starts a selection (the control takes pointer capture);
    drag extends it cell by cell; dragging above or below the control
    scrolls the view one line every 90 ms while held there.
  - Double-click (two presses within 400 ms on the same cell) selects the
    word / expression under the pointer, per the engine's rules.
  - Right-click opens the Copy / Paste context menu at the pointer (Copy is
    enabled only while a selection is active).
  - Wheel scrolls three lines per notch; positive delta scrolls back into
    history.
  - Mouse-reporting escape protocols (X10 / SGR / VT200 ...) are NOT
    forwarded to the host, so a full-screen application that wants mouse
    events never gets them.

Colors and attributes:
  - Palette indices resolve against the engine's 256-entry
    Color.DefaultAnsiColors table: 0-7 standard, 8-15 bright, 16-231 the
    6x6x6 cube, and the grayscale ramp after it. There is no property to
    replace that table; the three SKColor properties are the whole theme
    surface.
  - The default-foreground / default-background sentinels resolve to
    ForegroundColor / BackgroundColor.
  - BOLD promotes palette colors 0-7 to their bright 8-15 twin (classic
    bold-as-bright) AND selects the bold face; ITALIC selects the italic
    face; INVERSE swaps the two grounds; DIM darkens the foreground to 60%;
    INVISIBLE paints the foreground in the background color.
  - Underline is drawn 2 DIPs below the baseline, crossed-out at half the
    cell height, both at a stroke width of max(1, fontSize / 14).
  - TrueColor (24-bit SGR 38;2 / 48;2) is not in the engine's color model;
    only the 256-color palette is.
  - A background color applied by an ERASE (for example ESC[41m ESC[K)
    paints nothing beyond the line's last character: only cells up to the
    line's trimmed length are rendered.

Helper types (public, internal-purpose)
---------------------------------------
These live in CodeBrix.Platform.UI.TerminalView.Rendering, are public so they
can be unit-tested in isolation, and are NOT extension points: the control
calls them directly and never consults a replacement. Their shape may
change without notice.

    AttributeDecoder    static CellStyle Decode(int attribute,
                            SKColor defaultForeground, SKColor defaultBackground)
                        - packed engine attribute -> concrete colors + flags,
                          applying the policies listed above
    CellStyle           readonly record struct (SKColor Foreground,
                            SKColor Background, bool Bold, bool Italic,
                            bool Underline, bool CrossedOut);
                        bool HasVisibleBackground(SKColor defaultBackground)
    CellMetrics         readonly record struct; float Width, Height, Baseline;
                        static CellMetrics Measure(string? fontFamily, float fontSize)
                        - measures "x" through TextLayout; the cell geometry
    RunBuilder          static List<TextRunSegment> BuildRuns(BufferLine? line)
                        - one engine line -> drawable runs: consecutive
                          single-width cells sharing an attribute coalesce,
                          each wide character is its own two-cell segment,
                          zero-width continuation cells are skipped
    TextRunSegment      sealed class; int StartColumn, int CellCount,
                        string Text, int Attribute, bool IsWide


COMPLETE EXAMPLES
=================

1. Declare the control
----------------------
    <Page ...
          xmlns:term="using:CodeBrix.Platform.UI.TerminalView">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <TextBlock x:Name="TitleText" Grid.Row="0" />
            <term:TerminalControl x:Name="Terminal" Grid.Row="1" Margin="8" />
        </Grid>
    </Page>

A bounded star cell is the recommended host: the grid (columns x rows)
follows the control size.

2. Bridge to an SSH ShellStream (three wires)
---------------------------------------------
    using System.Text;

    // 1. keyboard / paste -> host
    Terminal.InputEmitted += text =>
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        shellStream.Write(bytes, 0, bytes.Length);
        shellStream.Flush();
    };

    // 2. grid size -> host (a PTY window-change request)
    Terminal.GridResized += (cols, rows) =>
        shellStream.ChangeWindowSize((uint)cols, (uint)rows, 0, 0);

    Terminal.TitleChanged += title => TitleText.Text = title;
    Terminal.CopyRequested += text => StatusText.Text = $"copied {text.Length} chars";

    // 3. host -> screen: a read loop on any thread
    _ = Task.Run(() =>
    {
        var buffer = new byte[8192];
        int read;
        while ((read = shellStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            Terminal.Feed(buffer, read);     // copies; buffer is reusable at once
        }
    });

    Terminal.GrabFocus();

Leave ConvertEol at its default (false): a remote shell emits CR+LF.

3. Host a local shell through a PTY (Unix / macOS only)
--------------------------------------------------------
The engine's Pty class forks and execs a process on a pseudo-terminal and
hands back the master file descriptor. The call shape, from the
CodeBrix.Terminal AGENT-README (PTY SUPPORT section):

    using CodeBrix.Terminal.Engine;
    using Microsoft.Win32.SafeHandles;
    using System.IO;
    using System.Text;

    var winSize = new UnixWindowSize { row = 25, col = 80 };
    int pid = Pty.ForkAndExec("/bin/bash", args, env, out int master, winSize);

    // Any Stream over the master descriptor will do:
    var pty = new FileStream(new SafeFileHandle((IntPtr)master, ownsHandle: true),
                             FileAccess.ReadWrite);

    Terminal.InputEmitted += text =>
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        pty.Write(bytes, 0, bytes.Length);
        pty.Flush();
    };

    Terminal.GridResized += (cols, rows) =>
    {
        var size = new UnixWindowSize { row = (ushort)rows, col = (ushort)cols };
        Pty.SetWinSize(master, ref size);
    };

    _ = Task.Run(() =>
    {
        var buffer = new byte[8192];
        int read;
        while ((read = pty.Read(buffer, 0, buffer.Length)) > 0)
        {
            Terminal.Feed(buffer, read);
        }
    });

    Terminal.GrabFocus();

For the argument and environment arrays, and the Unix-only caveat, read the
PTY SUPPORT section of the engine's AGENT-README:
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/AGENT-README.txt
Pty.ForkAndExec does not exist on Windows. There, a local process is a
System.Diagnostics.Process with redirected standard streams - which is a
PIPE, not a terminal: the child sees no tty, so there is no echo and no
line editing on the host side, and its output usually has bare LF line
endings, so set ConvertEol = true.

4. No host at all: a local echo (what the demo does)
----------------------------------------------------
    private readonly StringBuilder _line = new();

    Terminal.InputEmitted += data =>
    {
        foreach (var ch in data)
        {
            switch (ch)
            {
                case '\r':                                     // Enter
                    Terminal.Feed("\r\n");
                    Terminal.Feed($"\x1b[2myou typed:\x1b[0m {_line}\r\n");
                    _line.Clear();
                    Terminal.Feed("\x1b[1;32mdemo\x1b[0m$ ");
                    break;
                case '\x7f':                                   // Backspace
                    if (_line.Length > 0) { _line.Length--; Terminal.Feed("\b \b"); }
                    break;
                default:
                    if (ch >= ' ') { _line.Append(ch); Terminal.Feed(ch.ToString()); }
                    else { Terminal.Feed($"\x1b[2m^{(char)(ch + '@')}\x1b[0m"); }
                    break;
            }
        }
    };

5. A light color scheme
-----------------------
    Terminal.BackgroundColor = new SKColor(0xff, 0xff, 0xff);
    Terminal.ForegroundColor = new SKColor(0x00, 0x00, 0x00);
    Terminal.SelectionColor  = new SKColor(0x33, 0x66, 0xcc, 0x59);   // reads on white

6. Start a new session in the same control
------------------------------------------
    Terminal.Reset();          // RIS: clears screen, scrollback view, selection
    Terminal.GrabFocus();


MINIMUM VIABLE PROJECT
======================
In an existing CodeBrix.Platform application, add to the shared UI
project's csproj (alongside the framework reference it already has):

    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.TerminalView.ApacheLicenseForever" />
    </ItemGroup>

    <!-- MainPage.xaml -->
    <Page x:Class="MyApp.Views.MainPage"
          xmlns="clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:term="using:CodeBrix.Platform.UI.TerminalView">
        <Grid>
            <term:TerminalControl x:Name="Terminal" />
        </Grid>
    </Page>

    // MainPage.xaml.cs
    public MainPage()
    {
        InitializeComponent();
        Terminal.InputEmitted += text => Terminal.Feed(text);   // echo
        Loaded += (_, _) =>
        {
            Terminal.Feed("\x1b[1;36mready\x1b[0m\r\n$ ");     // after Loaded!
            Terminal.GrabFocus();
        };
    }

No head project changes are needed; the package works on all six heads.


PERFORMANCE TIPS
================
  - Feed in BATCHES. Every Feed call is one dispatcher hop plus one
    scheduled repaint; feeding a transport's whole read buffer per call
    (Feed(byte[], int) with a buffer of several KB) is far cheaper than
    feeding per line or per character.
  - Feed(byte[], int) allocates a copy of `length` bytes per call - another
    reason to prefer few large reads over many small ones.
  - Paint cost is proportional to the VISIBLE grid, not to the scrollback:
    each paint rebuilds the runs of the visible rows and lays out every run
    through the TextLayout engine (there is no glyph cache), so a grid full
    of attribute changes costs more than mostly-uniform text. Scrollback
    length affects memory (the engine keeps Scrollback x Columns cells),
    never paint time.
  - Cell metrics are measured once per font family / size and cached;
    changing either re-measures, re-fits the grid and fires GridResized.
    Do not toggle the font in response to input.
  - Each color property setter repaints the whole grid. Set a scheme once.
  - A focused terminal repaints twice a second for the cursor blink (500 ms
    timer) while it is loaded; the timer stops on Unloaded. A page with
    many idle terminals pays that many blink repaints - collapse or unload
    the ones not in use.
  - Wheel and scrollbar scrolling are a repaint each; the engine's scroll
    is a view offset, not a buffer copy.
  - Keep InputEmitted / GridResized / TitleChanged handlers short: they run
    on the UI thread. Hand the bytes to the transport and return; do the
    blocking write on the transport's own thread if it can block.


COMMON PITFALLS TO AVOID
========================
  - Give the control a bounded size (a Grid star cell, not a StackPanel or
    an Auto cell). The grid follows the control size; an unbounded control
    ends up 4 x 2.
  - Feeding before Loaded loses data: with no dispatcher queue yet, Feed
    returns without doing anything. Connect / write the banner in the
    Loaded handler.
  - An escape-driven resize request from the application (DECSLPP and the
    like) is deliberately ignored; the grid follows the control, and the
    host learns the size through GridResized.
  - ConvertEol defaults to FALSE. A bare-LF source (pipe-connected process,
    log tail) needs true, or every line steps one column to the right.
  - Set Scrollback before the control loads; afterwards it applies only at
    the next grid resize.
  - InputEmitted delivers a string; the host wants bytes. Encode as UTF-8
    before writing to a byte transport.
  - Pasted text arrives through InputEmitted with line endings already
    normalized to CR (what a terminal sends for Enter). Do not re-normalize.
  - CopyRequested is after-the-fact. To veto or transform a copy there is
    no hook; the clipboard already has the text.
  - No mouse reporting is forwarded, and there is no IME path: full-screen
    applications that need either will not get it.
  - The raw-key fallback is US-QWERTY. Layout-composed printables are
    preferred automatically where the head provides them, so this only
    matters for keys the head cannot compose.
  - A proportional TerminalFontFamily misaligns the grid; use a monospaced
    face (the bundled Roboto Mono, or another monospaced application font
    by its ms-appx:/// URI).
  - Only default-attributed text follows ForegroundColor / BackgroundColor;
    palette-colored text (e.g. a colored prompt) keeps its palette value,
    so a light scheme can leave bright-yellow text on white. That is
    terminal behaviour, not a bug.
  - Colored erase-to-end-of-line does not paint the erased span (see
    "Colors and attributes").
  - Reset() does not resubscribe or reconnect anything; it is the screen's
    RIS only.


WHAT THIS PACKAGE DOES NOT DO
=============================
  - It ships no transport: no SSH client, no PTY on Windows, no process
    launcher. It is the screen and keyboard; you bring the wire.
  - It does not forward mouse-reporting escape protocols to the host.
  - It has no IME / preedit path.
  - It does not support TrueColor (24-bit) SGR; the engine's color model is
    the 256-color palette.
  - It does not expose the palette for editing, and has no theme beyond the
    three SKColor properties.
  - It does not honour escape-driven window resizing or window
    manipulation queries (WindowCommand answers null).
  - It does not paint backgrounds applied by erase sequences beyond the
    line's last character.
  - It exposes no search API on the control (the engine has search
    services; the control does not surface them).
  - It does not expose the engine's buffer; consume the terminal through
    Feed / InputEmitted / the events, not by reaching into the engine.
  - It has no mobile (iOS / Android) or browser head, like the rest of the
    family.


WORKING EXAMPLES ON GITHUB
==========================
  - TerminalViewDemo - six heads; replays an ANSI / SGR showcase (the
    attribute set, the 16 named colors, a 256-color cube sweep, wide CJK
    and emoji, an OSC title), switches between a default and a light color
    scheme, and runs a local echo loop through InputEmitted / Feed - no
    shell or PTY required:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/TerminalViewDemo
  - The control's source (TerminalControl.cs is the whole application-
    facing surface; Rendering/ holds the helper types):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.TerminalView
  - The engine's AGENT-README (escape-sequence coverage, TerminalOptions,
    the buffer model, PTY support, key encoding):
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/AGENT-README.txt
  - The text engine this control draws with: see the TextLayout package's
    AGENT-README in this repository,
    src/AddIns/Platform.UI.TextLayout/AGENT-README.txt.


QUICK REFERENCE CARD
====================
    Package:    CodeBrix.Platform.TerminalView.ApacheLicenseForever
                (+ TextLayout + CodeBrix.Terminal + Fonts.RobotoMono, automatic)
    XAML:       xmlns:term="using:CodeBrix.Platform.UI.TerminalView"
                <term:TerminalControl x:Name="Terminal" />
    Code:       using CodeBrix.Platform.UI.TerminalView;  using SkiaSharp;

    TerminalControl : Control
      events    InputEmitted   Action<string>     VT input -> host (UI thread)
                GridResized    Action<int,int>    (cols, rows) -> host
                TitleChanged   Action<string>     OSC 0 / 2
                CopyRequested  Action<string>     after a copy; observational
      props     Columns, Rows              int (read-only; min 4 x 2)
                ForegroundColor            SKColor   white
                BackgroundColor            SKColor   black
                SelectionColor             SKColor   4d 8b d8 / alpha 66
                ConvertEol                 bool      false (true for bare LF)
                Scrollback                 int       1000; set before Loaded
                TerminalFontFamily         string    Roboto Mono URI; monospaced
                TerminalFontSize           float     14 (min 4)
      methods   Feed(string)               any thread; text
                Feed(byte[] data, int length)  any thread; copies; bytes
                Reset()                    RIS
                GrabFocus()                keyboard focus

    Three wires:   InputEmitted -> transport.Write(UTF8 bytes)
                   GridResized  -> ChangeWindowSize / Pty.SetWinSize
                   read loop    -> Feed(buffer, bytesRead)
    Keys:          Ctrl+Shift+C / V copy, paste | Shift+PgUp / PgDn scroll
    Mouse:         drag select | double-click word | right-click menu |
                   wheel 3 lines
    Rules:         bounded size | Feed after Loaded | ConvertEol per source |
                   Scrollback before load | monospaced font | no mouse
                   reporting, no IME, no TrueColor
