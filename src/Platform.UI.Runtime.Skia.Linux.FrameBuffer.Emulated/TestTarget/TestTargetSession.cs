using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;
using Windows.System;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;

/// <summary>
/// The way in to the test target: one virtual application on one fixed panel,
/// for the life of the process.
/// <para>
/// The panel's shape is chosen once, before anything runs, and the application
/// is launched separately, so a test fixture can hold a configured session and
/// decide later what to run on it:
/// </para>
/// <code>
/// var session = LinuxTestTarget.Setup(TestDisplayOrientation.Landscape);
/// await session.LaunchAsync(() => new MyApplication(), TimeSpan.FromSeconds(30));
/// </code>
/// </summary>
public static class LinuxTestTarget
{
	private static readonly object SetupLock = new();

	private static TestTargetSession? _session;

	/// <summary>
	/// Configures the test target's panel. Nothing runs yet — call
	/// <see cref="TestTargetSession.LaunchAsync(Func{Microsoft.UI.Xaml.Application}, TimeSpan)"/>
	/// on the returned session to start the application.
	/// </summary>
	/// <param name="orientation">The shape of the panel to render onto.</param>
	/// <returns>The session the application will run in.</returns>
	/// <exception cref="InvalidOperationException">
	/// A session already exists in this process. The window wrapper, the pointer
	/// input source and the dispatcher overrides are all process-wide, so a
	/// process can only ever host one virtual application.
	/// </exception>
	public static TestTargetSession Setup(TestDisplayOrientation orientation)
	{
		// Under the lock, and only after the check: a second call must not build a
		// session it is about to throw away.
		lock (SetupLock)
		{
			if (_session is not null)
			{
				throw new InvalidOperationException(
					"A test target session has already been set up in this process. One process hosts one virtual application: run a second panel in a second process.");
			}
			_session = new TestTargetSession(orientation);
			return _session;
		}
	}
}

/// <summary>
/// A configured test target: the panel, the application running on it, the
/// frames it publishes, and the touch and key input it receives.
/// </summary>
/// <remarks>
/// Every member is safe to call from a test thread. Input goes to the same
/// methods the emulator's socket thread calls, and those marshal onto the UI
/// thread themselves; frames are immutable snapshots, so nothing a test holds
/// can be rewritten underneath it.
/// </remarks>
public sealed class TestTargetSession
{
	private const int LandscapeWidth = 1920;
	private const int LandscapeHeight = 1080;
	private const int PortraitWidth = 1080;
	private const int PortraitHeight = 1920;

	private readonly object _frameLock = new();
	private readonly List<FrameWaiter> _waiters = new();
	private readonly TaskCompletionSource<TestFrame> _firstFrame =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource _hostExited =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private TestTargetHost? _host;
	private Thread? _hostThread;
	private ExceptionDispatchInfo? _hostFailure;
	private long _latestSequence;
	private long _latestRenderGeneration;
	private byte[]? _latestSnapshot;
	private bool _isLaunched;

	internal TestTargetSession(TestDisplayOrientation orientation)
	{
		Orientation = orientation;
		Width = orientation == TestDisplayOrientation.Portrait ? PortraitWidth : LandscapeWidth;
		Height = orientation == TestDisplayOrientation.Portrait ? PortraitHeight : LandscapeHeight;
	}

	/// <summary>The panel's width, in device pixels. Display scale is 1.0, so this is also logical pixels.</summary>
	public int Width { get; }

	/// <summary>The panel's height, in device pixels. Display scale is 1.0, so this is also logical pixels.</summary>
	public int Height { get; }

	/// <summary>The shape of the panel, as chosen at setup.</summary>
	public TestDisplayOrientation Orientation { get; }

	/// <summary>
	/// The sequence number of the most recently published frame, or 0 when the
	/// application has not drawn anything yet. Record this before changing
	/// something, then wait for a frame after it.
	/// </summary>
	public long LatestSequence
	{
		get
		{
			lock (_frameLock)
			{
				return _latestSequence;
			}
		}
	}

	/// <summary>
	/// The invalidation generation the most recently published frame was rendered
	/// from, or 0 when the application has not drawn anything yet. Generations only
	/// grow: a frame whose generation is <c>g</c> or larger was drawn entirely after
	/// invalidation <c>g</c> was requested.
	/// </summary>
	public long LatestRenderGeneration
	{
		get
		{
			lock (_frameLock)
			{
				return _latestRenderGeneration;
			}
		}
	}

