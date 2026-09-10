using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Xunit;

namespace CodeBrix.Platform.UI.Core.UIReqs.Hosting;

/// <summary>
/// The one virtual application a scenario run owns: it is set up and launched once, before the
/// first scenario, and shut down after the last one. Everything a step needs to reach the
/// running application - the session, the panel size, the UI thread and the frame handshake -
/// goes through here.
/// </summary>
public static class TestTargetFixture
{
	/// <summary>How long the application is given to launch and publish its first frame.</summary>
	public static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// How long a requested frame is waited for. Two seconds is deliberately short: a hang
	/// becomes a clear failure instead of a silently stale frame.
	/// </summary>
	public static readonly TimeSpan FrameTimeout = TimeSpan.FromSeconds(2);

	/// <summary>How long the application is given to lay itself out on the panel after launch.</summary>
	public static readonly TimeSpan LayoutTimeout = TimeSpan.FromSeconds(10);

	/// <summary>How long the application is given to shut down.</summary>
	public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

	private static readonly TimeSpan LayoutPollInterval = TimeSpan.FromMilliseconds(25);

	private static TestTargetSession? _session;
	private static TestDisplayOrientation? _orientation;

	/// <summary>The session that owns the panel.</summary>
	/// <exception cref="InvalidOperationException">The application has not launched yet.</exception>
	public static TestTargetSession Session =>
		_session ?? throw new InvalidOperationException(
			"The test target has not been launched. TestRunHooks launches it before the first scenario.");

	/// <summary>Whether the application is up.</summary>
	public static bool IsLaunched => _session is { IsLaunched: true };

	/// <summary>The orientation this assembly declared through its <see cref="TestPanelAttribute"/>.</summary>
	/// <exception cref="InvalidOperationException">The attribute has not been read yet.</exception>
	public static TestDisplayOrientation Orientation =>
		_orientation ?? throw new InvalidOperationException(
			"The panel orientation has not been read yet. TestRunHooks reads it before the first scenario.");

	/// <summary>The panel width in device pixels. Display scale is 1.0, so this is also logical pixels.</summary>
	public static int PanelWidth => Session.Width;

	/// <summary>The panel height in device pixels.</summary>
	public static int PanelHeight => Session.Height;

	/// <summary>
	/// The panel size in device pixels. Scenarios whose expectations depend on the panel derive
	/// their numbers from this, never from constants in a feature file.
	/// </summary>
	public static (int Width, int Height) PanelSize => (Session.Width, Session.Height);

	/// <summary>
	/// The tag that marks a scenario as belonging only to the OTHER orientation, and therefore
	/// as one this assembly skips.
	/// </summary>
	public static string ForeignTag =>
		Orientation == TestDisplayOrientation.Landscape ? "portrait-only" : "landscape-only";

	/// <summary>The tag that marks a scenario as belonging only to this assembly's orientation.</summary>
	public static string OwnTag =>
		Orientation == TestDisplayOrientation.Landscape ? "landscape-only" : "portrait-only";

	/// <summary>
	/// The most recent frame the fixture handed out. The scenario hooks save this one when a
	/// scenario fails.
	/// </summary>
	public static TestFrame? LatestFrame { get; private set; }

	/// <summary>
	/// Reads the orientation off the test assembly, sets the panel up, launches the virtual
	/// application and settles the first frame. Called once, from the test-run hook.
	/// </summary>
	/// <returns>A task that completes when the application is showing its laid-out first frame.</returns>
	public static async Task LaunchAsync()
	{
		if (_session is not null)
		{
			throw new InvalidOperationException("The test target has already been launched in this process.");
		}

		_orientation = ReadOrientation(typeof(TestTargetFixture).Assembly);

		var session = LinuxTestTarget.Setup(_orientation.Value);
		_session = session;

		await session.LaunchAsync(() => new VirtualApplication(), LaunchTimeout).ConfigureAwait(false);

		// The first published frame is the host's own forced repaint and can beat the
		// application's first layout pass, so it is legitimately empty. Wait for the tree to be
		// laid out on the panel, then do the handshake once: from here on every frame the
		// fixture hands out shows a laid-out application.
		await WaitForLayoutAsync().ConfigureAwait(false);
		LatestFrame = await NextFrameAsync().ConfigureAwait(false);
	}

	/// <summary>Shuts the virtual application down. Called once, from the test-run hook.</summary>
	/// <returns>A task that completes when the host thread has ended.</returns>
	public static async Task ShutdownAsync()
	{
		var session = _session;
		if (session is null)
		{
			return;
		}

		_session = null;
		LatestFrame = null;
		await session.ShutdownAsync(ShutdownTimeout).ConfigureAwait(false);
	}

