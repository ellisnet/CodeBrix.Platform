//
// AppSettingsService.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (adapted from MonoDevelop's PropertyService, via CodeBrix.Develop and
//      the sample settings stores, for CodeBrix.Platform: a SQLite-backed
//      AppSettingsStore instead of the MonoDevelopProperties.xml file)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace CodeBrix.Platform.AppSettings; //was previously: Doom.Brix.Settings.SettingsService (and MonoDevelop.Core.PropertyService before that)

/// <summary>
/// The static facade over the application's single
/// <see cref="AppSettingsStore"/>: every configurable value is read and
/// written through this service, so the whole configuration lives in one
/// portable settings.sqlite file.
/// </summary>
public static class AppSettingsService
{
    static AppSettingsStore? store;

    /// <summary>Whether <see cref="Initialize(string)"/> has been called.</summary>
    public static bool IsInitialized => store != null;

    /// <summary>
    /// The settings store; only available after <see cref="Initialize(string)"/>.
    /// </summary>
    public static AppSettingsStore Store =>
        store ?? throw new InvalidOperationException("AppSettingsService.Initialize must be called first");

    /// <summary>The folder the live settings store is kept in.</summary>
    public static string DirectoryPath => Store.DirectoryPath;

    /// <summary>
    /// The default settings folder for an application, without opening
    /// anything: see <see cref="AppSettingsStore.GetDefaultDirectory"/>.
    /// </summary>
    public static string GetDefaultDirectory(string appName) =>
        AppSettingsStore.GetDefaultDirectory(appName);

    /// <summary>
    /// Opens the settings store for the given application in its default
    /// folder, running the startup auto-backup and pruning sequence. Call once,
    /// before any UI renders.
    /// </summary>
    public static void Initialize(string appName)
    {
        EnsureNotInitialized();
        store = new AppSettingsStore(appName);
    }

    /// <summary>Opens the settings store in the given folder.</summary>
    public static void Initialize(string appName, string directoryPath)
    {
        EnsureNotInitialized();
        store = new AppSettingsStore(appName, directoryPath);
    }

    /// <summary>
    /// Closes the settings store and clears this service, so
    /// <see cref="Initialize(string)"/> may be called again. Intended for test
    /// hosts and for an application re-pointing at a different folder; doing
    /// nothing when the service was never initialized, so it is safe to call
    /// unconditionally during teardown.
    /// </summary>
    public static void Shutdown()
    {
        var current = store;
        store = null;
        current?.Dispose();
    }

    /// <summary>Wraps a setting in a typed <see cref="AppSettingProperty{T}"/> handle.</summary>
    public static AppSettingProperty<T> Wrap<T>(string key, T defaultValue) =>
        AppSettingProperty.Create(key, defaultValue);

    /// <summary>Whether a value is stored for the given key.</summary>
    public static bool HasValue(string key) => Store.HasValue(key);

    /// <summary>Returns the stored value for the key, or the given default when not set.</summary>
    public static T Get<T>(string key, T defaultValue) => Store.Get(key, defaultValue);

    /// <summary>Returns the stored value for the key, or the type's default when not set.</summary>
    public static T? Get<T>(string key) => Store.Get<T>(key);

    /// <summary>Stores a value for the key; a null value removes the key.</summary>
    public static void Set(string key, object? value) => Store.Set(key, value);

    /// <summary>Registers a handler raised when the given key's value changes.</summary>
    public static void AddSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler) =>
        Store.AddSettingHandler(key, handler);

    /// <summary>Removes a handler previously added with <see cref="AddSettingHandler"/>.</summary>
    public static void RemoveSettingHandler(string key, EventHandler<AppSettingChangedEventArgs> handler) =>
        Store.RemoveSettingHandler(key, handler);

    static void EnsureNotInitialized()
    {
        if (store != null)
            throw new InvalidOperationException("AppSettingsService is already initialized");
    }
}
