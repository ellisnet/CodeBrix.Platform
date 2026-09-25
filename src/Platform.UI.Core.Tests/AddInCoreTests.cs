#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Windows.Input;
using CodeBrix.Platform.AppSettings;
using CodeBrix.Platform.UI.AudioPlayer.Skia;
using CodeBrix.Platform.UI.CommandBar;
using CodeBrix.Platform.UI.FlexPanel;
using CodeBrix.Platform.UI.VideoPlayer.Skia;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using CodeBrix.Platform.OpenGL;
using Microsoft.Web.WebView2.Core;
using MediaPlayerEngine = Windows.Media.Playback.MediaPlayer;
using FlexPanelControl = CodeBrix.Platform.UI.FlexPanel.FlexPanel;
using AudioPlayerControl = CodeBrix.Platform.UI.AudioPlayer.Skia.AudioPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The host-free proof for the add-ins split into a platform-neutral Core assembly (Phase C): each add-in's Core
/// assembly is used on the Core framework assemblies alone - no Skia assembly of the framework or of the add-in, and
/// no SkiaSharp (or HarfBuzz) assembly is loaded into the process.
/// </summary>
public class AddInCoreTests
{
	[Fact]
	public void When_A_FlexPanel_Lays_Out_Its_Children_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		var panel = new FlexPanelControl { Direction = FlexDirection.Row, Width = 300, Height = 100 };
		var children = Enumerable.Range(0, 3).Select(_ => new Border { Width = 50, Height = 20 }).ToArray();
		foreach (var child in children)
		{
			panel.Children.Add(child);
		}

		//Act
		panel.Measure(new Size(300, 100));
		panel.Arrange(new Rect(0, 0, 300, 100));