	/// <summary>Whether the application has been launched and has published its first frame.</summary>
	public bool IsLaunched => Volatile.Read(ref _isLaunched);

	/// <summary>
	/// Launches <paramref name="appBuilder"/>'s application on this panel, on a
	/// dedicated thread of its own, and returns once it has published its first
	/// frame.
	/// </summary>
	/// <param name="appBuilder">Creates the application to run.</param>
	/// <param name="timeout">How long to wait for that first frame.</param>
	/// <returns>A task that completes when the application has drawn.</returns>
	/// <exception cref="TimeoutException">No frame was published in time.</exception>
	/// <exception cref="InvalidOperationException">The session has already been launched.</exception>
	public Task LaunchAsync(Func<Microsoft.UI.Xaml.Application> appBuilder, TimeSpan timeout)
	{
		ArgumentNullException.ThrowIfNull(appBuilder);
		if (_hostThread is not null)
		{
			throw new InvalidOperationException(
				"This test target session has already been launched. One session hosts one virtual application.");
		}

		// Build on this thread: the host's constructor is what hands the session
		// its transport, and that has to be in place before the first frame can
		// possibly be published.
		var host = CodeBrixPlatformHostBuilder.Create()
			.App(appBuilder)
			.UseLinuxTestTarget(this)
			.Build();

		_hostThread = new Thread(() =>
		{
			try
			{
				host.Run();
			}
			catch (Exception exception)
			{
				_hostFailure = ExceptionDispatchInfo.Capture(exception);
				_firstFrame.TrySetException(exception);
			}
			finally
			{
				_firstFrame.TrySetException(new InvalidOperationException(
					"The test target host ended before the application published a frame."));
				_hostExited.TrySetResult();
			}
		})
		{
			// A BACKGROUND thread on purpose: this host exists for test processes,
			// and a host that somehow gets stuck must never be the reason a test
			// runner refuses to exit. ShutdownAsync is the orderly path — it exits
			// the application on its own UI thread and joins this thread — and the
			// process is free to end without it.
			IsBackground = true,
			Name = "TestTarget host",
		};
		_hostThread.Start();

		return AwaitLaunchAsync(timeout);
	}

	/// <summary>
	/// Waits for the first frame published after <paramref name="afterSequence"/>,
	/// completing immediately when one already exists.
	/// </summary>
	/// <remarks>
	/// This says a frame ARRIVED, not that it shows anything in particular: a
	/// render that was already in flight when something changed publishes a frame
	/// that predates the change. Use
	/// <see cref="RequestFrameAsync(TimeSpan)"/> to look at a change.
	/// </remarks>
	/// <param name="afterSequence">The sequence number to wait past.</param>
	/// <param name="timeout">How long to wait.</param>
	/// <returns>The frame.</returns>
	/// <exception cref="TimeoutException">No such frame was published in time.</exception>
	public Task<TestFrame> WaitForFrameAsync(long afterSequence, TimeSpan timeout)
	{
		EnsureLaunched();

		FrameWaiter waiter;
		lock (_frameLock)
		{
			if (_latestSnapshot is { } snapshot && _latestSequence > afterSequence)
			{
				return Task.FromResult(
					new TestFrame(Width, Height, _latestSequence, _latestRenderGeneration, snapshot));
			}
			waiter = FrameWaiter.ForSequenceAfter(afterSequence);
			_waiters.Add(waiter);
		}

		return AwaitFrameAsync(waiter, timeout);
	}

