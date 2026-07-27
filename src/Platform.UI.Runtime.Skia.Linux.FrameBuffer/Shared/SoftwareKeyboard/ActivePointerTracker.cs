// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// Whether a finger (or the emulator's mouse standing in for one) is currently
/// down. Each head's pointer source reports presses and releases here; nothing
/// else writes to it.
/// <para>
/// It exists so the software keyboard never resizes the application out from
/// under a gesture in progress. Hiding the keyboard changes the visible bounds,
/// which re-lays-out the page — and a control that moves between a press and
/// its release is a control the user is no longer touching. A capturing control
/// such as Button survives that (the release routes to whatever captured the
/// pointer, wherever it now is), but one that does not capture is re-hit-tested
/// at the release position, so it can both miss its own press and fire on
/// whatever slid under the finger. Waiting for the finger to lift costs nothing
/// and removes the whole class of problem.
/// </para>
/// <para>
/// Two heads, two pointer sources, one keyboard: the tracker lives in shared
/// source so the controller does not need to know which head it is running on.
/// </para>
/// </summary>
internal static class ActivePointerTracker
{
	private static readonly HashSet<uint> Down = new();

	/// <summary>True while at least one pointer is down.</summary>
	internal static bool IsPointerDown
	{
		get
		{
			lock (Down)
			{
				return Down.Count > 0;
			}
		}
	}

	/// <summary>
	/// Raised on the dispatcher thread when the LAST pointer lifts — the moment
	/// a deferred layout change becomes safe. Never raised for a release that
	/// still leaves other fingers down.
	/// </summary>
	internal static event Action? AllPointersReleased;

	/// <summary>Reports a pointer going down. Re-reporting the same id is harmless.</summary>
	internal static void OnPointerDown(uint pointerId)
	{
		lock (Down)
		{
			Down.Add(pointerId);
		}
	}

	/// <summary>
	/// Reports a pointer lifting. Raises <see cref="AllPointersReleased"/> when
	/// it was the last one. A release for an id that was never down is ignored
	/// rather than treated as "all released" — that would fire the event on a
	/// stray event and defeat the point of tracking.
	/// </summary>
	internal static void OnPointerUp(uint pointerId)
	{
		bool wasLast;
		lock (Down)
		{
			wasLast = Down.Remove(pointerId) && Down.Count == 0;
		}
		if (wasLast)
		{
			AllPointersReleased?.Invoke();
		}
	}

	/// <summary>
	/// Forgets every pointer, without raising <see cref="AllPointersReleased"/>.
	/// For a cancelled or torn-down input sequence, where no release is coming
	/// and a stuck "pointer is down" would keep the keyboard up forever.
	/// </summary>
	internal static void Reset()
	{
		lock (Down)
		{
			Down.Clear();
		}
	}
}
