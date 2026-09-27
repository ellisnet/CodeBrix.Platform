#nullable enable

using Windows.Foundation;

namespace Windows.UI.Core;

public partial class CoreWindow
#if false
	: ICoreWindowEvents
#endif
{
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyDown;

	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyUp;

#if false
	void ICoreWindowEvents.RaiseKeyDown(KeyEventArgs eventArgs) =>
		KeyDown?.Invoke(this, eventArgs);

	void ICoreWindowEvents.RaiseKeyUp(KeyEventArgs eventArgs) =>
		KeyUp?.Invoke(this, eventArgs);
#endif

	private ICodeBrixKeyboardInputSource? _keyboardSource;

	internal void SetKeyboardInputSource(ICodeBrixKeyboardInputSource source)
	{
		if (_keyboardSource is not null)
		{
			return;
		}

		_keyboardSource = source;
		_keyboardSource.KeyDown += (_, args) => KeyDown?.Invoke(this, args);
		_keyboardSource.KeyUp += (_, args) => KeyUp?.Invoke(this, args);
	}

	internal ICodeBrixKeyboardInputSource? KeyboardSource => _keyboardSource;

	/// <summary>
	/// Raises <see cref="KeyDown"/> or <see cref="KeyUp"/> for a key injected by
	/// <see cref="Windows.UI.Input.Preview.Injection.InputInjector"/>, as the keyboard source does for a real key.
	/// </summary>
	/// <param name="args">The key.</param>
	/// <param name="down"><see langword="true"/> for a press, <see langword="false"/> for a release.</param>
	internal void RaiseInjectedKey(KeyEventArgs args, bool down)
	{
		if (down)
		{
			KeyDown?.Invoke(this, args);
		}
		else
		{
			KeyUp?.Invoke(this, args);
		}
	}
}
