using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace CodeBrix.Platform.PlayTest;

/// <summary>A lazy query for elements of the running application. Nothing is resolved when a locator is
/// created: every action and assertion re-resolves it on the UI thread and retries until the element is
/// ready or the timeout elapses. Single-element operations reject ambiguous matches (strict mode).</summary>
public sealed class Locator
{
    private readonly Func<IEnumerable<UIElement>> _query;
    internal Page Page { get; }
    internal PlayTestApplication App => Page.Application;
    internal string Description { get; }
    internal Locator(Page page, Func<IEnumerable<UIElement>> query, string description)
    { Page = page; _query = query; Description = description; }

    // Marks a strict-mode violation so retrying actions and assertions keep waiting for the
    // ambiguity to resolve, and report it (with the match count) only when the timeout elapses.
    internal const string StrictModeKey = "CodeBrix.Platform.PlayTest.StrictModeMatches";

    internal UIElement[] Resolve() => _query().Distinct().ToArray();
    internal UIElement? Single()
    {
        var matches = Resolve();
        if (matches.Length > 1) throw StrictModeViolation(Description, matches.Length);
        return matches.SingleOrDefault();
    }

    internal static PlayTestException StrictModeViolation(string description, int count)
    {
        var error = new PlayTestException($"Strict mode violation: {description} resolved to {count} elements. Use a unique name, test ID, or Nth().");
        error.Data[StrictModeKey] = count;
        return error;
    }

    internal static bool IsStrictModeViolation(Exception error) => error is PlayTestException && error.Data.Contains(StrictModeKey);

    /// <summary>The first match, in visual-tree order.</summary>
    public Locator First => Nth(0);

    /// <summary>The last match, in visual-tree order.</summary>
    public Locator Last => new(Page, () => Resolve().TakeLast(1), Description + ".Last");

