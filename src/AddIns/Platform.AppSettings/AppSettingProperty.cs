//
// AppSettingProperty.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2018 Microsoft Inc.
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (adapted from MonoDevelop's ConfigurationProperty, via CodeBrix.Develop
//      and the sample settings stores, for CodeBrix.Platform: .NET 10,
//      modern C#, AppSettingsService-backed)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.AppSettings; //was previously: Doom.Brix.Settings.ConfigurationProperty (and MonoDevelop.Core before that)

/// <summary>
/// Wraps a single <see cref="AppSettingsService"/> value as an easy-to-use
/// typed object with change notification.
/// </summary>
/// <remarks>
/// Named for the setting rather than "ConfigurationProperty" so that the type
/// does not compete with <c>System.Configuration.ConfigurationProperty</c>.
/// </remarks>
public abstract class AppSettingProperty<T>
{
    /// <summary>The current value of the property.</summary>
    public T Value
    {
        get => OnGetValue();
        set => OnSetValue(value);
    }

    /// <summary>
    /// Sets the property to the specified value.
    /// </summary>
    /// <param name='newValue'>The new value.</param>
    /// <returns>True if the property has changed, false otherwise.</returns>
    public bool Set(T newValue) => OnSetValue(newValue);

    /// <summary>A property implicitly converts to its current value.</summary>
    public static implicit operator T(AppSettingProperty<T> watch) => watch.Value;

    /// <summary>Returns the current value.</summary>
    protected abstract T OnGetValue();

    /// <summary>Stores a new value, returning true when it changed.</summary>
    protected abstract bool OnSetValue(T value);

    /// <summary>Raises <see cref="Changed"/>.</summary>
    protected void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Raised when the value of the property changes.</summary>
    public event EventHandler? Changed;
}

sealed class CoreAppSettingProperty<T> : AppSettingProperty<T>
{
    T value;

    public string Key { get; }

    public CoreAppSettingProperty(string key, T defaultValue, string? oldKey = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));

        // Migrate the setting from oldKey to key.
        if (!string.IsNullOrEmpty(oldKey) && AppSettingsService.HasValue(oldKey))
        {
            // Migrate the old value if the new one is not set.
            if (!AppSettingsService.HasValue(Key))
            {
                var oldValue = AppSettingsService.Get<T>(oldKey);
                AppSettingsService.Set(Key, oldValue);
            }
            AppSettingsService.Set(oldKey, null);
        }

        value = AppSettingsService.Get(Key, defaultValue);
    }

    protected override T OnGetValue() => value;

    protected override bool OnSetValue(T value)
    {
        if (EqualityComparer<T>.Default.Equals(this.value, value))
            return false;

        this.value = value;
        AppSettingsService.Set(Key, value);
        OnChanged();
        return true;
    }
}

/// <summary>Factory for <see cref="AppSettingProperty{T}"/> instances.</summary>
public abstract class AppSettingProperty
{
    /// <summary>
    /// Creates a typed property handle over the given setting key, optionally
    /// migrating the stored value from a previous key name.
    /// </summary>
    public static AppSettingProperty<T> Create<T>(string key, T defaultValue, string? oldKey = null)
        => new CoreAppSettingProperty<T>(key, defaultValue, oldKey);
}