		//Assert
		children.Select(c => LayoutInformation.GetLayoutSlot(c).X).Should().Equal(0d, 50d, 100d);
		typeof(FlexPanelControl).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.FlexPanel.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.UI.FlexPanel.Core");
	}

	[Fact]
	public void When_A_ToolBar_Is_Built_With_Buttons_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		var command = new CountingCommand();
		var open = new ToolButton { Text = "Open", Shortcut = "Ctrl+O", Command = command };
		var bold = new ToolToggleButton { Text = "Bold", IsChecked = true };
		var bar = new ToolBar();

		//Act
		bar.Items.Add(open);
		bar.Items.Add(new ToolBarSeparator());
		bar.Items.Add(bold);
		var tray = new ToolBarTray();
		tray.Children.Add(bar);

		//Assert
		bar.Items.Count.Should().Be(3);
		tray.Children.Should().ContainSingle();
		open.Text.Should().Be("Open");
		open.Command.Should().BeSameAs(command);
		bold.IsChecked.Should().BeTrue();
		typeof(ToolBar).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.CommandBar.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.UI.CommandBar.Core");
	}

	[Fact]
	public void When_Settings_Are_Stored_Watched_And_Wrapped_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		AppSettingLoggingService.ConsoleOutput = false;
		var changes = 0;
		AppSettingsService.Shutdown();
		AppSettingsService.Initialize("HostFreeCoreTests", "/memory/host-free");

		try
		{
			//Act
			AppSettingsService.AddSettingHandler("Volume", (_, _) => changes++);
			AppSettingsService.Set("Volume", 7);
			var theme = AppSettingsService.Wrap("Theme", HostFreeTheme.Light);
			theme.Value = HostFreeTheme.Dark;
			AppSettingsService.Shutdown();
			AppSettingsService.Initialize("HostFreeCoreTests", "/memory/host-free");

			//Assert
			AppSettingsService.Get("Volume", 0).Should().Be(7);
			AppSettingsService.Get("Theme", HostFreeTheme.Light).Should().Be(HostFreeTheme.Dark);
			AppSettingsService.Store.WasCreatedFresh.Should().BeFalse();
			AppSettingsService.DirectoryPath.Should().Be("/memory/host-free");
			changes.Should().Be(1);
			typeof(AppSettingsStore).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.AppSettings.Core");
			AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.AppSettings.Core");
		}
		finally
		{
			AppSettingsService.Shutdown();
		}
	}

	[Fact]
	public void When_An_AudioPlayer_Loads_Seeks_And_A_SoundEffect_Plays_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		var player = new AudioPlayerControl { Volume = 0.5, IsLooping = true };
		var output = AddInTestPlatform.LastAudioPlayer!;

		//Act
		player.SetSourceStream(new MemoryStream(new byte[] { 1, 2, 3 }));
		player.Seek(TimeSpan.FromSeconds(5));
		player.Pause();
		var effectPlayed = SoundEffect.Play(new MemoryStream(new byte[] { 1, 2, 3 }));

		//Assert
		output.Volume.Should().Be(0.5f);
		output.IsLooping.Should().BeTrue();
		player.Duration.Should().Be(TimeSpan.FromSeconds(3));
		player.DurationSeconds.Should().Be(3.0);
		player.Position.Should().Be(TimeSpan.FromSeconds(3), "a seek past the end is clamped to the duration");
		player.PositionSeconds.Should().Be(3.0);
		player.IsPlaying.Should().BeFalse();
		output.Calls.Should().Equal("Load(stream)", "Seek(3)", "Pause");
		effectPlayed.Should().BeTrue();
		AddInTestPlatform.AudioOutput.OneShotEffects.Should().Be(1);
		typeof(AudioPlayerControl).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.AudioPlayer.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.UI.AudioPlayer.Core");
	}

	[Fact]
	public void When_A_GLCanvasElement_Is_Created_And_Invalidated_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		var before = AddInTestPlatform.GLCanvas.Owners.Count;

		//Act
		var canvas = new HostFreeGLCanvas();
		canvas.Invalidate();
		canvas.OnVisualPainting();

		//Assert
		AddInTestPlatform.GLCanvas.Owners.Skip(before).Should().ContainSingle().Which.Should().BeSameAs(canvas);
		canvas.GetGLInitializationState().Status.Should().Be(GLInitializationStatus.NotYetInitialized);
		canvas.IsGLInitialized.Should().BeNull("the element is not loaded");
		canvas.RenderOverrides.Should().Be(0, "a canvas that is not loaded has no context to render with");
		typeof(GLCanvasElement).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.WinUI.Graphics3DGL.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.WinUI.Graphics3DGL.Core");
	}

	[Fact]
	public void When_A_Video_Source_Is_Resolved_And_A_Seek_Is_Clamped_Then_Only_Core_Assemblies_Are_Loaded()
	{
		//Arrange
		var error = new InvalidOperationException("no decoder");

		//Act
		var file = VideoSourceResolver.Resolve("file:///videos/clip.webm");
		var web = VideoSourceResolver.Resolve("https://example.com/clip.webm");
		var failed = new VideoPlayerFailedEventArgs("The video could not be opened.", error);

		//Assert
		file.PathOrUrl.Should().Be("/videos/clip.webm");
		file.Stream.Should().BeNull();
		web.PathOrUrl.Should().Be("https://example.com/clip.webm");
		VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(5)).Should().Be(TimeSpan.FromSeconds(5));
		VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(-1), TimeSpan.Zero).Should().Be(TimeSpan.Zero);
		VideoPlayerRules.IsRenderPathChangeAllowed(true, DayOfWeek.Monday, DayOfWeek.Tuesday).Should().BeFalse();
		VideoPlayerRules.IsRenderPathChangeAllowed(false, DayOfWeek.Monday, DayOfWeek.Tuesday).Should().BeTrue();
		failed.Message.Should().Be("The video could not be opened.");
		failed.Error.Should().BeSameAs(error);
		typeof(VideoPlayerFailedEventArgs).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.VideoPlayer.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.UI.VideoPlayer.Core");
	}

	/// <summary>
	/// The WebView package's Core content (plan section 4.6: the WebView2 API surface and the CoreWebView2 model) is the
	/// framework's own, in CodeBrix.Platform.UI.Core; the add-in assembly holds only the per-OS plumbing (the Linux WPE
	/// engine and its INativeWebViewProvider registration). Proof: the control and its model work with neither the
	/// add-in nor SkiaSharp in the process.
	/// </summary>
	[Fact]
	public void When_A_WebView2_Is_Created_Then_Its_Api_And_Model_Are_Framework_Core_And_No_Plumbing_Is_Loaded()
	{
		//Act
		var webView = new WebView2();

		//Assert
		webView.CoreWebView2.Should().NotBeNull();
		typeof(WebView2).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		typeof(CoreWebView2).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		typeof(INativeWebViewProvider).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.UI.Core");
	}

	/// <summary>
	/// The MediaPlayer package's Core content (plan section 4.6: the MediaPlayerElement API surface) is the framework's
	/// own, in CodeBrix.Platform.UI.Core and CodeBrix.Platform.Core; the add-in assembly holds only the LibVLC plumbing
	/// (CodeBrix.Platform.MediaPlayerCore) behind the framework's IMediaPlayerExtension. Proof: the element and its
	/// player work with neither the add-in, LibVLC's managed side nor SkiaSharp in the process.
	/// </summary>
	[Fact]
	public void When_A_MediaPlayerElement_Gets_A_Player_Then_Its_Api_Is_Framework_Core_And_No_LibVlc_Is_Loaded()
	{
		//Arrange
		var player = new MediaPlayerEngine();
		var element = new MediaPlayerElement();

		//Act
		element.SetMediaPlayer(player);

		//Assert
		element.MediaPlayer.Should().BeSameAs(player);
		typeof(MediaPlayerElement).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		typeof(MediaPlayerEngine).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.Core");
		typeof(global::CodeBrix.Platform.Media.Playback.IMediaPlayerExtension).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.Core");
		AssertOnlyCoreAssembliesAreLoaded("CodeBrix.Platform.Core");
	}

	/// <summary>
	/// The Skia-canvas add-ins (plan rule R7) draw with SkiaSharp, so their Core assemblies reference it - but never
	/// the framework's Skia assemblies, the heads or the add-ins' own Skia assemblies. Read from each assembly's
	/// metadata only: nothing is loaded, so the SkiaSharp these assemblies reference stays out of this process (the
	/// other tests here assert that it is not loaded). The paths come from this project's csproj (the R7Core
	/// assembly metadata), which builds these Core projects without referencing them.
	/// </summary>
	[Theory]
	[InlineData("CodeBrix.Platform.SkiaSharp.Views.Core")]
	[InlineData("CodeBrix.Platform.WinUI.Graphics2DSK.Core")]
	[InlineData("CodeBrix.Platform.UI.Svg.Core")]
	[InlineData("CodeBrix.Platform.UI.Lottie.Core")]
	[InlineData("CodeBrix.Platform.UI.TextLayout.Core")]
	[InlineData("CodeBrix.Platform.UI.AdvancedTextEdit.Core")]
	[InlineData("CodeBrix.Platform.UI.TerminalView.Core")]
	[InlineData("CodeBrix.Platform.UI.PlotterView.Core")]
	public void When_A_Skia_Canvas_AddIn_Core_Is_Read_Then_It_References_SkiaSharp_And_No_Skia_Assembly_Of_The_Platform(string addInCore)
	{
		//Arrange
		var path = typeof(AddInCoreTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Single(a => a.Key == "R7Core:" + addInCore).Value!;

		//Act
		using var stream = File.OpenRead(path);
		using var pe = new PEReader(stream);
		var metadata = pe.GetMetadataReader();
		var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);
		var references = metadata.AssemblyReferences
			.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name))
			.ToArray();

		//Assert
		name.Should().Be(addInCore);
		references.Should().Contain("SkiaSharp");
		if (addInCore == "CodeBrix.Platform.UI.TextLayout.Core")
		{
			// Since the text-engine home change (WPE1 C5) TextLayout is Core by whole and carries its own copy of the text
			// engine, which names no XAML or WinRT type: it references no framework XAML assembly at all.
			references.Should().NotContain(new[]
			{
				"CodeBrix.Platform.UI.Core", "CodeBrix.Platform.Core", "CodeBrix.Platform.UI.Composition.Core",
				"CodeBrix.Platform.UI.Dispatching.Core",
			});
		}
		else
		{
			references.Should().Contain("CodeBrix.Platform.UI.Core");
		}
		references.Should().NotContain(n => n.StartsWith("CodeBrix.Platform.UI.Runtime.Skia", StringComparison.Ordinal));
		references.Should().NotContain(new[]
		{
			"CodeBrix.Platform.UI", "CodeBrix.Platform.UI.Composition", "CodeBrix.Platform", "CodeBrix.Platform.UI.Dispatching",
			"CodeBrix.Platform.UI.Toolkit",
			"CodeBrix.Platform.SkiaSharp.Views", "CodeBrix.Platform.UI.Svg", "CodeBrix.Platform.UI.Lottie",
			"CodeBrix.Platform.WinUI.Graphics2DSK",
			"CodeBrix.Platform.UI.TextLayout", "CodeBrix.Platform.UI.AdvancedTextEdit", "CodeBrix.Platform.UI.TerminalView",
			"CodeBrix.Platform.UI.PlotterView",
		});
	}

	private static void AssertOnlyCoreAssembliesAreLoaded(string addInCore)
	{
		var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "").ToArray();
		loaded.Should().NotContain(n => n.StartsWith("SkiaSharp", StringComparison.Ordinal) || n.StartsWith("HarfBuzzSharp", StringComparison.Ordinal));
		loaded.Should().Contain(addInCore);
		loaded.Should().NotContain(new[]
		{
			"CodeBrix.Platform.UI", "CodeBrix.Platform.UI.Composition", "CodeBrix.Platform", "CodeBrix.Platform.UI.Dispatching",
			"CodeBrix.Platform.UI.CommandBar", "CodeBrix.Platform.UI.Svg", "CodeBrix.Platform.UI.FlexPanel",
			"CodeBrix.Platform.AppSettings", "CodeBrix.Sqlite", "Microsoft.Data.Sqlite",
			"CodeBrix.Platform.UI.AudioPlayer.Skia", "CodeBrix.Audio", "CodeBrix.Audio.Engine",
			"CodeBrix.Platform.WinUI.Graphics3DGL", "CodeBrix.Platform.UI.VideoPlayer.Skia", "CodeBrix.VideoPlayback",
			"CodeBrix.Platform.UI.WebView.Skia", "CodeBrix.Platform.UI.MediaPlayer.Skia", "CodeBrix.MediaCore",
		});
	}

	/// <summary>A setting value of an enum type, stored as its name.</summary>
	public enum HostFreeTheme
	{
		/// <summary>The light theme.</summary>
		Light,

		/// <summary>The dark theme.</summary>
		Dark,
	}

	/// <summary>A GLCanvasElement that counts its renders; it is never loaded, so it never gets a context.</summary>
	private sealed class HostFreeGLCanvas : GLCanvasElement
	{
		public HostFreeGLCanvas()
			: base(null)
		{
		}

		internal int RenderOverrides { get; private set; }

		protected override void Init(GL gl) { }

		protected override void OnDestroy(GL gl) { }

		protected override void RenderOverride(GL gl) => RenderOverrides++;
	}

	/// <summary>A command that counts its executions.</summary>
	private sealed class CountingCommand : ICommand
	{
		public event EventHandler? CanExecuteChanged { add { } remove { } }

		internal int Executions { get; private set; }

		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => Executions++;
	}
}
