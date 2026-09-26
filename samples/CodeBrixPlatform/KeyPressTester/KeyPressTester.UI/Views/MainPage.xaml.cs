using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Simple;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;

namespace KeyPressTester.Views;

public sealed partial class MainPage : Page
{
    //The independent layers that can "notice" a key press. They are listed in the order a key
    //  travels through CodeBrix.Platform: the host's native keyboard source raises
    //  CoreWindow.KeyDown first, then the AccessKeyManager gets a chance to consume the key
    //  (Alt menus), then PreviewKeyDown tunnels from the window root down to the focused
    //  element and KeyDown bubbles from the focused element back up to the root. Keyboard
    //  accelerators, access keys and menu clicks are the "app-level" results of a key press.
    private enum Listener
    {
        Native,
        RootPreview,
        GameSurface,
        TextBox,
        Page,
        RootBubble,
        Accelerator,
        AccessKey,
        MenuClick,
    }

    private static readonly (Listener Listener, string Tag, string Description)[] ListenerInfo =
    [
        (Listener.Native, "NATIVE", "CoreWindow.KeyDown/KeyUp - raw keys from the host (X11, Wayland, Win32, WPF, macOS, frame buffer)"),
        (Listener.RootPreview, "ROOT-PRV", "Window root PreviewKeyDown/Up (tunnelling, handledEventsToo)"),
        (Listener.GameSurface, "SURFACE", "Game surface KeyDown/KeyUp - only while it has focus (the Doom.Brix / GameEngine path)"),
        (Listener.TextBox, "TEXTBOX", "Text box KeyDown/KeyUp (handledEventsToo)"),
        (Listener.Page, "PAGE", "Page KeyDown/KeyUp (bubbling, handledEventsToo)"),
        (Listener.RootBubble, "ROOT", "Window root KeyDown/KeyUp (bubbling, handledEventsToo)"),
        (Listener.Accelerator, "ACCEL", "KeyboardAccelerator.Invoked (menu items and page)"),
        (Listener.AccessKey, "ACCESSKEY", "Access keys (Alt menus): display mode, AccessKeyInvoked"),
        (Listener.MenuClick, "MENU", "Menu item Click"),
    ];

    private const int MaxLogEntries = 500;
    private const double PlayerStep = 8;
    private const double ShotSpeed = 12;

    private static readonly string LogFilePath = Environment.GetEnvironmentVariable("KEYPRESSTESTER_LOG_FILE");

