using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Reqnroll;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps of the InjectedKeyboard scenarios: keys injected with the public
/// <see cref="InputInjector.InjectKeyboardInput"/> on the UI thread, as an application (or an automation harness) does,
/// instead of arriving on the panel's keyboard. An injected key must take the path a real key takes: the key events of
/// the focused element, focus navigation, access keys, keyboard accelerators and the character it types.
/// </summary>
[Binding]
public sealed class InjectedKeyboardSteps
{
	private const string ShiftedDigits = ")!@#$%^&*(";

	private static readonly Dictionary<char, (int Key, bool Shift)> Punctuation = new()
	{
		[' '] = ((int)VirtualKey.Space, false),
		[';'] = (0xBA, false), [':'] = (0xBA, true),
		['='] = (0xBB, false), ['+'] = (0xBB, true),
		[','] = (0xBC, false), ['<'] = (0xBC, true),
		['-'] = (0xBD, false), ['_'] = (0xBD, true),
		['.'] = (0xBE, false), ['>'] = (0xBE, true),
		['/'] = (0xBF, false), ['?'] = (0xBF, true),
		['`'] = (0xC0, false), ['~'] = (0xC0, true),
		['['] = (0xDB, false), ['{'] = (0xDB, true),
		['\\'] = (0xDC, false), ['|'] = (0xDC, true),
		[']'] = (0xDD, false), ['}'] = (0xDD, true),
		['\''] = (0xDE, false), ['"'] = (0xDE, true),
	};

	/// <summary>
	/// Types text with injected virtual keys, the way a US keyboard types it: each character is its key, with Shift
	/// pressed around it where the character needs Shift. All the keys go in one InjectKeyboardInput call.
	/// </summary>
	/// <param name="text">The text to type (letters, digits, space and US punctuation).</param>
	/// <returns>A task that completes once the keys have been injected and the UI thread is idle.</returns>
	[When("the text {string} is typed with injected keys")]
	public async Task When_the_text_is_typed_with_injected_keys(string text)
	{
		var input = new List<InjectedInputKeyboardInfo>();
		foreach (var character in text)
		{
			var (key, shift) = KeyOf(character);
			if (shift)
			{
				input.Add(Down(VirtualKey.Shift));
			}

			input.Add(Down((VirtualKey)key));
			input.Add(Up((VirtualKey)key));
			if (shift)
			{
				input.Add(Up(VirtualKey.Shift));
			}
		}

		await InjectAsync(input).ConfigureAwait(false);
	}

	/// <summary>Types text with injected Unicode characters (<see cref="InjectedInputKeyOptions.Unicode"/>).</summary>
	/// <param name="text">The text to type; any UTF-16 text.</param>
	/// <returns>A task that completes once the keys have been injected and the UI thread is idle.</returns>
	[When("the text {string} is typed with injected Unicode characters")]
	public async Task When_the_text_is_typed_with_injected_Unicode_characters(string text)
	{
		var input = new List<InjectedInputKeyboardInfo>();
		foreach (var character in text)
		{
			input.Add(new InjectedInputKeyboardInfo { ScanCode = character, KeyOptions = InjectedInputKeyOptions.Unicode });
			input.Add(new InjectedInputKeyboardInfo { ScanCode = character, KeyOptions = InjectedInputKeyOptions.Unicode | InjectedInputKeyOptions.KeyUp });
		}

		await InjectAsync(input).ConfigureAwait(false);
	}

	/// <summary>Injects one key, pressed and released.</summary>
	/// <param name="key">The key.</param>
	/// <returns>A task that completes once the key has been injected and the UI thread is idle.</returns>
	[When("the key {string} is injected")]
	public async Task When_the_key_is_injected(VirtualKey key) =>
		await InjectAsync([Down(key), Up(key)]).ConfigureAwait(false);

