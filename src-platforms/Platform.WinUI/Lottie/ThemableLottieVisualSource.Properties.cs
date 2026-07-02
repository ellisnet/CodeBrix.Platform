#nullable enable

// Ported from the CodeBrix.Platform (Uno-based) Lottie package:
// src/AddIns/Platform.UI.Lottie/ThemableLottieVisualSource.Properties.cs

using System.Collections.Generic;
using System.Json;
using Windows.UI;

namespace CodeBrix.Platform.WinUI.Lottie;

partial class ThemableLottieVisualSource
{
    private readonly Dictionary<string, ColorBinding> _colorsBindings
        = new Dictionary<string, ColorBinding>(2);

    /// <inheritdoc />
    public void SetColorThemeProperty(string propertyName, Color? color)
    {
        if (_colorsBindings.TryGetValue(propertyName, out var existing))
        {
            existing.NextValue = color;
        }
        else
        {
            _colorsBindings[propertyName] = new ColorBinding { NextValue = color };
        }

        if (_currentDocument == null)
        {
            return; // no document to change yet
        }

        if (ApplyProperties())
        {
            NotifyCallback();
        }
    }

    /// <inheritdoc />
    public Color? GetColorThemeProperty(string propertyName)
    {
        if (_colorsBindings.TryGetValue(propertyName, out var existing))
        {
            return existing.NextValue ?? existing.CurrentValue;
        }

        return default;
    }

    private class ColorBinding
    {
        internal List<JsonObject> Elements { get; } = new List<JsonObject>(1);
        internal Color? CurrentValue { get; set; }
        internal Color? NextValue { get; set; }
    }
}
