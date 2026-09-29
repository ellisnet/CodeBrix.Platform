using System.Text.RegularExpressions;

namespace CodeBrix.Platform.PlayTest;

// Names and call shapes intentionally follow Microsoft Playwright's C# API.
public class LocatorOptions
{
    public float? Timeout { get; set; }
}

public sealed class PageGetByRoleOptions
{
    public string Name { get; set; }
    public Regex NameRegex { get; set; }
    public bool? Exact { get; set; }
    public bool? IncludeHidden { get; set; }
}

public sealed class PageGetByTextOptions
{
    public bool? Exact { get; set; }
}

public sealed class LocatorFilterOptions
{
    public string HasText { get; set; }
    public Regex HasTextRegex { get; set; }
}

public sealed class LocatorClickOptions : LocatorOptions { }
public sealed class LocatorFillOptions : LocatorOptions { }
public sealed class LocatorPressOptions : LocatorOptions { }
public sealed class LocatorAssertionsToBeVisibleOptions : LocatorOptions { }
public sealed class LocatorAssertionsToBeEnabledOptions : LocatorOptions { }
public sealed class LocatorAssertionsToBeDisabledOptions : LocatorOptions { }
public sealed class LocatorAssertionsToHaveValueOptions : LocatorOptions { }
public sealed class LocatorAssertionsToHaveTextOptions : LocatorOptions { }
public sealed class LocatorAssertionsToContainTextOptions : LocatorOptions { }
public sealed class LocatorAssertionsToHaveCountOptions : LocatorOptions { }
public sealed class PageScreenshotOptions
{
    public string Path { get; set; }
}

public sealed class LocatorBoundingBoxResult
{
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public float Width { get; internal set; }
    public float Height { get; internal set; }
}
