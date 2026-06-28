#if true
using System;
using System.Threading;
using CodeBrix.Platform.UI.Dispatching;


#if HAS_CODEBRIX_WINUI && IS_CODEBRIX_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching;
#else
namespace Windows.System;
#endif

partial class DispatcherQueueTimer
{
	private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(1);

	private Timer _timer;

	private void ScheduleTickNative(TimeSpan interval)
	{
		_timer ??= new Timer(_ => DispatchRaiseTick());
		_timer.Change(ClampInterval(interval), Timeout.InfiniteTimeSpan);
	}

	private void DispatchRaiseTick()
	{
#if false
		if (!NativeDispatcher.IsThreadingSupported)
		{
			RaiseTick();
			return;
		}
#endif
		NativeDispatcher.Main.Enqueue(() => RaiseTick());
	}

	private void StopNative()
	{
		_timer?.Dispose();
		_timer = null;
	}

	/// <summary>
	/// Timer class does not support zero interval.
	/// This clamps it to at least single tick.
	/// </summary>
	/// <param name="interval">Interval.</param>
	/// <returns>Clamped interval.</returns>
	private TimeSpan ClampInterval(TimeSpan interval) =>
		interval <= _minInterval ?
			_minInterval :
			interval;
}
#endif
