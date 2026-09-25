#nullable enable

using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.TerminalView.Engine;
using CodeBrix.Terminal.Engine;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The TerminalView engine (WPE1 C6): TerminalRenderer owns a CodeBrix.Terminal terminal, paints its cell grid, cursor
/// and selection onto an SKCanvas and runs the selection gestures; TerminalInputEncoder turns the engine's own key
/// names into VT sequences. Both run with only the engine Cores, SkiaSharp/HarfBuzz and a platform font source in the
/// process - and no WinUI assembly loads.
/// </summary>
public class TerminalViewEngineTests
{
	[Fact]
	public void When_A_Terminal_Is_Fed_Sized_Painted_And_Selected_Then_The_Grid_Reaches_The_Canvas_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		TestFontSourcePlatform.EnsureRegistered();
		var renderer = new TerminalRenderer { FontFamily = "monospace", FontSize = 16f, IsFocused = true };
		var invalidations = 0;
		var titles = new List<string>();
		renderer.InvalidateRequested += () => invalidations++;
		renderer.TitleChanged += titles.Add;
		const int width = 640;
		const int height = 200;

		//Act
		var resized = renderer.FitToSize(width, height, out var columns, out var rows);
		renderer.Feed("\u001b]0;engine title\u0007");
		renderer.Feed("hello \u001b[31mred\u001b[0m world\r\nsecond line");
		using var bitmap = new SKBitmap(width, height);
		using var canvas = new SKCanvas(bitmap);
		renderer.Paint(canvas, new SKSize(width, height));
		canvas.Flush();
		var cell = renderer.Metrics;
		var dragStarted = renderer.PressAt(0.5, cell.Height * 0.5, 1_000);
		renderer.DragTo(cell.Width * 5.5, cell.Height * 0.5);
		var ended = renderer.EndDrag();
		var selected = renderer.GetSelectedText();
		var doubleClickFirst = renderer.PressAt(cell.Width * 13.5, cell.Height * 0.5, 5_000);
		renderer.EndDrag();
		var doubleClickSecond = renderer.PressAt(cell.Width * 13.5, cell.Height * 0.5, 5_100);
		var word = renderer.GetSelectedText();
		var pixels = bitmap.Pixels;

		//Assert
		resized.Should().BeTrue();
		(renderer.Columns, renderer.Rows).Should().Be((columns, rows));
		columns.Should().Be((int)(width / cell.Width));
		rows.Should().Be((int)(height / cell.Height));
		titles.Should().Equal("engine title");
		pixels.Count(p => p.Red > 200 && p.Green > 200 && p.Blue > 200).Should().BeGreaterThan(20); // white text + the block cursor
		pixels.Any(p => p.Red > 120 && p.Green < 80 && p.Blue < 80).Should().BeTrue(); // the SGR 31 run
		dragStarted.Should().BeTrue();
		ended.Should().BeTrue();
		selected.Should().Be("hello");
		doubleClickFirst.Should().BeTrue();
		doubleClickSecond.Should().BeFalse(); // the second press on the same cell selects the word instead of dragging
		word.Should().Be("world");
		invalidations.Should().BeGreaterThan(0);

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.TerminalView.Core");
		EngineIsolation.AssertNoWinUILoaded("TerminalView");
	}

	[Fact]
	public void When_Keys_Are_Encoded_With_The_Engine_Key_Names_Then_The_VT_Sequences_And_Chords_Are_Right_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var encoder = new TerminalInputEncoder();

		//Act
		var enter = encoder.Encode(TerminalKey.Enter, '\r', applicationCursor: false);
		var up = encoder.Encode(TerminalKey.Up, null, applicationCursor: false);
		var upApplication = encoder.Encode(TerminalKey.Up, null, applicationCursor: true);
		var composed = encoder.Encode(TerminalKey.D9, '(', applicationCursor: false);
		encoder.UpdateModifier(TerminalModifierKey.Control, isDown: true).Should().BeTrue();
		var controlC = encoder.Encode(TerminalKey.C, 'c', applicationCursor: false);
		encoder.UpdateModifier(TerminalModifierKey.Shift, isDown: true);
		var copy = encoder.GetCommand(TerminalKey.C);
		var paste = encoder.GetCommand(TerminalKey.V);
		encoder.UpdateModifier(TerminalModifierKey.Control, isDown: false);
		var pageUp = encoder.GetCommand(TerminalKey.PageUp);
		var backTab = encoder.Encode(TerminalKey.Tab, '\t', applicationCursor: false);
		encoder.UpdateModifier(TerminalModifierKey.Shift, isDown: false);
		var notAModifier = encoder.UpdateModifier(TerminalModifierKey.None, isDown: true);
		encoder.UpdateModifier(TerminalModifierKey.CapsLock, isDown: true);
		encoder.UpdateModifier(TerminalModifierKey.CapsLock, isDown: false);

		//Assert
		enter.Should().Be("\r");
		up.Should().Be("\u001b[A");
		upApplication.Should().Be("\u001bOA");
		composed.Should().Be("(");
		controlC.Should().Be("\u0003");
		copy.Should().Be(TerminalKeyCommand.Copy);
		paste.Should().Be(TerminalKeyCommand.Paste);
		pageUp.Should().Be(TerminalKeyCommand.ScrollPageUp);
		backTab.Should().Be("\u001b[Z");
		notAModifier.Should().BeFalse();
		encoder.CapsLock.Should().BeTrue(); // Caps Lock toggles on the press, not the release
		encoder.Modifiers.Should().Be(TerminalModifiers.CapsLock);

		EngineIsolation.AssertNoWinUILoaded("TerminalView");
	}
}
