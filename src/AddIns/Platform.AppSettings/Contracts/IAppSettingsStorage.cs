//
// IAppSettingsStorage.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
// SPDX-License-Identifier: Apache-2.0
//

using System;

namespace CodeBrix.Platform.AppSettings.Contracts;

/// <summary>
/// One open settings database, as returned by <see cref="IAppSettingsStoragePlatform.Open"/>: its location, how
/// the startup sequence went, the write-through of single values, and the file operations the store exposes.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// <see cref="AppSettingsStore"/> keeps one instance for its whole life and serializes every call that touches
/// the database under its own lock. After <see cref="IDisposable.Dispose"/> (idempotent) the members that need
/// the database throw <see cref="ObjectDisposedException"/> naming <c>AppSettingsStore</c>; the location, the
/// startup flags, <see cref="GetExportDestination"/> and <see cref="StageIncomingFile"/> keep working.
/// </para>
/// </remarks>
internal interface IAppSettingsStorage : IDisposable
{
    /// <summary>The full path of the folder holding the database and its backup copies.</summary>
    string DirectoryPath { get; }

    /// <summary>The full path of the database file.</summary>
    string DatabaseFilePath { get; }

    /// <summary>True when the startup found no usable settings file and created a fresh one.</summary>
    bool WasCreatedFresh { get; }

    /// <summary>True when the settings file was corrupt and was restored from the newest automatic backup.</summary>
    bool WasRestoredFromBackup { get; }

    /// <summary>True when a staged import replaced the previous settings file at startup.</summary>
    bool WasReplacedByImport { get; }

    /// <summary>Writes one value (inserting or replacing the key's row).</summary>
    /// <param name="key">The setting key.</param>
    /// <param name="json">The value as JSON text.</param>
    void Write(string key, string json);

    /// <summary>Removes one key's row.</summary>
    /// <param name="key">The setting key.</param>
    void Delete(string key);

    /// <summary>
    /// Creates the timestamped automatic backup of the open database, then prunes the automatic backups down to
    /// the newest <paramref name="retainCount"/>. Throws when the backup fails (the caller logs it and carries on).
    /// </summary>
    /// <param name="retainCount">How many automatic backups to keep (at least 1).</param>
    void CreateAutoBackupAndPrune(int retainCount);

    /// <summary>
    /// Validates an export destination and returns its full path. Throws <see cref="ArgumentException"/> for an
    /// empty path and <see cref="InvalidOperationException"/> for a destination inside the settings folder.
    /// </summary>
    /// <param name="destinationFilePath">The destination the application asked for.</param>
    /// <returns>The full path to export to.</returns>
    string GetExportDestination(string destinationFilePath);

    /// <summary>
    /// Writes a safe, complete, self-contained copy of the open database to a destination already validated by
    /// <see cref="GetExportDestination"/>.
    /// </summary>
    /// <param name="fullPath">The full destination path.</param>
    void ExportTo(string fullPath);

    /// <summary>
    /// Validates the given file as a settings database without opening it in place, and stages a clean copy of it
    /// to replace the settings file on the next start. Throws <see cref="System.IO.InvalidDataException"/> when the
    /// file appears to have problems.
    /// </summary>
    /// <param name="sourceFilePath">The file the application asked to import.</param>
    void StageIncomingFile(string sourceFilePath);
}
