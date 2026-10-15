using System;
using System.Diagnostics;
using CodeBrix.Platform.UI.WebGpu.Contracts;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>What happened when the scheduler asked the view for a frame.</summary>
internal enum WebGpuFrameOutcome
{
	/// <summary>A frame was rendered and presented.</summary>
	Rendered = 0,

	/// <summary>
	/// No frame this time, but one may be possible soon: the view has no pixel size yet, another thread holds the
	/// frame, or the frame's commands were rejected. Continuous keeps ticking.
	/// </summary>
	NotReady = 1,

	/// <summary>
	/// No frame is possible until the view is loaded again: there is no device on this machine, or the launch said
	/// there is none. The scheduler stops until <see cref="WebGpuFrameScheduler.Attach"/>.
	/// </summary>
	Unavailable = 2,
}

/// <summary>
/// The frame-driver ENGINE of the WebGpu add-in: decides WHEN a frame runs for each <see cref="WebGpuFrameDriver"/>,
/// and nothing else. It names no XAML, SkiaSharp or WebGPU type, so every platform's view reuses it unchanged; it
/// reaches the owner's thread through the <see cref="IWebGpuFrameTimer"/> contract and asks for a frame through a
/// delegate.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item><b>Continuous</b>: one frame per period of <see cref="TargetFrameRate"/>, timed from the previous frame's
/// DEADLINE (not from when it finished), so the rate holds when frames take part of a period; a frame that ends after
/// the next deadline makes the following one start at once and drops the missed ones (latest wins, never a backlog).
/// A rate of zero runs the next frame as soon as the thread is free.</item>
/// <item><b>OnDemand</b>: one frame per <see cref="Invalidate"/>, coalesced (any number of calls before the frame
/// runs make ONE frame), plus one on <see cref="Attach"/> and one per <see cref="NotifyResized"/>.</item>
/// <item><b>Manual</b>: nothing - the application calls the frame protocol itself.</item>
/// </list>
/// <para>
/// At most ONE tick is ever pending: a timed tick (the next Continuous deadline) or a queued one (as soon as
/// possible), never both and never two of either. Each kind has its own cached callback and its own "wanted" flag, so
/// a tick that was already queued when the plan changed finds its flag cleared and does nothing. Every member except
/// <see cref="Invalidate"/> is called on the owner's thread; <see cref="Invalidate"/> may be called from any thread.
/// Nothing here allocates per frame.
/// </para>
/// </remarks>
internal sealed class WebGpuFrameScheduler
{
	/// <summary>How long Continuous waits before trying again after a frame that could not run.</summary>
	internal static readonly TimeSpan NotReadyRetry = TimeSpan.FromMilliseconds(100);

	private readonly object _gate = new();
	private readonly IWebGpuFrameTimer _timer;
	private readonly Func<WebGpuFrameOutcome> _runFrame;
	private readonly Func<long> _clock;
	private readonly long _ticksPerSecond;
	private readonly Action _onTimedTick;
	private readonly Action _onQueuedTick;

	private WebGpuFrameDriver _driver = WebGpuFrameDriver.Continuous;
	private double _targetFrameRate = WebGpuRenderViewRules.DefaultTargetFrameRate;
	private long _periodTicks;
	private bool _isPaused;
	private bool _isAttached;
	private bool _isUnavailable;
	private bool _onDemandPending;

	private bool _wantTimed;
	private bool _wantQueued;
	private bool _queuedOutstanding;
	private long _deadline;
	private long _framesRendered;

	/// <summary>Creates the scheduler for one view.</summary>
	/// <param name="timer">The platform's timer for the view's thread.</param>
	/// <param name="runFrame">Runs one frame (the view's BeginFrame, Render, EndFrame) and says what happened.</param>
	/// <param name="clock">The clock, in ticks of <paramref name="ticksPerSecond"/>; null for <see cref="Stopwatch"/>.</param>
	/// <param name="ticksPerSecond">The clock's resolution; ignored when <paramref name="clock"/> is null.</param>
	internal WebGpuFrameScheduler(IWebGpuFrameTimer timer, Func<WebGpuFrameOutcome> runFrame, Func<long>? clock = null, long ticksPerSecond = 0)
	{
		_timer = timer ?? throw new ArgumentNullException(nameof(timer));
		_runFrame = runFrame ?? throw new ArgumentNullException(nameof(runFrame));
		_clock = clock ?? Stopwatch.GetTimestamp;
		_ticksPerSecond = clock is null ? Stopwatch.Frequency : ticksPerSecond;
		if (_ticksPerSecond <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "A clock needs a positive resolution.");
		}

