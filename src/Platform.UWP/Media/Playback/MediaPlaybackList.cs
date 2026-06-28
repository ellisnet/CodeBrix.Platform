#if __SKIA__
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackList : IMediaPlaybackList, IMediaPlaybackSource
	{
		public IObservableVector<MediaPlaybackItem> Items { get; } = new ObservableVector<MediaPlaybackItem>();
	}
}
#endif
