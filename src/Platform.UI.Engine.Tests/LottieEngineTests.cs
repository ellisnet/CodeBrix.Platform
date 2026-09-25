#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CodeBrix.Platform.UI.Lottie.Engine;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The Lottie engine (WPE1 C9; D-M7, our own player over Skottie): LottieColorTheme rewrites an animation's colour
/// bindings, LottiePlayer decodes it, plays segments on an ITickSource, and renders frames onto an SKCanvas - with only
/// the engine Core, SkiaSharp and Skottie in the process: no WinUI assembly loads.
/// </summary>
public class LottieEngineTests
{
	private static string ThemedJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "themed.json"));

	[Fact]
	public void When_A_Colour_Binding_Is_Set_Then_The_Theme_Rewrites_The_Bound_Shapes_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var theme = new LottieColorTheme();

		//Act
		var beforeLoad = theme.HasDocument;
		theme.Load(new MemoryStream(Encoding.UTF8.GetBytes(ThemedJson)));
		var appliedWithoutColour = theme.ApplyProperties();
		theme.SetColor("Foreground", LottieColorTheme.ToArgb(0xFF, 0xFF, 0x00, 0x00));
		var applied = theme.ApplyProperties();
		var json = theme.GetJson()!;
		var key = theme.GetCacheKey("themed");
		theme.SetColor("Foreground", null);
		var cleared = theme.GetColor("Foreground");

		//Assert
		beforeLoad.Should().BeFalse();
		theme.HasDocument.Should().BeTrue();
		appliedWithoutColour.Should().BeFalse();
		applied.Should().BeTrue();
		json.Replace(" ", string.Empty).Should().Contain("\"k\":[1,0,0,1]");
		key.Should().Be("themed-Foreground-#FFFF0000");
		cleared.Should().Be(LottieColorTheme.ToArgb(0xFF, 0xFF, 0x00, 0x00)); // a null pending colour falls back to the current one

		EngineIsolation.AssertNoWinUILoaded("Lottie (theme)");
	}

	[Fact]
	public void When_An_Animation_Is_Played_Paused_Sought_And_Rendered_Then_Frames_Reach_The_Canvas_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var theme = new LottieColorTheme();
		theme.Load(new MemoryStream(Encoding.UTF8.GetBytes(ThemedJson)));
		theme.SetColor("Foreground", LottieColorTheme.ToArgb(0xFF, 0xFF, 0x00, 0x00));
		theme.ApplyProperties();
		var tickSources = new List<TestTickSource>();
		var playing = new List<bool>();
		var player = new LottiePlayer(() =>
		{
			var source = new TestTickSource();
			tickSources.Add(source);
			return source;
		}, action => action());
		var invalidations = 0;
		player.InvalidateRequested += () => invalidations++;
		player.IsPlayingChanged += playing.Add;
		using var bitmap = new SKBitmap(200, 100);
		using var canvas = new SKCanvas(bitmap);

		//Act
		player.Play(0, 1, looped: true); // no animation yet: only remembered
		var rememberedBeforeAnimation = player.PlayState;
		player.Animation = LottiePlayer.CreateAnimation(theme.GetJson()!);
		player.Play(0, 1, looped: true);
		var timer = tickSources[0];
		timer.RaiseTick();
		canvas.Clear(SKColors.Transparent);
		player.Render(canvas, new SKSize(200, 100), LottieStretch.Uniform, playbackRate: 1, clearColor: null);
		canvas.Flush();
		var centre = bitmap.GetPixel(100, 50);
		var leftMargin = bitmap.GetPixel(10, 50); // Uniform: a 100x100 animation centred in 200x100
		player.Pause();
		var pausedTimerRunning = timer.IsRunning;
		player.Resume();
		player.SetProgress(2); // clamped to 1, stops
		var afterSetProgress = player.PlayState;
		player.Play(0, 0.0001, looped: false);
		Thread.Sleep(20);
		player.Render(canvas, new SKSize(200, 100), LottieStretch.Fill, playbackRate: 1, clearColor: SKColors.White);
		var afterSegmentEnd = player.PlayState;

		//Assert
		rememberedBeforeAnimation.Should().Be(new LottiePlayState(0, 1, true));
		tickSources.Count.Should().Be(2); // a new frame timer on each Play with an animation
		timer.Interval.Should().Be(TimeSpan.FromSeconds(1 / 30d));
		invalidations.Should().BeGreaterThanOrEqualTo(2); // the tick, the progress change
		centre.Red.Should().Be(255);
		centre.Green.Should().Be(0);
		leftMargin.Alpha.Should().Be(0);
		pausedTimerRunning.Should().BeFalse();
		afterSetProgress.Should().BeNull();
		afterSegmentEnd.Should().BeNull(); // a non-looped segment stops itself at its end
		playing.Should().Equal(true, false, true, false, true, false);
		Assert.Throws<InvalidOperationException>(() => LottiePlayer.CreateAnimation("{ \"not\": \"lottie\" }"));

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.Lottie.Core");
		EngineIsolation.AssertNoWinUILoaded("Lottie (player)");
	}

	[Fact]
	public void When_Scales_Are_Built_For_Each_Stretch_Then_They_Match_The_Image_Rules_And_No_WinUI_Assembly_Loads()
	{
		LottiePlayer.BuildScale(LottieStretch.None, 200, 100, 100, 100).Should().Be((1d, 1d));
		LottiePlayer.BuildScale(LottieStretch.Fill, 200, 100, 100, 100).Should().Be((2d, 1d));
		LottiePlayer.BuildScale(LottieStretch.Uniform, 200, 100, 100, 100).Should().Be((1d, 1d));
		LottiePlayer.BuildScale(LottieStretch.UniformToFill, 200, 100, 100, 100).Should().Be((2d, 2d));
		LottiePlayer.BuildScale(LottieStretch.Uniform, double.PositiveInfinity, 50, 100, 100).Should().Be((0.5d, 0.5d));

		EngineIsolation.AssertNoWinUILoaded("Lottie (stretch)");
	}

	private sealed class TestTickSource : ITickSource
	{
		public TimeSpan Interval { get; set; }

		public event Action? Tick;

		internal bool IsRunning { get; private set; }

		public void Start() => IsRunning = true;

		public void Stop() => IsRunning = false;

		internal void RaiseTick() => Tick?.Invoke();
	}
}
