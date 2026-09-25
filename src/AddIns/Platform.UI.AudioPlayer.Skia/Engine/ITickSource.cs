#nullable enable

using System;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Engine;

/// <summary>
/// A timer that ticks on its owner's UI thread (WPE1 C10): the audio transport's position polling and seek debounce.
/// The engine names no dispatcher type; on CodeBrix.Platform the AudioPlayer element passes timers of its
/// DispatcherQueue (Internal/DispatcherQueueTickSource), and a CodeBrix.Mobile view passes its own platform timer.
/// </summary>
internal interface ITickSource
{
	/// <summary>The time between ticks.</summary>
	TimeSpan Interval { get; set; }

	/// <summary>Whether the timer keeps ticking (true) or ticks once per <see cref="Start"/> (false).</summary>
	bool IsRepeating { get; set; }

	/// <summary>Raised on each tick, on the owner's UI thread.</summary>
	event Action? Tick;

	/// <summary>Starts (or restarts) the timer.</summary>
	void Start();

	/// <summary>Stops the timer.</summary>
	void Stop();
}
