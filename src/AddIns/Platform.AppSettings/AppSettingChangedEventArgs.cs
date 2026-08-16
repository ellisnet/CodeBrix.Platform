//
// AppSettingChangedEventArgs.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (extracted for CodeBrix.Platform from the sample settings stores;
//      inspired by MonoDevelop.Core.PropertyChangedEventArgs, simplified)
// SPDX-License-Identifier: Apache-2.0
//

using System;

namespace CodeBrix.Platform.AppSettings; //was previously: Doom.Brix.Settings.PropertyChangedEventArgs

/// <summary>
/// Event arguments describing a change to a single stored setting value.
/// </summary>
/// <remarks>
/// Named for the setting rather than for the property, so that a file with
/// <c>using System.ComponentModel;</c> in scope — which most XAML code has —
/// does not end up choosing between two types called
/// <c>PropertyChangedEventArgs</c>.
/// </remarks>
public class AppSettingChangedEventArgs : EventArgs
{
    /// <summary>The key of the setting that changed.</summary>
    public string Key { get; }

    /// <summary>
    /// The previously stored value in its serialized JSON form, or null when
    /// the setting was not set before.
    /// </summary>
    /// <remarks>
    /// This is the raw stored text rather than a deserialized object: the store
    /// writes through <c>Set(string, object)</c> and so never learns the type
    /// the old value should be read back as.
    /// </remarks>
    public object? OldValue { get; }

    /// <summary>The new value, or null when the setting was removed.</summary>
    public object? NewValue { get; }

    /// <summary>Creates event arguments for a changed setting.</summary>
    public AppSettingChangedEventArgs(string key, object? oldValue, object? newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
