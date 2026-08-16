using System;
using System.IO;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.AppSettings.Tests;

[Collection(AppSettingsCollection.Name)]
public class AppSettingPropertyTests : IDisposable
{
    const string AppName = "AppSettings.Test";

    readonly string root;

    public AppSettingPropertyTests()
    {
        root = Path.Combine(Path.GetTempPath(), "codebrix-appsettings-tests", Path.GetRandomFileName());
        AppSettingsService.Shutdown();
        AppSettingsService.Initialize(AppName, Path.Combine(root, "settings"));
    }

    public void Dispose()
    {
        AppSettingsService.Shutdown();
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void An_unset_property_reports_its_default()
    {
        //Act
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Unset", 17);

        //Assert
        property.Value.Should().Be(17);
        AppSettingsService.HasValue("AppSettings.Test.Prop.Unset").Should().BeFalse();
    }

    [Fact]
    public void A_property_reads_the_already_stored_value()
    {
        //Arrange
        AppSettingsService.Set("AppSettings.Test.Prop.Existing", "stored");

        //Act
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Existing", "fallback");

        //Assert
        property.Value.Should().Be("stored");
    }

    [Fact]
    public void Setting_the_value_writes_through_to_the_store()
    {
        //Arrange
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Written", 1);

        //Act
        property.Value = 42;

        //Assert
        property.Value.Should().Be(42);
        AppSettingsService.Get("AppSettings.Test.Prop.Written", 0).Should().Be(42);
    }

    [Fact]
    public void Set_returns_true_only_when_the_value_changes()
    {
        //Arrange
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Changed", "a");

        //Assert
        property.Set("a").Should().BeFalse();
        property.Set("b").Should().BeTrue();
        property.Set("b").Should().BeFalse();
    }

    [Fact]
    public void Changing_the_value_raises_Changed_once_per_real_change()
    {
        //Arrange
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Event", 0);
        var calls = 0;
        property.Changed += (_, _) => calls++;

        //Act
        property.Value = 5;
        property.Value = 5;
        property.Value = 6;

        //Assert
        calls.Should().Be(2);
    }

    [Fact]
    public void A_property_converts_implicitly_to_its_value()
    {
        //Arrange
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Implicit", 9);

        //Act
        int value = property;

        //Assert
        value.Should().Be(9);
    }

    [Fact]
    public void An_enum_property_round_trips()
    {
        //Arrange
        var property = AppSettingProperty.Create("AppSettings.Test.Prop.Enum", DayOfWeek.Monday);

        //Act
        property.Value = DayOfWeek.Thursday;
        var reread = AppSettingProperty.Create("AppSettings.Test.Prop.Enum", DayOfWeek.Monday);

        //Assert
        reread.Value.Should().Be(DayOfWeek.Thursday);
    }

    [Fact]
    public void A_renamed_setting_is_migrated_from_its_old_key()
    {
        //Arrange — a value stored under the previous key only.
        AppSettingsService.Set("AppSettings.Test.Prop.OldName", "carried over");

        //Act
        var property = AppSettingProperty.Create(
            "AppSettings.Test.Prop.NewName", "fallback", "AppSettings.Test.Prop.OldName");

        //Assert — the value moved across and the old key was cleared.
        property.Value.Should().Be("carried over");
        AppSettingsService.Get<string>("AppSettings.Test.Prop.NewName").Should().Be("carried over");
        AppSettingsService.HasValue("AppSettings.Test.Prop.OldName").Should().BeFalse();
    }

    [Fact]
    public void Migration_never_overwrites_a_value_already_under_the_new_key()
    {
        //Arrange — both keys set; the new one wins.
        AppSettingsService.Set("AppSettings.Test.Prop.Old2", "stale");
        AppSettingsService.Set("AppSettings.Test.Prop.New2", "current");

        //Act
        var property = AppSettingProperty.Create(
            "AppSettings.Test.Prop.New2", "fallback", "AppSettings.Test.Prop.Old2");

        //Assert
        property.Value.Should().Be("current");
        AppSettingsService.HasValue("AppSettings.Test.Prop.Old2").Should().BeFalse();
    }

    [Fact]
    public void A_null_key_is_rejected()
    {
        //Act
        Action act = () => AppSettingProperty.Create(null!, 0);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Wrap_creates_the_same_kind_of_handle()
    {
        //Act
        var property = AppSettingsService.Wrap("AppSettings.Test.Prop.Wrapped", "start");
        property.Value = "changed";

        //Assert
        AppSettingsService.Get<string>("AppSettings.Test.Prop.Wrapped").Should().Be("changed");
    }
}