	/// <summary>
	/// Asks for a repaint and waits for a frame whose RENDERING BEGAN AFTER this
	/// call — so anything already applied on the UI thread when the call was made
	/// is in the frame that comes back.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the frame handshake to use after changing something: apply the
	/// change on the UI thread, let the dispatcher go idle, then await this. A
	/// render that was already in flight when the call was made carries an older
	/// render generation and can never satisfy it, so the returned frame is never
	/// a stale one. It is equally the way to ask for a frame when nothing is
	/// expected to have changed and there would otherwise be no new frame to wait
	/// for.
	/// </para>
	/// <para>
	/// Rendering is serial, so the returned frame also carries a later
	/// <see cref="TestFrame.Sequence"/> than any frame published before the call.
	/// </para>
	/// </remarks>
	/// <param name="timeout">How long to wait.</param>
	/// <returns>The frame.</returns>
	/// <exception cref="TimeoutException">No such frame was published in time.</exception>
	public async Task<TestFrame> RequestFrameAsync(TimeSpan timeout)
	{
		EnsureLaunched();

		// TWO render passes, deliberately. The compositor DRAWS the picture it
		// recorded last and RECORDS the current tree on the UI thread, and it
		// only queues that recording when a frame is asked for - so a single
		// pass publishes the panel as it was BEFORE whatever was just applied.
		// The first pass is what makes the compositor record the tree as it is
		// now, the drain is what lets the UI thread run that recording, and the
		// second pass is the one that draws it.
		await RequestOnePassAsync(timeout).ConfigureAwait(false);
		await DrainUIThreadAsync().ConfigureAwait(false);
		return await RequestOnePassAsync(timeout).ConfigureAwait(false);
	}

	/// <summary>
	/// The most recently published frame, without waiting for anything.
	/// </summary>
	/// <returns>The frame.</returns>
	/// <exception cref="InvalidOperationException">The application has not drawn anything yet.</exception>
	public TestFrame CaptureLatestFrame()
	{
		lock (_frameLock)
		{
			if (_latestSnapshot is not { } snapshot)
			{
				throw new InvalidOperationException("The application has not published a frame yet.");
			}
			return new TestFrame(Width, Height, _latestSequence, _latestRenderGeneration, snapshot);
		}
	}

	/// <summary>Puts a finger down on the panel.</summary>
	/// <param name="pointerId">The finger's id.</param>
	/// <param name="x">The x coordinate, in device pixels.</param>
	/// <param name="y">The y coordinate, in device pixels.</param>
	public void TouchPress(int pointerId, int x, int y)
		=> SendTouch(FrameBufferEmulatorProtocol.TouchPressMessage, pointerId, x, y);

	/// <summary>Moves a finger that is already down.</summary>
	/// <param name="pointerId">The finger's id.</param>
	/// <param name="x">The x coordinate, in device pixels.</param>
	/// <param name="y">The y coordinate, in device pixels.</param>
	public void TouchMove(int pointerId, int x, int y)
		=> SendTouch(FrameBufferEmulatorProtocol.TouchMoveMessage, pointerId, x, y);

	/// <summary>Lifts a finger off the panel.</summary>
	/// <param name="pointerId">The finger's id.</param>
	/// <param name="x">The x coordinate, in device pixels.</param>
	/// <param name="y">The y coordinate, in device pixels.</param>
	public void TouchRelease(int pointerId, int x, int y)
		=> SendTouch(FrameBufferEmulatorProtocol.TouchReleaseMessage, pointerId, x, y);

	/// <summary>
	/// Taps the panel: one finger down and up again at the same point.
	/// </summary>
	/// <param name="x">The x coordinate, in device pixels.</param>
	/// <param name="y">The y coordinate, in device pixels.</param>
	public void Tap(int x, int y)
	{
		TouchPress(0, x, y);
		TouchRelease(0, x, y);
	}

	/// <summary>Presses a key.</summary>
	/// <param name="key">The key.</param>
	/// <param name="hardwareKeyCode">The X11-style hardware keycode, or 0 for none.</param>
	/// <param name="unicode">The Unicode codepoint this press types, or 0 for none.</param>
	public void KeyDown(VirtualKey key, uint hardwareKeyCode = 0, uint unicode = 0)
		=> SendKey(pressed: true, key, hardwareKeyCode, unicode);

	/// <summary>Releases a key.</summary>
	/// <param name="key">The key.</param>
	/// <param name="hardwareKeyCode">The X11-style hardware keycode, or 0 for none.</param>
	/// <param name="unicode">The Unicode codepoint, or 0 for none.</param>
	public void KeyUp(VirtualKey key, uint hardwareKeyCode = 0, uint unicode = 0)
		=> SendKey(pressed: false, key, hardwareKeyCode, unicode);

	/// <summary>
	/// Types <paramref name="text"/> one character at a time: a press carrying
	/// the character's codepoint, then a release. Letters, digits and the space
	/// get their own <see cref="VirtualKey"/>; anything else is typed as
	/// <see cref="VirtualKey.None"/> carrying only the codepoint, which is what a
	/// text control reads.
	/// </summary>
	/// <param name="text">The text to type.</param>
	public void TypeText(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		foreach (var character in text)
		{
			var key = KeyFor(character);
			KeyDown(key, unicode: character);
			KeyUp(key, unicode: character);
		}
	}

