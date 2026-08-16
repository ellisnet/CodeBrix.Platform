using System;
using System.IO;
using System.Linq;
using CodeBrix.Sqlite;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.AppSettings.Tests;

[Collection(AppSettingsCollection.Name)]
public class AppSettingsStoreTests : IDisposable
{
    const string AppName = "AppSettings.Test";

    readonly string root;
    readonly string directory;
    readonly string externalDirectory;

    public AppSettingsStoreTests()
    {
        root = Path.Combine(Path.GetTempPath(), "codebrix-appsettings-tests", Path.GetRandomFileName());
        directory = Path.Combine(root, "settings");
        externalDirectory = Path.Combine(root, "external");
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    AppSettingsStore CreateStore(DateTime? now = null) =>
        new AppSettingsStore(AppName, directory, now == null ? null : () => now.Value);

    string ExternalPath(string name)
    {
        Directory.CreateDirectory(externalDirectory);
        return Path.Combine(externalDirectory, name);
    }

    // Builds a valid settings.sqlite (in its own folder outside the store
    // under test) holding the given value, and returns its path.
    string CreateExternalSettingsFile(string key, string value)
    {
        var sourceDirectory = Path.Combine(externalDirectory, Path.GetRandomFileName());
        using (var source = new AppSettingsStore(AppName, sourceDirectory, () => new DateTime(2026, 1, 1, 0, 0, 0)))
            source.Set(key, value);
        return Path.Combine(sourceDirectory, "settings.sqlite");
    }

    static string BackupName(string timestamp) => $"settings_auto_backup_{timestamp}.sqlite";

    string[] AutoBackupFiles() =>
        Directory.EnumerateFiles(directory, "settings_auto_backup_*.sqlite")
            .Select(Path.GetFileName).OrderBy(name => name).ToArray()!;

    [Fact]
    public void Missing_file_is_silently_created_fresh()
    {
        //Act
        using var store = CreateStore();

        //Assert
        store.WasCreatedFresh.Should().BeTrue();
        File.Exists(Path.Combine(directory, "settings.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Get_returns_default_when_not_set()
    {
        //Arrange
        using var store = CreateStore();

        //Assert
        store.Get("AppSettings.Test.Missing", 42).Should().Be(42);
        store.Get<string>("AppSettings.Test.Missing").Should().BeNull();
        store.HasValue("AppSettings.Test.Missing").Should().BeFalse();
    }

    [Fact]
    public void Set_and_Get_round_trip_common_types()
    {
        //Arrange
        using var store = CreateStore();

        //Act
        store.Set("AppSettings.Test.String", "hello");
        store.Set("AppSettings.Test.Int", 7);
        store.Set("AppSettings.Test.Bool", true);
        store.Set("AppSettings.Test.Enum", DayOfWeek.Friday);

        //Assert
        store.Get<string>("AppSettings.Test.String").Should().Be("hello");
        store.Get<int>("AppSettings.Test.Int").Should().Be(7);
        store.Get<bool>("AppSettings.Test.Bool").Should().BeTrue();
        store.Get<DayOfWeek>("AppSettings.Test.Enum").Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public void Values_persist_across_reopen()
    {
        //Arrange
        using (var store = CreateStore())
            store.Set("AppSettings.Test.Persisted", "survives");

        //Act
        using var reopened = CreateStore();

        //Assert
        reopened.WasCreatedFresh.Should().BeFalse();
        reopened.Get<string>("AppSettings.Test.Persisted").Should().Be("survives");
    }

    [Fact]
    public void Set_null_removes_the_key()
    {
        //Arrange
        using var store = CreateStore();
        store.Set("AppSettings.Test.Removed", "value");

        //Act
        store.Set("AppSettings.Test.Removed", null);

        //Assert
        store.HasValue("AppSettings.Test.Removed").Should().BeFalse();
    }

    [Fact]
    public void Set_returns_true_only_when_the_value_changes()
    {
        //Arrange
        using var store = CreateStore();

        //Assert
        store.Set("AppSettings.Test.Changed", "a").Should().BeTrue();
        store.Set("AppSettings.Test.Changed", "a").Should().BeFalse();
        store.Set("AppSettings.Test.Changed", "b").Should().BeTrue();
    }

    [Fact]
    public void Set_raises_change_events()
    {
        //Arrange
        using var store = CreateStore();
        AppSettingChangedEventArgs? broadcast = null;
        AppSettingChangedEventArgs? keyed = null;
        store.SettingChanged += (_, args) => broadcast = args;
        store.AddSettingHandler("AppSettings.Test.Watched", (_, args) => keyed = args);

        //Act
        store.Set("AppSettings.Test.Watched", "new");

        //Assert
        broadcast.Should().NotBeNull();
        broadcast!.Key.Should().Be("AppSettings.Test.Watched");
        keyed.Should().NotBeNull();
        keyed!.NewValue.Should().Be("new");
    }

    [Fact]
    public void Removed_setting_handler_stops_receiving_changes()
    {
        //Arrange
        using var store = CreateStore();
        var calls = 0;
        void Handler(object? sender, AppSettingChangedEventArgs args) => calls++;
        store.AddSettingHandler("AppSettings.Test.Unwatched", Handler);

        //Act
        store.Set("AppSettings.Test.Unwatched", "one");
        store.RemoveSettingHandler("AppSettings.Test.Unwatched", Handler);
        store.Set("AppSettings.Test.Unwatched", "two");

        //Assert
        calls.Should().Be(1);
    }

    [Fact]
    public void Startup_creates_an_autobackup_with_the_timestamp_naming_scheme()
    {
        //Act
        using var store = CreateStore(new DateTime(2026, 7, 6, 14, 32, 5));

        //Assert
        AutoBackupFiles().Should().Equal(new[] { BackupName("2026-07-06_14-32-05") });
    }

    [Fact]
    public void Autobackup_is_a_complete_usable_database()
    {
        //Arrange
        using (var store = CreateStore(new DateTime(2026, 7, 6, 8, 0, 0)))
            store.Set("AppSettings.Test.InBackup", "captured");

        // Second start backs up the file that now contains the value.
        using (CreateStore(new DateTime(2026, 7, 6, 9, 0, 0))) { }

        //Act — pretend the main file was lost and only the newest backup remains.
        File.Delete(Path.Combine(directory, "settings.sqlite"));
        File.Copy(Path.Combine(directory, BackupName("2026-07-06_09-00-00")),
            Path.Combine(directory, "settings.sqlite"));
        using var restored = CreateStore(new DateTime(2026, 7, 6, 10, 0, 0));

        //Assert
        restored.Get<string>("AppSettings.Test.InBackup").Should().Be("captured");
    }

    [Fact]
    public void Prune_keeps_only_the_newest_n_by_filename_timestamp()
    {
        //Arrange — retention 3, with five stale backups whose file times are
        // deliberately misleading (all identical), so only the name matters.
        using (var store = CreateStore(new DateTime(2026, 7, 1, 0, 0, 0)))
            store.AutoBackupRetention = 3;
        foreach (var stamp in new[] { "2026-07-02_00-00-00", "2026-07-03_00-00-00", "2026-07-04_00-00-00" })
            File.Copy(Path.Combine(directory, BackupName("2026-07-01_00-00-00")),
                Path.Combine(directory, BackupName(stamp)));

        //Act
        using (CreateStore(new DateTime(2026, 7, 5, 0, 0, 0))) { }

        //Assert — newest three remain: the fresh backup counts toward n.
        AutoBackupFiles().Should().Equal(new[]
        {
            BackupName("2026-07-03_00-00-00"),
            BackupName("2026-07-04_00-00-00"),
            BackupName("2026-07-05_00-00-00"),
        });
    }

    [Fact]
    public void Retention_zero_creates_no_backup_and_prunes_nothing()
    {
        //Arrange
        using (var store = CreateStore(new DateTime(2026, 7, 1, 0, 0, 0)))
            store.AutoBackupRetention = 0;
        var before = AutoBackupFiles();

        //Act
        using (CreateStore(new DateTime(2026, 7, 2, 0, 0, 0))) { }

        //Assert
        AutoBackupFiles().Should().Equal(before);
    }

    [Fact]
    public void Files_not_matching_the_autobackup_scheme_are_never_deleted()
    {
        //Arrange
        using (var store = CreateStore(new DateTime(2026, 7, 1, 0, 0, 0)))
            store.AutoBackupRetention = 1;
        var manualCopy = Path.Combine(directory, "settings_bak_bob_before_changes.sqlite");
        File.Copy(Path.Combine(directory, "settings.sqlite"), manualCopy);
        // Matches the prefix and extension but has no parseable timestamp.
        var oddName = Path.Combine(directory, "settings_auto_backup_not-a-timestamp.sqlite");
        File.WriteAllText(oddName, "not a database");

        //Act
        using (CreateStore(new DateTime(2026, 7, 2, 0, 0, 0))) { }

        //Assert — the manual copy and the unparseable name survive; the real
        // 2026-07-01 backup was pruned by retention 1.
        File.Exists(manualCopy).Should().BeTrue();
        File.Exists(oddName).Should().BeTrue();
        AutoBackupFiles().Should().Equal(new[]
        {
            BackupName("2026-07-02_00-00-00"),
            "settings_auto_backup_not-a-timestamp.sqlite",
        });
    }

    [Fact]
    public void Corrupt_file_is_quarantined_and_restored_from_newest_backup()
    {
        //Arrange — a healthy run that leaves one backup containing the value.
        using (var store = CreateStore(new DateTime(2026, 7, 1, 0, 0, 0)))
            store.Set("AppSettings.Test.Value", "from-backup");
        using (CreateStore(new DateTime(2026, 7, 2, 0, 0, 0))) { }
        File.WriteAllText(Path.Combine(directory, "settings.sqlite"), "this is not a sqlite database");

        //Act
        using var store2 = CreateStore(new DateTime(2026, 7, 3, 0, 0, 0));

        //Assert
        store2.WasRestoredFromBackup.Should().BeTrue();
        store2.WasCreatedFresh.Should().BeFalse();
        store2.Get<string>("AppSettings.Test.Value").Should().Be("from-backup");
        File.Exists(Path.Combine(directory, "settings_corrupt_2026-07-03_00-00-00.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Corrupt_file_without_backups_starts_fresh()
    {
        //Arrange
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "settings.sqlite"), "garbage");

        //Act
        using var store = CreateStore(new DateTime(2026, 7, 3, 0, 0, 0));

        //Assert
        store.WasCreatedFresh.Should().BeTrue();
        store.WasRestoredFromBackup.Should().BeFalse();
        File.Exists(Path.Combine(directory, "settings_corrupt_2026-07-03_00-00-00.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Retention_read_is_clamped_to_the_legal_range()
    {
        //Arrange
        using var store = CreateStore();
        store.Set(AppSettingsStore.AutoBackupRetentionKey, 99);

        //Assert
        store.AutoBackupRetention.Should().Be(AppSettingsStore.MaxAutoBackupRetention);
    }

    [Fact]
    public void Retention_write_is_clamped_to_the_legal_range()
    {
        //Arrange
        using var store = CreateStore();

        //Act
        store.AutoBackupRetention = 99;
        var high = store.AutoBackupRetention;
        store.AutoBackupRetention = -4;
        var low = store.AutoBackupRetention;

        //Assert
        high.Should().Be(AppSettingsStore.MaxAutoBackupRetention);
        low.Should().Be(0);
    }

    [Fact]
    public void Retention_change_does_not_prune_until_the_next_start()
    {
        //Arrange — three backups on disk under the default retention.
        using (CreateStore(new DateTime(2026, 7, 1, 0, 0, 0))) { }
        using (CreateStore(new DateTime(2026, 7, 2, 0, 0, 0))) { }
        using var store = CreateStore(new DateTime(2026, 7, 3, 0, 0, 0));
        var before = AutoBackupFiles();

        //Act
        store.AutoBackupRetention = 1;

        //Assert — nothing is deleted while the store is open.
        before.Length.Should().Be(3);
        AutoBackupFiles().Should().Equal(before);
    }

    [Fact]
    public void Mismatched_type_read_returns_the_default()
    {
        //Arrange
        using var store = CreateStore();
        store.Set("AppSettings.Test.Typed", "not a number");

        //Assert
        store.Get("AppSettings.Test.Typed", 5).Should().Be(5);
    }

    [Fact]
    public void Byte_array_round_trips_as_a_base64_json_string()
    {
        //Arrange
        var payload = new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF, 0xAB };
        using (var store = CreateStore())
        {
            //Act
            store.Set("AppSettings.Test.Bytes", payload);
            store.Set("AppSettings.Test.EmptyBytes", Array.Empty<byte>());

            //Assert — round-trips through the live store.
            store.Get<byte[]>("AppSettings.Test.Bytes").Should().Equal(payload);
            store.Get<byte[]>("AppSettings.Test.EmptyBytes").Should().BeEmpty();
        }

        //Assert — and the stored form really is base64 text, which pins the
        // encoding against a future change to the serializer options.
        using var db = new SqliteDatabase(Path.Combine(directory, "settings.sqlite"), null, new SqliteDatabaseOptions());
        db.SafeOpen();
        var stored = db.ExecuteScalar("SELECT Value FROM Setting WHERE Key = 'AppSettings.Test.Bytes'") as string;
        stored.Should().Be($"\"{Convert.ToBase64String(payload)}\"");
    }

    [Fact]
    public void Byte_array_persists_across_reopen()
    {
        //Arrange — a payload well past the size of a JSON scalar.
        var payload = new byte[64 * 1024];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte) (i % 251);
        using (var store = CreateStore())
            store.Set("AppSettings.Test.BigBytes", payload);

        //Act
        using var reopened = CreateStore();

        //Assert
        reopened.Get<byte[]>("AppSettings.Test.BigBytes").Should().Equal(payload);
    }

    [Fact]
    public void Export_writes_a_complete_self_contained_copy()
    {
        //Arrange
        using var store = CreateStore();
        store.Set("AppSettings.Test.Exported", "travels");
        var exportPath = ExternalPath("my-settings.sqlite");

        //Act
        store.ExportToFile(exportPath);

        //Assert — the single exported file, used as settings.sqlite of a
        // brand-new installation, carries the value with no companion files.
        File.Exists(exportPath).Should().BeTrue();
        File.Exists(exportPath + "-wal").Should().BeFalse();
        File.Exists(exportPath + "-shm").Should().BeFalse();
        var otherInstallation = Path.Combine(root, "other-installation");
        Directory.CreateDirectory(otherInstallation);
        File.Copy(exportPath, Path.Combine(otherInstallation, "settings.sqlite"));
        using var reopened = new AppSettingsStore(AppName, otherInstallation);
        reopened.Get<string>("AppSettings.Test.Exported").Should().Be("travels");
    }

    [Fact]
    public void Export_into_the_settings_folder_is_rejected()
    {
        //Arrange
        using var store = CreateStore();

        //Act
        Action direct = () => store.ExportToFile(Path.Combine(directory, "copy.sqlite"));
        Action nested = () => store.ExportToFile(Path.Combine(directory, "sub", "copy.sqlite"));

        //Assert
        direct.Should().Throw<InvalidOperationException>();
        nested.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Import_stages_a_validated_incoming_file()
    {
        //Arrange
        using var store = CreateStore();
        var sourcePath = CreateExternalSettingsFile("AppSettings.Test.Imported", "incoming");

        //Act
        store.StageIncomingFile(sourcePath);

        //Assert
        File.Exists(Path.Combine(directory, "settings_incoming.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Import_rejects_a_non_database_file()
    {
        //Arrange
        using var store = CreateStore();
        var sourcePath = ExternalPath("not-a-database.sqlite");
        File.WriteAllText(sourcePath, "this is not a sqlite database");

        //Act
        Action act = () => store.StageIncomingFile(sourcePath);

        //Assert
        act.Should().Throw<InvalidDataException>();
        File.Exists(Path.Combine(directory, "settings_incoming.sqlite")).Should().BeFalse();
    }

    [Fact]
    public void Import_rejects_a_database_without_the_setting_table()
    {
        //Arrange — a healthy SQLite database that is not a settings file.
        using var store = CreateStore();
        var sourcePath = ExternalPath("other-database.sqlite");
        using (var other = new SqliteDatabase(sourcePath, null, new SqliteDatabaseOptions()))
        {
            other.SafeOpen();
            other.ExecuteNonQuery("CREATE TABLE NotSettings (Id INTEGER PRIMARY KEY)");
        }

        //Act
        Action act = () => store.StageIncomingFile(sourcePath);

        //Assert
        act.Should().Throw<InvalidDataException>();
        File.Exists(Path.Combine(directory, "settings_incoming.sqlite")).Should().BeFalse();
    }

    [Fact]
    public void Incoming_file_is_adopted_on_startup()
    {
        //Arrange — a store holding the old value, with an import staged.
        var sourcePath = CreateExternalSettingsFile("AppSettings.Test.Key", "new");
        using (var store = CreateStore(new DateTime(2026, 7, 1, 0, 0, 0)))
        {
            store.Set("AppSettings.Test.Key", "old");
            store.StageIncomingFile(sourcePath);
        }

        //Act
        using var reopened = CreateStore(new DateTime(2026, 7, 5, 12, 30, 45));

        //Assert — the import took over; the previous file was kept.
        reopened.WasReplacedByImport.Should().BeTrue();
        reopened.WasCreatedFresh.Should().BeFalse();
        reopened.Get<string>("AppSettings.Test.Key").Should().Be("new");
        File.Exists(Path.Combine(directory, "settings_incoming.sqlite")).Should().BeFalse();
        File.Exists(Path.Combine(directory, "settings_old_2026-07-05_12-30-45.sqlite")).Should().BeTrue();
    }

    [Fact]
    public void Adoption_without_an_existing_settings_file_uses_the_incoming_file_directly()
    {
        //Arrange — a staged import in a folder with no settings.sqlite yet.
        var sourcePath = CreateExternalSettingsFile("AppSettings.Test.Key", "fresh-import");
        Directory.CreateDirectory(directory);
        using (var source = new AppSettingsStore(AppName, Path.GetDirectoryName(sourcePath)!))
            source.ExportToFile(Path.Combine(directory, "settings_incoming.sqlite"));

        //Act
        using var store = CreateStore(new DateTime(2026, 7, 5, 12, 30, 45));

        //Assert
        store.WasReplacedByImport.Should().BeTrue();
        store.Get<string>("AppSettings.Test.Key").Should().Be("fresh-import");
        Directory.EnumerateFiles(directory, "settings_old_*.sqlite").Should().BeEmpty();
    }

    [Fact]
    public void Default_directory_groups_the_application_under_the_family_folder()
    {
        //Act
        var path = AppSettingsStore.GetDefaultDirectory("Doom.Brix");

        //Assert
        path.Should().Be(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CodeBrix", "Doom.Brix", "settings"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad/name")]
    public void An_unusable_application_name_is_rejected(string appName)
    {
        //Act
        Action act = () => AppSettingsStore.GetDefaultDirectory(appName);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_null_application_name_is_rejected()
    {
        //Act
        Action act = () => AppSettingsStore.GetDefaultDirectory(null!);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Use_after_dispose_is_reported_rather_than_crashing()
    {
        //Arrange
        var store = CreateStore();
        store.Dispose();

        //Act
        Action act = () => store.Set("AppSettings.Test.AfterDispose", "value");

        //Assert
        act.Should().Throw<ObjectDisposedException>();
    }
}
