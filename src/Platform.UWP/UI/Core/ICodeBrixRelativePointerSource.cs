#nullable enable

using Windows.Devices.Input;

namespace Windows.UI.Core;

/// <summary>
/// Implemented by a head's pointer input source when it can deliver relative (raw delta)
/// mouse motion for <see cref="MouseDevice.MouseMoved"/>, confining the pointer to the
/// window while the relative session is active.
/// </summary>
/// <remarks>
/// The source must stay completely inert until <see cref="StartRelativeMouse"/> is called:
/// no raw-event selection and no pointer-lock plumbing at startup, so the default pointer
/// pipeline is unchanged for apps that never use relative mouse.
/// </remarks>
internal interface ICodeBrixRelativePointerSource // CodeBrix Only
{
	/// <summary>
	/// Begins a relative mouse session: confines the pointer to the window and starts
	/// delivering motion deltas to <paramref name="device"/>.
	/// </summary>
	void StartRelativeMouse(MouseDevice device);

	/// <summary>
	/// Ends the relative mouse session: releases confinement and stops delivering deltas.
	/// </summary>
	void StopRelativeMouse();
}
