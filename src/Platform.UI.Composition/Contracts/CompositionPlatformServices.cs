#nullable enable

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// Holds the composition service contracts, each resolved once (on first use) and then kept for the life of the
/// process. The composition tree calls through these fields; it never looks a contract up per operation.
/// </summary>
internal static class CompositionPlatformServices
{
	private static ICompositionPlatform? _composition;
	private static ICompositionGeometryPlatform? _geometry;

	/// <summary>
	/// Gets the platform that creates the per-object platform state of visuals, brushes, clips, shapes and surfaces.
	/// </summary>
	internal static ICompositionPlatform Composition => _composition ??= PlatformContract.Resolve<ICompositionPlatform>();

	/// <summary>
	/// Gets the platform that builds the geometry sources behind the composition geometries.
	/// </summary>
	internal static ICompositionGeometryPlatform Geometry => _geometry ??= PlatformContract.Resolve<ICompositionGeometryPlatform>();
}
