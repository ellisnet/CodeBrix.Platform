using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Windows.System;
using Windows.UI.Core;

namespace CodeBrix.Platform.UI.Core; //Was previously: Uno.UI.Core

/// <summary>
/// Tracks keyboard key state.
/// </summary>
/// <remarks>
///	The behavior is based on description in https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.corevirtualkeystates.
///	In UWP/WinUI, every key has a locked state (not only Caps Lock, etc.). The sequence of states is as follows:
///	(None) -> (Down) -> (None) -> (Down + Locked) -> (None + Locked) -> (Down) -> (None) -> etc.
/// </remarks>
internal static partial class KeyboardStateTracker
{
	private static readonly Dictionary<VirtualKey, CoreVirtualKeyStates> _keyStates = new Dictionary<VirtualKey, CoreVirtualKeyStates>();

	/// <summary>
	/// Retrieves the current state for a given key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns>Key state.</returns>
	internal static CoreVirtualKeyStates GetKeyState(VirtualKey key)
	{
		if (_keyStates.TryGetValue(key, out var state))
		{
			return state;
		}

		return CoreVirtualKeyStates.None;
	}

	/// <remarks>
	/// Currently this uses the same implementation as GetKeyState, 
	/// but kept separate to be able to differentiate between original calls
	/// to CoreWindow.GetKeyState and CoreWindow.GetAsyncKeyState.
	/// </remarks>
	internal static CoreVirtualKeyStates GetAsyncKeyState(VirtualKey key) => GetKeyState(key);

	internal static void OnKeyDown(VirtualKey key)
	{
		if (!_keyStates.TryGetValue(key, out var state))
		{
			// The first key press should not cause Locked state.
			state = CoreVirtualKeyStates.Down;
		}

		if (!state.HasFlag(CoreVirtualKeyStates.Locked))
		{
			_keyStates[key] = CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked;
		}
		else
		{
			_keyStates[key] = CoreVirtualKeyStates.Down;
		}

		SetStateOnNonSideKeys(key);
	}

	internal static void OnKeyUp(VirtualKey key)
	{
		if (!_keyStates.TryGetValue(key, out var state))
		{
			// Edge case - key is released without previous press.
			state = CoreVirtualKeyStates.None;
		}

		if (state.HasFlag(CoreVirtualKeyStates.Locked))
		{
			_keyStates[key] = CoreVirtualKeyStates.None | CoreVirtualKeyStates.Locked;
		}
		else
		{
			_keyStates[key] = CoreVirtualKeyStates.None;
		}

		SetStateOnNonSideKeys(key);
	}

	/// <summary>
	/// Reconciles the tracked modifier keys against the modifier mask carried by an input event.
	/// </summary>
	/// <param name="modifiers">The modifier mask the event carries.</param>
	/// <param name="eventKey">
	/// The key the event is about, when it is a key event, so that key is left to
	/// <see cref="OnKeyDown"/>/<see cref="OnKeyUp"/>; <see cref="VirtualKey.None"/> otherwise.
	/// </param>
	/// <remarks>
	/// Key down and key up alone cannot keep the modifier keys honest: a window manager that keeps a
	/// modifier combination for itself (an Alt drag, or a window menu on Alt+Space) swallows the
	/// release, and the modifier would then read "held" for every later click until the window was
	/// deactivated. Every routed key and pointer event carries the modifier mask the system had at
	/// the time, so that mask is the authority on the modifier keys - and only on them: a key the
	/// mask cannot speak for keeps whatever state its own key events gave it.
	/// </remarks>
	internal static void ReconcileModifiers(VirtualKeyModifiers modifiers, VirtualKey eventKey = VirtualKey.None)
	{
		ReconcileModifier(modifiers, VirtualKeyModifiers.Shift, VirtualKey.Shift, VirtualKey.LeftShift, VirtualKey.RightShift, eventKey);
		ReconcileModifier(modifiers, VirtualKeyModifiers.Control, VirtualKey.Control, VirtualKey.LeftControl, VirtualKey.RightControl, eventKey);
		ReconcileModifier(modifiers, VirtualKeyModifiers.Menu, VirtualKey.Menu, VirtualKey.LeftMenu, VirtualKey.RightMenu, eventKey);

		// There is no side-agnostic Windows key in VirtualKey, so a Windows modifier the mask reports
		// as held cannot be attributed to a side; a Windows modifier it reports as NOT held still
		// clears both sides, which is the direction that leaves a key stuck.
		ReconcileModifier(modifiers, VirtualKeyModifiers.Windows, VirtualKey.None, VirtualKey.LeftWindows, VirtualKey.RightWindows, eventKey);
	}

	private static void ReconcileModifier(
		VirtualKeyModifiers modifiers,
		VirtualKeyModifiers flag,
		VirtualKey combined,
		VirtualKey left,
		VirtualKey right,
		VirtualKey eventKey)
	{
		if (eventKey != VirtualKey.None && (eventKey == combined || eventKey == left || eventKey == right))
		{
			// The event is this modifier's own. A mask is sampled before the key that raised the
			// event is applied on some backends and after it on others, so it may not agree with the
			// event yet; the key's own down/up is what counts.
			return;
		}

		var held = (modifiers & flag) == flag;
		var tracked = IsDown(combined) || IsDown(left) || IsDown(right);
		if (held == tracked)
		{
			return;
		}

		if (held)
		{
			// A press that never arrived. The mask does not say which side it was.
			if (combined != VirtualKey.None)
			{
				OnKeyDown(combined);
			}

			return;
		}

		// A release that never arrived. Both sides go up before the side-agnostic key, so the
		// side-agnostic key ends up released whichever side was down.
		OnKeyUp(left);
		OnKeyUp(right);
		if (combined != VirtualKey.None)
		{
			OnKeyUp(combined);
		}
	}

	private static bool IsDown(VirtualKey key)
		=> key != VirtualKey.None
			&& _keyStates.TryGetValue(key, out var state)
			&& state.HasFlag(CoreVirtualKeyStates.Down);

	private static void SetStateOnNonSideKeys(VirtualKey key)
	{
		if (key == VirtualKey.LeftShift || key == VirtualKey.RightShift)
		{
			_keyStates[VirtualKey.Shift] = _keyStates[key];
		}

		if (key == VirtualKey.LeftControl || key == VirtualKey.RightControl)
		{
			_keyStates[VirtualKey.Control] = _keyStates[key];
		}

		if (key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu)
		{
			_keyStates[VirtualKey.Menu] = _keyStates[key];
		}
	}

	internal static void Reset() => _keyStates.Clear();

#pragma warning disable IDE0051 // Remove unused private members
	[JSExport]
	private static void UpdateKeyStateNative(string key, bool down)
#pragma warning restore IDE0051 // Remove unused private members
	{
		if (down)
		{
			OnKeyDown(BrowserVirtualKeyHelper.FromKey(key));
		}
		else
		{
			OnKeyUp(BrowserVirtualKeyHelper.FromKey(key));
		}
	}
}
