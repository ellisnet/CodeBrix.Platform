using System;
using System.IO;
using CodeBrix.Platform.PlayTest.Recording;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

public sealed class PlayTestRecordingTests
{
    [Fact]
    public void ValidateFolder_never_creates_or_overwrites_user_content()
    {
        //Arrange
        var directory = NewTemporaryPath();

        //Act
        Action validate = () => PlayTestRecording.ValidateFolder(directory);

        //Assert
        validate.Should().Throw<ArgumentException>().WithMessage("*existing folder*");
        Directory.Exists(directory).Should().BeFalse();
    }

    [Fact]
    public void ValidateFolder_accepts_an_existing_empty_folder_and_leaves_it_empty()
    {
        //Arrange
        var directory = NewTemporaryPath();
        Directory.CreateDirectory(directory);
        try
        {
            //Act
            var validated = PlayTestRecording.ValidateFolder(directory);

            //Assert
            validated.Should().Be(Path.GetFullPath(directory));
            Directory.EnumerateFileSystemEntries(directory).Should().BeEmpty();
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void ValidateFolder_rejects_a_folder_holding_a_hidden_file_and_keeps_the_file()
    {
        //Arrange
        var directory = NewTemporaryPath();
        Directory.CreateDirectory(directory);
        var marker = Path.Combine(directory, ".hidden");
        File.WriteAllText(marker, "keep this");
        try
        {
            //Act
            Action validate = () => PlayTestRecording.ValidateFolder(directory);

            //Assert
            validate.Should().Throw<ArgumentException>().WithMessage("*must be empty*");
            File.ReadAllText(marker).Should().Be("keep this");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void ValidateFolder_rejects_a_folder_holding_an_empty_subfolder()
    {
        //Arrange
        var directory = NewTemporaryPath();
        Directory.CreateDirectory(Path.Combine(directory, "empty-child"));
        try
        {
            //Act
            Action validate = () => PlayTestRecording.ValidateFolder(directory);

            //Assert
            validate.Should().Throw<ArgumentException>().WithMessage("*must be empty*");
            Directory.Exists(Path.Combine(directory, "empty-child")).Should().BeTrue();
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFolder_rejects_a_blank_path(string folder)
    {
        //Act
        Action validate = () => PlayTestRecording.ValidateFolder(folder);

        //Assert
        validate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateFolder_expands_a_leading_tilde_to_the_user_profile()
    {
        //Arrange
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var directory = NewTemporaryPath();
        Directory.CreateDirectory(directory);
        var relative = Path.GetRelativePath(home, directory);
        try
        {
            //Act
            var validated = PlayTestRecording.ValidateFolder("~/" + relative);

            //Assert
            validated.Should().Be(Path.GetFullPath(directory));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void ValidateFolder_treats_a_bare_tilde_as_the_user_profile()
    {
        //Arrange
        var home = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        //Act
        Action validate = () => PlayTestRecording.ValidateFolder("~");

        //Assert (a profile folder is never empty, so the claim is refused - and names that folder)
        validate.Should().Throw<ArgumentException>().WithMessage($"*{home}*");
    }

    [Theory]
    [InlineData("ClickTests", "ClickTests")]
    [InlineData("a<b>c", "a%003Cb%003Ec")]
    [InlineData("x:y|z?*", "x%003Ay%007Cz%003F%002A")]
    [InlineData("50%", "50%0025")]
    [InlineData("tab\there", "tab%0009here")]
    [InlineData("", "_")]
    [InlineData(".", "._")]
    [InlineData("..", ".._")]
    [InlineData("name.", "name._")]
    [InlineData("name ", "name _")]
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("LPT9", "_LPT9")]
    [InlineData("CONSOLE", "CONSOLE")]
    public void SafeName_escapes_names_that_are_unsafe_on_any_operating_system(string value, string expected)
        => PlayTestRecording.SafeName(value).Should().Be(expected);

    [Fact]
    public void SafeName_shortens_long_names_with_a_stable_hash_suffix()
    {
        //Arrange
        var value = new string('a', 150);

        //Act
        var safe = PlayTestRecording.SafeName(value);

        //Assert
        safe.Should().Be(new string('a', 80) + "-" + PlayTestRecording.Hash(value));
        safe.Length.Should().Be(93);
        PlayTestRecording.SafeName(new string('a', 100)).Should().Be(new string('a', 100));
    }

    [Fact]
    public void Hash_is_the_first_twelve_lowercase_hex_digits_of_sha256()
        => PlayTestRecording.Hash("abc").Should().Be("ba7816bf8f01");

    [Fact]
    public void Hash_distinguishes_different_identities()
        => PlayTestRecording.Hash("Tests.A.Run").Should().NotBe(PlayTestRecording.Hash("Tests.B.Run"));

    private static string NewTemporaryPath() => Path.Combine(Path.GetTempPath(), "PlayTestFolder_" + Guid.NewGuid().ToString("N"));
}
