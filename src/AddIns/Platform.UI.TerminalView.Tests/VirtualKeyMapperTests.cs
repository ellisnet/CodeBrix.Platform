#nullable enable

using CodeBrix.Platform.UI.TerminalView.Input;
using CodeBrix.Terminal.Engine;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

//New with the add-in: Lily.Shell carried its own whole-keyboard encoder (now promoted into
//CodeBrix.Terminal as TerminalKeyEncoder, which has its own suite in that repo); the only
//platform-side logic left to test is the VirtualKey -> TerminalKey mapping.

public class VirtualKeyMapperTests
{
    [Fact]
    public void letters_map_to_the_letter_range()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.A).Should().Be(TerminalKey.A);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.M).Should().Be(TerminalKey.M);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Z).Should().Be(TerminalKey.Z);
    }

    [Fact]
    public void digit_row_maps_to_d_keys()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Number0).Should().Be(TerminalKey.D0);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Number9).Should().Be(TerminalKey.D9);
    }

    [Fact]
    public void numpad_maps_to_numpad_keys()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.NumberPad0).Should().Be(TerminalKey.NumPad0);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.NumberPad9).Should().Be(TerminalKey.NumPad9);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Multiply).Should().Be(TerminalKey.NumPadMultiply);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Add).Should().Be(TerminalKey.NumPadAdd);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Subtract).Should().Be(TerminalKey.NumPadSubtract);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Decimal).Should().Be(TerminalKey.NumPadDecimal);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Divide).Should().Be(TerminalKey.NumPadDivide);
    }

    [Fact]
    public void function_keys_map_to_the_f_range()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.F1).Should().Be(TerminalKey.F1);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.F12).Should().Be(TerminalKey.F12);
    }

    [Fact]
    public void named_specials_map_through()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Enter).Should().Be(TerminalKey.Enter);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Back).Should().Be(TerminalKey.Backspace);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Tab).Should().Be(TerminalKey.Tab);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Escape).Should().Be(TerminalKey.Escape);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Space).Should().Be(TerminalKey.Space);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Up).Should().Be(TerminalKey.Up);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Down).Should().Be(TerminalKey.Down);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Left).Should().Be(TerminalKey.Left);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Right).Should().Be(TerminalKey.Right);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Home).Should().Be(TerminalKey.Home);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.End).Should().Be(TerminalKey.End);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Insert).Should().Be(TerminalKey.Insert);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Delete).Should().Be(TerminalKey.Delete);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.PageUp).Should().Be(TerminalKey.PageUp);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.PageDown).Should().Be(TerminalKey.PageDown);
    }

    [Fact]
    public void us_oem_punctuation_codes_map_through()
    {
        //Assert - these arrive as raw VK codes with no VirtualKey names
        VirtualKeyMapper.ToTerminalKey((VirtualKey)186).Should().Be(TerminalKey.Semicolon);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)187).Should().Be(TerminalKey.Equal);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)188).Should().Be(TerminalKey.Comma);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)189).Should().Be(TerminalKey.Minus);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)190).Should().Be(TerminalKey.Period);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)191).Should().Be(TerminalKey.Slash);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)192).Should().Be(TerminalKey.Backquote);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)219).Should().Be(TerminalKey.LeftBracket);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)220).Should().Be(TerminalKey.Backslash);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)221).Should().Be(TerminalKey.RightBracket);
        VirtualKeyMapper.ToTerminalKey((VirtualKey)222).Should().Be(TerminalKey.Quote);
    }

    [Fact]
    public void bare_modifiers_and_unmapped_keys_yield_none()
    {
        //Assert
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Shift).Should().Be(TerminalKey.None);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Control).Should().Be(TerminalKey.None);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.Menu).Should().Be(TerminalKey.None);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.CapitalLock).Should().Be(TerminalKey.None);
        VirtualKeyMapper.ToTerminalKey(VirtualKey.F13).Should().Be(TerminalKey.None);
    }
}
