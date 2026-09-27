using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SilverAssertions;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Input_Preview_Injection;

/// <summary>
/// Timestamps of injected pointer input. A point injected with TimeOffsetInMilliseconds = 0 carries no time stamp
/// of its own: as with Win32 InjectTouchInput / SendInput, it is stamped with the current time. A non-zero offset
/// is added to the previous point's timestamp.
/// </summary>
[TestClass]
public class Given_InjectedInputPointerInfo
{
	// Real time between two injected points; large enough to be seen by a microsecond clock on any machine.
	private const int StepDelayMilliseconds = 15;

	private const InjectedInputPointerOptions Down = InjectedInputPointerOptions.New
		| InjectedInputPointerOptions.FirstButton
		| InjectedInputPointerOptions.PointerDown
		| InjectedInputPointerOptions.InContact
		| InjectedInputPointerOptions.InRange;

	private const InjectedInputPointerOptions Move = InjectedInputPointerOptions.Update
		| InjectedInputPointerOptions.FirstButton
		| InjectedInputPointerOptions.InContact
		| InjectedInputPointerOptions.InRange;

	private const InjectedInputPointerOptions Up = InjectedInputPointerOptions.FirstButton
		| InjectedInputPointerOptions.PointerUp;

	[TestMethod]
	public void When_Zero_Offset_Then_Successive_Points_Get_Increasing_Timestamps()
	{
		var state = new InjectedInputState(PointerDeviceType.Touch);

		var points = new List<PointerPoint> { Inject(state, Down, 10, 10) };
		for (var i = 1; i <= 5; i++)
		{
			Thread.Sleep(StepDelayMilliseconds);
			points.Add(Inject(state, Move, 10 + i * 10, 10));
		}
		Thread.Sleep(StepDelayMilliseconds);
		points.Add(Inject(state, Up, 60, 10));

		for (var i = 1; i < points.Count; i++)
		{
			points[i].Timestamp.Should().BeGreaterThan(points[i - 1].Timestamp, $"point {i} was injected {StepDelayMilliseconds} ms after point {i - 1}");
		}

		// The clock is the real one: the whole gesture spans at least the real time it took to inject it.
		(points[^1].Timestamp - points[0].Timestamp).Should().BeGreaterThanOrEqualTo(6UL * StepDelayMilliseconds * 1000);
	}

	[TestMethod]
	public void When_Zero_Offset_Without_Delay_Then_Timestamps_Still_Strictly_Increase()
	{
		var state = new InjectedInputState(PointerDeviceType.Touch);

		var previous = Inject(state, Down, 10, 10);
		for (var i = 1; i <= 50; i++)
		{
			var current = Inject(state, Move, 10 + i, 10);
			current.Timestamp.Should().BeGreaterThan(previous.Timestamp);
			previous = current;
		}
	}

	[TestMethod]
	public void When_Explicit_Offsets_Then_Timestamps_Are_The_Previous_Plus_The_Offset()
	{
		var state = new InjectedInputState(PointerDeviceType.Touch);

		var down = Inject(state, Down, 10, 10, offset: 5);
		down.Timestamp.Should().Be(state.Timestamp);

		var previous = down;
		foreach (var offset in new uint[] { 1, 16, 16, 100 })
		{
			Thread.Sleep(StepDelayMilliseconds); // Real time must NOT leak into explicitly timed input.
			var current = Inject(state, Move, 50, 10, offset);
			current.Timestamp.Should().Be(previous.Timestamp + offset * 1000UL);
			previous = current;
		}

		// Once the sequence carries its own time, a zero offset keeps its "same time as the previous point" meaning
		// (Win32: once a frame is injected with a time stamp, all subsequent frames of the sequence must have one).
		Thread.Sleep(StepDelayMilliseconds);
		var up = Inject(state, Up, 50, 10, offset: 0);
		up.Timestamp.Should().Be(previous.Timestamp);
	}

