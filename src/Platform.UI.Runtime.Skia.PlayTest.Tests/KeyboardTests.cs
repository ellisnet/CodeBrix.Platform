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

    [Theory]
    [InlineData("a", 'a')]
    [InlineData("Z", 'Z')]
    [InlineData("7", '7')]
    [InlineData("Space", ' ')]
    [InlineData("Enter", null)]
    [InlineData("ArrowRight", null)]
    public void Character_is_the_typed_character_of_single_character_keys_and_space(string key, char? expected)
        => Keyboard.Character(key).Should().Be(expected);

    [Theory]
    [InlineData("ArrowRight", VirtualKey.Right)]
    [InlineData("Shift", VirtualKey.Shift)]
    [InlineData("x", VirtualKey.X)]
    public void ParseSingle_accepts_one_key(string key, VirtualKey expected)
        => Keyboard.ParseSingle(key).Should().Be(expected);

    [Theory]
    [InlineData("Shift+ArrowRight")]
    [InlineData("Control+z")]
    public void ParseSingle_rejects_chords(string key)
    {
        //Act
        Action parse = () => Keyboard.ParseSingle(key);

        //Assert
        parse.Should().Throw<ArgumentException>().WithMessage($"'{key}' is a chord*");
    }

    [Theory]
    [InlineData(null, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(150.5f, 150.5f)]
    public void ValidDelay_accepts_finite_non_negative_milliseconds(float? delay, float expected)
        => Keyboard.ValidDelay(delay).Should().Be(expected);

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void ValidDelay_rejects_negative_and_non_finite_values(float delay)
    {
        //Act
        Action validate = () => Keyboard.ValidDelay(delay);

        //Assert
        validate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Held_keys_are_released_most_recent_first_and_only_once()
    {
        //Arrange
        var held = new HeldKeys();
        held.Add(VirtualKey.Shift);
        held.Add(VirtualKey.Right);
        held.Add(VirtualKey.Shift);
        held.Add(VirtualKey.Up);
        held.Remove(VirtualKey.Up);

        //Act
        var released = held.TakeAll();

        //Assert
        released.Should().Equal(VirtualKey.Right, VirtualKey.Shift);
        held.Keys.Should().BeEmpty();
    }
}
