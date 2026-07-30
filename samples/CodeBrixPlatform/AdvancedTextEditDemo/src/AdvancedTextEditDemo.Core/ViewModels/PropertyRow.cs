using Microsoft.UI.Xaml;
using System;
using System.Reflection;

namespace AdvancedTextEditDemo.ViewModels;

/// <summary>
/// One row of the demo's reflection-driven property pane: a readable public property of the
/// object being inspected. Read-only values display as "Name = Value" text; settable bool
/// properties display as a CheckBox that writes the toggled value back through reflection.
/// The pane is the demo's stand-in for the property grid the upstream sample hosts.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class PropertyRow
{
    private readonly object _target;
    private readonly PropertyInfo _property;
    private readonly bool _initialized;

    /// <summary>Creates the row for one property of the inspected object.</summary>
    public PropertyRow(object target, PropertyInfo property)
    {
        _target = target;
        _property = property;
        Name = property.Name;
        IsToggle = property.PropertyType == typeof(bool) && property.CanWrite;

        object value;
        try
        {
            value = property.GetValue(target);
        }
        catch (Exception ex)
        {
            //Some framework property getters are not implemented on every head
            value = "<" + ex.GetType().Name + ">";
        }

        IsChecked = IsToggle && value is true;
        Display = Name + " = " + FormatValue(value);
        _initialized = true;
    }

    /// <summary>Gets the property name.</summary>
    public string Name { get; }

    /// <summary>Gets whether the row renders as an editable CheckBox (a settable bool property).</summary>
    public bool IsToggle { get; }

    /// <summary>Gets the "Name = Value" text shown for read-only rows.</summary>
    public string Display { get; }

    /// <summary>Gets the visibility of the read-only text presentation.</summary>
    public Visibility TextVisibility => IsToggle ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Gets the visibility of the CheckBox presentation.</summary>
    public Visibility ToggleVisibility => IsToggle ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets or sets the checked state of a toggle row; setting it re-applies the value to the
    /// underlying property through reflection.
    /// </summary>
    public bool IsChecked
    {
        get;
        set
        {
            field = value;
            if (_initialized && IsToggle)
            {
                _property.SetValue(_target, value);
            }
        }
    }

    private static string FormatValue(object value)
    {
        if (value == null) { return "(null)"; }

        var text = value.ToString() ?? value.GetType().Name;
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length > 120 ? text.Substring(0, 120) + "..." : text;
    }
}
