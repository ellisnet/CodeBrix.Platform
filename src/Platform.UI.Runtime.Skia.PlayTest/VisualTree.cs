using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace CodeBrix.Platform.PlayTest;

internal static class VisualTree
{
    internal static IEnumerable<UIElement> Walk(UIElement? root, bool includePopups = false)
    {
        if (root == null) yield break;
        var seen = new HashSet<UIElement>();
        var pending = new Stack<UIElement>();
        pending.Push(root);
        if (includePopups && root.XamlRoot != null)
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot))
                if (popup.Child != null) pending.Push(popup.Child);
        while (pending.Count != 0)
        {
            var current = pending.Pop();
            if (!seen.Add(current)) continue;
            yield return current;
            for (var i = VisualTreeHelper.GetChildrenCount(current) - 1; i >= 0; i--)
                if (VisualTreeHelper.GetChild(current, i) is UIElement child) pending.Push(child);
        }
    }

    internal static bool Within(UIElement child, UIElement ancestor)
    {
        for (DependencyObject? node = child; node != null; node = VisualTreeHelper.GetParent(node))
            if (node == ancestor) return true;
        return false;
    }

    internal static Rect Bounds(UIElement element) => element is FrameworkElement f
        ? element.TransformToVisual(null).TransformBounds(new Rect(0, 0, f.ActualWidth, f.ActualHeight)) : default;

    // A stretched ToggleSwitch includes its header and empty layout space. Its thumb
    // is the actual pointer target, including with the framework's default template.
    internal static UIElement ClickTarget(UIElement element) => element is ToggleSwitch
        ? Walk(element).OfType<Thumb>().FirstOrDefault() ?? element : element;

    internal static bool Visible([NotNullWhen(true)] UIElement? element)
    {
        if (element == null || element.XamlRoot == null) return false;
        for (DependencyObject? node = element; node != null; node = VisualTreeHelper.GetParent(node))
            if (node is UIElement ui && ui.Visibility != Visibility.Visible) return false;
        var bounds = Bounds(element);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    internal static bool Enabled(UIElement element)
    {
        for (DependencyObject? node = element; node != null; node = VisualTreeHelper.GetParent(node))
            if (node is Control control && !control.IsEnabled) return false;
        return true;
    }

    internal static string Normalize(string? value) => Regex.Replace(value ?? "", @"\s+", " ").Trim();
    // Browser textarea values use LF; WinUI TextBox internally uses CR. Expose
    // the same portable value to Playwright-style tests without altering the app.
    internal static string InputText(string? value) => (value ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    internal static bool Matches(string? actual, string? expected, bool exact) => exact
        ? string.Equals(Normalize(actual), Normalize(expected), StringComparison.Ordinal)
        : Normalize(actual).Contains(Normalize(expected), StringComparison.OrdinalIgnoreCase);

    internal static string Text(UIElement element)
    {
        if (element is TextBlock text) return text.Text ?? "";
        if (element is TextBox input) return input.Text ?? "";
        if (element is ContentControl content && content.Content is string value) return value;
        return string.Join(" ", Walk(element).OfType<TextBlock>().Select(t => t.Text));
    }

    internal static string Name(UIElement element)
    {
        var name = AutomationProperties.GetName(element);
        if (!string.IsNullOrEmpty(name)) return name;
        var label = AutomationProperties.GetLabeledBy(element);
        if (label != null) return Text(label);
        // The default menu templates include arrow/check glyphs in their peer's
        // aggregated name. Those adornments are not part of the command label.
        if (element is MenuFlyoutItem item) return item.Text ?? "";
        if (element is MenuFlyoutSubItem submenu) return submenu.Text ?? "";
        if (element is MenuBarItem topMenu) return topMenu.Title ?? "";
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        var peerName = peer?.GetName();
        return string.IsNullOrEmpty(peerName) ? (element is TextBox ? "" : Text(element)) : peerName;
    }

    internal static AriaRole Role(UIElement element)
    {
        if (element is ContentDialog) return AriaRole.Dialog;
        if (element is MenuBar) return AriaRole.Menubar;
        if (element is MenuBarItem) return AriaRole.Menuitem;
        if (element is ToggleMenuFlyoutItem) return AriaRole.Menuitemcheckbox;
        if (element is MenuFlyoutSubItem) return AriaRole.Menuitem;
        if (element is MenuFlyoutPresenter) return AriaRole.Menu;
        if (element is ToggleSwitch) return AriaRole.Switch;
        if (element is ComboBoxItem) return AriaRole.Option;
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        return peer?.GetAutomationControlType() switch
        {
            AutomationControlType.Button => AriaRole.Button,
            AutomationControlType.SplitButton => AriaRole.Button,
            AutomationControlType.ToolBar => AriaRole.Toolbar,
            AutomationControlType.Group => AriaRole.Group,
            AutomationControlType.Menu => AriaRole.Menu,
            AutomationControlType.MenuBar => AriaRole.Menubar,
            AutomationControlType.MenuItem => AriaRole.Menuitem,
            AutomationControlType.Separator => AriaRole.Separator,
            AutomationControlType.CheckBox => AriaRole.Checkbox,
            AutomationControlType.ComboBox => AriaRole.Combobox,
            AutomationControlType.Edit => AriaRole.Textbox,
            AutomationControlType.Hyperlink => AriaRole.Link,
            AutomationControlType.Image => AriaRole.Img,
            AutomationControlType.List => AriaRole.Listbox,
            AutomationControlType.ListItem => AriaRole.Option,
            AutomationControlType.RadioButton => AriaRole.Radio,
            AutomationControlType.ProgressBar => AriaRole.Progressbar,
            AutomationControlType.Slider => AriaRole.Slider,
            AutomationControlType.Tab => AriaRole.Tablist,
            AutomationControlType.TabItem => AriaRole.Tab,
            AutomationControlType.Tree => AriaRole.Tree,
            AutomationControlType.TreeItem => AriaRole.Treeitem,
            _ => AriaRole.Generic,
        };
    }

    internal static string Value(UIElement element) => element switch
    {
        PasswordBox password => password.Password ?? "",
        TextBox text => InputText(text.Text),
        ComboBox combo => combo.SelectedItem?.ToString() ?? "",
        _ when ValueProvider(element) is { } value => InputText(value.Value),
        _ => throw new PlayTestException("InputValueAsync/ToHaveValueAsync requires a text box, password box, combo box, or automation Value provider."),
    };

    internal static bool Checked(UIElement element) => element switch
    {
        ToggleSwitch toggle => toggle.IsOn,
        ToggleButton toggle => toggle.IsChecked == true,
        ToggleMenuFlyoutItem toggle => toggle.IsChecked,
        _ when FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle
            => toggle.ToggleState == ToggleState.On,
        _ => throw new PlayTestException("Checked state requires a checkable control or automation Toggle provider."),
    };

    internal static IValueProvider? ValueProvider(UIElement element) =>
        FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetPattern(PatternInterface.Value) as IValueProvider;

    internal static bool ReceivesEvents(UIElement target, Point point)
    {
        var hit = VisualTreeHelper.HitTest(point, target.XamlRoot).element;
        return hit != null && Within(hit, target);
    }
}
