#nullable enable

#if HAS_SKOTTIE

using System;
using CodeBrix.Platform.UI.Lottie.Engine;
using Windows.System;

namespace CodeBrix.Platform.UI.Lottie.Internal;

/// <summary>
/// The Lottie engine's frame timer on CodeBrix.Platform (WPE1 C9): a DispatcherQueueTimer of the current thread's queue,
/// created exactly where the source classes created theirs (each Play).
/// </summary>
internal sealed class DispatcherQueueTickSource : ITickSource
{
	private readonly DispatcherQueueTimer _timer;

	private DispatcherQueueTickSource(DispatcherQueueTimer timer)
	{
		_timer = timer;
		_timer.Tick += (s, e) => Tick?.Invoke();
	}

	/// <summary>A timer on the calling thread's dispatcher queue.</summary>
	/// <returns>The tick source.</returns>
	internal static ITickSource ForCurrentThread() =>
		new DispatcherQueueTickSource(DispatcherQueue.GetForCurrentThread().CreateTimer());

	/// <inheritdoc/>
	public TimeSpan Interval
	{
		get => _timer.Interval;
		set => _timer.Interval = value;
	}

	/// <inheritdoc/>
	public event Action? Tick;

	/// <inheritdoc/>
	public void Start() => _timer.Start();

	/// <inheritdoc/>
	public void Stop() => _timer.Stop();
}

#endif