    /// <summary>The match at a zero-based position, in visual-tree order.</summary>
    /// <param name="index">Zero-based position.</param>
    /// <returns>A locator for that one element.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public Locator Nth(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return new(Page, () => Resolve().Skip(index).Take(1), Description + $".Nth({index})");
    }
    /// <summary>Keeps only the matches whose text satisfies <paramref name="options"/>.</summary>
    /// <param name="options">Text filters; unset filters are ignored.</param>
    /// <returns>The narrowed locator.</returns>
    public Locator Filter(LocatorFilterOptions options) => new(Page, () => Resolve().Where(e =>
        (options.HasText == null || VisualTree.Matches(VisualTree.Text(e), options.HasText, false)) &&
        (options.HasTextRegex == null || options.HasTextRegex.IsMatch(VisualTree.Text(e)))), Description + ".Filter(...)");
    /// <summary>Finds elements with an ARIA role inside this locator's matches.</summary>
    /// <param name="role">The role to match.</param>
    /// <param name="options">Optional accessible-name filters.</param>
    /// <returns>A scoped locator.</returns>
    public Locator GetByRole(AriaRole role, PageGetByRoleOptions? options = null)
    {
        var child = Page.GetByRole(role, options);
        return new(Page, () => child.Resolve().Where(e => Resolve().Any(parent => e != parent && VisualTree.Within(e, parent))), Description + "." + child.Description);
    }
    /// <summary>Finds elements whose <c>AutomationProperties.AutomationId</c> equals <paramref name="id"/>
    /// inside this locator's matches.</summary>
    /// <param name="id">The automation ID.</param>
    /// <returns>A scoped locator.</returns>
    /// <param name="options">Optional hidden-element filter.</param>
    public Locator GetByTestId(string id, PageGetByTestIdOptions? options = null)
    {
        var child = Page.GetByTestId(id, options);
        return new(Page, () => child.Resolve().Where(e => Resolve().Any(parent => e != parent && VisualTree.Within(e, parent))), Description + "." + child.Description);
    }

    /// <summary>Finds elements of type <typeparamref name="T"/> inside this locator's matches.</summary>
    /// <typeparam name="T">An application- or add-in-owned element type.</typeparam>
    /// <param name="includeHidden">True also matches attached elements that are not visible.</param>
    /// <returns>A scoped locator.</returns>
    public Locator GetByType<T>(bool includeHidden = false) where T : UIElement => Scope(Page.GetByType<T>(includeHidden));

    /// <summary>Finds text blocks and string-content controls showing <paramref name="text"/> inside this
    /// locator's matches.</summary>
    /// <param name="text">The text to match.</param>
    /// <param name="options">Optional exact matching.</param>
    /// <returns>A scoped locator.</returns>
    public Locator GetByText(string text, PageGetByTextOptions? options = null) => Scope(Page.GetByText(text, options));

    /// <summary>Finds controls whose accessible name matches <paramref name="text"/> inside this locator's matches.</summary>
    /// <param name="text">The label to match.</param>
    /// <param name="options">Optional exact matching.</param>
    /// <returns>A scoped locator.</returns>
    public Locator GetByLabel(string text, PageGetByTextOptions? options = null) => Scope(Page.GetByLabel(text, options));
    private Locator Scope(Locator child) => new(Page,
        () => child.Resolve().Where(e => Resolve().Any(parent => e != parent && VisualTree.Within(e, parent))),
        Description + "." + child.Description);

    /// <summary>Counts the current matches once, without retrying.</summary>
    /// <returns>The number of matching elements.</returns>
    public Task<int> CountAsync() => App.EvaluateAsync(() => Resolve().Length);

    /// <summary>Reports once, without retrying, whether the single match is visible.</summary>
    /// <returns>False when nothing matches.</returns>
    /// <exception cref="PlayTestException">The locator matches more than one element.</exception>
    public Task<bool> IsVisibleAsync() => App.EvaluateAsync(() => VisualTree.Visible(Single()));

    /// <summary>Reports once, without retrying, whether the single match and its ancestors are enabled.</summary>
    /// <returns>False when nothing matches.</returns>
    /// <exception cref="PlayTestException">The locator matches more than one element.</exception>
    public Task<bool> IsEnabledAsync() => App.EvaluateAsync(() => Single() is { } e && VisualTree.Enabled(e));

    /// <summary>Reports once, without retrying, whether the single match or an ancestor is disabled.</summary>
    /// <returns>False when nothing matches.</returns>
    /// <exception cref="PlayTestException">The locator matches more than one element.</exception>
    public Task<bool> IsDisabledAsync() => App.EvaluateAsync(() => Single() is { } e && !VisualTree.Enabled(e));

    /// <summary>Waits for the element to attach, then reads its checked state.</summary>
    /// <returns>True when checked or on.</returns>
    /// <exception cref="PlayTestException">The element did not attach in time, or is not checkable.</exception>
    public Task<bool> IsCheckedAsync() => ReadAsync(VisualTree.Checked);

    /// <summary>Checks a CheckBox, RadioButton, ToggleButton, ToggleSwitch or toggle menu item with real
    /// pointer input, then waits for the checked state. Does nothing when already checked.</summary>
    /// <param name="options">Optional click position, button and timeout.</param>
    /// <returns>A task that completes once the element reports checked.</returns>
    public Task CheckAsync(LocatorClickOptions? options = null) => SetCheckedAsync(true, options);

    /// <summary>Unchecks a checkable control with real pointer input, then waits for the unchecked state.
    /// A selected RadioButton cannot be unchecked by clicking, so that call times out.</summary>
    /// <param name="options">Optional click position, button and timeout.</param>
    /// <returns>A task that completes once the element reports unchecked.</returns>
    public Task UncheckAsync(LocatorClickOptions? options = null) => SetCheckedAsync(false, options);

    /// <summary>Clicks the control only when its checked state differs from <paramref name="value"/>,
    /// then waits for the requested state.</summary>
    /// <param name="value">The required checked state.</param>
    /// <param name="options">Optional click position, button and timeout.</param>
    /// <returns>A task that completes once the element reports <paramref name="value"/>.</returns>
    /// <exception cref="PlayTestException">The element is not checkable, or the state was not reached in time.</exception>
    public async Task SetCheckedAsync(bool value, LocatorClickOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "SetChecked", Description);
        var alreadySet = false;
        ToggleMenuFlyoutItem? dismissingItem = null;
        await RetryAsync(() =>
        {
            var element = Single();
            if (element == null) return false;
            alreadySet = VisualTree.Checked(element) == value;
            dismissingItem = element as ToggleMenuFlyoutItem;
            return true;
        }, options?.Timeout, "checkable element attached").ConfigureAwait(false);
        if (alreadySet) return;
        await ClickAsync(options).ConfigureAwait(false);
        // A toggle menu item dismisses its flyout on activation. Its resulting checked
        // state still belongs to that item even though a visible-only locator loses it.
        await RetryAsync(() => (Single() ?? dismissingItem) is { } element && VisualTree.Checked(element) == value,
            options?.Timeout, value ? "checked" : "unchecked").ConfigureAwait(false);
    }

    /// <summary>Bring an attached element into its scrollable viewport. Virtualized items must
    /// first be materialized by scrolling their container.</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task ScrollIntoViewIfNeededAsync(LocatorOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "ScrollIntoView", Description);
        await RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element)) return false;
            element.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            return true;
        }, options?.Timeout, "visible element attached").ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }
    /// <summary>Waits for the element to attach, then reads its input value (text boxes report LF line endings).</summary>
    /// <returns>The value of a text box, password box, combo box or IValueProvider peer.</returns>
    /// <exception cref="PlayTestException">The element did not attach in time, or has no value.</exception>
    public async Task<string> InputValueAsync() => await ReadAsync(VisualTree.Value).ConfigureAwait(false);

    /// <summary>Waits for the element to attach, then reads its text (a TextBlock's or TextBox's text, a
    /// string Content, or the space-joined text of its descendant text blocks).</summary>
    /// <returns>The element's text.</returns>
    /// <exception cref="PlayTestException">The element did not attach in time.</exception>
    public async Task<string> InnerTextAsync() => await ReadAsync(VisualTree.Text).ConfigureAwait(false);

    /// <summary>Same as <see cref="InnerTextAsync"/>.</summary>
    /// <returns>The element's text.</returns>
    public Task<string> TextContentAsync() => InnerTextAsync();

    /// <summary>Reads the single match's bounds once, without retrying.</summary>
    /// <returns>The bounds in logical virtual-screen pixels, or null when the element is missing or not visible.</returns>
    /// <exception cref="PlayTestException">The locator matches more than one element.</exception>
    public Task<LocatorBoundingBoxResult?> BoundingBoxAsync() => App.EvaluateAsync<LocatorBoundingBoxResult?>(() =>
    {
        var element = Single();
        if (!VisualTree.Visible(element)) return null;
        var r = VisualTree.Bounds(element);
        return new LocatorBoundingBoxResult { X = (float)r.X, Y = (float)r.Y, Width = (float)r.Width, Height = (float)r.Height };
    });

    private async Task<T> ReadAsync<T>(Func<UIElement, T> read)
    {
        // RetryAsync either returns after a successful read or throws, so the slot is always filled.
        var value = new T[1];
        await RetryAsync(() => { var e = Single(); if (e == null) return false; value[0] = read(e); return true; }, null, "element attached").ConfigureAwait(false);
        return value[0];
    }

    /// <summary>Clicks the single match with real pointer input once it is visible, enabled, at the same
    /// bounds over two rendered frames, and actually receives the hit test at the click point.</summary>
    /// <param name="options">Optional position, button, click count (1-3) and timeout.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The button, click count or position is invalid.</exception>
    /// <exception cref="PlayTestException">The element was not ready in time, or the locator is ambiguous.</exception>
    public async Task ClickAsync(LocatorClickOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Click", Description);
        options ??= new();
        if (!Enum.IsDefined(options.Button)) throw new ArgumentOutOfRangeException(nameof(options.Button));
        if (options.ClickCount < 1 || options.ClickCount > 3) throw new ArgumentOutOfRangeException(nameof(options.ClickCount));
        // A matching control must remain at the same bounds over two rendered frames,
        // and the compositor's real hit test must reach it before pointer injection.
        Rect? previous = null;
        await RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element) || !VisualTree.Enabled(element)) { previous = null; return false; }
            var bounds = VisualTree.Bounds(options.Position == null ? VisualTree.ClickTarget(element) : element);
            var point = TargetPoint(bounds, options.Position);
            if (previous != bounds) { previous = bounds; return false; }
            if (point.X < 0 || point.X >= App.Width || point.Y < 0 || point.Y >= App.Height || !VisualTree.ReceivesEvents(element, point)) return false;
            App.Host.Input.Move(point.X, point.Y);
            for (var i = 0; i < options.ClickCount; i++)
            {
                try { App.Host.Input.Down(options.Button); }
                finally { App.Host.Input.Up(options.Button); }
            }
            return true;
        }, options.Timeout, "visible, enabled, stable element receiving pointer events", render: true).ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    /// <summary>Moves the pointer over the element, using the same hit-test and stability checks as a click.</summary>
    /// <param name="options">Optional position and timeout.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task HoverAsync(LocatorHoverOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Hover", Description);
        await PointerReadyAsync(options?.Position, options?.Timeout, point => App.Host.Input.Move(point.X, point.Y)).ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    /// <summary>Drags a control by a logical-pixel offset with real pointer capture and intermediate layouts.
    /// Suitable for splitters, pane dividers, sliders and selection gestures.</summary>
    /// <param name="deltaX">Horizontal distance in logical pixels.</param>
    /// <param name="deltaY">Vertical distance in logical pixels.</param>
    /// <param name="options">Optional start position, step count (1-1000) and timeout.</param>
    /// <returns>A task that completes after the release has rendered.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A delta is not finite, the step count is out of range,
    /// or the endpoint is outside the virtual screen.</exception>
    public async Task DragByAsync(float deltaX, float deltaY, LocatorDragOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Drag", Description);
        options ??= new();
        if (!float.IsFinite(deltaX) || !float.IsFinite(deltaY)) throw new ArgumentOutOfRangeException(nameof(deltaX));
        if (options.Steps < 1 || options.Steps > 1000) throw new ArgumentOutOfRangeException(nameof(options.Steps));
        Point origin = default;
        var pressed = false;
        try
        {
            await PointerReadyAsync(options.Position, options.Timeout, point =>
            {
                var end = new Point(point.X + deltaX, point.Y + deltaY);
                if (!InsideScreen(end)) throw new ArgumentOutOfRangeException(nameof(deltaX), "The drag endpoint must be within the virtual screen.");
                origin = point;
                App.Host.Input.Move(point.X, point.Y);
                pressed = true;
                App.Host.Input.Down();
            }).ConfigureAwait(false);
            for (var i = 1; i <= options.Steps; i++)
            {
                var fraction = (double)i / options.Steps;
                await App.EvaluateAsync(() => App.Host.Input.Move(origin.X + deltaX * fraction, origin.Y + deltaY * fraction)).ConfigureAwait(false);
                await App.Host.CaptureAsync().ConfigureAwait(false);
            }
        }
        finally { if (pressed) await App.EvaluateAsync(App.Host.Input.Up).ConfigureAwait(false); }
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    private Task PointerReadyAsync(LocatorPosition? position, float? timeout, Action<Point> action)
    {
        Rect? previous = null;
        return RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element) || !VisualTree.Enabled(element)) { previous = null; return false; }
            var bounds = VisualTree.Bounds(element);
            var point = TargetPoint(bounds, position);
            if (previous != bounds) { previous = bounds; return false; }
            if (!InsideScreen(point) || !VisualTree.ReceivesEvents(element, point)) return false;
            action(point);
            return true;
        }, timeout, "visible, enabled, stable element receiving pointer events", render: true);
    }

    private bool InsideScreen(Point point) => point.X >= 0 && point.X < App.Width && point.Y >= 0 && point.Y < App.Height;
    private static Point TargetPoint(Rect bounds, LocatorPosition? position)
    {
        if (position == null) return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || position.X < 0 || position.Y < 0 ||
            position.X >= bounds.Width || position.Y >= bounds.Height)
            throw new ArgumentOutOfRangeException(nameof(position), "The position must be inside the element's bounds.");
        return new Point(bounds.X + position.X, bounds.Y + position.Y);
    }

    /// <summary>Focuses an editable TextBox, PasswordBox or IValueProvider control and replaces its whole value,
    /// raising the control's normal change and binding behavior. Waits while the control is read-only.</summary>
    /// <param name="value">The new value.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="PlayTestException">The element cannot hold a value, or was not editable in time.</exception>
    public async Task FillAsync(string value, LocatorFillOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Fill", Description);
        ArgumentNullException.ThrowIfNull(value);
        await RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element) || !VisualTree.Enabled(element)) return false;
            switch (element)
            {
                case PasswordBox password:
                    password.Focus(FocusState.Programmatic);
                    password.Password = value;
                    return true;
                case TextBox box when !box.IsReadOnly:
                    box.Focus(FocusState.Programmatic);
                    box.Text = value;
                    box.Select(box.Text.Length, 0);
                    return true;
                case TextBox: return false;
                default:
                    if (VisualTree.ValueProvider(element) is not { } provider)
                        throw new PlayTestException($"{Description}: FillAsync requires a text box, password box, or automation Value provider.");
                    if (provider.IsReadOnly || !Focus(element)) return false;
                    provider.SetValue(value);
                    return true;
            }
        }, options?.Timeout, "visible, enabled, editable element").ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    /// <summary>Focuses the element (a control, or any element made focusable with <c>IsTabStop</c>), then
    /// presses a key or chord (see <see cref="Keyboard.PressAsync"/>).</summary>
    /// <param name="key">Key name or <c>+</c>-joined chord, for example <c>Control+z</c>.</param>
    /// <param name="options">Optional timeout for focusing and hold time (<see cref="LocatorPressOptions.Delay"/>).</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task PressAsync(string key, LocatorPressOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Press", Description);
        await RetryAsync(() => Focus(Single()), options?.Timeout, "focusable element").ConfigureAwait(false);
        await Page.Keyboard.PressAsync(key, new KeyboardPressOptions { Delay = options?.Delay }).ConfigureAwait(false);
    }

    /// <summary>Focuses the control and types through its normal key handlers, including editor completion and indentation.</summary>
    /// <param name="text">The characters to type; newlines and tabs are typed as Enter and Tab.</param>
    /// <param name="options">Optional timeout for focusing and wait between characters (<see cref="LocatorPressOptions.Delay"/>).</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task PressSequentiallyAsync(string text, LocatorPressOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Type", Description);
        ArgumentNullException.ThrowIfNull(text);
        await RetryAsync(() => Focus(Single()), options?.Timeout, "focusable element").ConfigureAwait(false);
        await Page.Keyboard.TypeAsync(text, new KeyboardTypeOptions { Delay = options?.Delay }).ConfigureAwait(false);
    }

    private static bool Focus(UIElement? element)
    {
        // Controls, and other elements made focusable with IsTabStop (a game surface, a canvas).
        if (element is not (Control or { IsTabStop: true }) || !VisualTree.Visible(element) || !VisualTree.Enabled(element)) return false;
        if (element.XamlRoot is not { } root) return false;
        // Composite editors own a focused child. Refocusing their outer control can
        // defer focus forwarding and lose the first key, or dismiss a completion popup.
        if (FocusManager.GetFocusedElement(root) is UIElement focused && VisualTree.Within(focused, element)) return true;
        if (element is Control && FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer) peer.SetFocus();
        else element.Focus(FocusState.Programmatic);
        return FocusManager.GetFocusedElement(root) is UIElement result && VisualTree.Within(result, element);
    }

    /// <summary>Captures the single matching element as a PNG cropped to its bounds (clipped to the virtual
    /// screen), once it is visible and at the same bounds over two rendered frames. An element that is
    /// partly outside its scrolling viewport is brought into view first.</summary>
    /// <param name="options">Optional file path, stable capture and timeout.</param>
    /// <returns>The PNG bytes.</returns>
    /// <exception cref="PlayTestException">The element was not ready in time, is outside the virtual screen,
    /// a stable capture kept changing, or the locator is ambiguous.</exception>
    public async Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Screenshot", Description);
        var limit = options?.Timeout ?? App.Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(options), "The timeout must be positive and finite.");
        await ScreenshotReadyAsync(options?.Timeout).ConfigureAwait(false);
        var bytes = await Screenshots.CaptureAsync(App, VisibleBounds, options?.Stable == true, Stopwatch.StartNew(), limit, Description).ConfigureAwait(false);
        if (options?.Path != null) await Screenshots.WriteAsync(options.Path, bytes).ConfigureAwait(false);
        return bytes;
    }

    // UI thread: the single match's bounds, or null when it is gone or hidden.
    internal Rect? VisibleBounds() => Single() is { } element && VisualTree.Visible(element) ? VisualTree.Bounds(element) : (Rect?)null;

    internal Task ScreenshotReadyAsync(float? timeout)
    {
        Rect? previous = null;
        var broughtIntoView = false;
        return RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element)) { previous = null; return false; }
            var bounds = VisualTree.Bounds(element);
            if (!broughtIntoView && (bounds.X < 0 || bounds.Y < 0 || bounds.Right > App.Width || bounds.Bottom > App.Height))
            {
                broughtIntoView = true;
                element.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
                previous = null;
                return false;
            }
            if (previous != bounds) { previous = bounds; return false; }
            return true;
        }, timeout, "visible element at stable bounds", render: true);
    }

    /// <summary>Selects the options whose value or label equals <paramref name="values"/> in a ComboBox,
    /// ListBox, ListView or GridView, without opening its drop-down or realizing its rows, raising the
    /// control's normal SelectionChanged event. A single-selection control selects the first matching option
    /// in item order; a multiple-selection control selects exactly the matching options.</summary>
    /// <param name="values">The option value or label (exact after whitespace normalization).</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>The values of the options selected afterwards.</returns>
    /// <exception cref="PlayTestException">The element is not a selector, does not allow selection, or no
    /// matching option appeared in time.</exception>
    public Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions? options = null) =>
        SelectOptionAsync(new[] { values }, options);

    /// <summary>Selects the options whose value or label equals one of <paramref name="values"/>; see
    /// <see cref="SelectOptionAsync(string, LocatorSelectOptionOptions)"/>. An empty list clears the selection.</summary>
    /// <param name="values">Option values or labels; each must match at least one option.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>The values of the options selected afterwards.</returns>
    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToArray();
        if (list.Any(v => v == null)) throw new ArgumentNullException(nameof(values));
        return SelectAsync(list.Select(v => (Func<SelectorOption, bool>)(o => o.Value == v || VisualTree.Matches(o.Label, v, true))).ToArray(),
            string.Join(", ", list.Select(v => $"'{v}'")), options);
    }

    /// <summary>Selects the option matching every property set on <paramref name="values"/> (value, label
    /// and/or index); see <see cref="SelectOptionAsync(string, LocatorSelectOptionOptions)"/>.</summary>
    /// <param name="values">The option to select.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>The values of the options selected afterwards.</returns>
    public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions? options = null) =>
        SelectOptionAsync(new[] { values }, options);

    /// <summary>Selects the options matching each of <paramref name="values"/>; see
    /// <see cref="SelectOptionAsync(string, LocatorSelectOptionOptions)"/>. An empty list clears the selection.</summary>
    /// <param name="values">The options to select; each must match at least one option.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>The values of the options selected afterwards.</returns>
    /// <exception cref="ArgumentException">An entry sets none of Value, Label and Index.</exception>
    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, LocatorSelectOptionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToArray();
        if (list.Any(v => v == null || (v.Value == null && v.Label == null && v.Index == null)))
            throw new ArgumentException("Each SelectOptionValue must set Value, Label or Index.", nameof(values));
        return SelectAsync(list.Select(v => (Func<SelectorOption, bool>)(o => SelectorOptions.Matches(o, v))).ToArray(),
            string.Join(", ", list.Select(SelectorOptions.Describe)), options);
    }

    private async Task<IReadOnlyList<string>> SelectAsync(Func<SelectorOption, bool>[] wanted, string requested, LocatorSelectOptionOptions? options)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "SelectOption", Description);
        IReadOnlyList<string> selected = Array.Empty<string>();
        await RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element) || !VisualTree.Enabled(element)) return false;
            if (element is not Selector selector)
                throw new PlayTestException($"{Description}: SelectOptionAsync requires a ComboBox, ListBox, ListView or GridView.");
            var all = SelectorOptions.Read(selector);
            // Like a browser, wait until every requested option is present.
            if (!wanted.All(matches => all.Any(matches))) return false;
            var chosen = all.Where(option => wanted.Any(matches => matches(option))).ToArray();
            SelectorOptions.Apply(selector, chosen, Description);
            selected = SelectorOptions.Read(selector).Where(option => option.Selected).Select(option => option.Value).ToArray();
            return true;
        }, options?.Timeout, $"visible, enabled selector with options {requested}").ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
        return selected;
    }

    internal async Task RetryAsync(Func<bool> check, float? timeout, string expectation, bool render = false)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "WaitForElement", Description + "; " + expectation);
        var limit = timeout ?? App.Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var elapsed = Stopwatch.StartNew();
        Exception? ambiguous = null;
        do
        {
            if (render) await App.Host.CaptureAsync().ConfigureAwait(false);
            try
            {
                if (await App.EvaluateAsync(check).ConfigureAwait(false)) return;
                ambiguous = null;
            }
            // Two matches can be momentary (one dialog replacing another), so keep waiting.
            catch (Exception error) when (IsStrictModeViolation(error)) { ambiguous = error; }
            await Task.Delay(25).ConfigureAwait(false);
        } while (elapsed.Elapsed.TotalMilliseconds < limit);
        if (ambiguous != null)
            throw await App.FailureAsync($"{ambiguous.Message} It was still ambiguous when the {limit}ms timeout elapsed; expected {expectation}.").ConfigureAwait(false);
        throw await App.FailureAsync($"Timeout {limit}ms: {Description}; expected {expectation}.").ConfigureAwait(false);
    }
}
