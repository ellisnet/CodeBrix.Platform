using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

/// <summary>
/// Puts every test class that draws with Skia into ONE xUnit collection, so they run one after
/// another rather than side by side.
/// </summary>
/// <remarks>
/// <para>
/// The suite shares two process-wide things: the headless graphics context
/// (<see cref="HeadlessGraphicsContext"/>, one EGL context living on one thread) and Skia's own
/// global state. xUnit runs different collections in PARALLEL by default, and with three or more
/// drawing classes in flight at once the graphics-device comparison in
/// <see cref="LutShaderInterpolationTests"/> intermittently read back a picture that was not the one
/// it had just drawn - worst channel difference 255 instead of 1, in roughly five runs in eight.
/// Measured 2026-08-30: with the drawing classes serialized the same suite is clean, run after run.
/// </para>
/// <para>
/// This is not a tolerance being relaxed: every assertion is exactly what it was. It is the standard
/// xUnit answer to a shared resource. The classes that touch no Skia surface - the element's rules,
/// the source resolver, the failure explanation, the letterbox geometry - are not in this collection
/// and still run in parallel with it.
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
public sealed class GraphicsSerializedCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Skia drawing";
}
