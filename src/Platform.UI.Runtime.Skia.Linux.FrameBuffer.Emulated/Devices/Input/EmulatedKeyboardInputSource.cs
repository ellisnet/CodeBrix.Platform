using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

/// <summary>
/// The emulated device's keyboard: there isn't one. V1 of the frame-buffer
/// emulator forwards no key events whatever the IDE's stored "Hardware
/// keyboard support" option says, so this source never raises anything — the
/// app behaves exactly like it would on a kiosk with no keyboard attached.
/// </summary>
internal class EmulatedKeyboardInputSource : ICodeBrixKeyboardInputSource
{
#pragma warning disable CS0067 // Deliberately never raised — see the class remarks.
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067

	internal VirtualKeyModifiers GetCurrentModifiersState() => VirtualKeyModifiers.None;
}
