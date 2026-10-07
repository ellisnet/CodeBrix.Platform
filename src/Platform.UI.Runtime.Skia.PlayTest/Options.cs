using System.Text.RegularExpressions;

namespace CodeBrix.Platform.PlayTest;

// Names and call shapes intentionally follow Microsoft Playwright's C# API.

/// <summary>Options shared by every locator action and assertion.</summary>
public class LocatorOptions
{
    /// <summary>Retry limit in milliseconds for this call. Null uses the application timeout
    /// (<see cref="PlayTestOptions.Timeout"/>, or the value set by <see cref="Page.SetDefaultTimeout"/>).</summary>
    public float? Timeout { get; set; }
}

/// <summary>Filters for <see cref="Page.GetByRole"/> and <see cref="Locator.GetByRole"/>.</summary>
public sealed class PageGetByRoleOptions
{
    /// <summary>Accessible name to match: a case-insensitive substring, or the whole
    /// whitespace-normalized name when <see cref="Exact"/> is true.</summary>
    public string? Name { get; set; }
    /// <summary>Regular expression the accessible name must match.</summary>
    public Regex? NameRegex { get; set; }
    /// <summary>True requires <see cref="Name"/> to equal the accessible name (case-sensitive,
    /// whitespace-normalized) instead of being contained in it.</summary>
    public bool? Exact { get; set; }
    /// <summary>True also matches attached elements that are collapsed, have no size, or are kept
    /// off-screen for reuse by an <c>ItemsRepeater</c>.</summary>
    public bool? IncludeHidden { get; set; }
}

/// <summary>Options for <see cref="Page.GetByText(string, PageGetByTextOptions)"/> and
/// <see cref="Page.GetByLabel"/>.</summary>
public sealed class PageGetByTextOptions
{
    /// <summary>True requires the whole whitespace-normalized text to match, case-sensitively;
    /// otherwise a case-insensitive substring match is used.</summary>
    public bool? Exact { get; set; }
    /// <summary>True also matches attached elements that are collapsed, have no size, or are kept
    /// off-screen for reuse by an <c>ItemsRepeater</c>.</summary>
    public bool? IncludeHidden { get; set; }
}

/// <summary>Options for <see cref="Page.GetByTestId"/> and <see cref="Locator.GetByTestId"/>.</summary>
public sealed class PageGetByTestIdOptions
{
    /// <summary>True also matches attached elements that are collapsed, have no size, or are kept
    /// off-screen for reuse by an <c>ItemsRepeater</c>.</summary>
    public bool? IncludeHidden { get; set; }
}

/// <summary>Narrows a locator with <see cref="Locator.Filter"/>.</summary>
public sealed class LocatorFilterOptions
{
    /// <summary>Keeps elements whose text contains this value (case-insensitive, whitespace-normalized).</summary>
    public string? HasText { get; set; }
    /// <summary>Keeps elements whose text matches this regular expression.</summary>
    public Regex? HasTextRegex { get; set; }
}

/// <summary>The mouse button a click presses.</summary>
public enum MouseButton
{
    /// <summary>The primary (left) button.</summary>
    Left,
    /// <summary>The secondary (right) button, which normally opens a context menu.</summary>
    Right,
    /// <summary>The middle button.</summary>
    Middle,
}

/// <summary>Logical-pixel position relative to the element's top-left corner.</summary>
public sealed class LocatorPosition
{
    /// <summary>Horizontal offset from the element's left edge; must be inside the element.</summary>
    public float X { get; set; }
    /// <summary>Vertical offset from the element's top edge; must be inside the element.</summary>
    public float Y { get; set; }
}

/// <summary>Options for <see cref="Locator.ClickAsync"/>, <see cref="Locator.CheckAsync"/>,
/// <see cref="Locator.UncheckAsync"/> and <see cref="Locator.SetCheckedAsync"/>.</summary>
public sealed class LocatorClickOptions : LocatorOptions
{
    /// <summary>Point to click inside the element. Null clicks the element's center.</summary>
    public LocatorPosition? Position { get; set; }
    /// <summary>The button to press; <see cref="MouseButton.Left"/> by default.</summary>
    public MouseButton Button { get; set; }
    /// <summary>Number of press/release pairs: 1 (default), 2 for a double click, or 3.</summary>
    public int ClickCount { get; set; } = 1;
}

/// <summary>Options for <see cref="Locator.HoverAsync"/>.</summary>
public sealed class LocatorHoverOptions : LocatorOptions
{
    /// <summary>Point to hover inside the element. Null uses the element's center.</summary>
    public LocatorPosition? Position { get; set; }
}

/// <summary>Options for <see cref="Locator.DragByAsync"/>.</summary>
public sealed class LocatorDragOptions : LocatorOptions
{
    /// <summary>Point inside the element where the drag starts. Null uses the element's center.</summary>
    public LocatorPosition? Position { get; set; }
    /// <summary>Number of intermediate pointer moves, from 1 to 1000; 10 by default.</summary>
    public int Steps { get; set; } = 10;
}

/// <summary>Options for <see cref="Locator.FillAsync"/>.</summary>
public sealed class LocatorFillOptions : LocatorOptions { }

