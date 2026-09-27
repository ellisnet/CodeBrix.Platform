#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Logging;
using Windows.Devices.Input;
using Windows.System;

namespace Windows.UI.Input.Preview.Injection;

public partial class InputInjector
{
	[ThreadStatic] private static IInputInjectorTarget? _inputManager;

	internal static void SetTargetForCurrentThread(IInputInjectorTarget manager)
	{
		if (_inputManager is not null &&
			_inputManager != manager &&
			manager.Log().IsEnabled(LogLevel.Warning))
		{
			manager.Log().LogWarning($"InputInjector is already set for this thread.");
		}

		_inputManager ??= manager; // Set only once per thread.
	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public static InputInjector? TryCreate()
		=> _inputManager is not null ? new InputInjector(_inputManager) : null;

	private readonly InjectedInputState _mouse = new(PointerDeviceType.Mouse);
	private readonly InjectedKeyboardState _keyboard = new();
	private (InjectedInputState state, bool isAdded)? _touch;

	/// <summary>
	/// Gets the current state of the mouse pointer
	/// </summary>
	internal InjectedInputState Mouse => _mouse;

	private readonly IInputInjectorTarget _target;

	/// <summary>
	/// Gets the keys this injector holds down (the modifiers they add to injected keyboard, mouse and touch input).
	/// </summary>
	internal InjectedKeyboardState Keyboard => _keyboard;

	// Internal (not private) so that host-free tests can inject into a target of their own.
	internal InputInjector(IInputInjectorTarget target)
	{
		_target = target;
	}

	/// <summary>
	/// Injects keyboard input. Each key goes to the target's keyboard path exactly as a key of the real keyboard does:
	/// the key events of the focused element (with the tunneling Preview events), access keys, keyboard accelerators,
	/// and the character the key types.
	/// </summary>
	/// <param name="input">The keys, pressed and released in order.</param>
	/// <remarks>
	/// Like the pointer injection, the keys are delivered synchronously, on the calling (UI) thread. The modifier keys
	/// injected as down stay down until they are injected as up, and apply to the injected mouse and touch input too.
	/// </remarks>
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public void InjectKeyboardInput(IEnumerable<InjectedInputKeyboardInfo> input)
	{
		ArgumentNullException.ThrowIfNull(input);

		foreach (var info in input)
		{
			if (info is null)
			{
				continue;
			}

			var args = _keyboard.Apply(info, out var isDown);
			if (args is not null)
			{
				_target.InjectKey(args, isDown);
			}
		}
	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public void InitializeTouchInjection(InjectedInputVisualizationMode visualMode)
	{
		UninitializeTouchInjection();

		_touch = (new InjectedInputState(PointerDeviceType.Touch), isAdded: false);
	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public void UninitializeTouchInjection()
	{
		if (_touch is not null)
		{
			var cancel = new InjectedInputTouchInfo
			{
				PointerInfo = new()
				{
					PointerId = 42,
					PointerOptions = InjectedInputPointerOptions.Canceled
				}
			};

			_target.InjectPointerRemoved(cancel.ToEventArgs(_touch.Value.state));

			_touch = null;
		}
	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public void InjectTouchInput(IEnumerable<InjectedInputTouchInfo> input)
	{
		if (_touch is null)
		{
			InitializeTouchInjection(InjectedInputVisualizationMode.Default);
		}

		var touch = _touch!.Value.state;
		foreach (var info in input)
		{
			var args = info.ToEventArgs(touch, _keyboard.HeldModifiers);

			if (_touch is { isAdded: false })
			{
				_target.InjectPointerAdded(args);
				_touch = (touch, isAdded: true);
			}

			touch.Update(args);

			_target.InjectPointerUpdated(args);

			if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				_target.InjectPointerRemoved(args);
				_touch = (touch, isAdded: false);
			}
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectTouchInputAsync(IEnumerable<InjectedInputTouchInfo> input, CancellationToken ct)
	{
		if (_touch is null)
		{
			InitializeTouchInjection(InjectedInputVisualizationMode.Default);
		}

		var touch = _touch!.Value.state;
		foreach (var info in input)
		{
			var args = info.ToEventArgs(touch, _keyboard.HeldModifiers);

			if (_touch is { isAdded: false })
			{
				_target.InjectPointerAdded(args);
				_touch = (touch, isAdded: true);
				await WaitForIdle(ct);
			}

			touch.Update(args);

			_target.InjectPointerUpdated(args);
			await WaitForIdle(ct);

			if (info.PointerInfo.PointerOptions.HasFlag(InjectedInputPointerOptions.PointerUp))
			{
				_target.InjectPointerRemoved(args);
				_touch = (touch, isAdded: false);
				await WaitForIdle(ct);
			}
		}
	}

	private const InjectedInputMouseOptions _mouseButtonDown = InjectedInputMouseOptions.LeftDown | InjectedInputMouseOptions.MiddleDown | InjectedInputMouseOptions.RightDown | InjectedInputMouseOptions.XDown;

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo> input)
	{
		foreach (var info in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, _keyboard.HeldModifiers);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectMouseInputAsync(IEnumerable<InjectedInputMouseInfo> input, CancellationToken ct)
	{
		foreach (var info in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, _keyboard.HeldModifiers);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
			await WaitForIdle(ct);
		}
	}

	// TODO: Move as extension method
	internal void InjectMouseInput(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input)
	{
		foreach (var (info, modifiers) in input)
		{
			var args = info.ToEventArgs(_mouse!, modifiers);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
		}
	}

	// TODO: Move as extension method
	internal async ValueTask InjectMouseInputAsync(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)> input, CancellationToken ct)
	{
		foreach (var (info, modifiers) in input)
		{
			if ((info.MouseOptions & _mouseButtonDown) != 0 && !_mouse.Properties.HasPressedButton)
			{
				_mouse.StartNewSequence();
			}

			var args = info.ToEventArgs(_mouse!, modifiers);
			_mouse!.Update(args);

			_target.InjectPointerUpdated(args);
			await WaitForIdle(ct);
		}
	}

	private async ValueTask WaitForIdle(CancellationToken ct)
	{
		var dispatcher = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException();
		var tcs = new TaskCompletionSource();
		await using var _ = ct.Register(() => tcs.TrySetCanceled());

		Enqueue(() => Enqueue(() => tcs.TrySetResult()));

		await tcs.Task;

		void Enqueue(DispatcherQueueHandler action)
		{
			if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, action))
			{
				tcs.TrySetException(new Exception("Cannot enqueue work item on dispatcher"));
			}
		}
	}
}
