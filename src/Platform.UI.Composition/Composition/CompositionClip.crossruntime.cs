#if !__NETSTD_REFERENCE__
#nullable enable
using System;
using System.Linq;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class CompositionClip
{
	private ICompositionClipPlatform? _platform;

	/// <summary>
	/// Gets the platform state of this clip (what applies it), created the first time the renderer needs it.
	/// </summary>
	internal ICompositionClipPlatform Platform => _platform ??= CompositionPlatformServices.Composition.CreateClipPlatform(this);

	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that this already handles TransformMatrix
	/// </summary>
	internal Rect? GetBounds(Visual visual)
	{
		if (GetBoundsCore(visual) is { } bounds)
		{
			return TransformMatrix.Transform(bounds);
		}

		return null;
	}

	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that implementors should not handle TransformMatrix. The result is already transformed by <see cref="GetBounds"/>.
	/// </summary>
	private protected virtual Rect? GetBoundsCore(Visual visual)
		=> null;
}
#endif
