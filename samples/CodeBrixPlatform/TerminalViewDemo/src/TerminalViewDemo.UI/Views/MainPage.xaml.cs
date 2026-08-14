using System;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace TerminalViewDemo.Views;

public sealed partial class MainPage : Page
{
    private readonly StringBuilder _lineBuffer = new();

    public MainPage()
    {
        this.InitializeComponent();

        //The control is transport-agnostic: InputEmitted carries the VT-encoded
        //keyboard/paste input a PTY or SSH stream would receive. This demo has no
        //shell behind it, so a tiny local line discipline echoes instead.
        Terminal.InputEmitted += OnTerminalInput;
        Terminal.TitleChanged += title =>
            TitleText.Text = string.IsNullOrWhiteSpace(title) ? "TerminalView demo" : title;
        Terminal.GridResized += (cols, rows) =>
            GridText.Text = $"{cols} x {rows}";

        //Start on the control's default scheme (fires SchemeCombo_SelectionChanged)
        SchemeCombo.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            PlayShowcase();
            Terminal.GrabFocus();
        };
    }

    private void Showcase_Click(object sender, RoutedEventArgs e)
    {
        PlayShowcase();
        Terminal.GrabFocus();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        Terminal.Reset();
        Prompt();
        Terminal.GrabFocus();
    }

    private void SchemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //A color scheme on TerminalControl is just its three color properties; each
        //setter repaints, so the whole grid (scrollback included) restyles instantly.
        //Only default-attributed cells follow ForegroundColor/BackgroundColor - text
        //that names an ANSI/256 palette color keeps its palette value on either ground.
        if (SchemeCombo.SelectedIndex == 1)
        {
            //Light scheme: black-on-white, with a selection tint that reads on white
            Terminal.BackgroundColor = new SKColor(0xff, 0xff, 0xff);
            Terminal.ForegroundColor = new SKColor(0x00, 0x00, 0x00);
            Terminal.SelectionColor = new SKColor(0x33, 0x66, 0xcc, 0x59);
        }
        else
        {
            //Default scheme: the control's own defaults (white on the engine's black)
            Terminal.BackgroundColor = new SKColor(0x00, 0x00, 0x00);
            Terminal.ForegroundColor = new SKColor(0xff, 0xff, 0xff);
            Terminal.SelectionColor = new SKColor(0x4d, 0x8b, 0xd8, 0x66);
        }

        Terminal.GrabFocus();
    }

    private void OnTerminalInput(string data)
    {
        //A minimal local echo: printable input echoes, Enter starts a new line,
        //Backspace erases. Everything else (arrows, Ctrl chords) is shown as its
        //escape-notation so the encoding is visible - instructive, not a shell.
        foreach (var ch in data)
        {
            switch (ch)
            {
                case '\r':
                    var line = _lineBuffer.ToString();
                    _lineBuffer.Clear();
                    Terminal.Feed("\r\n");
                    if (line.Length > 0)
                    {
                        Terminal.Feed($"\x1b[2myou typed:\x1b[0m {line}\r\n");
                    }
                    Prompt();
                    break;

                case '\x7f':
                    if (_lineBuffer.Length > 0)
                    {
                        _lineBuffer.Length--;
                        Terminal.Feed("\b \b");
                    }
                    break;

                default:
                    if (ch >= ' ')
                    {
                        _lineBuffer.Append(ch);
                        Terminal.Feed(ch.ToString());
                    }
                    else
                    {
                        //Control bytes render as ^X in dim text (and do not join the line)
                        Terminal.Feed($"\x1b[2m^{(char)(ch + '@')}\x1b[0m");
                    }
                    break;
            }
        }
    }

    private void Prompt() => Terminal.Feed("\x1b[1;32mdemo\x1b[0m$ ");

    private void PlayShowcase()
    {
        var b = new StringBuilder();

        b.Append("\x1b[2J\x1b[H");                       //clear + home
        b.Append("\x1b[1;36mCodeBrix.Platform.TerminalView\x1b[0m");
        b.Append("  \x1b[2m(CodeBrix.Terminal engine on Skia)\x1b[0m\r\n\r\n");

        //Text attributes
        b.Append("attributes  ");
        b.Append("\x1b[1mbold\x1b[0m ");
        b.Append("\x1b[3mitalic\x1b[0m ");
        b.Append("\x1b[4munderline\x1b[0m ");
        b.Append("\x1b[7minverse\x1b[0m ");
        b.Append("\x1b[2mdim\x1b[0m ");
        b.Append("\x1b[9mcrossed\x1b[0m\r\n\r\n");

        //16 named colors, normal and bright
        b.Append("colors      ");
        for (var i = 0; i < 8; i++) { b.Append($"\x1b[3{i}m#\x1b[0m"); }
        b.Append("  ");
        for (var i = 0; i < 8; i++) { b.Append($"\x1b[9{i}m#\x1b[0m"); }
        b.Append("\r\n");

        //256-color cube sweep on the background
        b.Append("256-color   ");
        for (var i = 16; i < 52; i++) { b.Append($"\x1b[48;5;{i}m \x1b[0m"); }
        b.Append("\r\n\r\n");

        //Wide characters and combining text
        b.Append("wide chars  CJK \x1b[33m中文字\x1b[0m and emoji \U0001F600 stay grid-aligned\r\n\r\n");

        b.Append("scrollback  wheel, scrollbar, or Shift+PageUp/PageDown; typing snaps back\r\n");
        b.Append("clipboard   select with the mouse; right-click or Ctrl+Shift+C/V\r\n\r\n");

        b.Append("\x1b]0;TerminalView demo - showcase\x07");  //OSC 0: set the title

        Terminal.Feed(b.ToString());
        Prompt();
    }
}
