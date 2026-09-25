//
// IAppSettingsStoragePlatform.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.AppSettings.Contracts;

/// <summary>
/// The storage contract behind <see cref="AppSettingsStore"/>: where an application's settings are kept, and
/// the whole life of the file they are kept in. The settings API, the JSON values, the change notification and
/// the typed property handles are platform-neutral and live in the Core assembly; everything that knows the
/// database engine, the folder the database lives in, and what happens to that file at startup (adoption of a
/// staged import, silent creation, quarantine of a corrupt file and restore from the newest good backup, the
/// timestamped backup with retention pruning, export to one file, import with validation) is the platform's.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Where Android or iOS keep the database is decided by their implementation of this contract, never by the
/// Core assembly. Resolved once, lazily, by <see cref="AppSettingsStore"/> through <see cref="PlatformContract"/>.
/// The Skia implementation is <c>CodeBrix.Platform.AppSettings.Skia.AppSettingsStorageSkiaPlatform</c>
/// (assembly CodeBrix.Platform.AppSettings, over CodeBrix.Sqlite), registered by that assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IAppSettingsStoragePlatform
{
    /// <summary>
    /// Returns the folder an application's settings are kept in when the application does not name one.
    /// Nothing is created or opened.
    /// </summary>
    /// <param name="appName">The application name, already validated by the caller.</param>
    /// <returns>The full path of the default settings folder.</returns>
    string GetDefaultDirectory(string appName);

    /// <summary>
    /// Opens (or silently creates) the settings database in the given folder and runs the whole startup
    /// sequence up to, but not including, the automatic backup: adoption of a staged import, the open with its
    /// integrity check, and on failure the quarantine of the corrupt file, the restore from the newest automatic
    /// backup, and as the last resort a fresh first-run database.
    /// </summary>
    /// <param name="appName">The application name, already validated by the caller.</param>
    /// <param name="directoryPath">The folder the application asked for (not empty; may be relative).</param>
    /// <param name="values">The store's key to JSON-text map. The implementation clears it and fills it with the
    /// rows of the database that ends up open (cleared again whenever a file is quarantined).</param>
    /// <param name="clock">The local-time clock that timestamps quarantine and backup file names.</param>
    /// <returns>The open storage.</returns>
    IAppSettingsStorage Open(string appName, string directoryPath, IDictionary<string, string> values, Func<DateTime> clock);
}
