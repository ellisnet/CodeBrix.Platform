using System;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The GPU-backed panel, which Skia heads do not support.
/// </summary>
/// <remarks>
/// <para>
/// On a Skia head <c>SKSwapChainPanel</c>'s constructor throws by default, so an application that
/// ports XAML from a Windows head learns about it immediately rather than seeing a blank area. An
/// application that wants the type to exist anyway - a shared XAML page that hides the panel on
/// Skia, say - sets <c>RaiseOnUnsupported</c> to false and gets an inert control.
/// </para>
/// <para>
/// <c>RaiseOnUnsupported</c> is a public static, so every test that changes it restores it in a
/// finally, and the suite runs one test at a time (see AssemblyInfo.cs).
/// </para>
/// </remarks>
public class SKSwapChainPanelTests
{
	[Fact]
	public void RaiseOnUnsupported_is_true_by_default()
	{
		//Every other test in this class depends on this being the state it restores to.
		SKSwapChainPanel.RaiseOnUnsupported.Should().BeTrue();
	}

	[Fact]
	public void ctor_throws_NotSupportedException_by_default()
	{
		//Arrange
		//Act
		var thrown = Record.Exception(() => new SKSwapChainPanel());

		//Assert
		thrown.Should().BeOfType<NotSupportedException>();
		thrown!.Message.Should().Contain("not supported");
	}

	[Fact]
	public void ctor_succeeds_when_RaiseOnUnsupported_is_false()
	{
		//Arrange
		SKSwapChainPanel.RaiseOnUnsupported = false;

		try
		{
			//Act
			var panel = new SKSwapChainPanel();

			//Assert
			panel.CanvasSize.Should().Be(SKSize.Empty);
			panel.GRContext.Should().BeNull();
			panel.EnableRenderLoop.Should().BeFalse();
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}
	}

	[Fact]
	public void CanvasSize_and_GRContext_throw_while_RaiseOnUnsupported_is_true()
	{
		//Arrange
		//The two properties carry their own guard, so a panel constructed while the switch was off
		//starts throwing again the moment an application turns the switch back on.
		SKSwapChainPanel.RaiseOnUnsupported = false;
		SKSwapChainPanel panel;
		try
		{
			panel = new SKSwapChainPanel();
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}

		//Act
		var canvasSize = Record.Exception(() => panel.CanvasSize);
		var grContext = Record.Exception(() => panel.GRContext);

		//Assert
		canvasSize.Should().BeOfType<NotSupportedException>();
		grContext.Should().BeOfType<NotSupportedException>();
	}

	[Fact]
	public void Invalidate_does_nothing_and_never_raises_PaintSurface()
	{
		//Arrange
		SKSwapChainPanel.RaiseOnUnsupported = false;

		try
		{
			var panel = new SKSwapChainPanel();
			var paintCount = 0;
			panel.PaintSurface += (s, e) => paintCount++;

			//Act
			var thrown = Record.Exception(() => panel.Invalidate());

			//Assert
			//The Skia implementation's DoInvalidate is deliberately empty: there is no swap chain
			//to present. It must be a no-op rather than a throw, so a shared XAML page that calls
			//it on a timer does not fall over on Skia.
			thrown.Should().BeNull();
			paintCount.Should().Be(0);
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}
	}

	[Fact]
	public void EnableRenderLoop_round_trips()
	{
		//Arrange
		SKSwapChainPanel.RaiseOnUnsupported = false;

		try
		{
			var panel = new SKSwapChainPanel();

			//Act
			panel.EnableRenderLoop = true;
			var whileOn = panel.EnableRenderLoop;
			panel.EnableRenderLoop = false;

			//Assert
			//The property has an equality guard and forwards to a partial method the Skia flavour
			//does not implement, so the value is simply remembered.
			whileOn.Should().BeTrue();
			panel.EnableRenderLoop.Should().BeFalse();
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}
	}

	[Fact]
	public void DrawInBackground_throws_NotImplementedException_both_ways()
	{
		//Arrange
		SKSwapChainPanel.RaiseOnUnsupported = false;

		try
		{
			var panel = new SKSwapChainPanel();

			//Act
			var read = Record.Exception(() => panel.DrawInBackground);
			var written = Record.Exception(() => panel.DrawInBackground = true);

			//Assert
			//This one is unimplemented rather than unsupported, and says so with a different
			//exception type - the distinction is worth keeping.
			read.Should().BeOfType<NotImplementedException>();
			written.Should().BeOfType<NotImplementedException>();
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}
	}

	[Fact]
	public void ContentsScale_is_left_at_zero_because_the_Skia_ctor_does_not_initialize()
	{
		//Arrange
		//CHARACTERISATION. The shared constructor path calls Initialize(), which reads the display
		//and sets ContentsScale; the Skia flavour's constructor does not call it, because the
		//control is unsupported there. So ContentsScale keeps its default. An application must not
		//read it on a Skia head expecting a display scale.
		SKSwapChainPanel.RaiseOnUnsupported = false;

		try
		{
			//Act
			var panel = new SKSwapChainPanel();

			//Assert
			panel.ContentsScale.Should().Be(0d);
		}
		finally
		{
			SKSwapChainPanel.RaiseOnUnsupported = true;
		}
	}

	[Fact]
	public void RaiseOnUnsupported_is_restored_after_every_test_in_this_class()
	{
		//The class's whole discipline in one assertion, so a forgotten finally is caught here
		//rather than as an unrelated failure elsewhere.
		SKSwapChainPanel.RaiseOnUnsupported.Should().BeTrue();
	}
}