    private readonly ObservableCollection<string> _log = [];
    private readonly Dictionary<Listener, int> _counts = [];
    private readonly Dictionary<Listener, TextBlock> _countTexts = [];
    private readonly Dictionary<Listener, TextBlock> _lastTexts = [];
    private readonly Dictionary<Listener, HashSet<VirtualKey>> _downKeys = [];
    private readonly HashSet<VirtualKey> _nativeHeld = [];
    private readonly Dictionary<VirtualKey, bool> _nativeKeyIsRepeat = [];
    private readonly Dictionary<MenuBarItem, TextBlock> _menuTitleTextBlocks = [];
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly DispatcherTimer _gameTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };

    private CoreWindow _coreWindow;
    private UIElement _root;
    private int _nativePressNumber;
    private bool _shotActive;
    private int _jumpFrames;
    private string _windowState = "(not reported yet)";
    private string _lastFocusDescription = "";

    public MainPage()
    {
        this.InitializeComponent();

        LogList.ItemsSource = _log;
        BuildListenerRows();

        foreach (var info in ListenerInfo)
        {
            _downKeys[info.Listener] = [];
        }

        //Game surface: keys arrive only while it holds keyboard focus, so (like the GameEngine
        //  CodeBrixKeyboardAdapter and Doom.Brix) grab focus on load and on every click.
        GameSurface.KeyDown += OnGameSurfaceKeyDown;
        GameSurface.KeyUp += OnGameSurfaceKeyUp;
        GameSurface.GotFocus += (_, _) => UpdateGameSurfaceFocusVisuals();
        GameSurface.LostFocus += (_, _) =>
        {
            //A key released after focus moved away (into a menu or a dialog) never sends its KeyUp here, so
            //  it would look held for good - a game forgets its held keys when it loses focus, and so do we.
            _downKeys[Listener.GameSurface].Clear();
            UpdateHeldKeys();
            UpdateGameSurfaceFocusVisuals();
        };
        GameSurface.PointerPressed += (_, _) => GameSurface.Focus(FocusState.Programmatic);
        GameSurface.AddHandler(
            UIElement.PointerReleasedEvent,
            new PointerEventHandler((_, _) => FocusGameSurfaceDeferred()),
            handledEventsToo: true);

        //Text box: handledEventsToo, because the TextBox marks most of its keys handled
        TypingBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) => OnXamlKey(Listener.TextBox, e, true)), true);
        TypingBox.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, e) => OnXamlKey(Listener.TextBox, e, false)), true);
        TypingBox.TextChanged += (_, _) => Log(Listener.TextBox, $"TextChanged  text=\"{TypingBox.Text}\"", countIt: false);

        //Page: bubbling KeyDown/KeyUp, including events already handled below it
        AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) => OnXamlKey(Listener.Page, e, true)), true);
        AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, e) => OnXamlKey(Listener.Page, e, false)), true);

        //Menus: accelerators on the items, access keys on the menus, and the item clicks
        //  (File and Help items always do something visible: a dialog, or clearing the log).
        WireMenuItem(NewGameItem, VirtualKey.N, VirtualKeyModifiers.Control, () =>
        {
            ResetGame();
            ShowMessage("New Game", "A new game has been started.");
        });
        WireMenuItem(PrintItem, VirtualKey.P, VirtualKeyModifiers.Control, () => ShowMessage("Print", "The file would have been printed."));
        WireMenuItem(SaveItem, VirtualKey.S, VirtualKeyModifiers.Control, () => ShowMessage("Save", "The file would have been saved."));
        WireMenuItem(ClearLogItem, VirtualKey.L, VirtualKeyModifiers.Control, ClearLog);
        WireMenuItem(MoveUpItem, null, VirtualKeyModifiers.None, () => MovePlayer(0, -4 * PlayerStep));
        WireMenuItem(MoveDownItem, null, VirtualKeyModifiers.None, () => MovePlayer(0, 4 * PlayerStep));
        WireMenuItem(MoveLeftItem, null, VirtualKeyModifiers.None, () => MovePlayer(-4 * PlayerStep, 0));
        WireMenuItem(MoveRightItem, null, VirtualKeyModifiers.None, () => MovePlayer(4 * PlayerStep, 0));
        WireMenuItem(AboutItem, VirtualKey.F1, VirtualKeyModifiers.None, () =>
            ShowMessage("About Key Press Tester", "This is an app for testing key presses in CodeBrix.Platform applications."));

        foreach (var menu in new[] { FileMenu, MoveMenu, HelpMenu })
        {
            menu.AccessKeyInvoked += (sender, _) =>
                Log(Listener.AccessKey, $"AccessKeyInvoked  menu \"{((MenuBarItem)sender).Title}\" (Alt+{((MenuBarItem)sender).AccessKey})");
        }

        //Page-level accelerators that are not on any menu item
        AddPageAccelerator(VirtualKey.J, VirtualKeyModifiers.Menu, "Jump (page accelerator)");
        AddPageAccelerator(VirtualKey.K, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, "Page accelerator");
        AddPageAccelerator(VirtualKey.F5, VirtualKeyModifiers.None, "Refresh (page accelerator)");

        AccessKeyDisplayRequested += (_, _) => Log(Listener.AccessKey, "AccessKeyDisplayRequested (access key tips shown)");
        AccessKeyDisplayDismissed += (_, _) => Log(Listener.AccessKey, "AccessKeyDisplayDismissed (access key tips hidden)");

        FocusSurfaceButton.Click += (_, _) => FocusGameSurfaceDeferred();
        ClearLogButton.Click += (_, _) => ClearLog();

        _statusTimer.Tick += (_, _) => UpdateStatus();
        _gameTimer.Tick += (_, _) => AdvanceGame();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    //Called by the App when the window is activated or deactivated - the same hook Doom.Brix
    //  uses to give its game canvas focus back after alt-tabbing away and back.
    internal void OnWindowActivated(CoreWindowActivationState state)
    {
        _windowState = state.ToString();
        Log(null, $"WINDOW       {state}");

        if (state == CoreWindowActivationState.Deactivated)
        {
            //Keys released while another window is active (e.g. the Alt of Alt+Tab) never
            //  deliver a KeyUp here, so forget them rather than show them held forever.
            _nativeHeld.Clear();
            _nativeKeyIsRepeat.Clear();
            UpdateMenuBarUnderlines();
        }

        if (state != CoreWindowActivationState.Deactivated && RefocusOnActivateCheck.IsChecked == true)
        {
            FocusGameSurfaceDeferred();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        //Native layer: CoreWindow.KeyDown/KeyUp are raised straight from the host's keyboard
        //  input source, BEFORE the AccessKeyManager and the XAML routed events. A key seen here
        //  but by none of the XAML listeners was lost inside CodeBrix.Platform, not in the host.
        _coreWindow = CoreWindow.GetForCurrentThread();
        if (_coreWindow is not null)
        {
            _coreWindow.KeyDown += OnNativeKeyDown;
            _coreWindow.KeyUp += OnNativeKeyUp;
        }
        else
        {
            Log(null, "WARNING      CoreWindow.GetForCurrentThread() returned null - the NATIVE listener is not active");
        }

        //Window root: the top of the visual tree for this window. PreviewKeyDown tunnels
        //  through it first; KeyDown bubbles up to it last.
        _root = XamlRoot?.Content;
        if (_root is not null)
        {
            _root.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((_, a) => OnXamlKey(Listener.RootPreview, a, true)), true);
            _root.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((_, a) => OnXamlKey(Listener.RootPreview, a, false)), true);
            _root.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, a) => OnXamlKey(Listener.RootBubble, a, true)), true);
            _root.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, a) => OnXamlKey(Listener.RootBubble, a, false)), true);
        }

        FocusManager.GotFocus += OnFocusManagerGotFocus;
        AccessKeyManager.IsDisplayModeEnabledChanged += OnAccessKeyDisplayModeChanged;

        _statusTimer.Start();
        _gameTimer.Start();

        Log(null, $"STARTED      {Environment.OSVersion}, .NET {Environment.Version}" +
            (string.IsNullOrEmpty(LogFilePath) ? "" : $", also logging to {LogFilePath}"));

        FocusGameSurfaceDeferred();

        //The menu titles' TextBlocks exist once the MenuBar has applied its templates
        DispatcherQueue.TryEnqueue(UpdateMenuBarUnderlines);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_coreWindow is not null)
        {
            _coreWindow.KeyDown -= OnNativeKeyDown;
            _coreWindow.KeyUp -= OnNativeKeyUp;
        }

        FocusManager.GotFocus -= OnFocusManagerGotFocus;
        AccessKeyManager.IsDisplayModeEnabledChanged -= OnAccessKeyDisplayModeChanged;

        _statusTimer.Stop();
        _gameTimer.Stop();
    }

    #region Key listeners

    private void OnNativeKeyDown(CoreWindow sender, Windows.UI.Core.KeyEventArgs args) => RunOnUiThread(() => OnNativeKey(args.VirtualKey, args.KeyStatus, true));

    private void OnNativeKeyUp(CoreWindow sender, Windows.UI.Core.KeyEventArgs args) => RunOnUiThread(() => OnNativeKey(args.VirtualKey, args.KeyStatus, false));

    private void OnNativeKey(VirtualKey key, CorePhysicalKeyStatus status, bool down)
    {
        var modifiers = CurrentModifiers();

        //The native layer sees every key, so it decides whether a KeyDown is an auto-repeat for
        //  all listeners - a listener that missed a KeyUp (it was routed to an open menu, say)
        //  must not report the next real press of that key as a repeat.
        if (down)
        {
            _nativeKeyIsRepeat[key] = !_nativeHeld.Add(key);
        }
        else
        {
            _nativeHeld.Remove(key);
        }

        if (IsAltKey(key))
        {
            UpdateMenuBarUnderlines();
        }

        if (!ShouldLog(Listener.Native, key, down, out var repeat))
        {
            return;
        }

        if (down && !repeat)
        {
            _nativePressNumber++;
        }

        var press = down && !repeat ? $"  press #{_nativePressNumber}" : "";
        Log(Listener.Native,
            $"{(down ? "KeyDown" : "KeyUp  ")}  {Combo(key, modifiers),-20} key={KeyName(key)}  scan=0x{status.ScanCode:X}" +
            $"  menuKeyDown={status.IsMenuKeyDown}{(repeat ? "  (repeat)" : "")}{press}");
    }

    private void OnGameSurfaceKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var repeat = IsRepeat(e.Key, _downKeys[Listener.GameSurface].Contains(e.Key));
        OnXamlKey(Listener.GameSurface, e, true);

        if (!repeat && IsControlKey(e.Key))
        {
            Fire();
        }
        else if (!repeat && e.Key == VirtualKey.Space)
        {
            Jump();
        }
        else
        {
            var step = IsShiftDown() ? 2 * PlayerStep : PlayerStep;
            switch (e.Key)
            {
                case VirtualKey.Left or VirtualKey.A:
                    MovePlayer(-step, 0);
                    break;
                case VirtualKey.Right or VirtualKey.D:
                    MovePlayer(step, 0);
                    break;
                case VirtualKey.Up or VirtualKey.W:
                    MovePlayer(0, -step);
                    break;
                case VirtualKey.Down or VirtualKey.S:
                    MovePlayer(0, step);
                    break;
            }
        }

        if (HandleSurfaceKeysCheck.IsChecked == true)
        {
            e.Handled = true;
        }

        UpdateHeldKeys();
    }

    private void OnGameSurfaceKeyUp(object sender, KeyRoutedEventArgs e)
    {
        OnXamlKey(Listener.GameSurface, e, false);

        if (HandleSurfaceKeysCheck.IsChecked == true)
        {
            e.Handled = true;
        }

        UpdateHeldKeys();
    }

    private void OnXamlKey(Listener listener, KeyRoutedEventArgs e, bool down)
    {
        if (!ShouldLog(listener, e.Key, down, out var repeat))
        {
            return;
        }

        var original = e.OriginalKey != e.Key ? $" (original {KeyName(e.OriginalKey)})" : "";
        Log(listener,
            $"{(down ? "KeyDown" : "KeyUp  ")}  {Combo(e.Key, CurrentModifiers()),-20} key={KeyName(e.Key)}{original}" +
            $"  source={Describe(e.OriginalSource)}  handled={e.Handled}{(repeat ? "  (repeat)" : "")}");
    }

    //Tracks which keys each listener currently sees held, so auto-repeat KeyDowns can be told
    //  apart from new presses, and applies the "log KeyUp" / "log repeats" options.
    private bool ShouldLog(Listener listener, VirtualKey key, bool down, out bool repeat)
    {
        var held = _downKeys[listener];
        if (down)
        {
            repeat = IsRepeat(key, !held.Add(key));
            return !repeat || LogRepeatsCheck.IsChecked == true;
        }

        repeat = false;
        held.Remove(key);
        return LogKeyUpCheck.IsChecked == true;
    }

    //Whether a KeyDown is an auto-repeat: the native layer's verdict when it saw the key,
    //  otherwise the listener's own record of keys it has seen go down.
    private bool IsRepeat(VirtualKey key, bool heldByListener) =>
        _nativeKeyIsRepeat.TryGetValue(key, out var nativeRepeat) ? nativeRepeat : heldByListener;

    #endregion

    #region Menus, accelerators and access keys

    private void WireMenuItem(MenuFlyoutItem item, VirtualKey? acceleratorKey, VirtualKeyModifiers modifiers, Action action)
    {
        if (acceleratorKey is { } key)
        {
            var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
            accelerator.Invoked += (_, _) =>
                Log(Listener.Accelerator, $"Invoked  {Combo(key, modifiers),-20} -> menu item \"{item.Text}\"");
            item.KeyboardAccelerators.Add(accelerator);
        }

        item.AccessKeyInvoked += (_, _) => Log(Listener.AccessKey, $"AccessKeyInvoked  menu item \"{item.Text}\" ({item.AccessKey})");

        //Menu items always show their access key underlined (the menu's item template is realized
        //  each time the menu opens). The accelerator text ("Ctrl+P") is shown by the item itself.
        item.Loaded += (_, _) =>
        {
            if (FindTextBlock(item, item.Text) is { } textBlock)
            {
                ShowAccessKey(textBlock, item.Text, item.AccessKey, underline: true);
            }
        };
        item.Click += (_, _) =>
        {
            Log(Listener.MenuClick, $"Click  \"{item.Text}\"");
            action?.Invoke();
        };
    }

    private void AddPageAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, string description)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            args.Handled = true;
            Log(Listener.Accelerator, $"Invoked  {Combo(key, modifiers),-20} -> {description}");
            if (key == VirtualKey.J)
            {
                Jump();
            }
        };
        KeyboardAccelerators.Add(accelerator);
    }

    private void OnAccessKeyDisplayModeChanged(object sender, object args) =>
        RunOnUiThread(() =>
        {
            Log(Listener.AccessKey,
                $"AccessKeyManager display mode {(AccessKeyManager.IsDisplayModeEnabled ? "ENTERED" : "EXITED")}");
            UpdateMenuBarUnderlines();
        });

    //The menu titles underline their access key (the F of File) only while Alt is held down, or
    //  while access-key mode is on (after a tap of Alt), i.e. exactly while Alt+letter or the bare
    //  letter would open that menu.
    private void UpdateMenuBarUnderlines()
    {
        var underline = _nativeHeld.Any(IsAltKey) || AccessKeyManager.IsDisplayModeEnabled;

        foreach (var menu in new[] { FileMenu, MoveMenu, HelpMenu })
        {
            if (!_menuTitleTextBlocks.TryGetValue(menu, out var textBlock))
            {
                textBlock = FindTextBlock(menu, menu.Title);
                if (textBlock is null)
                {
                    continue;
                }

                _menuTitleTextBlocks[menu] = textBlock;
            }

            ShowAccessKey(textBlock, menu.Title, menu.AccessKey, underline);
        }
    }

    //Rewrites a TextBlock's text as runs with the access-key letter underlined (or as plain text).
    private static void ShowAccessKey(TextBlock textBlock, string text, string accessKey, bool underline)
    {
        var index = string.IsNullOrEmpty(accessKey) ? -1 : text.IndexOf(accessKey, StringComparison.OrdinalIgnoreCase);

        textBlock.Inlines.Clear();
        if (!underline || index < 0)
        {
            textBlock.Inlines.Add(new Run { Text = text });
            return;
        }

        if (index > 0)
        {
            textBlock.Inlines.Add(new Run { Text = text[..index] });
        }

        textBlock.Inlines.Add(new Run { Text = text.Substring(index, accessKey.Length), TextDecorations = TextDecorations.Underline });

        if (index + accessKey.Length < text.Length)
        {
            textBlock.Inlines.Add(new Run { Text = text[(index + accessKey.Length)..] });
        }
    }

    //The TextBlock that a control's template uses to display the given text
    private static TextBlock FindTextBlock(DependencyObject parent, string text)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock textBlock && textBlock.Text == text)
            {
                return textBlock;
            }

            if (FindTextBlock(child, text) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private async void ShowMessage(string title, string message)
    {
        Log(null, $"DIALOG       \"{title}\": {message}");
        using var dialog = SimpleDialog.Create(() => XamlRoot, DispatcherQueue, message, title);
        await dialog.ShowAsync();
        FocusGameSurfaceDeferred();
    }

    #endregion

    #region Focus

    private void OnFocusManagerGotFocus(object sender, FocusManagerGotFocusEventArgs e) =>
        RunOnUiThread(() => Log(null, $"FOCUS        -> {Describe(e.NewFocusedElement)}"));

    //Deferred to the dispatcher so focus lands after whatever took it (a click) has finished.
    private void FocusGameSurfaceDeferred() =>
        DispatcherQueue.TryEnqueue(() => GameSurface.Focus(FocusState.Programmatic));

    private void UpdateGameSurfaceFocusVisuals()
    {
        var focused = GameSurface.FocusState != FocusState.Unfocused;
        GameSurfaceBorder.BorderBrush = new SolidColorBrush(focused ? Colors.LimeGreen : Colors.Gray);
        GameSurfaceStatus.Text = focused
            ? "GAME SURFACE HAS FOCUS - arrows/WASD move (Shift = faster), Space jumps, Ctrl fires"
            : "Game surface does NOT have keyboard focus - click it to focus it";
    }

    #endregion

    #region The "game"

    private void MovePlayer(double dx, double dy)
    {
        var maxX = Math.Max(0, GameCanvas.ActualWidth - Player.Width);
        var maxY = Math.Max(0, GameCanvas.ActualHeight - Player.Height);
        Canvas.SetLeft(Player, Math.Clamp(Canvas.GetLeft(Player) + dx, 0, maxX));
        Canvas.SetTop(Player, Math.Clamp(Canvas.GetTop(Player) + dy, 0, maxY));
    }

    private void Fire()
    {
        _shotActive = true;
        Canvas.SetLeft(Shot, Canvas.GetLeft(Player) + Player.Width);
        Canvas.SetTop(Shot, Canvas.GetTop(Player) + (Player.Height - Shot.Height) / 2);
        Shot.Visibility = Visibility.Visible;
    }

    private void Jump()
    {
        _jumpFrames = 12;
        Player.Fill = new SolidColorBrush(Colors.Cyan);
    }

    private void ResetGame()
    {
        Canvas.SetLeft(Player, 120);
        Canvas.SetTop(Player, 96);
        _shotActive = false;
        Shot.Visibility = Visibility.Collapsed;
    }

    private void AdvanceGame()
    {
        if (_shotActive)
        {
            var x = Canvas.GetLeft(Shot) + ShotSpeed;
            if (x > GameCanvas.ActualWidth)
            {
                _shotActive = false;
                Shot.Visibility = Visibility.Collapsed;
            }
            else
            {
                Canvas.SetLeft(Shot, x);
            }
        }

        if (_jumpFrames > 0 && --_jumpFrames == 0)
        {
            Player.Fill = new SolidColorBrush(Color(0xFF4CAF50));
        }
    }

    private void UpdateHeldKeys()
    {
        var held = _downKeys[Listener.GameSurface];
        HeldKeysText.Text = held.Count == 0
            ? "Held keys (game surface): none"
            : "Held keys (game surface): " + string.Join(" + ", held.Select(KeyName));
    }

    #endregion

    #region Status, log and listener totals

    private void UpdateStatus()
    {
        var focused = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot);
        var focusDescription = Describe(focused);
        if (focusDescription != _lastFocusDescription)
        {
            _lastFocusDescription = focusDescription;
            UpdateGameSurfaceFocusVisuals();
        }

        var xamlModifiers = FormatModifiers(XamlModifiers());
        var nativeHeld = _nativeHeld.Count == 0 ? "none" : string.Join(" + ", _nativeHeld.Select(KeyName));
        StatusText.Text =
            $"Window: {_windowState}   |   Focused element: {focusDescription}   |   " +
            $"Access-key mode: {(AccessKeyManager.IsDisplayModeEnabled ? "ON" : "off")}   |   " +
            $"Modifiers (XAML key state): {(xamlModifiers.Length == 0 ? "none" : xamlModifiers)}   |   " +
            $"Held (native): {nativeHeld}";
    }

    private void BuildListenerRows()
    {
        for (var i = 0; i < ListenerInfo.Length; i++)
        {
            var (listener, tag, description) = ListenerInfo[i];
            ListenerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tagText = new TextBlock { Text = tag, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            ToolTipService.SetToolTip(tagText, description);
            var countText = new TextBlock { Text = "0", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right };
            var lastText = new TextBlock { Text = description, FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = 0.7 };

            Grid.SetRow(tagText, i);
            Grid.SetRow(countText, i);
            Grid.SetRow(lastText, i);
            Grid.SetColumn(countText, 1);
            Grid.SetColumn(lastText, 2);
            ListenerGrid.Children.Add(tagText);
            ListenerGrid.Children.Add(countText);
            ListenerGrid.Children.Add(lastText);

            _counts[listener] = 0;
            _countTexts[listener] = countText;
            _lastTexts[listener] = lastText;
        }
    }

    private void Log(Listener? listener, string text, bool countIt = true)
    {
        var tag = listener is { } l ? ListenerInfo.First(info => info.Listener == l).Tag : "";
        var line = $"{DateTime.Now:HH:mm:ss.fff}  {tag,-9}  {text}";

        _log.Insert(0, line);
        while (_log.Count > MaxLogEntries)
        {
            _log.RemoveAt(_log.Count - 1);
        }

        if (listener is { } counted && countIt)
        {
            _counts[counted]++;
            _countTexts[counted].Text = _counts[counted].ToString();
            _lastTexts[counted].Text = text;
            _lastTexts[counted].Opacity = 1;
        }

        Console.WriteLine($"[KeyPressTester] {line}");
        if (!string.IsNullOrEmpty(LogFilePath))
        {
            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch (Exception)
            {
                //The log file is a convenience only; never let it break the tester
            }
        }
    }

    private void ClearLog()
    {
        _log.Clear();
        _nativePressNumber = 0;
        foreach (var info in ListenerInfo)
        {
            _counts[info.Listener] = 0;
            _countTexts[info.Listener].Text = "0";
            _lastTexts[info.Listener].Text = info.Description;
            _lastTexts[info.Listener].Opacity = 0.7;
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            DispatcherQueue.TryEnqueue(() => action());
        }
    }

    #endregion

    #region Key naming helpers

    //Modifiers as the XAML layer tracks them (InputKeyboardSource), combined with the keys the
    //  native listener has seen held - so a modifier is still shown if only one layer saw it.
    private VirtualKeyModifiers CurrentModifiers()
    {
        var modifiers = XamlModifiers();
        if (_nativeHeld.Any(IsControlKey)) modifiers |= VirtualKeyModifiers.Control;
        if (_nativeHeld.Any(IsAltKey)) modifiers |= VirtualKeyModifiers.Menu;
        if (_nativeHeld.Any(IsShiftKey)) modifiers |= VirtualKeyModifiers.Shift;
        if (_nativeHeld.Any(IsWindowsKey)) modifiers |= VirtualKeyModifiers.Windows;
        return modifiers;
    }

    private static VirtualKeyModifiers XamlModifiers()
    {
        var modifiers = VirtualKeyModifiers.None;
        if (IsXamlKeyDown(VirtualKey.Control)) modifiers |= VirtualKeyModifiers.Control;
        if (IsXamlKeyDown(VirtualKey.Menu)) modifiers |= VirtualKeyModifiers.Menu;
        if (IsXamlKeyDown(VirtualKey.Shift)) modifiers |= VirtualKeyModifiers.Shift;
        if (IsXamlKeyDown(VirtualKey.LeftWindows) || IsXamlKeyDown(VirtualKey.RightWindows)) modifiers |= VirtualKeyModifiers.Windows;
        return modifiers;
    }

    private static bool IsXamlKeyDown(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;

    private bool IsShiftDown() => (CurrentModifiers() & VirtualKeyModifiers.Shift) != 0;

    private static bool IsControlKey(VirtualKey key) => key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl;

    private static bool IsAltKey(VirtualKey key) => key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu;

    private static bool IsShiftKey(VirtualKey key) => key is VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift;

    private static bool IsWindowsKey(VirtualKey key) => key is VirtualKey.LeftWindows or VirtualKey.RightWindows;

    private static string FormatModifiers(VirtualKeyModifiers modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & VirtualKeyModifiers.Control) != 0) parts.Add("Ctrl");
        if ((modifiers & VirtualKeyModifiers.Menu) != 0) parts.Add("Alt");
        if ((modifiers & VirtualKeyModifiers.Shift) != 0) parts.Add("Shift");
        if ((modifiers & VirtualKeyModifiers.Windows) != 0) parts.Add("Win");
        return string.Join("+", parts);
    }

    //"Ctrl+Alt+M" style text for a key press. A modifier key on its own is shown by name only
    //  (pressing Ctrl gives "Ctrl", not "Ctrl+Ctrl").
    private static string Combo(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (IsControlKey(key)) modifiers &= ~VirtualKeyModifiers.Control;
        if (IsAltKey(key)) modifiers &= ~VirtualKeyModifiers.Menu;
        if (IsShiftKey(key)) modifiers &= ~VirtualKeyModifiers.Shift;
        if (IsWindowsKey(key)) modifiers &= ~VirtualKeyModifiers.Windows;

        var prefix = FormatModifiers(modifiers);
        return prefix.Length == 0 ? KeyName(key) : $"{prefix}+{KeyName(key)}";
    }

    private static string KeyName(VirtualKey key) => key switch
    {
        >= VirtualKey.Number0 and <= VirtualKey.Number9 => ((int)(key - VirtualKey.Number0)).ToString(),
        >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9 => $"NumPad{(int)(key - VirtualKey.NumberPad0)}",
        VirtualKey.Control => "Ctrl",
        VirtualKey.LeftControl => "LeftCtrl",
        VirtualKey.RightControl => "RightCtrl",
        VirtualKey.Menu => "Alt",
        VirtualKey.LeftMenu => "LeftAlt",
        VirtualKey.RightMenu => "RightAlt",
        VirtualKey.LeftWindows => "LeftWin",
        VirtualKey.RightWindows => "RightWin",
        _ when Enum.IsDefined(key) => key.ToString(),
        _ => $"VK 0x{(int)key:X2}",
    };

    private static string Describe(object element)
    {
        if (element is null)
        {
            return "(nothing)";
        }

        var typeName = element.GetType().Name;
        return element is FrameworkElement { Name: { Length: > 0 } name } ? $"{name} ({typeName})" : typeName;
    }

    private static Windows.UI.Color Color(uint argb) =>
        Windows.UI.Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    #endregion
}
