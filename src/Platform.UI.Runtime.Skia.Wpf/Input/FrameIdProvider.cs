#nullable enable

using System.Threading;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Input //Was previously: Uno.UI.Runtime.Skia.Wpf.Input
{
	internal static class FrameIdProvider
	{
		private static int _currentFrameId;

		internal static uint GetNextFrameId() =>
			(uint)Interlocked.Increment(ref _currentFrameId);
	}
}