	/// <summary>Reads the assembly-level <see cref="TestPanelAttribute"/>.</summary>
	/// <param name="testAssembly">The assembly the scenarios were generated into.</param>
	/// <returns>The orientation the assembly declared.</returns>
	/// <exception cref="InvalidOperationException">The assembly carries no such attribute.</exception>
	public static TestDisplayOrientation ReadOrientation(Assembly testAssembly)
	{
		ArgumentNullException.ThrowIfNull(testAssembly);

		var attribute = testAssembly.GetCustomAttribute<TestPanelAttribute>()
			?? throw new InvalidOperationException(
				$"Assembly '{testAssembly.GetName().Name}' carries no [assembly: TestPanel(...)] attribute, "
				+ "so there is no way to know which panel its scenarios run against.");

		return attribute.Orientation;
	}

	/// <summary>Runs an action on the UI thread and waits for it.</summary>
	/// <param name="action">The work to run.</param>
	/// <returns>A task that completes once the action has run.</returns>
	public static Task RunOnUIThreadAsync(Action action) => Session.RunOnUIThreadAsync(action);

	/// <summary>Runs asynchronous work on the UI thread and waits for it.</summary>
	/// <param name="action">The work to run.</param>
	/// <returns>A task that completes once the work has run.</returns>
	public static Task RunOnUIThreadAsync(Func<Task> action) => Session.RunOnUIThreadAsync(action);

	/// <summary>
	/// Waits for the UI thread to go idle: everything already queued has run by the time this
	/// returns. Implemented as one low-priority no-op queued behind the existing work, which is
	/// what makes an input event's effect complete before a frame is asked for.
	/// </summary>
	/// <returns>A task that completes when the dispatcher has drained.</returns>
	public static async Task WaitForIdleAsync()
	{
		var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		await RunOnUIThreadAsync(() =>
		{
			var queue = VirtualApplication.Running?.Root.DispatcherQueue;
			if (queue is null || !queue.TryEnqueue(DispatcherQueuePriority.Low, () => idle.TrySetResult()))
			{
				idle.TrySetResult();
			}
		}).ConfigureAwait(false);

		await idle.Task.ConfigureAwait(false);
	}

	/// <summary>
	/// The frame handshake: lay the tree out on the UI thread, let the dispatcher drain, then
	/// ask for ONE frame. The session only completes such a request with a frame whose
	/// rendering began after the request, so a render that was already in flight cannot answer
	/// it - there is no polling and no "after N frames" anywhere in this harness.
	/// </summary>
	/// <param name="timeout">How long to wait; the default is <see cref="FrameTimeout"/>.</param>
	/// <param name="label">
	/// The name the step is capturing this frame under, or <c>null</c> for the scenario's
	/// unnamed "current" frame. It is only used to name the file when the opt-in review archive
	/// is on; see <see cref="FrameReview"/>.
	/// </param>
	/// <returns>The frame that shows everything applied before the call.</returns>
	public static async Task<TestFrame> NextFrameAsync(TimeSpan? timeout = null, string? label = null)
	{
		await RunOnUIThreadAsync(() => VirtualApplication.Running?.Root.UpdateLayout()).ConfigureAwait(false);
		await WaitForIdleAsync().ConfigureAwait(false);

		var frame = await Session.RequestFrameAsync(timeout ?? FrameTimeout).ConfigureAwait(false);
		LatestFrame = frame;

		// Every frame the harness hands out passes through here, which is what makes "every
		// frame a scenario evaluated is on disk" a fact rather than a promise. Saving is off
		// unless the reviewer asked for it, and it never throws.
		FrameReview.Save(frame, label);
		return frame;
	}

	/// <summary>Shows one element, replacing whatever the root was holding.</summary>
	/// <param name="content">The element to show.</param>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	public static Task SetContentAsync(UIElement content) => VirtualApplication.Instance.SetContentAsync(content);

	/// <summary>Empties the root panel.</summary>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	public static Task ClearContentAsync() => VirtualApplication.Instance.ClearContentAsync();

	/// <summary>Finds an element by name anywhere below the root.</summary>
	/// <param name="name">The element name.</param>
	/// <returns>The element, or <c>null</c> when the tree holds no element of that name.</returns>
	public static FrameworkElement? FindByName(string name) => VirtualApplication.Instance.FindByName(name);

	private static async Task WaitForLayoutAsync()
	{
		var stopwatch = Stopwatch.StartNew();
		var width = Session.Width;
		var height = Session.Height;

		while (stopwatch.Elapsed < LayoutTimeout)
		{
			var laidOut = false;
			await RunOnUIThreadAsync(() =>
			{
				var root = VirtualApplication.Running?.Root;
				laidOut = root is not null
					&& (int) root.ActualWidth == width
					&& (int) root.ActualHeight == height;
			}).ConfigureAwait(false);

			if (laidOut)
			{
				return;
			}

			await Task.Delay(LayoutPollInterval, TestContext.Current.CancellationToken).ConfigureAwait(false);
		}

		throw new InvalidOperationException(FormattableString.Invariant(
			$"The virtual application did not lay out on a {width} x {height} panel within {LayoutTimeout}."));
	}

	/// <summary>A one-line description of the panel, for failure messages.</summary>
	/// <returns>The orientation and the panel size.</returns>
	public static string DescribePanel() => string.Create(CultureInfo.InvariantCulture,
		$"{Orientation} panel, {PanelWidth} x {PanelHeight} device pixels");
}
