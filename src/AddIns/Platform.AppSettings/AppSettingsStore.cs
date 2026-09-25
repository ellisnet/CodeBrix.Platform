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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CodeBrix.Platform.AppSettings.Contracts;

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
/// <para>
/// The values, their JSON form and the change notification are handled
/// here; the database itself, its folder and every step of the file's
/// life are the platform's, reached through the storage contract
/// <see cref="IAppSettingsStoragePlatform"/>.
/// </para>
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

    // The options of the reflection-based members (Get<T>(key...), Set(key, object)). Created on first use, so an
    // application that only uses the JsonTypeInfo<T> members never builds reflection serialization state.
    static JsonSerializerOptions? serializerOptions;

    /// <summary>The message of the trimming / AOT annotations on the reflection-based members.</summary>
    internal const string ReflectionSerializationMessage =
        "Serializes the value with reflection-based System.Text.Json, which trimming and native AOT can break. "
        + "Use the overload that takes a JsonTypeInfo<T> (from a JsonSerializerContext) in a trimmed or AOT application.";

    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    static JsonSerializerOptions GetSerializerOptions() => serializerOptions ??= new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
    };

    readonly object gate = new object();
    readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
    readonly Dictionary<string, EventHandler<AppSettingChangedEventArgs>> keyHandlers =
        new Dictionary<string, EventHandler<AppSettingChangedEventArgs>>(StringComparer.Ordinal);
    readonly IAppSettingsStorage storage;

    // The platform's storage contract, resolved once for the process.
    static IAppSettingsStoragePlatform? platform;

    static IAppSettingsStoragePlatform Platform =>
        platform ??= PlatformContract.Resolve<IAppSettingsStoragePlatform>();

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
    public bool WasCreatedFresh { get; }

    /// <summary>
    /// True when the existing settings file was corrupt and the store was
    /// restored from the most recent automatic backup.
    /// </summary>
    public bool WasRestoredFromBackup { get; }

    /// <summary>
    /// True when a staged settings_incoming.sqlite file was adopted at startup,
    /// replacing the previous settings.sqlite (kept as a settings_old_ copy).
    /// </summary>
    public bool WasReplacedByImport { get; }

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
        return Platform.GetDefaultDirectory(appName);
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
        storage = Platform.Open(appName, directoryPath, values, testClock ?? (() => DateTime.Now));
        DirectoryPath = storage.DirectoryPath;
        DatabaseFilePath = storage.DatabaseFilePath;
        WasCreatedFresh = storage.WasCreatedFresh;
        WasRestoredFromBackup = storage.WasRestoredFromBackup;
        WasReplacedByImport = storage.WasReplacedByImport;

        var retention = AutoBackupRetention;
        if (retention > 0)
        {
            try
            {
                storage.CreateAutoBackupAndPrune(retention);
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
        get => Math.Clamp(Get(AutoBackupRetentionKey, DefaultAutoBackupRetention, AppSettingsJsonContext.Default.Int32), 0, MaxAutoBackupRetention);
        set => Set(AutoBackupRetentionKey, Math.Clamp(value, 0, MaxAutoBackupRetention), AppSettingsJsonContext.Default.Int32);
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
        var fullPath = storage.GetExportDestination(destinationFilePath);
        lock (gate)
            storage.ExportTo(fullPath);
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
    public void StageIncomingFile(string sourceFilePath) => storage.StageIncomingFile(sourceFilePath);

    /// <summary>Whether a value is stored for the given key.</summary>
    public bool HasValue(string key)
    {
        lock (gate)
            return values.ContainsKey(key);
    }

    /// <summary>Returns the stored value for the key, or the type's default when not set.</summary>
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public T? Get<T>(string key) => Get(key, default(T));

    /// <summary>
    /// Returns the stored value for the key, or the given default when the key
    /// is not set or its stored JSON cannot be read as the requested type.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public T Get<T>(string key, T defaultValue)
        => Read(key, defaultValue, json => JsonSerializer.Deserialize<T>(json, GetSerializerOptions()));

    /// <summary>
    /// Returns the stored value for the key, or the given default when the key is not set or its stored JSON cannot be
    /// read as the requested type; the value is read with <paramref name="jsonTypeInfo"/> (source-generated
    /// System.Text.Json metadata), so no reflection is used - the form a trimmed or native AOT application uses.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The value returned when the key is not set or cannot be read.</param>
    /// <param name="jsonTypeInfo">The JSON metadata of <typeparamref name="T"/>, e.g. MyJsonContext.Default.MyType.</param>
    public T Get<T>(string key, T defaultValue, JsonTypeInfo<T> jsonTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        return Read(key, defaultValue, json => JsonSerializer.Deserialize(json, jsonTypeInfo));
    }

    T Read<T>(string key, T defaultValue, Func<string, T?> deserialize)
    {
        string? json;
        lock (gate)
        {
            if (!values.TryGetValue(key, out json))
                return defaultValue;
        }
        try
        {
            var value = deserialize(json);
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
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public bool Set(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("A setting key is required", nameof(key));

        return Write(key, value, value == null ? null : JsonSerializer.Serialize(value, value.GetType(), GetSerializerOptions()));
    }

    /// <summary>
    /// Stores a value for the key (writing through to settings.sqlite immediately), serialized with
    /// <paramref name="jsonTypeInfo"/> (source-generated System.Text.Json metadata), so no reflection is used - the form
    /// a trimmed or native AOT application uses; a null value removes the key. Returns true when the stored value
    /// actually changed.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value, or null to remove the key.</param>
    /// <param name="jsonTypeInfo">The JSON metadata of <typeparamref name="T"/>, e.g. MyJsonContext.Default.MyType.</param>
    public bool Set<T>(string key, T? value, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("A setting key is required", nameof(key));
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        return Write(key, value, value is null ? null : JsonSerializer.Serialize(value, jsonTypeInfo));
    }

    /// <summary>Removes the key (the null-value form of Set, without serializing anything).</summary>
    internal bool Remove(string key) => Write(key, null, null);

    bool Write(string key, object? value, string? newJson)
    {
        object? oldValue;
        lock (gate)
        {
            values.TryGetValue(key, out var oldJson);
            if (newJson == null)
            {
                if (oldJson == null)
                    return false;
                oldValue = oldJson;
                values.Remove(key);
                storage.Delete(key);
            }
            else
            {
                if (newJson == oldJson)
                    return false;
                oldValue = oldJson;
                values[key] = newJson;
                storage.Write(key, newJson);
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
            storage.Dispose();
    }

    static void ValidateAppName(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("An application name is required", nameof(appName));
        if (appName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException(
                "An application name becomes a folder name, so it cannot contain characters that are invalid in one.",
                nameof(appName));
    }
}
