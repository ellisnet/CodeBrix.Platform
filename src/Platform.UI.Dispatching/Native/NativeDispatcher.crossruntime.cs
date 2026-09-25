#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using CodeBrix.Platform.UI.Dispatching.Contracts;

namespace CodeBrix.Platform.UI.Dispatching;

internal sealed partial class NativeDispatcher
{
	/// <summary>
	/// The dispatch callback handed to the pump, allocated once so that scheduling allocates nothing.
	/// </summary>
	private static readonly Action _dispatchItemsCallback = DispatchItems;

	private static IDispatcherPumpPlatform? _pumpPlatform;

	/// <summary>
	/// Gets the platform pump, resolved once on first use.
	/// </summary>
	private static IDispatcherPumpPlatform PumpPlatform => _pumpPlatform ??= PlatformContract.Resolve<IDispatcherPumpPlatform>();

	private bool GetHasThreadAccess() => PumpPlatform.HasThreadAccess;

	partial void EnqueueNative(NativeDispatcherPriority priority) => PumpPlatform.Schedule(_dispatchItemsCallback, priority);
}
#endif
