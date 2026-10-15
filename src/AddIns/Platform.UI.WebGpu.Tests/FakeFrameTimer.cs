using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.WebGpu.Contracts;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// A frame timer the test drives by hand: it records what the scheduler asked for and runs it only when told, so the
/// scheduler's decisions can be asserted one tick at a time.
/// </summary>
internal sealed class FakeFrameTimer : IWebGpuFrameTimer
{
	private readonly Queue<Action> _posted = new();
	private Action? _timed;

	/// <summary>The delay of the pending timed callback, or null.</summary>
	internal TimeSpan? PendingDelay { get; private set; }

	/// <summary>How many callbacks are queued.</summary>
	internal int PostedCount => _posted.Count;

	/// <summary>How many times a timed callback was scheduled.</summary>
	internal int ScheduleCalls { get; private set; }

	/// <summary>How many times Cancel was called.</summary>
	internal int CancelCalls { get; private set; }

	/// <summary>False makes Post report that the thread is gone.</summary>
	internal bool AcceptPosts { get; set; } = true;

	public void Schedule(TimeSpan delay, Action callback)
	{
		ScheduleCalls++;
		PendingDelay = delay;
		_timed = callback;
	}

	public void Cancel()
	{
		CancelCalls++;
		PendingDelay = null;
		_timed = null;
	}

	public bool Post(Action callback)
	{
		if (!AcceptPosts)
		{
			return false;
		}

		_posted.Enqueue(callback);
		return true;
	}

	/// <summary>Runs the callbacks queued so far (one dispatcher turn); ones they queue wait for the next call.</summary>
	/// <returns>How many ran.</returns>
	internal int RunPosted()
	{
		var count = _posted.Count;
		for (var i = 0; i < count; i++)
		{
			_posted.Dequeue()();
		}

		return count;
	}

	/// <summary>Runs the pending timed callback, if any.</summary>
	/// <returns>True when one ran.</returns>
	internal bool RunTimed()
	{
		if (_timed is not { } timed)
		{
			return false;
		}

		_timed = null;
		PendingDelay = null;
		timed();
		return true;
	}
}
