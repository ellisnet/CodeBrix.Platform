using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The frame scheduler engine, driven one tick at a time through a hand-run timer and a hand-set clock (1 tick = 1
/// microsecond): when each driver runs a frame, and that nothing ever piles up.
/// </summary>
public class WebGpuFrameSchedulerTests
{
	private readonly FakeFrameTimer _timer = new();
	private readonly Queue<WebGpuFrameOutcome> _outcomes = new();
	private long _now = 1_000_000;
	private int _frames;
	private Action? _duringFrame;

	private WebGpuFrameScheduler Create() => new(_timer, RunFrame, () => _now, 1_000_000);

	private WebGpuFrameOutcome RunFrame()
	{
		_frames++;
		_duringFrame?.Invoke();
		return _outcomes.Count > 0 ? _outcomes.Dequeue() : WebGpuFrameOutcome.Rendered;
	}

	[Fact]
	public void nothing_runs_before_attach()
	{
		//Arrange
		var scheduler = Create();

		//Act
		scheduler.Invalidate();

		//Assert
		_timer.PostedCount.Should().Be(0);
		_timer.ScheduleCalls.Should().Be(0);
		scheduler.IsTickPending.Should().BeFalse();
	}

	[Fact]
	public void continuous_starts_on_attach_and_then_waits_one_period_from_the_deadline()
	{
		//Arrange
		var scheduler = Create();

		//Act
		scheduler.Attach();
		_now += 2_000; // the queued tick runs 2 ms later, the frame takes no time
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(1);
		_timer.PendingDelay.Should().Be(TimeSpan.FromTicks((16_667 - 2_000) * 10));
	}

	[Fact]
	public void continuous_keeps_its_rate_frame_after_frame()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_timer.RunPosted();

		//Act
		for (var i = 0; i < 10; i++)
		{
			_now += 16_667;
			_timer.RunTimed().Should().BeTrue();
		}

		//Assert
		_frames.Should().Be(11);
		scheduler.FramesRendered.Should().Be(11);
	}

	[Fact]
	public void a_late_continuous_frame_queues_one_tick_never_a_backlog()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_duringFrame = () => _now += 100_000; // every frame takes six periods

		//Act
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(1);
		_timer.PostedCount.Should().Be(1);   // exactly one "as soon as possible" tick
		_timer.PendingDelay.Should().BeNull(); // and no timed one
	}

	[Fact]
	public void a_rate_of_zero_queues_the_next_frame_at_once()
	{
		//Arrange
		var scheduler = Create();
		scheduler.TargetFrameRate = 0;
		scheduler.Attach();

		//Act
		_timer.RunPosted().Should().Be(1);

		//Assert
		_timer.PostedCount.Should().Be(1);
		_timer.ScheduleCalls.Should().Be(0);
	}

	[Fact]
	public void pausing_stops_continuous_and_resuming_restarts_it()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_timer.RunPosted();

		//Act
		scheduler.IsPaused = true;
		var ranWhilePaused = _timer.RunTimed();
		scheduler.IsPaused = false;
		_timer.RunPosted();

		//Assert
		ranWhilePaused.Should().BeFalse();
		_frames.Should().Be(2);
	}

	[Fact]
	public void on_demand_renders_once_on_attach_and_once_per_coalesced_invalidate()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Driver = WebGpuFrameDriver.OnDemand;
		scheduler.Attach();
		_timer.RunPosted();

		//Act
		scheduler.Invalidate();
		scheduler.Invalidate();
		scheduler.Invalidate();
		_timer.RunPosted();
		scheduler.Invalidate();
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(3);
		_timer.ScheduleCalls.Should().Be(0);
	}

	[Fact]
	public void on_demand_renders_again_after_a_resize()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Driver = WebGpuFrameDriver.OnDemand;
		scheduler.Attach();
		_timer.RunPosted();

		//Act
		scheduler.NotifyResized();
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(2);
	}

	[Fact]
	public void manual_never_renders_by_itself()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Driver = WebGpuFrameDriver.Manual;

		//Act
		scheduler.Attach();
		scheduler.Invalidate();
		scheduler.NotifyResized();
		_timer.RunPosted();
		_timer.RunTimed();

		//Assert
		_frames.Should().Be(0);
	}

	[Fact]
	public void invalidate_is_a_no_op_under_continuous()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_timer.RunPosted();
		var posted = _timer.PostedCount;

		//Act
		scheduler.Invalidate();

		//Assert
		_timer.PostedCount.Should().Be(posted);
	}

	[Fact]
	public void detach_cancels_and_a_tick_already_queued_does_nothing()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach(); // one tick queued

		//Act
		scheduler.Detach();
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(0);
		scheduler.IsAttached.Should().BeFalse();
	}

	[Fact]
	public void attach_after_detach_re_arms_driving()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_timer.RunPosted();
		scheduler.Detach();

		//Act
		scheduler.Attach();
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(2);
	}

	[Fact]
	public void an_unavailable_frame_stops_driving_until_the_next_attach()
	{
		//Arrange
		var scheduler = Create();
		_outcomes.Enqueue(WebGpuFrameOutcome.Unavailable);
		scheduler.Attach();

		//Act
		_timer.RunPosted();
		var pendingAfterFailure = scheduler.IsTickPending;
		scheduler.Attach();
		_timer.RunPosted();

		//Assert
		pendingAfterFailure.Should().BeFalse();
		_frames.Should().Be(2);
		scheduler.IsUnavailable.Should().BeFalse();
	}

	[Fact]
	public void a_frame_that_is_not_ready_is_retried_after_a_short_wait()
	{
		//Arrange
		var scheduler = Create();
		_outcomes.Enqueue(WebGpuFrameOutcome.NotReady);
		scheduler.Attach();

		//Act
		_timer.RunPosted();

		//Assert
		_timer.PendingDelay.Should().Be(WebGpuFrameScheduler.NotReadyRetry);
		scheduler.FramesRendered.Should().Be(0);
	}

	[Fact]
	public void switching_drivers_cancels_what_the_old_one_had_pending()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_timer.RunPosted(); // continuous: a timed tick is pending

		//Act
		scheduler.Driver = WebGpuFrameDriver.OnDemand;
		var ran = _timer.RunTimed();
		scheduler.Driver = WebGpuFrameDriver.Manual;
		_timer.RunPosted();

		//Assert
		ran.Should().BeFalse();
		_frames.Should().Be(1);
	}

	[Fact]
	public void switching_back_to_continuous_starts_at_once()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Driver = WebGpuFrameDriver.Manual;
		scheduler.Attach();

		//Act
		scheduler.Driver = WebGpuFrameDriver.Continuous;
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(1);
	}

	[Fact]
	public void a_render_handler_that_changes_the_driver_is_obeyed()
	{
		//Arrange
		var scheduler = Create();
		scheduler.Attach();
		_duringFrame = () => scheduler.Driver = WebGpuFrameDriver.Manual;

		//Act
		_timer.RunPosted();
		_timer.RunTimed();
		_timer.RunPosted();

		//Assert
		_frames.Should().Be(1);
		scheduler.IsTickPending.Should().BeFalse();
	}

	[Fact]
	public void a_post_the_owner_thread_refuses_leaves_nothing_pending()
	{
		//Arrange
		var scheduler = Create();
		_timer.AcceptPosts = false;

		//Act
		scheduler.Attach();

		//Assert
		scheduler.IsTickPending.Should().BeFalse();
	}

	[Fact]
	public void an_invalid_rate_is_refused_and_the_old_rate_kept()
	{
		//Arrange
		var scheduler = Create();

		//Act
		Action act = () => scheduler.TargetFrameRate = -5;

		//Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
		scheduler.TargetFrameRate.Should().Be(60.0);
	}
}
