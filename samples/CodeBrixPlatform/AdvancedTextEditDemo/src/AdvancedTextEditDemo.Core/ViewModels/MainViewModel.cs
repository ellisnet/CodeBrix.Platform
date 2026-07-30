using CodeBrix.Platform.Simple;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AdvancedTextEditDemo.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public class MainViewModel : SimpleViewModel
{
    public MainViewModel()
    {
        if (IsDesignMode(true)) { return; } //Leave as the first line of constructor

        Debug.WriteLine("Main view model startup.");
    }

    #region | Bindable properties |

    /// <summary>Gets or sets the status-bar text that shows the caret's line and column.</summary>
    public string StatusText
    {
        get;
        set => SetProperty(ref field, value ?? string.Empty);
    } = "Line 1, Column 1";

    /// <summary>
    /// Gets or sets the full path of the file the editor is working with; empty until a file
    /// has been opened or saved.
    /// </summary>
    public string CurrentFileName
    {
        get;
        set => SetProperty(ref field, value ?? string.Empty);
    } = string.Empty;

    /// <summary>
    /// Gets the rows of the reflection-driven property pane for the object selected in the
    /// pane's ComboBox.
    /// </summary>
    public IReadOnlyList<PropertyRow> PropertyRows
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>
    /// Rebuilds <see cref="PropertyRows"/> to show every readable public property of the
    /// given object, or clears the pane when the object is null.
    /// </summary>
    public void ShowProperties(object target)
    {
        if (target == null)
        {
            PropertyRows = [];
            return;
        }

        PropertyRows =
        [
            .. target.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => new PropertyRow(target, p)),
        ];
    }

    #endregion

    #region | Commands and their implementations |

    //No commands - the page's toolbar buttons call the editor control directly

    #endregion
}
