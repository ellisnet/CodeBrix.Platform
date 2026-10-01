using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.PlayTest;

public sealed class Locator
{
    private readonly Func<IEnumerable<UIElement>> _query;
    internal Page Page { get; }
    internal PlayTestApplication App => Page.Application;
    internal string Description { get; }
    internal Locator(Page page, Func<IEnumerable<UIElement>> query, string description)
    { Page = page; _query = query; Description = description; }

    internal UIElement[] Resolve() => _query().Distinct().ToArray();
    internal UIElement Single()
    {
        var matches = Resolve();
        if (matches.Length > 1) throw new PlayTestException($"Strict mode violation: {Description} resolved to {matches.Length} elements. Use a unique name, test ID, or Nth().");
        return matches.SingleOrDefault();
    }

    public Locator First => Nth(0);
    public Locator Last => new(Page, () => Resolve().TakeLast(1), Description + ".Last");
    public Locator Nth(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return new(Page, () => Resolve().Skip(index).Take(1), Description + $".Nth({index})");
    }
    public Locator Filter(LocatorFilterOptions options) => new(Page, () => Resolve().Where(e =>
        (options.HasText == null || VisualTree.Matches(VisualTree.Text(e), options.HasText, false)) &&
        (options.HasTextRegex == null || options.HasTextRegex.IsMatch(VisualTree.Text(e)))), Description + ".Filter(...)");
    public Locator GetByRole(AriaRole role, PageGetByRoleOptions options = null)
    {
        var child = Page.GetByRole(role, options);
        return new(Page, () => child.Resolve().Where(e => Resolve().Any(parent => e != parent && VisualTree.Within(e, parent))), Description + "." + child.Description);
    }
    public Locator GetByTestId(string id)
    {
        var child = Page.GetByTestId(id);
        return new(Page, () => child.Resolve().Where(e => Resolve().Any(parent => e != parent && VisualTree.Within(e, parent))), Description + "." + child.Description);
    }

    public Task<int> CountAsync() => App.EvaluateAsync(() => Resolve().Length);
    public Task<bool> IsVisibleAsync() => App.EvaluateAsync(() => VisualTree.Visible(Single()));
    public Task<bool> IsEnabledAsync() => App.EvaluateAsync(() => Single() is { } e && VisualTree.Enabled(e));
    public Task<bool> IsDisabledAsync() => App.EvaluateAsync(() => Single() is { } e && !VisualTree.Enabled(e));
    public Task<bool> IsCheckedAsync() => ReadAsync(VisualTree.Checked);
    public Task CheckAsync(LocatorClickOptions options = null) => SetCheckedAsync(true, options);
    public Task UncheckAsync(LocatorClickOptions options = null) => SetCheckedAsync(false, options);

    public async Task SetCheckedAsync(bool value, LocatorClickOptions options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "SetChecked", Description);
        var alreadySet = false;
        await RetryAsync(() =>
        {
            var element = Single();
            if (element == null) return false;
            alreadySet = VisualTree.Checked(element) == value;
            return true;
        }, options?.Timeout, "checkable element attached").ConfigureAwait(false);
        if (alreadySet) return;
        await ClickAsync(options).ConfigureAwait(false);
        await RetryAsync(() => Single() is { } element && VisualTree.Checked(element) == value,
            options?.Timeout, value ? "checked" : "unchecked").ConfigureAwait(false);
    }

    /// <summary>Bring an attached element into its scrollable viewport. Virtualized items must
    /// first be materialized by scrolling their container.</summary>
    public async Task ScrollIntoViewIfNeededAsync(LocatorOptions options = null)
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
    public async Task<string> InputValueAsync() => await ReadAsync(VisualTree.Value).ConfigureAwait(false);
    public async Task<string> InnerTextAsync() => await ReadAsync(VisualTree.Text).ConfigureAwait(false);
    public Task<string> TextContentAsync() => InnerTextAsync();
    public Task<LocatorBoundingBoxResult> BoundingBoxAsync() => App.EvaluateAsync(() =>
    {
        var element = Single();
        if (!VisualTree.Visible(element)) return null;
        var r = VisualTree.Bounds(element);
        return new LocatorBoundingBoxResult { X = (float)r.X, Y = (float)r.Y, Width = (float)r.Width, Height = (float)r.Height };
    });

    private async Task<T> ReadAsync<T>(Func<UIElement, T> read)
    {
        T value = default;
        await RetryAsync(() => { var e = Single(); if (e == null) return false; value = read(e); return true; }, null, "element attached").ConfigureAwait(false);
        return value;
    }

    public async Task ClickAsync(LocatorClickOptions options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Click", Description);
        // A matching control must remain at the same bounds over two rendered frames,
        // and the compositor's real hit test must reach it before pointer injection.
        Rect? previous = null;
        await RetryAsync(() =>
        {
            var element = Single();
            if (!VisualTree.Visible(element) || !VisualTree.Enabled(element)) { previous = null; return false; }
            var bounds = VisualTree.Bounds(VisualTree.ClickTarget(element));
            var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            if (previous != bounds) { previous = bounds; return false; }
            if (point.X < 0 || point.X >= App.Width || point.Y < 0 || point.Y >= App.Height || !VisualTree.ReceivesEvents(element, point)) return false;
            App.Host.Input.Move(point.X, point.Y);
            App.Host.Input.Down();
            App.Host.Input.Up();
            return true;
        }, options?.Timeout, "visible, enabled, stable element receiving pointer events", render: true).ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    public async Task FillAsync(string value, LocatorFillOptions options = null)
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
                default: throw new PlayTestException($"{Description}: FillAsync requires a text box or password box.");
            }
        }, options?.Timeout, "visible, enabled, editable element").ConfigureAwait(false);
        await App.Host.CaptureAsync().ConfigureAwait(false);
        await App.SlowAsync().ConfigureAwait(false);
    }

    public async Task PressAsync(string key, LocatorPressOptions options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "Press", Description);
        await RetryAsync(() => Single() is Control control && VisualTree.Visible(control) &&
            VisualTree.Enabled(control) && control.Focus(FocusState.Programmatic), options?.Timeout, "focusable element").ConfigureAwait(false);
        await Page.Keyboard.PressAsync(key).ConfigureAwait(false);
    }

    internal async Task RetryAsync(Func<bool> check, float? timeout, string expectation, bool render = false)
    {
        await using var step = Recording.PlayTestRecording.Step(App, "WaitForElement", Description + "; " + expectation);
        var limit = timeout ?? App.Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var elapsed = Stopwatch.StartNew();
        do
        {
            if (render) await App.Host.CaptureAsync().ConfigureAwait(false);
            if (await App.EvaluateAsync(check).ConfigureAwait(false)) return;
            await Task.Delay(25).ConfigureAwait(false);
        } while (elapsed.Elapsed.TotalMilliseconds < limit);
        throw await App.FailureAsync($"Timeout {limit}ms: {Description}; expected {expectation}.").ConfigureAwait(false);
    }
}
