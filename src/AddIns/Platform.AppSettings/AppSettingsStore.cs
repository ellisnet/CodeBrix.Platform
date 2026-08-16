//
// AppSettingsStore.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (extracted for CodeBrix.Platform from the identical settings stores
//      vendored into the Doom.Brix, Wolfenstein.Brix, Pinta.Brix and
//      KenneyAssetBrowser samples; those descend from CodeBrix.Develop's
//      OptionsStore, itself inspired by MonoDevelop.Core.Properties/
//      PropertyService, rebuilt on SQLite storage)
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBrix.Sqlite;

namespace CodeBrix.Platform.AppSettings; //was previously: Doom.Brix.Settings (and CodeBrix.Develop.Core.Options before that)

/// <summary>
/// The persistent key/value store behind an application's configuration:
/// a single portable SQLite database file ("settings.sqlite") holding
/// everything the application wants to remember between runs.
/// Handles the full startup sequence — adoption of a staged
/// settings_incoming.sqlite import, silent re-creation when the file is
/// missing, quarantine and backup-restore when it is corrupt, and the
/// automatic timestamped backup plus retention pruning on every start.
/// </summary>
/// <remarks>
/// Values are stored as JSON text. This store is deliberately not a place to
/// put binary data: an application with bytes to keep should encode them
/// itself. <see cref="Set"/> does accept a <c>byte[]</c>, because
/// <see cref="JsonSerializer"/> renders one as a base64 JSON string, but the
/// column holding it is still text and the base64 cost is real.
/// </remarks>
public sealed class AppSettingsStore : IDisposable
{
    /// <summary>The name of the settings database file.</summary>
    public const string SettingsFileName = "settings.sqlite";

    /// <summary>The file-name prefix of automatic startup backups.</summary>
    public const string AutoBackupFilePrefix = "settings_auto_backup_";

    /// <summary>The file-name prefix a corrupt settings file is quarantined under.</summary>
    public const string CorruptFilePrefix = "settings_corrupt_";

    /// <summary>
    /// The name an imported settings file is staged under; when present at
    /// startup it replaces settings.sqlite before the store opens.
    /// </summary>
    public const string IncomingFileName = "settings_incoming.sqlite";

    /// <summary>
    /// The file-name prefix the previous settings.sqlite is renamed to when an
    /// imported file is adopted at startup. These copies are never pruned.
    /// </summary>
    public const string OldFilePrefix = "settings_old_";

    /// <summary>
    /// The local-time timestamp format used in backup and quarantine file
    /// names; fixed-width so an alphabetical listing is chronological.
    /// </summary>
    public const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    /// <summary>The setting key holding the auto-backup retention count.</summary>
    public const string AutoBackupRetentionKey = "CodeBrix.Platform.AppSettings.AutoBackupRetention";

    /// <summary>The default auto-backup retention count.</summary>
    public const int DefaultAutoBackupRetention = 5;

    /// <summary>The maximum selectable auto-backup retention count.</summary>
    public const int MaxAutoBackupRetention = 10;

    /// <summary>
    /// The folder, under the per-user configuration folder, that groups every
    /// CodeBrix application's settings rather than scattering them across it.
    /// </summary>
    public const string FamilyFolderName = "CodeBrix";

    static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // SQLite's companion files, kept (or moved) with the database they belong to.
    static readonly string[] sidecarSuffixes = { "-wal", "-shm", "-journal" };

    readonly object gate = new object();
    readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
    readonly Dictionary<string, EventHandler<AppSettingChangedEventArgs>> keyHandlers =
        new Dictionary<string, EventHandler<AppSettingChangedEventArgs>>(StringComparer.Ordinal);
    readonly Func<DateTime> clock;
    SqliteDatabase? database;

    /// <summary>The application name this store belongs to.</summary>
    public string AppName { get; }

    /// <summary>The folder holding settings.sqlite and its backup copies.</summary>
    public string DirectoryPath { get; }

    /// <summary>The full path of the settings.sqlite file.</summary>
    public string DatabaseFilePath { get; }