	[TestMethod]
	public void When_New_Contact_After_Explicit_Offsets_Then_Zero_Offset_Uses_The_Current_Time_Again()
	{
		var state = new InjectedInputState(PointerDeviceType.Touch);

		Inject(state, Down, 10, 10, offset: 1);
		Inject(state, Move, 20, 10, offset: 1);
		var lastUp = Inject(state, Up, 20, 10, offset: 1);

		Thread.Sleep(StepDelayMilliseconds);
		var down = Inject(state, Down, 10, 10);
		Thread.Sleep(StepDelayMilliseconds);
		var move = Inject(state, Move, 20, 10);

		down.Timestamp.Should().BeGreaterThan(lastUp.Timestamp);
		move.Timestamp.Should().BeGreaterThanOrEqualTo(down.Timestamp + StepDelayMilliseconds * 1000UL);
	}

	[TestMethod]
	public void When_Zero_Offset_Drag_Then_The_Release_Carries_A_Velocity()
	{
		var state = new InjectedInputState(PointerDeviceType.Touch);
		var sut = new GestureRecognizer
		{
			GestureSettings = GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY
		};
		var completed = new List<ManipulationCompletedEventArgs>();
		sut.ManipulationCompleted += (snd, e) => completed.Add(e);

		// A flick to the right, injected the way a UI test harness does it: no explicit time offsets.
		sut.ProcessDownEvent(Inject(state, Down, 10, 10));
		for (var i = 1; i <= 8; i++)
		{
			Thread.Sleep(StepDelayMilliseconds);
			sut.ProcessMoveEvents(new[] { Inject(state, Move, 10 + i * 30, 10) });
		}
		Thread.Sleep(StepDelayMilliseconds);
		sut.ProcessUpEvent(Inject(state, Up, 10 + 9 * 30, 10));

		completed.Should().HaveCount(1);
		var velocity = completed.Single().Velocities.Linear;
		double.IsFinite(velocity.X).Should().BeTrue($"the velocity is {velocity.X}");
		velocity.X.Should().BeGreaterThan(0);
		// 30 px every >= 15 ms is at most 2 px/ms; allow for a machine that injected much slower than asked.
		velocity.X.Should().BeLessThanOrEqualTo(2.1);
	}

	[TestMethod]
	public void When_Mouse_Zero_Offset_Then_Successive_Points_Get_Increasing_Timestamps()
	{
		var state = new InjectedInputState(PointerDeviceType.Mouse);

		var previous = InjectMouse(state, new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.Move, DeltaX = 1 });
		for (var i = 0; i < 3; i++)
		{
			Thread.Sleep(StepDelayMilliseconds);
			var current = InjectMouse(state, new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.Move, DeltaX = 1 });
			current.Timestamp.Should().BeGreaterThanOrEqualTo(previous.Timestamp + StepDelayMilliseconds * 1000UL);
			previous = current;
		}

		// An explicit offset keeps its meaning.
		var timed = InjectMouse(state, new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.Move, DeltaX = 1, TimeOffsetInMilliseconds = 7 });
		timed.Timestamp.Should().Be(previous.Timestamp + 7000UL);
	}

	// Mirrors InputInjector.InjectTouchInput: the args are built from the state, then the state is updated from them.
	private static PointerPoint Inject(InjectedInputState state, InjectedInputPointerOptions options, int x, int y, uint offset = 0)
	{
		var info = new InjectedInputTouchInfo
		{
			PointerInfo = new InjectedInputPointerInfo
			{
				PointerId = 1,
				PointerOptions = options,
				PixelLocation = new InjectedInputPoint { PositionX = x, PositionY = y },
				TimeOffsetInMilliseconds = offset,
			}
		};

		var args = info.ToEventArgs(state);
		state.Update(args);
		return args.CurrentPoint;
	}

	// Mirrors InputInjector.InjectMouseInput.
	private static PointerPoint InjectMouse(InjectedInputState state, InjectedInputMouseInfo info)
	{
		var args = info.ToEventArgs(state, global::Windows.System.VirtualKeyModifiers.None);
		state.Update(args);
		return args.CurrentPoint;
	}
}
