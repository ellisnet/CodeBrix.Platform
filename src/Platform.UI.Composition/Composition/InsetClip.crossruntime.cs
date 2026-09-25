#if !__NETSTD_REFERENCE__
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class InsetClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		return new Rect(
			x: LeftInset,
			y: TopInset,
			width: visual.Size.X - LeftInset - RightInset,
			height: visual.Size.Y - TopInset - BottomInset);
	}
}
#endif