/// <summary>Options for <see cref="Locator.PressAsync"/> and <see cref="Locator.PressSequentiallyAsync"/>.</summary>
public sealed class LocatorPressOptions : LocatorOptions
{
    /// <summary>Milliseconds. For <see cref="Locator.PressAsync"/>, how long the key or chord is held between
    /// key down and key up (see <see cref="KeyboardPressOptions.Delay"/>); for
    /// <see cref="Locator.PressSequentiallyAsync"/>, the wait between characters. Zero or null: no wait.</summary>
    public float? Delay { get; set; }
}

/// <summary>Options for <see cref="Keyboard.PressAsync"/>.</summary>
public sealed class KeyboardPressOptions
{
    /// <summary>Milliseconds to hold the key or chord between key down and key up. While it is held the
    /// application keeps running and rendering, so code that samples key state once per frame or game
    /// cycle sees the key down. Zero or null sends key down and key up together.</summary>
    public float? Delay { get; set; }
}

/// <summary>Options for <see cref="Keyboard.TypeAsync"/>.</summary>
public sealed class KeyboardTypeOptions
{
    /// <summary>Milliseconds to wait between characters. Zero or null: no wait.</summary>
    public float? Delay { get; set; }
}

/// <summary>One option for <see cref="Locator.SelectOptionAsync(SelectOptionValue, LocatorSelectOptionOptions)"/>.
/// Every property that is set must match.</summary>
public sealed class SelectOptionValue
{
    /// <summary>The option's value: the <c>SelectedValuePath</c> member of its item when the control sets
    /// one, otherwise the same as its label.</summary>
    public string? Value { get; set; }
    /// <summary>The option's label (exact after whitespace normalization): the <c>DisplayMemberPath</c>
    /// member, a string item, a string <c>Content</c>, the realized row's text, or the item's
    /// <c>ToString()</c>.</summary>
    public string? Label { get; set; }
    /// <summary>The option's zero-based position in the control's items.</summary>
    public int? Index { get; set; }
}

/// <summary>Options for the <c>Locator.SelectOptionAsync</c> overloads.</summary>
public sealed class LocatorSelectOptionOptions : LocatorOptions { }

/// <summary>Options for <see cref="Locator.ScreenshotAsync"/>.</summary>
public sealed class LocatorScreenshotOptions : LocatorOptions
{
    /// <summary>When set, the PNG is also written to this file (its folder is created if needed).</summary>
    public string? Path { get; set; }
    /// <summary>True waits until two consecutive captures of the element are identical (animations and
    /// transitions have settled) before returning, failing when that does not happen within the timeout.</summary>
    public bool Stable { get; set; }
}

/// <summary>Options for <see cref="LocatorAssertions.ToBeVisibleAsync"/> and <see cref="LocatorAssertions.ToBeHiddenAsync"/>.</summary>
public sealed class LocatorAssertionsToBeVisibleOptions : LocatorOptions { }

/// <summary>Options for <see cref="LocatorAssertions.ToBeEnabledAsync"/>.</summary>
public sealed class LocatorAssertionsToBeEnabledOptions : LocatorOptions { }

/// <summary>Options for <see cref="LocatorAssertions.ToBeDisabledAsync"/>.</summary>
public sealed class LocatorAssertionsToBeDisabledOptions : LocatorOptions { }

/// <summary>Options for the <see cref="LocatorAssertions.ToHaveValueAsync(string, LocatorAssertionsToHaveValueOptions)"/> overloads.</summary>
public sealed class LocatorAssertionsToHaveValueOptions : LocatorOptions { }

/// <summary>Options for the <see cref="LocatorAssertions.ToHaveTextAsync(string, LocatorAssertionsToHaveTextOptions)"/> overloads.</summary>
public sealed class LocatorAssertionsToHaveTextOptions : LocatorOptions { }

/// <summary>Options for <see cref="LocatorAssertions.ToContainTextAsync"/>.</summary>
public sealed class LocatorAssertionsToContainTextOptions : LocatorOptions { }

/// <summary>Options for <see cref="LocatorAssertions.ToHaveCountAsync"/>.</summary>
public sealed class LocatorAssertionsToHaveCountOptions : LocatorOptions { }

/// <summary>Options for <see cref="Page.ScreenshotAsync"/>.</summary>
public sealed class PageScreenshotOptions
{
    /// <summary>When set, the PNG is also written to this file (its folder is created if needed).</summary>
    public string? Path { get; set; }
    /// <summary>True waits until two consecutive captures are identical (animations and transitions have
    /// settled) before returning, failing when that does not happen within <see cref="Timeout"/>.</summary>
    public bool Stable { get; set; }
    /// <summary>Limit in milliseconds for a <see cref="Stable"/> capture. Null uses the application timeout.</summary>
    public float? Timeout { get; set; }
}

/// <summary>An element's bounds on the virtual screen, in logical pixels, from <see cref="Locator.BoundingBoxAsync"/>.</summary>
public sealed class LocatorBoundingBoxResult
{
    /// <summary>Left edge relative to the virtual screen.</summary>
    public float X { get; internal set; }
    /// <summary>Top edge relative to the virtual screen.</summary>
    public float Y { get; internal set; }
    /// <summary>Rendered width.</summary>
    public float Width { get; internal set; }
    /// <summary>Rendered height.</summary>
    public float Height { get; internal set; }
}
