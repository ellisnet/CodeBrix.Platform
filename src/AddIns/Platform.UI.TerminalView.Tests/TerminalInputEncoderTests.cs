#nullable enable

using CodeBrix.Platform.UI.TerminalView.Engine;
using CodeBrix.Terminal.Engine;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

/// <summary>
/// WPE1-21 (FIXLIST_codebrix_android_buildout [AP7-B TerminalView] non-BMP characters): a soft keyboard's emoji reaches
/// the terminal as two key presses, one UTF-16 half each. The encoder must hand the host ONE string holding the whole
/// character, never a lone surrogate. Host-free: a bare TerminalInputEncoder.
/// </summary>
public class TerminalInputEncoderTests
{
    private const string Grinning = "\U0001F600";

    [Fact]
    public void an_emoji_typed_as_two_halves_is_emitted_once_as_the_whole_character()
    {
        //Arrange
        var encoder = new TerminalInputEncoder();

        //Act
        var first = encoder.Encode(TerminalKey.None, Grinning[0], applicationCursor: false);
        var second = encoder.Encode(TerminalKey.None, Grinning[1], applicationCursor: false);

        //Assert
        first.Should().BeNull();
        second.Should().Be(Grinning);
    }

    [Fact]
    public void a_high_half_followed_by_an_ordinary_key_is_dropped_and_the_key_is_sent()
    {
        //Arrange
        var encoder = new TerminalInputEncoder();
        encoder.Encode(TerminalKey.None, Grinning[0], applicationCursor: false);

        //Act
        var letter = encoder.Encode(TerminalKey.A, 'a', applicationCursor: false);
        var strayLow = encoder.Encode(TerminalKey.None, Grinning[1], applicationCursor: false);

        //Assert
        letter.Should().Be("a");
        strayLow.Should().BeNull();
    }

    [Fact]
    public void a_lone_low_half_sends_nothing_and_the_next_pair_still_arrives_whole()
    {
        //Arrange
        var encoder = new TerminalInputEncoder();

        //Act
        var lone = encoder.Encode(TerminalKey.None, Grinning[1], applicationCursor: false);
        encoder.Encode(TerminalKey.None, Grinning[0], applicationCursor: false);
        var pair = encoder.Encode(TerminalKey.None, Grinning[1], applicationCursor: false);

        //Assert
        lone.Should().BeNull();
        pair.Should().Be(Grinning);
    }

    [Fact]
    public void a_modifier_pressed_between_the_halves_does_not_break_the_pair()
    {
        //Arrange
        var encoder = new TerminalInputEncoder();

        //Act
        encoder.Encode(TerminalKey.None, Grinning[0], applicationCursor: false);
        var isModifier = encoder.UpdateModifier(TerminalModifierKey.Shift, isDown: true);
        var pair = encoder.Encode(TerminalKey.None, Grinning[1], applicationCursor: false);

        //Assert
        isModifier.Should().BeTrue();
        pair.Should().Be(Grinning);
    }

    [Fact]
    public void ordinary_text_is_unchanged()
    {
        //Arrange
        var encoder = new TerminalInputEncoder();

        //Act
        var letter = encoder.Encode(TerminalKey.A, 'a', applicationCursor: false);
        var accented = encoder.Encode(TerminalKey.None, 'é', applicationCursor: false);

        //Assert
        letter.Should().Be("a");
        accented.Should().Be("é");
    }
}
