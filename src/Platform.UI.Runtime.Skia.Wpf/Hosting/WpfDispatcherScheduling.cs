namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf;

/// <summary>
/// Selects the WPF <see cref="System.Windows.Threading.DispatcherPriority"/> the CodeBrix
/// dispatcher pump is scheduled at on the WPF head.
/// </summary>
/// <remarks>
/// <para>
/// This only governs how CodeBrix's pump competes with WPF's own dispatcher queues. It never
/// changes how CodeBrix orders its own work: the pump callback is
/// <c>NativeDispatcher.DispatchItems</c>, which runs exactly one queued item per invocation and
/// re-enqueues itself while items remain, so CodeBrix's four internal priority queues (and its
/// render-fairness accounting) still decide what runs next.
/// </para>
/// </remarks>
public enum WpfDispatcherScheduling
{
	/// <summary>
	/// The historical default: the pump runs at <c>DispatcherPriority.Render</c> (7), above
	/// <c>DispatcherPriority.Input</c> (5). UI work is favored over input delivery.
	/// </summary>
	/// <remarks>
	/// An app that schedules UI work <em>continuously</em> — a game presenting a frame every tic, a
	/// perpetually animating canvas — keeps a Render-tier item pending at all times, so WPF never
	/// descends to the Input queue. Keyboard and pointer input is then starved outright rather than
	/// merely delayed: the app keeps rendering but stops responding, which reads as a hang. Such
	/// apps should opt into <see cref="InputFair"/>.
	/// </remarks>
	RenderFirst = 0,

	/// <summary>
	/// The pump runs at <c>DispatcherPriority.Input</c> (5) — the same tier WPF delivers keyboard
	/// and pointer input on — so pump items and input events share one FIFO queue and interleave.
	/// Input can no longer be starved by continuous UI work, at the cost of letting WPF's own
	/// higher tiers (Loaded, Render, DataBind, Normal) preempt CodeBrix work.
	/// </summary>
	/// <remarks>
	/// Opt in for continuously-repainting apps:
	/// <code>
	/// CodeBrixPlatformHostBuilder.Create()
	///     .App(() =&gt; new App())
	///     .UseWindowsWpf(wpf =&gt; wpf.DispatcherScheduling(WpfDispatcherScheduling.InputFair))
	///     .Build();
	/// </code>
	/// </remarks>
	InputFair = 1,
}
