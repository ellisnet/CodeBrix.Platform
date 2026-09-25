//
// SkiaPlatformBootstrap.skia.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
// SPDX-License-Identifier: Apache-2.0
//

using System.Runtime.CompilerServices;
using CodeBrix.Platform.AppSettings.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.AppSettings.Skia;

/// <summary>
/// Registers this assembly's implementation of the AppSettings add-in's storage contract with
/// <see cref="ApiExtensibility"/>. Runs as a module initializer, so the registration exists before any code of
/// this assembly runs; the Core assembly's <see cref="PlatformContract"/> runs it when it first needs the contract.
/// </summary>
internal static class SkiaPlatformBootstrap
{
    static readonly object gate = new();
    static bool registered;

    // A module initializer is the one place that runs before the first use of any type in this assembly,
    // which is exactly when the contract must be in place (CA2255 is aimed at application-level code).
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => EnsureRegistered();

    /// <summary>
    /// Registers every contract implementation of this assembly. Safe to call more than once.
    /// </summary>
    internal static void EnsureRegistered()
    {
        lock (gate)
        {
            if (registered)
                return;

            var storage = new AppSettingsStorageSkiaPlatform();
            ApiExtensibility.Register(typeof(IAppSettingsStoragePlatform), _ => storage);

            registered = true;
        }
    }
}
