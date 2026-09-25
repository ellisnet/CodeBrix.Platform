//
// AppSettingsStorageSkiaPlatform.skia.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Collections.Generic;
using System.IO;
using CodeBrix.Platform.AppSettings.Contracts;

namespace CodeBrix.Platform.AppSettings.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IAppSettingsStoragePlatform"/>: the settings live in the
/// per-user configuration folder, in a single SQLite database file handled by <see cref="SqliteAppSettingsStorage"/>.
/// </summary>
internal sealed class AppSettingsStorageSkiaPlatform : IAppSettingsStoragePlatform
{
    /// <summary>
    /// The "settings" subfolder of a per-application folder grouped under "CodeBrix" in the user's configuration
    /// folder (on Linux ~/.config/CodeBrix/{appName}/settings, and the equivalent per-user application-data
    /// location on Windows and macOS).
    /// </summary>
    public string GetDefaultDirectory(string appName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppSettingsStore.FamilyFolderName, appName, "settings");

    /// <inheritdoc/>
    public IAppSettingsStorage Open(string appName, string directoryPath, IDictionary<string, string> values, Func<DateTime> clock) =>
        new SqliteAppSettingsStorage(appName, directoryPath, values, clock);
}
