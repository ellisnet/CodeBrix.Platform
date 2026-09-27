#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal class InjectedInputState
{
	private static long _initialTimestamp = Stopwatch.GetTimestamp();

	public InjectedInputState(PointerDeviceType type)
	{
		Type = type;
		StartNewSequence(true);
	}

	public PointerDeviceType Type { get; }

	public uint FrameId { get; set; }

	public ulong Timestamp { get; set; }

	public Point Position { get; set; }

	public PointerPointProperties Properties { get; set; } = new();

	/// <summary>
	/// True once a point of the current sequence carried an explicit (non-zero) time offset.
	/// Like Win32 injection (once a frame carries its own time stamp, the following frames of the
	/// sequence must too), a zero offset then keeps its "same time as the previous point" meaning
	/// instead of switching the sequence to the current time.
	/// </summary>
	private bool _hasExplicitTime;

	/// <summary>
	/// Gets the current time on the clock used for injected input, in microseconds.
	/// </summary>
	internal static ulong GetCurrentTimestamp()
		=> (ulong)Stopwatch.GetElapsedTime(_initialTimestamp).TotalMicroseconds;

	/// <summary>
	/// Gets the timestamp, in microseconds, of the next injected point.
	/// A non-zero <paramref name="timeOffsetInMilliseconds"/> is added to the previous point's timestamp (unchanged
	/// meaning). A zero offset means "no time stamp given": as with Win32 InjectTouchInput / SendInput, the point is
	/// stamped with the current time (never earlier than, and never equal to, the previous point), unless the
	/// current sequence already carries explicit time offsets.
	/// </summary>
	/// <param name="timeOffsetInMilliseconds">The offset given by the injected point.</param>
	/// <param name="isNewSequence">True when the point starts a new contact (resets the explicit-time mode).</param>
	internal ulong GetNextTimestamp(uint timeOffsetInMilliseconds, bool isNewSequence)
	{
		if (isNewSequence)
		{
			_hasExplicitTime = false;
		}

		if (timeOffsetInMilliseconds != 0)
		{
			_hasExplicitTime = true;
			return Timestamp + timeOffsetInMilliseconds * 1000; // Same (uint) arithmetic as before.
		}

		if (_hasExplicitTime)
		{
			return Timestamp;
		}

		var now = GetCurrentTimestamp();
		return now > Timestamp ? now : Timestamp + 1;
	}

	public void StartNewSequence(bool initial = false)
	{
		_hasExplicitTime = false;

		if (initial)
		{
			Timestamp = GetCurrentTimestamp();
		}
		else
		{
			Timestamp = Timestamp + 1000; // Continue from the previous timestamp, but move forward in time by 1ms
		}

		FrameId = (uint)(Timestamp / 1000);
	}

	public void Update(PointerEventArgs args)
	{
		FrameId = args.CurrentPoint.FrameId;
		Timestamp = args.CurrentPoint.Timestamp;
		Position = args.CurrentPoint.Position;
		Properties = args.CurrentPoint.Properties;
	}
}
