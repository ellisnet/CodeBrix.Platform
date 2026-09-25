#if !__NETSTD_REFERENCE__
#nullable enable

using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition //Was previously: Uno.UI.Composition
{
	/// <summary>
	/// Implemented by composition surfaces defined outside this assembly (a XAML <c>LoadedImageSurface</c>) that draw
	/// from a <see cref="Microsoft.UI.Composition.PlatformCompositionSurface"/>.
	/// </summary>
	internal interface IPlatformCompositionSurfaceProvider
	{
		/// <summary>
		/// Gets the surface drawn from, or <see langword="null"/> while nothing is loaded.
		/// </summary>
		PlatformCompositionSurface? PlatformCompositionSurface { get; }
	}
}
#endif
