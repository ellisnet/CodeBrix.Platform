using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Requires an orientation for a test method. A fixture must apply this metadata before page setup.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class PlayTestOrientationAttribute : Attribute
{
    /// <summary>Trait key for an individual theory row; takes precedence over the method attribute.</summary>
    public const string CaseTraitName = "PlayTestOrientation";
    public ScreenOrientation Orientation { get; }

    public PlayTestOrientationAttribute(ScreenOrientation orientation)
    {
        if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
        Orientation = orientation;
    }

    /// <summary>Runner-neutral fixture adapter: row metadata, method metadata, or null to inherit the fixture.</summary>
    public static ScreenOrientation? Resolve(MethodInfo method, IEnumerable<string> caseOrientations = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        var orientations = caseOrientations?.Select(value => OrientationPreference.Parse(value, CaseTraitName)).Distinct().ToArray();
        if (orientations?.Length > 1)
            throw new ArgumentException("A test case cannot require both Landscape and Portrait.", nameof(caseOrientations));
        if (orientations?.Length == 1) return orientations[0];
        return method.GetCustomAttribute<PlayTestOrientationAttribute>(inherit: true)?.Orientation;
    }
}

internal static class OrientationPreference
{
    internal static ScreenOrientation Parse(string value, string source)
    {
        if (string.Equals(value, "landscape", StringComparison.OrdinalIgnoreCase)) return ScreenOrientation.Landscape;
        if (string.Equals(value, "portrait", StringComparison.OrdinalIgnoreCase)) return ScreenOrientation.Portrait;
        throw new ArgumentException($"{source} must be Landscape or Portrait; received '{value}'.");
    }
}
