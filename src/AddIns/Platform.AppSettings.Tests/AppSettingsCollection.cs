using Xunit;

namespace CodeBrix.Platform.AppSettings.Tests;

/// <summary>
/// Every suite in this project shares process-wide static state — the
/// <see cref="AppSettingsService"/> singleton that
/// <see cref="AppSettingProperty{T}"/> reads through, and the sink lists and
/// replay history inside <see cref="AppSettingLoggingService"/>, which every
/// store operation writes to. Running them in parallel would let one suite's
/// log lines land in another's sink, so they are serialized into one
/// collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class AppSettingsCollection
{
    /// <summary>The collection name every suite in this project joins.</summary>
    public const string Name = "AppSettings";
}
