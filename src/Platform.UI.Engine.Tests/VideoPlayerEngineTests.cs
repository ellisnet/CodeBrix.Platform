#nullable enable

using System;
using System.IO;
using System.Reflection;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.VideoPlayer.Skia;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Contracts;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The VideoPlayer engine part (WPE1 C11): the source resolver finds ms-appx:/// assets and embedded://./ resources
/// through the platform's IAssetLocation, and the transport rules (seek clamp, render-path rule) and the failure args are
/// plain - all with only the engine Core in the process: no WinUI assembly loads. (The element itself stays in the Skia
/// assembly: its API is made of CodeBrix.VideoPlayback and SkiaSharp types.)
/// </summary>
public class VideoPlayerEngineTests
{
	[Fact]
	public void When_Video_Sources_Are_Resolved_Through_The_Asset_Location_Then_Every_Form_Resolves_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var assets = TestAssetLocation.EnsureRegistered();

		//Act
		var asset = VideoSourceResolver.Resolve("ms-appx:///Videos/My%20Clip.webm");
		var library = VideoSourceResolver.Resolve("ms-appx://SomeLibrary/Clip.cbv");
		var file = VideoSourceResolver.Resolve("file:///videos/clip.webm");
		var web = VideoSourceResolver.Resolve("https://example.com/clip.webm");
		var embedded = VideoSourceResolver.Resolve("embedded://./(assembly).Assets.themed.json");
		using var _ = embedded.Stream;
		var failed = new VideoPlayerFailedEventArgs("The video could not be opened.", new InvalidOperationException("no decoder"));

		//Assert
		asset.PathOrUrl.Should().Be(Path.Join(assets.InstalledPath, "Videos/My Clip.webm"));
		library.PathOrUrl.Should().Be(Path.Join(assets.InstalledPath, "SomeLibrary/Clip.cbv"));
		file.PathOrUrl.Should().Be("/videos/clip.webm");
		web.PathOrUrl.Should().Be("https://example.com/clip.webm");
		embedded.PathOrUrl.Should().BeNull();
		embedded.Stream.Should().NotBeNull();
		failed.Message.Should().Be("The video could not be opened.");
		VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(5)).Should().Be(TimeSpan.FromSeconds(5));
		VideoPlayerRules.IsRenderPathChangeAllowed(true, DayOfWeek.Monday, DayOfWeek.Tuesday).Should().BeFalse();

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.VideoPlayer.Core");
		EngineIsolation.AssertNoWinUILoaded("VideoPlayer (source resolver)");
	}

	/// <summary>A platform's asset location for the VideoPlayer contract.</summary>
	private sealed class TestAssetLocation : IAssetLocation
	{
		private static readonly object _gate = new();
		private static TestAssetLocation? _instance;

		internal static TestAssetLocation EnsureRegistered()
		{
			lock (_gate)
			{
				if (_instance is null)
				{
					var location = new TestAssetLocation();
					ApiExtensibility.Register(typeof(IAssetLocation), _ => location);
					_instance = location;
				}

				return _instance;
			}
		}

		public string InstalledPath => Path.Combine(Path.GetTempPath(), "wpe1-3-video-package");

		public Assembly ApplicationAssembly => typeof(VideoPlayerEngineTests).Assembly;
	}
}
