using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;

namespace Windows.UI.ViewManagement
{
	public enum ApplicationViewMode
	{
		Default,

		[NotImplemented]
		CompactOverlay,

#if true
		[NotImplemented]
#endif
		Spanning,
	}
}
