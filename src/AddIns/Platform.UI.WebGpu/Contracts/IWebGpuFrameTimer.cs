using System;

namespace CodeBrix.Platform.UI.WebGpu.Contracts;

/// <summary>
/// The one thing the frame scheduler (Internal/WebGpuFrameScheduler) needs from a platform: a way to run its tick on
/// the thread that owns the element, either soon or after a delay. A per-element hook, created once per element
/// through <see cref="PlatformContract.Create{TContract}"/> with the element as the owner.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia: <c>CodeBrix.Platform.UI.WebGpu.Skia.WebGpuFrameTimerSkiaPlatform</c>, a
/// non-repeating DispatcherQueueTimer on the element's dispatcher, registered by the Skia assembly's
/// <c>SkiaPlatformBootstrap</c>), Android, Mobile.
/// <para>
/// Every callback runs on the owner's thread. Neither method may allocate per call in steady state: the scheduler
/// calls them once per frame.
/// </para>
/// </remarks>
internal interface IWebGpuFrameTimer
{
	/// <summary>
	/// Runs <paramref name="callback"/> once on the owner's thread after <paramref name="delay"/>, replacing whatever
	/// timed callback was scheduled before. Called on the owner's thread.
	/// </summary>
	/// <param name="delay">How long to wait (positive; a delay below the platform timer's resolution waits that
	/// resolution).</param>
	/// <param name="callback">What to run. The scheduler always passes the same delegate.</param>
	void Schedule(TimeSpan delay, Action callback);

	/// <summary>Cancels a pending timed callback. A callback already queued to the thread may still run.</summary>
	void Cancel();

	/// <summary>
	/// Queues <paramref name="callback"/> to run once on the owner's thread as soon as it is free, behind input and
	/// layout work. Callable from any thread.
	/// </summary>
	/// <param name="callback">What to run.</param>
	/// <returns>False when the owner's thread is gone (the application is shutting down).</returns>
	bool Post(Action callback);
}