    /// <summary>
    /// True when this run started without a usable existing settings file and
    /// the store was created fresh with first-run settings.
    /// </summary>
    public bool WasCreatedFresh { get; private set; }

    /// <summary>
    /// True when the existing settings file was corrupt and the store was
    /// restored from the most recent automatic backup.
    /// </summary>
    public bool WasRestoredFromBackup { get; private set; }

    /// <summary>
    /// True when a staged settings_incoming.sqlite file was adopted at startup,
    /// replacing the previous settings.sqlite (kept as a settings_old_ copy).
    /// </summary>
    public bool WasReplacedByImport { get; private set; }

    /// <summary>Raised after any setting value changes.</summary>
    public event EventHandler<AppSettingChangedEventArgs>? SettingChanged;

    /// <summary>
    /// The default settings folder for an application: the "settings" subfolder
    /// of a per-application folder grouped under "CodeBrix" in the user's
    /// configuration folder (on Linux
    /// ~/.config/CodeBrix/{appName}/settings, and the equivalent
    /// per-user application-data location on Windows and macOS).
    /// </summary>
    public static string GetDefaultDirectory(string appName)
    {
        ValidateAppName(appName);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            FamilyFolderName, appName, "settings");
    }

    /// <summary>
    /// Opens (or silently creates) the settings store for the given application
    /// in its default folder, and runs the startup auto-backup and retention
    /// pruning.
    /// </summary>
    public AppSettingsStore(string appName) : this(appName, GetDefaultDirectory(appName), null)
    {
    }

    /// <summary>
    /// Opens (or silently creates) the settings store in the given folder and
    /// runs the startup auto-backup and retention pruning.
    /// </summary>
    public AppSettingsStore(string appName, string directoryPath) : this(appName, directoryPath, null)
    {
    }

    internal AppSettingsStore(string appName, string directoryPath, Func<DateTime>? testClock)
    {
        ValidateAppName(appName);
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentException("A directory path is required", nameof(directoryPath));

        AppName = appName;
        clock = testClock ?? (() => DateTime.Now);
        DirectoryPath = Path.GetFullPath(directoryPath);
        DatabaseFilePath = Path.Combine(DirectoryPath, SettingsFileName);
        Directory.CreateDirectory(DirectoryPath);

        AdoptIncomingFile();
        OpenWithRecovery();

        var retention = AutoBackupRetention;
        if (retention > 0)
        {
            try
            {
                CreateAutoBackup();
                PruneAutoBackups(retention);
            }
            catch (Exception ex)
            {
                // A failed backup must never prevent the application from starting.
                AppSettingLoggingService.LogError("Settings auto-backup failed", ex);
            }
        }
    }

    /// <summary>
    /// How many automatic startup backups to keep, clamped to the legal
    /// 0..<see cref="MaxAutoBackupRetention"/> range on both read and write;
    /// zero disables automatic backups entirely.
    /// </summary>
    /// <remarks>
    /// The backup-and-prune sequence runs once, during construction, so a value
    /// set here takes effect on the application's NEXT start — lowering it does
    /// not delete existing backup files straight away.
    /// </remarks>
    public int AutoBackupRetention
    {
        get => Math.Clamp(Get(AutoBackupRetentionKey, DefaultAutoBackupRetention), 0, MaxAutoBackupRetention);
        set => Set(AutoBackupRetentionKey, Math.Clamp(value, 0, MaxAutoBackupRetention));
    }

    /// <summary>
    /// Exports the live settings database to the given file as a safe,
    /// complete, self-contained copy (quiesce, WAL checkpoint, then SQLite
    /// online backup — no companion files needed). The destination must lie
    /// outside the settings folder, which holds nothing but the live store
    /// and its own backup copies.
    /// </summary>
    public void ExportToFile(string destinationFilePath)
    {
        if (string.IsNullOrEmpty(destinationFilePath))
            throw new ArgumentException("A destination file path is required", nameof(destinationFilePath));

        var fullPath = Path.GetFullPath(destinationFilePath);
        var insideSettingsFolder = string.Equals(Path.GetDirectoryName(fullPath), DirectoryPath, StringComparison.Ordinal)
            || fullPath.StartsWith(DirectoryPath + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        if (insideSettingsFolder)
            throw new InvalidOperationException(
                "Settings cannot be exported into the settings folder itself; please choose another location.");

        lock (gate)
            Database.BackupToFile(fullPath);
        AppSettingLoggingService.LogInfo($"Settings exported to {fullPath}");
    }

    /// <summary>
    /// Validates that the given file looks like a real settings database
    /// (a SQLite database that passes an integrity check and contains the
    /// Setting table) and stages it as settings_incoming.sqlite, to be adopted
    /// in place of settings.sqlite on the next start. Throws
    /// <see cref="InvalidDataException"/> when the file appears to have
    /// problems; the validation never opens the user's file in place.
    /// </summary>
    public void StageIncomingFile(string sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
            throw new ArgumentException("A source file path is required", nameof(sourceFilePath));
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("The selected file does not exist.", sourceFilePath);

        // Work on a private copy so the selected file is never opened (or
        // given WAL companion files) where the user keeps it.
        var tempDirectory = Path.Combine(Path.GetTempPath(), FamilyFolderName, AppName, Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var tempPath = Path.Combine(tempDirectory, SettingsFileName);
            File.Copy(sourceFilePath, tempPath);
            foreach (var suffix in sidecarSuffixes)
            {
                if (File.Exists(sourceFilePath + suffix))
                    File.Copy(sourceFilePath + suffix, tempPath + suffix);
            }

            using var candidate = new SqliteDatabase(tempPath, null, new SqliteDatabaseOptions());
            try
            {
                candidate.SafeOpen();
                if (!string.Equals(candidate.ExecuteScalar("PRAGMA integrity_check") as string, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The file failed the SQLite integrity check.");
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"The file could not be opened as a SQLite database: {ex.Message}", ex);
            }

            if (candidate.ExecuteScalar("SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'Setting'") == null)
                throw new InvalidDataException("The file is a SQLite database, but does not contain the Setting table a settings file holds.");
            try
            {
                // Reading every row proves the table is usable, not merely present;
                // the rows themselves are of no interest here.
                _ = candidate.Connection.Query("SELECT Key, Value FROM Setting").Count();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"The file's Setting table could not be read: {ex.Message}", ex);
            }

            // Stage a clean, checkpointed, self-contained copy — never the raw
            // source bytes, which may depend on companion files.
            candidate.BackupToFile(Path.Combine(DirectoryPath, IncomingFileName));
            AppSettingLoggingService.LogInfo($"Settings file {sourceFilePath} staged as {IncomingFileName}");
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>Whether a value is stored for the given key.</summary>
    public bool HasValue(string key)
    {
        lock (gate)
            return values.ContainsKey(key);
    }

    /// <summary>Returns the stored value for the key, or the type's default when not set.</summary>
    public T? Get<T>(string key) => Get(key, default(T));

    /// <summary>
    /// Returns the stored value for the key, or the given default when the key
    /// is not set or its stored JSON cannot be read as the requested type.
    /// </summary>
    public T Get<T>(string key, T defaultValue)
    {
        string? json;
        lock (gate)
        {
            if (!values.TryGetValue(key, out json))
                return defaultValue;
        }
        try
        {
            var value = JsonSerializer.Deserialize<T>(json, serializerOptions);
            return value is null ? defaultValue : value;
        }
        catch (Exception ex)
        {
            AppSettingLoggingService.LogWarning($"Setting '{key}' could not be read as {typeof(T).Name}: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Stores a value for the key (writing through to settings.sqlite
    /// immediately); a null value removes the key. Returns true when the
    /// stored value actually changed.
    /// </summary>
    public bool Set(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("A setting key is required", nameof(key));

        object? oldValue;
        lock (gate)
        {
            values.TryGetValue(key, out var oldJson);
            if (value == null)
            {
                if (oldJson == null)
                    return false;
                oldValue = oldJson;
                values.Remove(key);
                Database.Connection.Execute("DELETE FROM Setting WHERE Key = @key", new { key });
            }
            else
            {
                var newJson = JsonSerializer.Serialize(value, value.GetType(), serializerOptions);
                if (newJson == oldJson)
                    return false;
                oldValue = oldJson;
                values[key] = newJson;
                Database.Connection.Execute(
                    "INSERT INTO Setting (Key, Value) VALUES (@key, @newJson) " +
                    "ON CONFLICT (Key) DO UPDATE SET Value = @newJson",
                    new { key, newJson });
            }
        }

        var args = new AppSettingChangedEventArgs(key, oldValue, value);
        SettingChanged?.Invoke(this, args);
        EventHandler<AppSettingChangedEventArgs>? handler;
        lock (gate)
            keyHandlers.TryGetValue(key, out handler);
        handler?.Invoke(this, args);
        return true;
    }

    /// <summary>Registers a handler raised when the given key's value changes.</summary>
    public void AddSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)
    {
        lock (gate)
        {
            keyHandlers.TryGetValue(key, out var existing);
            keyHandlers[key] = (EventHandler<AppSettingChangedEventArgs>) Delegate.Combine(existing, handler);
        }
    }

    /// <summary>Removes a handler previously added with <see cref="AddSettingHandler"/>.</summary>
    public void RemoveSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler)
    {
        lock (gate)
        {
            if (!keyHandlers.TryGetValue(key, out var existing))
                return;
            var remaining = (EventHandler<AppSettingChangedEventArgs>?) Delegate.Remove(existing, handler);
            if (remaining == null)
                keyHandlers.Remove(key);
            else
                keyHandlers[key] = remaining;
        }
    }

    /// <summary>Closes the underlying database.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            database?.Dispose();
            database = null;
        }
    }

    SqliteDatabase Database =>
        database ?? throw new ObjectDisposedException(nameof(AppSettingsStore));

    static void ValidateAppName(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("An application name is required", nameof(appName));
        if (appName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException(
                "An application name becomes a folder name, so it cannot contain characters that are invalid in one.",
                nameof(appName));
    }

    void AdoptIncomingFile()
    {
        var incomingPath = Path.Combine(DirectoryPath, IncomingFileName);
        if (!File.Exists(incomingPath))
            return;
        try
        {
            if (File.Exists(DatabaseFilePath))
            {
                var oldPath = Path.Combine(DirectoryPath,
                    $"{OldFilePrefix}{clock().ToString(TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
                File.Move(DatabaseFilePath, oldPath, overwrite: true);
                foreach (var suffix in sidecarSuffixes)
                {
                    var sidecar = DatabaseFilePath + suffix;
                    if (File.Exists(sidecar))
                        File.Move(sidecar, oldPath + suffix, overwrite: true);
                }
                AppSettingLoggingService.LogInfo($"Previous settings kept as {Path.GetFileName(oldPath)}");
            }
            else
            {
                // Orphaned companion files must not pair up with the adopted file.
                foreach (var suffix in sidecarSuffixes)
                {
                    var sidecar = DatabaseFilePath + suffix;
                    if (File.Exists(sidecar))
                        File.Delete(sidecar);
                }
            }

            File.Move(incomingPath, DatabaseFilePath);
            WasReplacedByImport = true;
            AppSettingLoggingService.LogInfo($"Imported settings file {IncomingFileName} adopted as {SettingsFileName}");
        }
        catch (Exception ex)
        {
            // A failed adoption must never prevent the application from
            // starting; continue with whatever settings file is in place.
            AppSettingLoggingService.LogError("The imported settings file could not be adopted", ex);
        }
    }

    void OpenWithRecovery()
    {
        WasCreatedFresh = !File.Exists(DatabaseFilePath);
        try
        {
            OpenAndLoad();
            return;
        }
        catch (Exception ex)
        {
            AppSettingLoggingService.LogError($"The settings file '{DatabaseFilePath}' could not be opened; quarantining it", ex);
            QuarantineCorruptFile();
        }

        // The corrupt file has been renamed away; try the most recent
        // automatic backup, and fall back to a fresh first-run store.
        if (TryRestoreNewestAutoBackup())
        {
            try
            {
                OpenAndLoad();
                WasRestoredFromBackup = true;
                return;
            }
            catch (Exception ex)
            {
                AppSettingLoggingService.LogError("The restored settings backup could not be opened either; starting fresh", ex);
                QuarantineCorruptFile();
            }
        }

        WasCreatedFresh = true;
        OpenAndLoad();
    }

    void OpenAndLoad()
    {
        var db = new SqliteDatabase(DatabaseFilePath, null, new SqliteDatabaseOptions());
        try
        {
            db.SafeOpen();
            if (!string.Equals(db.ExecuteScalar("PRAGMA integrity_check") as string, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("PRAGMA integrity_check did not report 'ok'");
            db.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Setting (Key TEXT NOT NULL PRIMARY KEY, Value TEXT NOT NULL)");

            values.Clear();
            foreach (var row in db.Connection.Query("SELECT Key, Value FROM Setting"))
                values[(string) row.Key] = (string) row.Value;
        }
        catch
        {
            db.Dispose();
            throw;
        }
        database = db;
    }

    void QuarantineCorruptFile()
    {
        database?.Dispose();
        database = null;
        values.Clear();

        if (!File.Exists(DatabaseFilePath))
            return;
        var quarantinePath = Path.Combine(DirectoryPath,
            $"{CorruptFilePrefix}{clock().ToString(TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
        File.Move(DatabaseFilePath, quarantinePath, overwrite: true);
        foreach (var suffix in sidecarSuffixes)
        {
            var sidecar = DatabaseFilePath + suffix;
            if (File.Exists(sidecar))
                File.Move(sidecar, quarantinePath + suffix, overwrite: true);
        }
    }

    bool TryRestoreNewestAutoBackup()
    {
        var newest = EnumerateAutoBackups().OrderByDescending(backup => backup.Timestamp).FirstOrDefault();
        if (newest.Path == null)
            return false;
        try
        {
            File.Copy(newest.Path, DatabaseFilePath, overwrite: true);
            AppSettingLoggingService.LogInfo($"Settings restored from backup {Path.GetFileName(newest.Path)}");
            return true;
        }
        catch (Exception ex)
        {
            AppSettingLoggingService.LogError($"Could not restore settings backup {newest.Path}", ex);
            return false;
        }
    }

    void CreateAutoBackup()
    {
        var backupPath = Path.Combine(DirectoryPath,
            $"{AutoBackupFilePrefix}{clock().ToString(TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
        // Orchestrated clean copy: quiesce, checkpoint the WAL, then run
        // SQLite's online backup — the single resulting file is the
        // complete database.
        Database.BackupToFile(backupPath);
        AppSettingLoggingService.LogInfo($"Settings auto-backup created: {Path.GetFileName(backupPath)}");
    }

    void PruneAutoBackups(int retainCount)
    {
        // Recency comes from the timestamp encoded in the file name — never
        // from file-system created/modified metadata. Files that do not
        // match the auto-backup naming scheme exactly (including manual
        // copies a user made) are never deleted.
        var expired = EnumerateAutoBackups()
            .OrderByDescending(backup => backup.Timestamp)
            .Skip(retainCount);
        foreach (var backup in expired)
        {
            try
            {
                File.Delete(backup.Path);
                AppSettingLoggingService.LogInfo($"Settings auto-backup pruned: {Path.GetFileName(backup.Path)}");
            }
            catch (Exception ex)
            {
                AppSettingLoggingService.LogWarning($"Could not prune settings auto-backup {backup.Path}: {ex.Message}");
            }
        }
    }

    IEnumerable<(string Path, DateTime Timestamp)> EnumerateAutoBackups()
    {
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, $"{AutoBackupFilePrefix}*.sqlite"))
        {
            var name = System.IO.Path.GetFileName(path);
            var stampText = name.Substring(AutoBackupFilePrefix.Length, name.Length - AutoBackupFilePrefix.Length - ".sqlite".Length);
            if (DateTime.TryParseExact(stampText, TimestampFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var stamp))
                yield return (path, stamp);
        }
    }
}
