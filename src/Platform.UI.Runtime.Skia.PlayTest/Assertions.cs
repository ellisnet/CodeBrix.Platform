using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest;

public static class Assertions
{
    public static LocatorAssertions Expect(Locator locator) => new(locator, false);
}

public sealed class LocatorAssertions
{
    private readonly Locator _locator;
    private readonly bool _not;
    internal LocatorAssertions(Locator locator, bool not) { _locator = locator; _not = not; }
    public LocatorAssertions Not => new(_locator, !_not);

    public Task ToBeVisibleAsync(LocatorAssertionsToBeVisibleOptions options = null) => Check(
        VisualTree.Visible, options?.Timeout, "visible", missingMatches: _not);
    public Task ToBeHiddenAsync(LocatorAssertionsToBeVisibleOptions options = null) => Not.ToBeVisibleAsync(options);
    public Task ToBeEnabledAsync(LocatorAssertionsToBeEnabledOptions options = null) => Check(
        VisualTree.Enabled, options?.Timeout, "enabled");
    public Task ToBeDisabledAsync(LocatorAssertionsToBeDisabledOptions options = null) => Check(
        e => !VisualTree.Enabled(e), options?.Timeout, "disabled");
    public Task ToHaveCountAsync(int count, LocatorAssertionsToHaveCountOptions options = null) => _locator.RetryAsync(
        () => (_locator.Resolve().Length == count) != _not, options?.Timeout, (_not ? "not " : "") + $"count {count}");
    public Task ToHaveValueAsync(string value, LocatorAssertionsToHaveValueOptions options = null) => Check(
        e => VisualTree.Value(e) == value, options?.Timeout, $"value '{value}'");
    public Task ToHaveValueAsync(Regex value, LocatorAssertionsToHaveValueOptions options = null) => Check(
        e => value.IsMatch(VisualTree.Value(e)), options?.Timeout, $"value /{value}/");
    public Task ToHaveTextAsync(string text, LocatorAssertionsToHaveTextOptions options = null) => Check(
        e => VisualTree.Normalize(VisualTree.Text(e)) == VisualTree.Normalize(text), options?.Timeout, $"text '{text}'");
    public Task ToHaveTextAsync(Regex text, LocatorAssertionsToHaveTextOptions options = null) => Check(
        e => text.IsMatch(VisualTree.Text(e)), options?.Timeout, $"text /{text}/");
    public Task ToContainTextAsync(string text, LocatorAssertionsToContainTextOptions options = null) => Check(
        e => VisualTree.Normalize(VisualTree.Text(e)).Contains(VisualTree.Normalize(text), StringComparison.Ordinal), options?.Timeout, $"text containing '{text}'");

    private Task Check(Func<UIElement, bool> predicate, float? timeout, string expected, bool missingMatches = false) =>
        _locator.RetryAsync(() =>
        {
            var element = _locator.Single();
            return element == null ? missingMatches : predicate(element) != _not;
        }, timeout, (_not ? "not " : "") + expected);
}
