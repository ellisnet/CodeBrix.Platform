using System;
using System.ComponentModel;
using System.Diagnostics;
using CodeBrix.Platform.UI.Dispatching.Contracts;

namespace CodeBrix.Platform.UI.Dispatching.Skia;

/// <summary>
/// The Skia implementation of <see cref="IDispatcherPumpPlatform"/>: it forwards to the two delegates that the
/// running Skia head installs at startup (<see cref="DispatchOverride"/> and <see cref="HasThreadAccessOverride"/>),
/// which post work to the head's own event loop.
/// </summary>
internal sealed class DispatcherPumpSkiaPlatform : IDispatcherPumpPlatform
{
	/// <summary>
	/// The head's scheduler: posts the dispatcher callback to the head's UI event loop.
	/// </summary>
	/// <remarks>
	/// Host-free test processes install an inline scheduler here by reflection (see the test projects'
	/// <c>DispatcherInitializer</c>), so the field's name and type are part of that contract.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static Action<Action, NativeDispatcherPriority> DispatchOverride;

	/// <summary>
	/// The head's thread check: reports whether the calling thread is the head's UI thread.
	/// </summary>
	/// <remarks>
	/// Host-free test processes install an always-true check here by reflection, so the field's name and type
	/// are part of that contract.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static Func<bool> HasThreadAccessOverride;

	/// <inheritdoc />
	public bool HasThreadAccess
	{
		get
		{
			Debug.Assert(HasThreadAccessOverride != null, "HasThreadAccessOverride must be set.");

			return HasThreadAccessOverride();
		}
	}

	/// <inheritdoc />
	public void Schedule(Action dispatchCallback, NativeDispatcherPriority priority)
	{
		Debug.Assert(DispatchOverride != null, "DispatchOverride must be set.");

		DispatchOverride(dispatchCallback, priority);
	}
}
