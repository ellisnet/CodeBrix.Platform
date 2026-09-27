#nullable enable
using System;
using System.Linq;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal interface IInputInjectorTarget
{
	void InjectPointerAdded(PointerEventArgs args);

	void InjectPointerUpdated(PointerEventArgs args);

	void InjectPointerRemoved(PointerEventArgs args);

	/// <summary>
	/// Delivers one injected key transition along the path a key of the real keyboard takes (the keyboard source's
	/// KeyDown / KeyUp for the target's content root).
	/// </summary>
	/// <param name="args">The key, as a keyboard source raises it.</param>
	/// <param name="isDown"><see langword="true"/> for a press, <see langword="false"/> for a release.</param>
	void InjectKey(KeyEventArgs args, bool isDown);
}
