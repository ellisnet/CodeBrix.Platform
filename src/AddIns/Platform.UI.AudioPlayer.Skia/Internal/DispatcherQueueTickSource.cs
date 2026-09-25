using System;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Engine;
using Microsoft.UI.Dispatching;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

/// <summary>
/// The audio transport's timers on CodeBrix.Platform (WPE1 C10): a DispatcherQueueTimer of the AudioPlayer element's
/// queue, created exactly where the element created its timers.
/// </summary>
internal sealed class DispatcherQueueTickSource : ITickSource
{
	private readonly DispatcherQueueTimer _timer;

	internal DispatcherQueueTickSource(DispatcherQueueTimer timer)
	{
		_timer = timer;
		_timer.Tick += (_, _) => Tick?.Invoke();
	}

	/// <inheritdoc/>
	public TimeSpan Interval
	{
		get => _timer.Interval;
		set => _timer.Interval = value;
	}

	/// <inheritdoc/>
	public bool IsRepeating
	{
		get => _timer.IsRepeating;
		set => _timer.IsRepeating = value;
	}

	/// <inheritdoc/>
	public event Action? Tick;

	/// <inheritdoc/>
	public void Start() => _timer.Start();

	/// <inheritdoc/>
	public void Stop() => _timer.Stop();
}