	/// <summary>Injects one key, pressed and released while an injected Control key is held down.</summary>
	/// <param name="key">The key.</param>
	/// <returns>A task that completes once the keys have been injected and the UI thread is idle.</returns>
	[When("the key {string} is injected with the Control key held down")]
	public async Task When_the_key_is_injected_with_the_Control_key_held_down(VirtualKey key) =>
		await InjectAsync([Down(VirtualKey.Control), Down(key), Up(key), Up(VirtualKey.Control)]).ConfigureAwait(false);

	/// <summary>
	/// Gives an element a keyboard accelerator (the key with the Control modifier) and records its Invoked as
	/// "Invoked". The accelerator is not handled by the recorder, so the element's own action (a Button's Click) runs.
	/// </summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="key">The accelerator's key.</param>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	[Given("{string} has the keyboard accelerator Control+{string}")]
	public async Task Given_has_the_keyboard_accelerator_Control(string name, VirtualKey key)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var element = ElementRegistry.Resolve(name) as UIElement
				?? throw new NotSupportedException($"\"{name}\" is not a UIElement, so it cannot have a keyboard accelerator.");
			var accelerator = new KeyboardAccelerator { Key = key, Modifiers = VirtualKeyModifiers.Control };
			accelerator.Invoked += (_, _) => EventRecorder.Record(name, "Invoked");
			element.KeyboardAccelerators.Add(accelerator);
		}).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	/// <summary>Gives an element an access key.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="accessKey">The access key.</param>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	[Given("{string} has the access key {string}")]
	public async Task Given_has_the_access_key(string name, string accessKey)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var element = ElementRegistry.Resolve(name) as UIElement
				?? throw new NotSupportedException($"\"{name}\" is not a UIElement, so it cannot have an access key.");
			element.AccessKey = accessKey;
		}).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Records the key events an element sees, by name: PreviewKeyDown, KeyDown, KeyUp and PreviewKeyUp (handled
	/// events too, since a TextBox handles the keys it types).
	/// </summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes once the UI thread has attached the recorders.</returns>
	[Given("the key events of {string} are recorded")]
	public async Task Given_the_key_events_of_are_recorded(string name)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var element = ElementRegistry.Resolve(name) as UIElement
				?? throw new NotSupportedException($"\"{name}\" is not a UIElement, so it has no key events.");
			element.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((_, _) => EventRecorder.Record(name, "PreviewKeyDown")), true);
			element.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, _) => EventRecorder.Record(name, "KeyDown")), true);
			element.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler((_, _) => EventRecorder.Record(name, "PreviewKeyUp")), true);
			element.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, _) => EventRecorder.Record(name, "KeyUp")), true);
		}).ConfigureAwait(false);
	}

	private static async Task InjectAsync(IReadOnlyList<InjectedInputKeyboardInfo> input)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var injector = InputInjector.TryCreate()
				?? throw new InvalidOperationException("The UI thread has no input injector target.");
			injector.InjectKeyboardInput(input);
		}).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	private static (int Key, bool Shift) KeyOf(char character) => character switch
	{
		>= 'a' and <= 'z' => ((int)VirtualKey.A + (character - 'a'), false),
		>= 'A' and <= 'Z' => ((int)VirtualKey.A + (character - 'A'), true),
		>= '0' and <= '9' => ((int)VirtualKey.Number0 + (character - '0'), false),
		_ when ShiftedDigits.IndexOf(character) is var digit and >= 0 => ((int)VirtualKey.Number0 + digit, true),
		_ when Punctuation.TryGetValue(character, out var key) => key,
		_ => throw new NotSupportedException($"'{character}' has no key on a US keyboard; type it with injected Unicode characters."),
	};

	private static InjectedInputKeyboardInfo Down(VirtualKey key) => new() { VirtualKey = (ushort)key };

	private static InjectedInputKeyboardInfo Up(VirtualKey key) =>
		new() { VirtualKey = (ushort)key, KeyOptions = InjectedInputKeyOptions.KeyUp };
}
