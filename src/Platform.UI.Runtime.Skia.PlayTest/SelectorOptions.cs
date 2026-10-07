using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CodeBrix.Platform.PlayTest;

// One item of a selector as SelectOptionAsync sees it, whether or not its row is realized.
internal sealed record SelectorOption(int Index, string Label, string Value, bool Selected);

// Reads and changes a selector's options from its items, so drop-down rows that were never
// materialized can be chosen. Setting the selection raises the control's normal SelectionChanged.
internal static class SelectorOptions
{
    internal static IReadOnlyList<SelectorOption> Read(Selector selector)
    {
        var options = new List<SelectorOption>();
        var multiple = AllowsMultiple(selector);
        for (var index = 0; index < selector.Items.Count; index++)
        {
            var item = selector.Items[index];
            var label = Label(selector, item, index);
            var value = string.IsNullOrEmpty(selector.SelectedValuePath) ? label : VisualTree.Normalize(Member(item, selector.SelectedValuePath)?.ToString());
            var selected = multiple ? SelectedItems(selector)?.Contains(item) == true : selector.SelectedIndex == index;
            options.Add(new SelectorOption(index, label, value, selected));
        }
        return options;
    }

    internal static string Label(Selector selector, object? item, int index)
    {
        if (!string.IsNullOrEmpty(selector.DisplayMemberPath)) return VisualTree.Normalize(Member(item, selector.DisplayMemberPath)?.ToString());
        return VisualTree.Normalize(item switch
        {
            string text => text,
            ContentControl { Content: string content } => content,
            UIElement element => VisualTree.Text(element),
            _ when selector.ContainerFromIndex(index) is UIElement container && VisualTree.Text(container) is { Length: > 0 } text => text,
            _ => item?.ToString(),
        });
    }

    // A dotted property path such as "Address.City", read by reflection.
    internal static object? Member(object? item, string path)
    {
        foreach (var name in path.Split('.'))
        {
            if (item == null) return null;
            item = item.GetType().GetProperty(name)?.GetValue(item);
        }
        return item;
    }

    internal static bool Matches(SelectorOption option, SelectOptionValue wanted) =>
        (wanted.Value == null || option.Value == wanted.Value)
        && (wanted.Label == null || VisualTree.Matches(option.Label, wanted.Label, true))
        && (wanted.Index == null || option.Index == wanted.Index);

    internal static string Describe(SelectOptionValue value) => "{" + string.Join(", ", new[]
    {
        value.Value == null ? null : $"Value '{value.Value}'",
        value.Label == null ? null : $"Label '{value.Label}'",
        value.Index == null ? null : $"Index {value.Index}",
    }.Where(part => part != null)) + "}";

    internal static bool AllowsMultiple(Selector selector) => selector switch
    {
        ListViewBase list => list.SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended,
        ListBox list => list.SelectionMode is SelectionMode.Multiple or SelectionMode.Extended,
        _ => false,
    };

    private static IList<object>? SelectedItems(Selector selector) => selector switch
    {
        ListViewBase list => list.SelectedItems,
        ListBox list => list.SelectedItems,
        _ => null,
    };

    internal static void Apply(Selector selector, IReadOnlyList<SelectorOption> chosen, string description)
    {
        if (selector is ListViewBase { SelectionMode: ListViewSelectionMode.None })
            throw new PlayTestException($"{description}: the list does not allow selection (SelectionMode is None).");
        if (AllowsMultiple(selector) && SelectedItems(selector) is { } selected)
        {
            var wanted = chosen.Select(option => selector.Items[option.Index]).ToList();
            foreach (var item in selected.Where(item => !wanted.Contains(item)).ToArray()) selected.Remove(item);
            foreach (var item in wanted.Where(item => !selected.Contains(item))) selected.Add(item);
            return;
        }
        // A single-selection control takes the first matching option, as a browser <select> does.
        var index = chosen.Count == 0 ? -1 : chosen[0].Index;
        if (selector.SelectedIndex != index) selector.SelectedIndex = index;
        // A user's choice closes an open drop-down.
        if (selector is ComboBox { IsDropDownOpen: true } combo) combo.IsDropDownOpen = false;
    }
}
