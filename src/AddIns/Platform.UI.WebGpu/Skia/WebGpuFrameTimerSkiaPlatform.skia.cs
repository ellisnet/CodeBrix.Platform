using System;
using CodeBrix.Platform.UI.WebGpu.Contracts;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.WebGpu.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IWebGpuFrameTimer"/>: a non-repeating DispatcherQueueTimer on the
/// element's dispatcher for timed ticks, and the dispatcher queue itself (at low priority, behind input and layout)
/// for "as soon as possible" ticks - the same user-interface-thread machinery the VideoPlayer driver posts its frames
/// through.
/// </summary>
/// <remarks>
/// The callbacks are cached delegates and the timer is created once, so neither method allocates per frame.
/// </remarks>
internal sealed class WebGpuFrameTimerSkiaPlatform : IWebGpuFrameTimer
{
	private readonly FrameworkElement _owner;
	private readonly DispatcherQueue? _creationQueue;
	private readonly DispatcherQueueHandler _postHandler;
	private DispatcherQueueTimer? _timer;
	private Action? _timedCallback;
	private volatile Action? _postedCallback;

	/// <summary>Creates the timer for one element (on the element's thread).</summary>
	/// <param name="owner">The element.</param>
	internal WebGpuFrameTimerSkiaPlatform(FrameworkElement owner)
	{
		_owner = owner;
		_creationQueue = DispatcherQueue.GetForCurrentThread();
		_postHandler = RunPosted;
	}

	private DispatcherQueue? Queue => _owner.DispatcherQueue ?? _creationQueue;

	/// <inheritdoc/>
	public void Schedule(TimeSpan delay, Action callback)
	{
		if (Queue is not { } queue)
		{
			return;
		}

		if (_timer is null)
		{
			_timer = queue.CreateTimer();
			_timer.IsRepeating = false;
			_timer.Tick += OnTimerTick;
		}

		_timedCallback = callback;
		_timer.Stop();
		_timer.Interval = delay;
		_timer.Start();
	}

	/// <inheritdoc/>
	public void Cancel() => _timer?.Stop();

	/// <inheritdoc/>
	public bool Post(Action callback)
	{
		_postedCallback = callback;
		return Queue is { } queue && queue.TryEnqueue(DispatcherQueuePriority.Low, _postHandler);
	}

	private void OnTimerTick(DispatcherQueueTimer sender, object args)
	{
		sender.Stop();
		_timedCallback?.Invoke();
	}

	private void RunPosted() => _postedCallback?.Invoke();
}