		_periodTicks = WebGpuRenderViewRules.FramePeriodTicks(_targetFrameRate, _ticksPerSecond);
		_onTimedTick = OnTimedTick;
		_onQueuedTick = OnQueuedTick;
	}

	/// <summary>The driver. Changing it cancels whatever was pending; Continuous starts at once.</summary>
	internal WebGpuFrameDriver Driver
	{
		get
		{
			lock (_gate)
			{
				return _driver;
			}
		}
		set
		{
			lock (_gate)
			{
				if (_driver == value)
				{
					return;
				}

				_driver = value;
				_onDemandPending = false;
				CancelLocked();
				if (value == WebGpuFrameDriver.Continuous && _isAttached && !_isPaused && !_isUnavailable)
				{
					_deadline = _clock();
					RequestSoonLocked();
				}
			}
		}
	}

	/// <summary>The Continuous rate in frames per second (zero = as fast as possible). Takes effect from the next frame.</summary>
	/// <exception cref="ArgumentOutOfRangeException">The rate is negative, NaN or infinite.</exception>
	internal double TargetFrameRate
	{
		get
		{
			lock (_gate)
			{
				return _targetFrameRate;
			}
		}
		set
		{
			var period = WebGpuRenderViewRules.FramePeriodTicks(value, _ticksPerSecond);
			lock (_gate)
			{
				_targetFrameRate = value;
				_periodTicks = period;
			}
		}
	}

	/// <summary>Pauses (true) or resumes (false) Continuous. No effect on the other drivers.</summary>
	internal bool IsPaused
	{
		get
		{
			lock (_gate)
			{
				return _isPaused;
			}
		}
		set
		{
			lock (_gate)
			{
				if (_isPaused == value)
				{
					return;
				}

				_isPaused = value;
				if (_driver != WebGpuFrameDriver.Continuous)
				{
					return;
				}

				CancelLocked();
				if (!value && _isAttached && !_isUnavailable)
				{
					_deadline = _clock();
					RequestSoonLocked();
				}
			}
		}
	}

	/// <summary>Whether the view is in a live visual tree (between <see cref="Attach"/> and <see cref="Detach"/>).</summary>
	internal bool IsAttached
	{
		get
		{
			lock (_gate)
			{
				return _isAttached;
			}
		}
	}

	/// <summary>Whether the last frame said no frame is possible until the view is loaded again.</summary>
	internal bool IsUnavailable
	{
		get
		{
			lock (_gate)
			{
				return _isUnavailable;
			}
		}
	}

	/// <summary>How many frames the built-in drivers have rendered (diagnostics and tests).</summary>
	internal long FramesRendered
	{
		get
		{
			lock (_gate)
			{
				return _framesRendered;
			}
		}
	}

	/// <summary>Whether a tick is pending (diagnostics and tests).</summary>
	internal bool IsTickPending
	{
		get
		{
			lock (_gate)
			{
				return _wantTimed || _wantQueued;
			}
		}
	}

	/// <summary>
	/// The view entered a live visual tree: re-arms driving. Continuous starts; OnDemand renders once (so a view
	/// that comes back shows its picture again); a previous "unavailable" is forgotten.
	/// </summary>
	internal void Attach()
	{
		lock (_gate)
		{
			_isAttached = true;
			_isUnavailable = false;
			CancelLocked();
			switch (_driver)
			{
				case WebGpuFrameDriver.Continuous when !_isPaused:
					_deadline = _clock();
					RequestSoonLocked();
					break;
				case WebGpuFrameDriver.OnDemand:
					_onDemandPending = true;
					RequestSoonLocked();
					break;
			}
		}
	}

	/// <summary>The view left the visual tree: nothing more runs, and a tick already queued becomes a no-op.</summary>
	internal void Detach()
	{
		lock (_gate)
		{
			_isAttached = false;
			_onDemandPending = false;
			CancelLocked();
		}
	}

	/// <summary>
	/// OnDemand: asks for one frame (coalesced with any other request that has not run yet). A no-op for the other
	/// drivers, while detached, and while no frame is possible. Callable from any thread.
	/// </summary>
	internal void Invalidate()
	{
		lock (_gate)
		{
			if (_driver != WebGpuFrameDriver.OnDemand || !_isAttached || _isUnavailable)
			{
				return;
			}

			_onDemandPending = true;
			if (!_wantQueued)
			{
				RequestSoonLocked();
			}
		}
	}

	/// <summary>The view's pixel size changed: OnDemand re-renders once at the new size.</summary>
	internal void NotifyResized() => Invalidate();

	// Owner thread. A timed tick (the next Continuous deadline).
	private void OnTimedTick()
	{
		lock (_gate)
		{
			if (!_wantTimed)
			{
				return;
			}

			_wantTimed = false;
		}

		Tick();
	}

	// Owner thread. A queued tick (as soon as possible).
	private void OnQueuedTick()
	{
		lock (_gate)
		{
			_queuedOutstanding = false;
			if (!_wantQueued)
			{
				return;
			}

			_wantQueued = false;
		}

		Tick();
	}

	private void Tick()
	{
		WebGpuFrameDriver driver;
		long frameDeadline;
		lock (_gate)
		{
			if (!_isAttached || _isUnavailable)
			{
				return;
			}

			driver = _driver;
			switch (driver)
			{
				case WebGpuFrameDriver.Continuous when _isPaused:
				case WebGpuFrameDriver.Manual:
					return;
				case WebGpuFrameDriver.OnDemand:
					if (!_onDemandPending)
					{
						return;
					}

					_onDemandPending = false;
					break;
			}

			frameDeadline = _deadline;
		}

		// Outside the lock: the frame raises the application's Render handler, which may set Driver, IsPaused or
		// call Invalidate.
		var outcome = _runFrame();

		lock (_gate)
		{
			if (outcome == WebGpuFrameOutcome.Rendered)
			{
				_framesRendered++;
			}
			else if (outcome == WebGpuFrameOutcome.Unavailable)
			{
				_isUnavailable = true;
				_onDemandPending = false;
				CancelLocked();
				return;
			}

			if (driver != WebGpuFrameDriver.Continuous || _driver != WebGpuFrameDriver.Continuous
				|| !_isAttached || _isPaused || _wantTimed || _wantQueued)
			{
				// OnDemand runs once per request; a NotReady OnDemand frame waits for the next request (attach and
				// resize make one). A plan change made by the Render handler already scheduled what comes next.
				return;
			}

			var now = _clock();
			if (outcome == WebGpuFrameOutcome.NotReady)
			{
				_deadline = now;
				RequestAtLocked(NotReadyRetry);
				return;
			}

			_deadline = WebGpuRenderViewRules.NextDeadline(frameDeadline, now, _periodTicks);
			var wait = _deadline - now;
			if (wait <= 0)
			{
				RequestSoonLocked();
			}
			else
			{
				RequestAtLocked(TimeSpan.FromTicks(wait * TimeSpan.TicksPerSecond / _ticksPerSecond));
			}
		}
	}

	private void RequestSoonLocked()
	{
		if (_wantTimed)
		{
			_wantTimed = false;
			_timer.Cancel();
		}

		_wantQueued = true;
		if (!_queuedOutstanding)
		{
			_queuedOutstanding = true;
			if (!_timer.Post(_onQueuedTick))
			{
				_queuedOutstanding = false;
				_wantQueued = false;
			}
		}
	}

	private void RequestAtLocked(TimeSpan delay)
	{
		_wantQueued = false;
		_wantTimed = true;
		_timer.Schedule(delay, _onTimedTick);
	}

	private void CancelLocked()
	{
		_wantQueued = false;
		if (_wantTimed)
		{
			_wantTimed = false;
			_timer.Cancel();
		}
	}
}