	/// <summary>
	/// Runs <paramref name="action"/> on the application's UI thread and waits
	/// for it to finish. An exception it throws is re-raised to the caller.
	/// </summary>
	/// <param name="action">The work to run.</param>
	/// <returns>A task that completes when the work has run.</returns>
	public Task RunOnUIThreadAsync(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		EnsureLaunched();

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_host!.RunOnUIThread(() =>
		{
			try
			{
				action();
				completion.TrySetResult();
			}
			catch (Exception exception)
			{
				completion.TrySetException(exception);
			}
		});
		return completion.Task;
	}

	/// <summary>
	/// Starts <paramref name="action"/> on the application's UI thread and waits
	/// for the task it returns. An exception it throws is re-raised to the caller.
	/// </summary>
	/// <param name="action">The work to run.</param>
	/// <returns>A task that completes when the work has run.</returns>
	public Task RunOnUIThreadAsync(Func<Task> action)
	{
		ArgumentNullException.ThrowIfNull(action);
		EnsureLaunched();

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_host!.RunOnUIThread(() =>
		{
			try
			{
				var running = action();
				if (running is null)
				{
					completion.TrySetResult();
					return;
				}
				running.ContinueWith(
					finished =>
					{
						if (finished.IsFaulted)
						{
							completion.TrySetException(finished.Exception!.InnerExceptions);
						}
						else if (finished.IsCanceled)
						{
							completion.TrySetCanceled();
						}
						else
						{
							completion.TrySetResult();
						}
					},
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}
			catch (Exception exception)
			{
				completion.TrySetException(exception);
			}
		});
		return completion.Task;
	}

	/// <summary>
	/// Shuts the application down the way it would shut itself down: the
	/// application exits on its own UI thread, the host's run loop returns, and
	/// the host thread ends. The process is never killed.
	/// </summary>
	/// <param name="timeout">How long to wait for the host thread to end.</param>
	/// <returns>A task that completes when the host thread has ended.</returns>
	/// <exception cref="TimeoutException">The host thread did not end in time.</exception>
	public async Task ShutdownAsync(TimeSpan timeout)
	{
		if (_hostThread is null)
		{
			return;
		}

		if (!_hostExited.Task.IsCompleted)
		{
			await RunOnUIThreadAsync(() => Microsoft.UI.Xaml.Application.Current?.Exit()).ConfigureAwait(false);
		}

		using (var cancellation = new CancellationTokenSource(timeout))
		using (cancellation.Token.Register(() => _hostExited.TrySetException(new TimeoutException(
			FormattableString.Invariant($"The test target host thread did not end within {timeout}.")))))
		{
			await _hostExited.Task.ConfigureAwait(false);
		}

		_hostThread.Join();
		Volatile.Write(ref _isLaunched, false);
		_hostFailure?.Throw();
	}

	private Task<TestFrame> RequestOnePassAsync(TimeSpan timeout)
	{
		// The generation is taken first: from this point only a frame rendered
		// from it or from a later one can answer the wait.
		var generation = _host!.RequestRenderAndGetGeneration();

		FrameWaiter waiter;
		lock (_frameLock)
		{
			if (_latestSnapshot is { } snapshot && _latestRenderGeneration >= generation)
			{
				return Task.FromResult(
					new TestFrame(Width, Height, _latestSequence, _latestRenderGeneration, snapshot));
			}
			waiter = FrameWaiter.ForRenderGeneration(generation);
			_waiters.Add(waiter);
		}

		return AwaitFrameAsync(waiter, timeout);
	}

	// Lowest dispatcher priority on purpose: the dispatcher runs a pending
	// compositor recording ahead of every queued job, and every job queued
	// before this one ahead of it, so once this has run the picture the next
	// pass draws is the current tree.
	private Task DrainUIThreadAsync()
	{
		var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_host!.RunOnUIThread(() =>
		{
			var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			if (queue is null
				|| !queue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
					() => drained.TrySetResult()))
			{
				drained.TrySetResult();
			}
		});
		return drained.Task;
	}

	internal void AttachHost(TestTargetHost host)
	{
		_host = host;
		host.Transport.FramePublished += OnFramePublished;
	}

