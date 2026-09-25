//
// SqliteAppSettingsStorage.skia.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (the file lifecycle of AppSettingsStore, moved behind the storage
//      contract by the Core/Skia split; extracted for CodeBrix.Platform from
//      the identical settings stores vendored into the Doom.Brix,
//      Wolfenstein.Brix, Pinta.Brix and KenneyAssetBrowser samples; those
//      descend from CodeBrix.Develop's OptionsStore, itself inspired by
//      MonoDevelop.Core.Properties/PropertyService, rebuilt on SQLite storage)
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CodeBrix.Platform.AppSettings.Contracts;
using CodeBrix.Sqlite;

namespace CodeBrix.Platform.AppSettings.Skia;

/// <summary>
/// One settings.sqlite file and everything that happens to it: adoption of a staged
/// settings_incoming.sqlite import, silent re-creation when the file is missing,
/// quarantine and backup-restore when it is corrupt, the automatic timestamped
/// backup plus retention pruning, export to one file and import with validation.
/// </summary>
internal sealed class SqliteAppSettingsStorage : IAppSettingsStorage
{
    // SQLite's companion files, kept (or moved) with the database they belong to.
    static readonly string[] sidecarSuffixes = { "-wal", "-shm", "-journal" };

    readonly string appName;
    readonly IDictionary<string, string> values;
    readonly Func<DateTime> clock;
    SqliteDatabase? database;

    /// <inheritdoc/>
    public string DirectoryPath { get; }

    /// <inheritdoc/>
    public string DatabaseFilePath { get; }

    /// <inheritdoc/>
    public bool WasCreatedFresh { get; private set; }

    /// <inheritdoc/>
    public bool WasRestoredFromBackup { get; private set; }

    /// <inheritdoc/>
    public bool WasReplacedByImport { get; private set; }

    /// <summary>
    /// Creates the folder when needed, adopts a staged import, and opens the
    /// database with recovery, filling <paramref name="values"/> with its rows.
    /// </summary>
    public SqliteAppSettingsStorage(string appName, string directoryPath, IDictionary<string, string> values, Func<DateTime> clock)
    {
        this.appName = appName;
        this.values = values;
        this.clock = clock;
        DirectoryPath = Path.GetFullPath(directoryPath);
        DatabaseFilePath = Path.Combine(DirectoryPath, AppSettingsStore.SettingsFileName);
        Directory.CreateDirectory(DirectoryPath);

        AdoptIncomingFile();
        OpenWithRecovery();
    }

    /// <inheritdoc/>
    public void Write(string key, string json) =>
        Database.Connection.Execute(
            "INSERT INTO Setting (Key, Value) VALUES (@key, @newJson) " +
            "ON CONFLICT (Key) DO UPDATE SET Value = @newJson",
            new { key, newJson = json });

    /// <inheritdoc/>
    public void Delete(string key) =>
        Database.Connection.Execute("DELETE FROM Setting WHERE Key = @key", new { key });

    /// <inheritdoc/>
    public void CreateAutoBackupAndPrune(int retainCount)
    {
        CreateAutoBackup();
        PruneAutoBackups(retainCount);
    }

    /// <inheritdoc/>
    public string GetExportDestination(string destinationFilePath)
    {
        if (string.IsNullOrEmpty(destinationFilePath))
            throw new ArgumentException("A destination file path is required", nameof(destinationFilePath));

        var fullPath = Path.GetFullPath(destinationFilePath);
        var insideSettingsFolder = string.Equals(Path.GetDirectoryName(fullPath), DirectoryPath, StringComparison.Ordinal)
            || fullPath.StartsWith(DirectoryPath + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        if (insideSettingsFolder)
            throw new InvalidOperationException(
                "Settings cannot be exported into the settings folder itself; please choose another location.");
        return fullPath;
    }

    /// <inheritdoc/>
    public void ExportTo(string fullPath) => Database.BackupToFile(fullPath);

    /// <inheritdoc/>
    public void StageIncomingFile(string sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
            throw new ArgumentException("A source file path is required", nameof(sourceFilePath));
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("The selected file does not exist.", sourceFilePath);

        // Work on a private copy so the selected file is never opened (or
        // given WAL companion files) where the user keeps it.
        var tempDirectory = Path.Combine(Path.GetTempPath(), AppSettingsStore.FamilyFolderName, appName, Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var tempPath = Path.Combine(tempDirectory, AppSettingsStore.SettingsFileName);
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
            candidate.BackupToFile(Path.Combine(DirectoryPath, AppSettingsStore.IncomingFileName));
            AppSettingLoggingService.LogInfo($"Settings file {sourceFilePath} staged as {AppSettingsStore.IncomingFileName}");
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>Closes the database (idempotent).</summary>
    public void Dispose()
    {
        database?.Dispose();
        database = null;
    }

    SqliteDatabase Database =>
        database ?? throw new ObjectDisposedException(nameof(AppSettingsStore));

    void AdoptIncomingFile()
    {
        var incomingPath = Path.Combine(DirectoryPath, AppSettingsStore.IncomingFileName);
        if (!File.Exists(incomingPath))
            return;
        try
        {
            if (File.Exists(DatabaseFilePath))
            {
                var oldPath = Path.Combine(DirectoryPath,
                    $"{AppSettingsStore.OldFilePrefix}{clock().ToString(AppSettingsStore.TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
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
            AppSettingLoggingService.LogInfo($"Imported settings file {AppSettingsStore.IncomingFileName} adopted as {AppSettingsStore.SettingsFileName}");
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
            $"{AppSettingsStore.CorruptFilePrefix}{clock().ToString(AppSettingsStore.TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
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
            $"{AppSettingsStore.AutoBackupFilePrefix}{clock().ToString(AppSettingsStore.TimestampFormat, CultureInfo.InvariantCulture)}.sqlite");
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
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, $"{AppSettingsStore.AutoBackupFilePrefix}*.sqlite"))
        {
            var name = System.IO.Path.GetFileName(path);
            var stampText = name.Substring(AppSettingsStore.AutoBackupFilePrefix.Length, name.Length - AppSettingsStore.AutoBackupFilePrefix.Length - ".sqlite".Length);
            if (DateTime.TryParseExact(stampText, AppSettingsStore.TimestampFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var stamp))
                yield return (path, stamp);
        }
    }
}
