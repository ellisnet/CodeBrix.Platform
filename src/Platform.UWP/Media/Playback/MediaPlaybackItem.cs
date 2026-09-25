#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
using Windows.Media.Core;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackItem : IMediaPlaybackSource
	{
		public MediaSource Source { get; }

		public MediaPlaybackItem(MediaSource source)
		{
			Source = source;
		}
	}
}
#endif
