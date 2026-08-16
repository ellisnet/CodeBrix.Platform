using System;
using System.IO;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.AppSettings.Tests;

[Collection(AppSettingsCollection.Name)]
public class AppSettingsServiceTests : IDisposable
{
    const string AppName = "AppSettings.Test";

    readonly string root;
    readonly string directory;

    public AppSettingsServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "codebrix-appsettings-tests", Path.GetRandomFileName());
        directory = Path.Combine(root, "settings");
        // A previous suite may have left the static populated on failure.
        AppSettingsService.Shutdown();
    }

    public void Dispose()
    {
        AppSettingsService.Shutdown();
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Store_is_unavailable_before_initialize()
    {
        //Assert
        AppSettingsService.IsInitialized.Should().BeFalse();
        Action act = () => _ = AppSettingsService.Store;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Initialize_opens_the_store_in_the_given_folder()
    {
        //Act
        AppSettingsService.Initialize(AppName, directory);

        //Assert
        AppSettingsService.IsInitialized.Should().BeTrue();
        AppSettingsService.DirectoryPath.Should().Be(Path.GetFullPath(directory));
        AppSettingsService.Store.AppName.Should().Be(AppName);
        File.Exists(Path.Combine(directory, "settings.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Initialize_twice_throws()
    {
        //Arrange
        AppSettingsService.Initialize(AppName, directory);

        //Act
        Action act = () => AppSettingsService.Initialize(AppName, directory);

        //Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Shutdown_allows_initialize_again()
    {
        //Arrange
        AppSettingsService.Initialize(AppName, directory);
        AppSettingsService.Set("AppSettings.Test.Kept", "value");

        //Act
        AppSettingsService.Shutdown();
        var betweenRuns = AppSettingsService.IsInitialized;
        AppSettingsService.Initialize(AppName, directory);

        //Assert — the store closed cleanly and reopened over the same file.
        betweenRuns.Should().BeFalse();
        AppSettingsService.IsInitialized.Should().BeTrue();
        AppSettingsService.Get<string>("AppSettings.Test.Kept").Should().Be("value");
    }

    [Fact]
    public void Shutdown_without_initialize_is_a_no_op()
    {
        //Act
        Action act = () => AppSettingsService.Shutdown();

        //Assert
        act.Should().NotThrow();
        AppSettingsService.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Facade_reads_and_writes_through_the_store()
    {
        //Arrange
        AppSettingsService.Initialize(AppName, directory);

        //Act
        AppSettingsService.Set("AppSettings.Test.Facade", 12);

        //Assert
        AppSettingsService.HasValue("AppSettings.Test.Facade").Should().BeTrue();
        AppSettingsService.Get("AppSettings.Test.Facade", 0).Should().Be(12);
        AppSettingsService.Store.Get("AppSettings.Test.Facade", 0).Should().Be(12);

        //Act — and removal travels the same path.
        AppSettingsService.Set("AppSettings.Test.Facade", null);

        //Assert
        AppSettingsService.HasValue("AppSettings.Test.Facade").Should().BeFalse();
    }

    [Fact]
    public void Facade_setting_handlers_are_added_and_removed()
    {
        //Arrange
        AppSettingsService.Initialize(AppName, directory);
        var calls = 0;
        void Handler(object? sender, AppSettingChangedEventArgs args) => calls++;

        //Act
        AppSettingsService.AddSettingHandler("AppSettings.Test.Handled", Handler);
        AppSettingsService.Set("AppSettings.Test.Handled", "one");
        AppSettingsService.RemoveSettingHandler("AppSettings.Test.Handled", Handler);
        AppSettingsService.Set("AppSettings.Test.Handled", "two");

        //Assert
        calls.Should().Be(1);
    }

    [Fact]
    public void Get_default_directory_matches_the_store()
    {
        //Assert
        AppSettingsService.GetDefaultDirectory("Doom.Brix")
            .Should().Be(AppSettingsStore.GetDefaultDirectory("Doom.Brix"));
    }
}
