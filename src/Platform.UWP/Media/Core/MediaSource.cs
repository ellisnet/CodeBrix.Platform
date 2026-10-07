using System;
using Windows.Media.Playback;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		private bool _isDisposed;

		public Uri Uri { get; private set; }

		/// <summary>
		/// Gets the current state of the source: <see cref="MediaSourceState.Initial"/> until the source is
		/// disposed, then <see cref="MediaSourceState.Closed"/>. Opening is done by the media player engine, which
		/// does not report back to the source, so the opening states are not reported here.
		/// </summary>
		public MediaSourceState State => _isDisposed ? MediaSourceState.Closed : MediaSourceState.Initial;

		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}

		/// <summary>
		/// Closes the source: <see cref="State"/> becomes <see cref="MediaSourceState.Closed"/>. Calling it more
		/// than once has no further effect. A player that still has this source keeps whatever it already opened;
		/// give the player a new source (or <see langword="null"/>) when the source is replaced.
		/// </summary>
		public void Dispose()
		{
			_isDisposed = true;
		}
	}
}
