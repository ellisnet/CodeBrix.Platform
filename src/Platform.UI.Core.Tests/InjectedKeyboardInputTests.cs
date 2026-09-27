#nullable enable

using System.Collections.Generic;
using System.Linq;
using SilverAssertions;
using Windows.Devices.Haptics;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-9 (decision D5): InputInjector.InjectKeyboardInput and KnownSimpleHapticsControllerWaveforms. Host-free: the
/// injector delivers into a recording target, so what is pinned here is the key event a real keyboard source would raise
/// for each injected key (virtual key, character, modifiers, key status). That the target's keyboard path then routes
/// the key to the focused element, access keys and accelerators exactly as a real key is fenced by the UIReqs feature
/// ThemeFocus/InjectedKeyboard.
/// </summary>
public class InjectedKeyboardInputTests
{
	/// <summary>A target that records what the injector delivers.</summary>
	private sealed class RecordingTarget : IInputInjectorTarget
	{
		public List<(KeyEventArgs Args, bool IsDown)> Keys { get; } = new();

		public List<PointerEventArgs> Pointers { get; } = new();

		public void InjectPointerAdded(PointerEventArgs args) => Pointers.Add(args);

		public void InjectPointerUpdated(PointerEventArgs args) => Pointers.Add(args);

		public void InjectPointerRemoved(PointerEventArgs args) => Pointers.Add(args);

		public void InjectKey(KeyEventArgs args, bool isDown) => Keys.Add((args, isDown));
	}

	private static InjectedInputKeyboardInfo Down(VirtualKey key) => new() { VirtualKey = (ushort)key };

	private static InjectedInputKeyboardInfo Up(VirtualKey key) => new() { VirtualKey = (ushort)key, KeyOptions = InjectedInputKeyOptions.KeyUp };

	private static (InputInjector Injector, RecordingTarget Target) NewInjector()
	{
		var target = new RecordingTarget();
		return (new InputInjector(target), target);
	}

	// ---------------------------------------------------------------- keys

