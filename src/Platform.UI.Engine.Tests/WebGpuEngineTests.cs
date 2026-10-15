#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.WebGpu;
using CodeBrix.Platform.UI.WebGpu.Contracts;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The WebGpu engine part: the frame scheduler decides when each driver runs a frame, over the platform's frame timer
/// contract, and the timing and size rules are plain arithmetic - all with only the add-in's Core in the process: no
/// WinUI assembly loads. (The element itself stays in the Skia assembly: its API is made of CodeBrix.WebGpu and
/// SkiaSharp types.)
/// </summary>
public class WebGpuEngineTests
{
	[Fact]
	public void When_The_Frame_Scheduler_Drives_Continuous_And_OnDemand_Frames_Then_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var timer = new TestFrameTimer();
		var frames = 0;
		var now = 0L;
		var scheduler = new WebGpuFrameScheduler(timer, () =>
		{
			frames++;
			return WebGpuFrameOutcome.Rendered;
		}, () => now, 1_000);

		//Act
		scheduler.Attach();          // Continuous: a frame at once,
		timer.RunQueued();
		now += 17;
		timer.RunTimed();           // then one per period.
		scheduler.Driver = WebGpuFrameDriver.OnDemand;
		scheduler.Invalidate();
		scheduler.Invalidate();
		timer.RunQueued();           // two requests, one frame

		//Assert
		frames.Should().Be(3);
		WebGpuRenderViewRules.FramePeriodTicks(60, 1_000).Should().Be(17L);
		WebGpuRenderViewRules.NextDeadline(0, 100, 17).Should().Be(100L);
		WebGpuRenderViewRules.TargetPixelSize(320, 240, 1.25).Should().Be((400, 300));
		new WebGpuDeviceLostEventArgs("gone").Reason.Should().Be("gone");

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.WebGpu.Core");
		EngineIsolation.AssertNoWinUILoaded("WebGpu (frame scheduler)");
	}

	/// <summary>A platform's frame timer for the WebGpu contract, run by hand.</summary>
	private sealed class TestFrameTimer : IWebGpuFrameTimer
	{
		private readonly Queue<Action> _queued = new();
		private Action? _timed;

		public void Schedule(TimeSpan delay, Action callback) => _timed = callback;

		public void Cancel() => _timed = null;

		public bool Post(Action callback)
		{
			_queued.Enqueue(callback);
			return true;
		}

		internal void RunQueued()
		{
			var count = _queued.Count;
			for (var i = 0; i < count; i++)
			{
				_queued.Dequeue()();
			}
		}

		internal void RunTimed()
		{
			var timed = _timed;
			_timed = null;
			timed?.Invoke();
		}
	}
}
