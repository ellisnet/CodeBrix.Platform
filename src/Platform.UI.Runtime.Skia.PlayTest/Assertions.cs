using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Entry point for retrying, web-first style assertions on locators.</summary>
public static class Assertions
{
    /// <summary>Starts an assertion on <paramref name="locator"/>. Every assertion retries until it
    /// holds or the timeout elapses, then fails with a screenshot and a visual-tree description.</summary>
    /// <param name="locator">The element(s) to assert on.</param>
    /// <returns>The assertion builder.</returns>
    public static LocatorAssertions Expect(Locator locator) => new(locator, false);
}

/// <summary>Retrying assertions on one <see cref="Locator"/>. Obtain it from <see cref="Assertions.Expect"/>.</summary>
public sealed class LocatorAssertions
{
    private readonly Locator _locator;
    private readonly bool _not;
    internal LocatorAssertions(Locator locator, bool not) { _locator = locator; _not = not; }

    /// <summary>The same assertions, negated.</summary>
    public LocatorAssertions Not => new(_locator, !_not);

    /// <summary>Waits until the single matching element is attached, visible and has a non-zero size.</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout, or the locator is ambiguous.</exception>
    public Task ToBeVisibleAsync(LocatorAssertionsToBeVisibleOptions? options = null) => Check(
        VisualTree.Visible, options?.Timeout, "visible", missingMatches: _not);

    /// <summary>Waits until no visible element matches (a missing element counts as hidden).</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToBeHiddenAsync(LocatorAssertionsToBeVisibleOptions? options = null) => Not.ToBeVisibleAsync(options);

    /// <summary>Waits until the element and all of its ancestor controls are enabled.</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToBeEnabledAsync(LocatorAssertionsToBeEnabledOptions? options = null) => Check(
        VisualTree.Enabled, options?.Timeout, "enabled");

    /// <summary>Waits until the element or one of its ancestor controls is disabled.</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToBeDisabledAsync(LocatorAssertionsToBeDisabledOptions? options = null) => Check(
        e => !VisualTree.Enabled(e), options?.Timeout, "disabled");

    /// <summary>Waits until a checkable control (CheckBox, RadioButton, ToggleButton, ToggleSwitch,
    /// toggle menu item or IToggleProvider peer) is checked.</summary>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout, or the element is not checkable.</exception>
    public Task ToBeCheckedAsync(LocatorOptions? options = null) => Check(VisualTree.Checked, options?.Timeout, "checked");

    /// <summary>Waits until exactly <paramref name="count"/> elements match.</summary>
    /// <param name="count">The expected number of matches.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToHaveCountAsync(int count, LocatorAssertionsToHaveCountOptions? options = null) => _locator.RetryAsync(
        () => (_locator.Resolve().Length == count) != _not, options?.Timeout, (_not ? "not " : "") + $"count {count}");

    /// <summary>Waits until the input value equals <paramref name="value"/>. Text boxes report LF line endings.</summary>
    /// <param name="value">The expected value.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout, or the element has no value.</exception>
    public Task ToHaveValueAsync(string value, LocatorAssertionsToHaveValueOptions? options = null) => Check(
        e => VisualTree.Value(e) == value, options?.Timeout, $"value '{value}'");

    /// <summary>Waits until the input value matches <paramref name="value"/>.</summary>
    /// <param name="value">The pattern the value must match.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout, or the element has no value.</exception>
    public Task ToHaveValueAsync(Regex value, LocatorAssertionsToHaveValueOptions? options = null) => Check(
        e => value.IsMatch(VisualTree.Value(e)), options?.Timeout, $"value /{value}/");

    /// <summary>Waits until the element's text equals <paramref name="text"/> after whitespace normalization.</summary>
    /// <param name="text">The expected text.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToHaveTextAsync(string text, LocatorAssertionsToHaveTextOptions? options = null) => Check(
        e => VisualTree.Normalize(VisualTree.Text(e)) == VisualTree.Normalize(text), options?.Timeout, $"text '{text}'");

    /// <summary>Waits until the element's text matches <paramref name="text"/>.</summary>
    /// <param name="text">The pattern the text must match.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToHaveTextAsync(Regex text, LocatorAssertionsToHaveTextOptions? options = null) => Check(
        e => text.IsMatch(VisualTree.Text(e)), options?.Timeout, $"text /{text}/");

    /// <summary>Waits until the element's normalized text contains <paramref name="text"/> (case-sensitive).</summary>
    /// <param name="text">The expected substring.</param>
    /// <param name="options">Optional timeout.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    /// <exception cref="PlayTestException">The condition did not hold before the timeout.</exception>
    public Task ToContainTextAsync(string text, LocatorAssertionsToContainTextOptions? options = null) => Check(
        e => VisualTree.Normalize(VisualTree.Text(e)).Contains(VisualTree.Normalize(text), StringComparison.Ordinal), options?.Timeout, $"text containing '{text}'");

    private Task Check(Func<UIElement, bool> predicate, float? timeout, string expected, bool missingMatches = false) =>
        _locator.RetryAsync(() =>
        {
            var element = _locator.Single();
            return element == null ? missingMatches : predicate(element) != _not;
        }, timeout, (_not ? "not " : "") + expected);
}
