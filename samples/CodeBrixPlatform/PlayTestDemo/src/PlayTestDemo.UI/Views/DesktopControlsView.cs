using System;
using System.Windows.Input;
using CodeBrix.Platform.UI.AdvancedTextEdit;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace PlayTestDemo.Views;

/// <summary>Interactive examples shared by the desktop heads and their PlayTests.</summary>
public sealed class DesktopControlsView : Grid
{
    public AdvancedTextEdit Editor { get; } = new() { ShowLineNumbers = true, MinHeight = 250 };
    public int CommandCount { get; private set; }
    public int ContextCount { get; private set; }
    public int DoubleClickCount { get; private set; }
    public ToolToggleButton Toggle { get; } = new() { Text = "Pin" };
    public TabView Tabs { get; } = new() { IsAddTabButtonVisible = false };
    public ToggleMenuFlyoutItem MenuToggle { get; } = new() { Text = "Show details" };
    public ToolBar Toolbar { get; } = new() { Title = "Editor tools", LabelMode = LabelMode.TextOnly, HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 600 };

    public DesktopControlsView()
    {
        Padding = new Thickness(24);
        RowSpacing = 12;
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Insert(3, new() { Height = GridLength.Auto });
        var menu = new MenuBar();
        var view = new MenuBarItem { Title = "View" };
        view.Items.Add(MenuToggle);
        var submenu = new MenuFlyoutSubItem { Text = "More" };
        var action = new MenuFlyoutItem { Text = "Record action" };
        action.Click += (_, _) => CommandCount++;
        submenu.Items.Add(action);
        view.Items.Add(submenu);
        menu.Items.Add(view);
        Children.Add(menu);

        var command = new DelegateCommand(() => CommandCount++);
        Toolbar.Items.Add(new ToolButton { Text = "Run", Command = command });
        Toolbar.Items.Add(Toggle);
        var flyout = new MenuFlyout();
        var choose = new MenuFlyoutItem { Text = "Choose action", Command = command };
        flyout.Items.Add(choose);
        Toolbar.Items.Add(new ToolDropDownButton { Text = "Actions", PopupMode = PopupMode.MenuButton, Command = command, Flyout = flyout });
        Toolbar.Items.Add(new ToolButton { Text = "Unavailable", IsEnabled = false });
        Grid.SetRow(Toolbar, 1);
        Children.Add(Toolbar);

        var settings = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var readOnly = new CheckBox { Content = "Read only" };
        readOnly.Checked += (_, _) => Editor.IsReadOnly = true;
        readOnly.Unchecked += (_, _) => Editor.IsReadOnly = false;
        settings.Children.Add(readOnly);
        var narrow = new CheckBox { Content = "Narrow toolbar" };
        narrow.Checked += (_, _) => Toolbar.Width = 130;
        narrow.Unchecked += (_, _) => Toolbar.Width = double.NaN;
        settings.Children.Add(narrow);
        var status = new TextBlock { Text = "Type into the editor. Try Undo, menus, toolbar overflow and read-only mode." };
        settings.Children.Add(status);
        Grid.SetRow(settings, 2);
        Children.Add(settings);

        AutomationProperties.SetName(Editor, "Source editor");
        var context = new MenuFlyout();
        var record = new MenuFlyoutItem { Text = "Context action" };
        record.Click += (_, _) => ContextCount++;
        context.Items.Add(record);
        Editor.ContextFlyout = context;
        Editor.DoubleTapped += (_, _) => DoubleClickCount++;
        var tabs = Tabs;
        tabs.TabItems.Add(new TabViewItem { Header = new TextBlock { Text = "Score" }, IsClosable = false });
        var preview = new TabViewItem { Header = new TextBlock { Text = "Preview" }, IsClosable = false };
        AutomationProperties.SetName(preview, "Score preview");
        tabs.TabItems.Add(preview);
        Grid.SetRow(tabs, 3);
        Children.Add(tabs);
        Grid.SetRow(Editor, 4);
        Children.Add(Editor);
    }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => execute();
    }
}