	[Fact]
	public void When_A_Letter_Key_Is_Injected_Then_The_Target_Gets_Its_KeyDown_With_The_Character_And_Its_KeyUp()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[] { Down(VirtualKey.A), Up(VirtualKey.A) });

		//Assert
		target.Keys.Should().HaveCount(2);
		var (down, isDown) = target.Keys[0];
		isDown.Should().BeTrue();
		down.VirtualKey.Should().Be(VirtualKey.A);
		down.UnicodeKey.Should().Be('a');
		down.KeyboardModifiers.Should().Be(VirtualKeyModifiers.None);
		down.KeyStatus.IsKeyReleased.Should().BeFalse();
		down.KeyStatus.WasKeyDown.Should().BeFalse();
		down.KeyStatus.RepeatCount.Should().Be(1u);
		down.DeviceId.Should().Be("keyboard");

		var (up, isUpDown) = target.Keys[1];
		isUpDown.Should().BeFalse();
		up.VirtualKey.Should().Be(VirtualKey.A);
		up.UnicodeKey.Should().BeNull();
		up.KeyStatus.IsKeyReleased.Should().BeTrue();
		up.KeyStatus.WasKeyDown.Should().BeTrue();
	}

	[Fact]
	public void When_Shift_Is_Held_Then_A_Letter_Types_Uppercase_And_Carries_Shift_Until_Shift_Is_Released()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[]
		{
			Down(VirtualKey.Shift), Down(VirtualKey.H), Up(VirtualKey.H), Up(VirtualKey.Shift),
			Down(VirtualKey.I), Up(VirtualKey.I),
		});

		//Assert
		target.Keys.Select(k => k.Args.UnicodeKey).Should().Equal(new char?[] { null, 'H', null, null, 'i', null });
		target.Keys[1].Args.KeyboardModifiers.Should().Be(VirtualKeyModifiers.Shift);
		target.Keys[4].Args.KeyboardModifiers.Should().Be(VirtualKeyModifiers.None);
		injector.Keyboard.HeldModifiers.Should().Be(VirtualKeyModifiers.None);
	}

	[Fact]
	public void When_Control_Is_Held_Then_A_Letter_Types_No_Character_And_Carries_Control()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[] { Down(VirtualKey.Control), Down(VirtualKey.S) });

		//Assert
		var s = target.Keys[1].Args;
		s.VirtualKey.Should().Be(VirtualKey.S);
		s.UnicodeKey.Should().BeNull();
		s.KeyboardModifiers.Should().Be(VirtualKeyModifiers.Control);
		injector.Keyboard.HeldModifiers.Should().Be(VirtualKeyModifiers.Control);
	}

	[Fact]
	public void When_Alt_Is_Held_Then_The_Key_Reports_The_Menu_Key_Down_And_Types_No_Character()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[] { Down(VirtualKey.Menu), Down(VirtualKey.F) });

		//Assert
		var f = target.Keys[1].Args;
		f.KeyboardModifiers.Should().Be(VirtualKeyModifiers.Menu);
		f.KeyStatus.IsMenuKeyDown.Should().BeTrue();
		f.UnicodeKey.Should().BeNull();
	}

	[Fact]
	public void When_A_Key_Is_Injected_Down_Twice_Then_The_Second_Press_Is_A_Repeat()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[] { Down(VirtualKey.Down), Down(VirtualKey.Down), Up(VirtualKey.Down) });

		//Assert
		target.Keys.Select(k => k.Args.KeyStatus.WasKeyDown).Should().Equal(false, true, true);
		target.Keys.Select(k => k.Args.UnicodeKey).Should().Equal(new char?[] { null, null, null });
	}

	[Fact]
	public void When_The_Unicode_Option_Is_Used_Then_The_Key_Is_The_Packet_Key_Carrying_The_Character()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[]
		{
			new InjectedInputKeyboardInfo { ScanCode = 'é', KeyOptions = InjectedInputKeyOptions.Unicode },
			new InjectedInputKeyboardInfo { ScanCode = 'é', KeyOptions = InjectedInputKeyOptions.Unicode | InjectedInputKeyOptions.KeyUp },
		});

		//Assert
		target.Keys.Should().HaveCount(2);
		target.Keys[0].Args.VirtualKey.Should().Be((VirtualKey)0xE7);
		target.Keys[0].Args.UnicodeKey.Should().Be('é');
		target.Keys[0].Args.KeyStatus.ScanCode.Should().Be((uint)'é');
		target.Keys[1].IsDown.Should().BeFalse();
		target.Keys[1].Args.UnicodeKey.Should().BeNull();
	}

	[Fact]
	public void When_The_ScanCode_Option_Is_Used_Then_The_Scan_Code_Names_The_Key()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[]
		{
			new InjectedInputKeyboardInfo { ScanCode = 0x1E, KeyOptions = InjectedInputKeyOptions.ScanCode },
			new InjectedInputKeyboardInfo { ScanCode = 0x48, KeyOptions = InjectedInputKeyOptions.ScanCode | InjectedInputKeyOptions.ExtendedKey },
		});

		//Assert
		target.Keys[0].Args.VirtualKey.Should().Be(VirtualKey.A);
		target.Keys[0].Args.UnicodeKey.Should().Be('a');
		target.Keys[0].Args.KeyStatus.ScanCode.Should().Be(0x1Eu);
		target.Keys[1].Args.VirtualKey.Should().Be(VirtualKey.Up);
		target.Keys[1].Args.KeyStatus.IsExtendedKey.Should().BeTrue();
	}

	[Fact]
	public void When_An_Entry_Names_No_Key_Then_Nothing_Is_Delivered()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectKeyboardInput(new[] { new InjectedInputKeyboardInfo() });

		//Assert
		target.Keys.Should().BeEmpty();
	}

	[Fact]
	public void When_Control_Is_Held_By_Injection_Then_Injected_Mouse_Input_Carries_Control()
	{
		//Arrange
		var (injector, target) = NewInjector();

		//Act
		injector.InjectMouseInput(new[] { new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.Move, DeltaX = 1 } });
		injector.InjectKeyboardInput(new[] { Down(VirtualKey.Control) });
		injector.InjectMouseInput(new[] { new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.LeftDown } });
		injector.InjectKeyboardInput(new[] { Up(VirtualKey.Control) });
		injector.InjectMouseInput(new[] { new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.LeftUp } });

		//Assert
		target.Pointers.Select(p => p.KeyModifiers).Should().Equal(
			VirtualKeyModifiers.None, VirtualKeyModifiers.Control, VirtualKeyModifiers.None);
	}

	// ---------------------------------------------------------------- characters (US layout)

	[Theory]
	[InlineData(VirtualKey.Number1, false, false, '1')]
	[InlineData(VirtualKey.Number1, true, false, '!')]
	[InlineData(VirtualKey.Number0, true, false, ')')]
	[InlineData(VirtualKey.Z, false, true, 'Z')]
	[InlineData(VirtualKey.Z, true, true, 'z')]
	[InlineData(VirtualKey.Space, false, false, ' ')]
	[InlineData(VirtualKey.Enter, false, false, '\r')]
	[InlineData(VirtualKey.NumberPad7, false, false, '7')]
	[InlineData(VirtualKey.Divide, false, false, '/')]
	[InlineData((VirtualKey)0xBA, false, false, ';')]
	[InlineData((VirtualKey)0xBA, true, false, ':')]
	[InlineData((VirtualKey)0xDE, true, false, '"')]
	[InlineData((VirtualKey)0xDC, false, false, '\\')]
	public void When_A_Character_Key_Is_Pressed_Then_It_Types_What_A_US_Layout_Types(VirtualKey key, bool shift, bool capsLock, char expected)
	{
		//Act
		var character = InjectedKeyboardState.CharacterFor(key, shift ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None, capsLock);

		//Assert
		character.Should().Be(expected);
	}

	[Theory]
	[InlineData(VirtualKey.Tab)]
	[InlineData(VirtualKey.Back)]
	[InlineData(VirtualKey.Escape)]
	[InlineData(VirtualKey.Left)]
	[InlineData(VirtualKey.F5)]
	[InlineData(VirtualKey.Shift)]
	public void When_A_Key_That_Types_Nothing_Is_Pressed_Then_It_Has_No_Character(VirtualKey key)
		=> InjectedKeyboardState.CharacterFor(key, VirtualKeyModifiers.None, capsLock: false).Should().BeNull();

	[Theory]
	[InlineData((ushort)0x02, false, false, VirtualKey.Number1)]
	[InlineData((ushort)0x0B, false, false, VirtualKey.Number0)]
	[InlineData((ushort)0x10, false, false, VirtualKey.Q)]
	[InlineData((ushort)0x2C, false, false, VirtualKey.Z)]
	[InlineData((ushort)0x1C, false, false, VirtualKey.Enter)]
	[InlineData((ushort)0x3B, false, false, VirtualKey.F1)]
	[InlineData((ushort)0x58, false, false, VirtualKey.F12)]
	[InlineData((ushort)0x47, false, true, VirtualKey.NumberPad7)]
	[InlineData((ushort)0x47, false, false, VirtualKey.Home)]
	[InlineData((ushort)0x47, true, true, VirtualKey.Home)]
	[InlineData((ushort)0x53, true, false, VirtualKey.Delete)]
	[InlineData((ushort)0x5B, true, false, VirtualKey.LeftWindows)]
	[InlineData((ushort)0x7F, false, false, VirtualKey.None)]
	public void When_A_Scan_Code_Is_Mapped_Then_It_Gives_The_Key_Of_The_US_Layout(ushort scanCode, bool extended, bool numLock, VirtualKey expected)
		=> InjectedKeyboardState.VirtualKeyFromScanCode(scanCode, extended, numLock).Should().Be(expected);

	// ---------------------------------------------------------------- haptics

	[Fact]
	public void When_The_Known_Waveforms_Are_Read_Then_They_Are_The_HID_Haptics_Usage_Values()
	{
		//Act
		var values = new Dictionary<string, ushort>
		{
			["Click"] = KnownSimpleHapticsControllerWaveforms.Click,
			["BuzzContinuous"] = KnownSimpleHapticsControllerWaveforms.BuzzContinuous,
			["RumbleContinuous"] = KnownSimpleHapticsControllerWaveforms.RumbleContinuous,
			["Press"] = KnownSimpleHapticsControllerWaveforms.Press,
			["Release"] = KnownSimpleHapticsControllerWaveforms.Release,
			["Hover"] = KnownSimpleHapticsControllerWaveforms.Hover,
			["Success"] = KnownSimpleHapticsControllerWaveforms.Success,
			["Error"] = KnownSimpleHapticsControllerWaveforms.Error,
			["InkContinuous"] = KnownSimpleHapticsControllerWaveforms.InkContinuous,
			["PencilContinuous"] = KnownSimpleHapticsControllerWaveforms.PencilContinuous,
			["MarkerContinuous"] = KnownSimpleHapticsControllerWaveforms.MarkerContinuous,
			["ChiselMarkerContinuous"] = KnownSimpleHapticsControllerWaveforms.ChiselMarkerContinuous,
			["BrushContinuous"] = KnownSimpleHapticsControllerWaveforms.BrushContinuous,
			["EraserContinuous"] = KnownSimpleHapticsControllerWaveforms.EraserContinuous,
			["GalaxyPenContinuous"] = KnownSimpleHapticsControllerWaveforms.GalaxyPenContinuous,
		};

		//Assert
		values.Should().Equal(new Dictionary<string, ushort>
		{
			["Click"] = 0x1003,
			["BuzzContinuous"] = 0x1004,
			["RumbleContinuous"] = 0x1005,
			["Press"] = 0x1006,
			["Release"] = 0x1007,
			["Hover"] = 0x1008,
			["Success"] = 0x1009,
			["Error"] = 0x100A,
			["InkContinuous"] = 0x100B,
			["PencilContinuous"] = 0x100C,
			["MarkerContinuous"] = 0x100D,
			["ChiselMarkerContinuous"] = 0x100E,
			["BrushContinuous"] = 0x100F,
			["EraserContinuous"] = 0x1010,
			["GalaxyPenContinuous"] = 0x1011,
		});
	}
}
