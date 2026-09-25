#nullable enable

using System;

namespace CodeBrix.Platform.UI.Lottie.Engine;

/// <summary>
/// A repeating timer that ticks on its owner's UI thread (WPE1 C9): what drives the Lottie engine's frames. The engine
/// names no dispatcher type; on CodeBrix.Platform the source classes pass one backed by a DispatcherQueueTimer
/// (Internal/DispatcherQueueTickSource), and a CodeBrix.Mobile view passes one backed by its own platform timer.
/// </summary>
internal interface ITickSource
{
	/// <summary>The time between ticks.</summary>
	TimeSpan Interval { get; set; }

	/// <summary>Raised on each tick, on the owner's UI thread.</summary>
	event Action? Tick;

	/// <summary>Starts (or restarts) ticking.</summary>
	void Start();

	/// <summary>Stops ticking.</summary>
	void Stop();
}
