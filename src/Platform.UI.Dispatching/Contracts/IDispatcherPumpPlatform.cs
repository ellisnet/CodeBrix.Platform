namespace CodeBrix.Platform.UI.Dispatching.Contracts;

/// <summary>
/// The platform pump that drives the UI-thread dispatcher queue: it schedules the queue's dispatch callback
/// on the platform's UI thread and reports whether the calling thread is that UI thread. The queue itself
/// (priorities, render-action interleaving, the dispatch loop) is platform-neutral and lives in
/// <see cref="NativeDispatcher"/>; only the pump is platform-specific.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once by <see cref="NativeDispatcher"/> (lazily, on first use) through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Dispatching.Skia.DispatcherPumpSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IDispatcherPumpPlatform
{
	/// <summary>
	/// Gets a value indicating whether the calling thread is the platform's UI thread. The dispatcher caches
	/// the answer per thread, so this is asked at most once per thread.
	/// </summary>
	bool HasThreadAccess { get; }

	/// <summary>
	/// Schedules <paramref name="dispatchCallback"/> to run once on the platform's UI thread. The dispatcher calls
	/// this whenever its queue goes from empty to non-empty, and again after each dispatched item while items remain.
	/// </summary>
	/// <param name="dispatchCallback">The dispatcher's dispatch callback; it runs one queued item per invocation.</param>
	/// <param name="priority">The priority of the item that caused the request, for pumps that map it to a native priority.</param>
	void Schedule(global::System.Action dispatchCallback, NativeDispatcherPriority priority);
}
