using System;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The key-name table behind Keyboard.PressAsync and Locator.PressAsync.
public sealed class KeyboardTests
{
    [Theory]
    [InlineData("ControlOrMeta", VirtualKey.Control)]
    [InlineData("Control", VirtualKey.Control)]
    [InlineData("Alt", VirtualKey.Menu)]
    [InlineData("Meta", VirtualKey.LeftWindows)]
    [InlineData("Backspace", VirtualKey.Back)]
    [InlineData("ArrowLeft", VirtualKey.Left)]
    [InlineData("ArrowRight", VirtualKey.Right)]
    [InlineData("ArrowUp", VirtualKey.Up)]
    [InlineData("ArrowDown", VirtualKey.Down)]
    public void Parse_maps_browser_key_names(string key, VirtualKey expected)
        => Keyboard.Parse(key).Should().Be(expected);

    [Theory]
    [InlineData("a", VirtualKey.A)]
    [InlineData("Z", VirtualKey.Z)]
    [InlineData("0", VirtualKey.Number0)]
    [InlineData("7", VirtualKey.Number7)]
    public void Parse_maps_single_letters_and_digits(string key, VirtualKey expected)
        => Keyboard.Parse(key).Should().Be(expected);

    [Theory]
    [InlineData("Enter", VirtualKey.Enter)]
    [InlineData("tab", VirtualKey.Tab)]
    [InlineData("ESCAPE", VirtualKey.Escape)]
    [InlineData("Shift", VirtualKey.Shift)]
    [InlineData("F5", VirtualKey.F5)]
    [InlineData("Delete", VirtualKey.Delete)]
    public void Parse_accepts_virtual_key_names_in_any_case(string key, VirtualKey expected)
        => Keyboard.Parse(key).Should().Be(expected);

    [Theory]
    [InlineData("NotAKey")]
    [InlineData("Ctrl")]
    [InlineData("@")]
    public void Parse_rejects_unsupported_keys(string key)
    {
        //Act
        Action parse = () => Keyboard.Parse(key);

        //Assert
        parse.Should().Throw<ArgumentException>().WithMessage($"Unsupported key '{key}'*");
    }
}
