#nullable enable

using CodeBrix.Terminal.Engine;
using Windows.System;

namespace CodeBrix.Platform.UI.TerminalView.Input;

//was previously: Lily.Shell.TerminalView.Input.KeyboardEncoder carried the whole
//VirtualKey-to-VT mapping in-app. CodeBrix.Terminal 1.0.223+ ships the encoding itself
//(TerminalKeyEncoder, prompted by that first consumer); what remains platform-side is only
//this mapping from WinUI VirtualKey onto the engine's neutral TerminalKey enum.

/// <summary>
/// Maps WinUI <see cref="VirtualKey"/> values onto the engine's neutral
/// <see cref="TerminalKey"/> for <see cref="TerminalKeyEncoder"/>.
/// </summary>
internal static class VirtualKeyMapper
{
    /// <summary>
    /// Maps a key, returning <see cref="TerminalKey.None"/> for keys the
    /// encoder has no mapping for (bare modifiers, unmapped function keys).
    /// </summary>
    public static TerminalKey ToTerminalKey(VirtualKey key)
    {
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return TerminalKey.A + (key - VirtualKey.A);
        }

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return TerminalKey.D0 + (key - VirtualKey.Number0);
        }

        if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
        {
            return TerminalKey.NumPad0 + (key - VirtualKey.NumberPad0);
        }

        if (key >= VirtualKey.F1 && key <= VirtualKey.F12)
        {
            return TerminalKey.F1 + (key - VirtualKey.F1);
        }

        switch (key)
        {
            case VirtualKey.Enter: return TerminalKey.Enter;
            case VirtualKey.Back: return TerminalKey.Backspace;
            case VirtualKey.Tab: return TerminalKey.Tab;
            case VirtualKey.Escape: return TerminalKey.Escape;
            case VirtualKey.Space: return TerminalKey.Space;
            case VirtualKey.Up: return TerminalKey.Up;
            case VirtualKey.Down: return TerminalKey.Down;
            case VirtualKey.Left: return TerminalKey.Left;
            case VirtualKey.Right: return TerminalKey.Right;
            case VirtualKey.Home: return TerminalKey.Home;
            case VirtualKey.End: return TerminalKey.End;
            case VirtualKey.Insert: return TerminalKey.Insert;
            case VirtualKey.Delete: return TerminalKey.Delete;
            case VirtualKey.PageUp: return TerminalKey.PageUp;
            case VirtualKey.PageDown: return TerminalKey.PageDown;
            case VirtualKey.Multiply: return TerminalKey.NumPadMultiply;
            case VirtualKey.Add: return TerminalKey.NumPadAdd;
            case VirtualKey.Subtract: return TerminalKey.NumPadSubtract;
            case VirtualKey.Decimal: return TerminalKey.NumPadDecimal;
            case VirtualKey.Divide: return TerminalKey.NumPadDivide;
        }

        //US OEM punctuation keys arrive as raw VK codes with no VirtualKey names
        return (int)key switch
        {
            186 => TerminalKey.Semicolon,
            187 => TerminalKey.Equal,
            188 => TerminalKey.Comma,
            189 => TerminalKey.Minus,
            190 => TerminalKey.Period,
            191 => TerminalKey.Slash,
            192 => TerminalKey.Backquote,
            219 => TerminalKey.LeftBracket,
            220 => TerminalKey.Backslash,
            221 => TerminalKey.RightBracket,
            222 => TerminalKey.Quote,
            _ => TerminalKey.None,
        };
    }
}
