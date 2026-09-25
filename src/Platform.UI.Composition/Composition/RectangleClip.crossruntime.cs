#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class RectangleClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: Left,
			y: Top,
			width: Right - Left,
			height: Bottom - Top);
	}
}
#endif