	private async Task AwaitLaunchAsync(TimeSpan timeout)
	{
		using (var cancellation = new CancellationTokenSource(timeout))
		using (cancellation.Token.Register(() => _firstFrame.TrySetException(new TimeoutException(
			FormattableString.Invariant($"The test target did not publish its first frame within {timeout}.")))))
		{
			await _firstFrame.Task.ConfigureAwait(false);
		}
		Volatile.Write(ref _isLaunched, true);
	}

	private async Task<TestFrame> AwaitFrameAsync(FrameWaiter waiter, TimeSpan timeout)
	{
		using (var cancellation = new CancellationTokenSource(timeout))
		using (cancellation.Token.Register(() =>
		{
			lock (_frameLock)
			{
				_waiters.Remove(waiter);
			}
			waiter.Completion.TrySetException(new TimeoutException(FormattableString.Invariant(
				$"No {waiter.Description} was published within {timeout}.")));
		}))
		{
			return await waiter.Completion.Task.ConfigureAwait(false);
		}
	}

	private void OnFramePublished(long sequence, long renderGeneration, byte[] snapshot)
	{
		List<FrameWaiter>? ready = null;
		lock (_frameLock)
		{
			_latestSequence = sequence;
			_latestRenderGeneration = renderGeneration;
			_latestSnapshot = snapshot;
			for (var index = _waiters.Count - 1; index >= 0; index--)
			{
				if (_waiters[index].IsSatisfiedBy(sequence, renderGeneration))
				{
					(ready ??= new List<FrameWaiter>()).Add(_waiters[index]);
					_waiters.RemoveAt(index);
				}
			}
		}

		var frame = new TestFrame(Width, Height, sequence, renderGeneration, snapshot);
		_firstFrame.TrySetResult(frame);
		if (ready is not null)
		{
			foreach (var waiter in ready)
			{
				waiter.Completion.TrySetResult(frame);
			}
		}
	}

	private void SendTouch(uint messageType, int pointerId, int x, int y)
	{
		EnsureLaunched();
		_host!.Transport.SendTouch(messageType, pointerId, x, y);
	}

	private void SendKey(bool pressed, VirtualKey key, uint hardwareKeyCode, uint unicode)
	{
		EnsureLaunched();
		_host!.Transport.SendKey(pressed, (uint) key, hardwareKeyCode, unicode);
	}

	private void EnsureLaunched()
	{
		if (_host is null)
		{
			throw new InvalidOperationException(
				"The test target has not been launched yet. Call LaunchAsync first.");
		}
	}

	// Letters and digits are contiguous in VirtualKey and coincide with their
	// ASCII values, so the mapping is arithmetic; everything else types by
	// codepoint alone.
	private static VirtualKey KeyFor(char character) => character switch
	{
		>= 'a' and <= 'z' => VirtualKey.A + (char.ToUpperInvariant(character) - 'A'),
		>= 'A' and <= 'Z' => VirtualKey.A + (character - 'A'),
		>= '0' and <= '9' => VirtualKey.Number0 + (character - '0'),
		' ' => VirtualKey.Space,
		_ => VirtualKey.None,
	};

	// One registration list and one timeout path for both ways of waiting: past a
	// sequence number ("a frame arrived") and from a render generation ("a frame
	// drawn after I asked").
	private sealed class FrameWaiter
	{
		private readonly long? _afterSequence;
		private readonly long? _minimumRenderGeneration;

		private FrameWaiter(long? afterSequence, long? minimumRenderGeneration, string description)
		{
			_afterSequence = afterSequence;
			_minimumRenderGeneration = minimumRenderGeneration;
			Description = description;
		}

		internal string Description { get; }

		internal TaskCompletionSource<TestFrame> Completion { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		internal static FrameWaiter ForSequenceAfter(long afterSequence)
			=> new(afterSequence, null,
				FormattableString.Invariant($"frame after sequence {afterSequence}"));

		internal static FrameWaiter ForRenderGeneration(long minimumRenderGeneration)
			=> new(null, minimumRenderGeneration, FormattableString.Invariant(
				$"frame rendered from invalidation generation {minimumRenderGeneration} or later"));

		internal bool IsSatisfiedBy(long sequence, long renderGeneration)
			=> (_afterSequence is { } afterSequence && sequence > afterSequence)
				|| (_minimumRenderGeneration is { } minimum && renderGeneration >= minimum);
	}
}
