#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.AudioPlayer.Skia;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Engine;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The AudioPlayer engine (WPE1 C10): AudioTransport runs the whole transport (load, play/pause/stop, clamped and
/// debounced seeking, position polling, the natural end, failures) over a platform player and ITickSource timers, and the
/// source resolver finds assets through IAssetLocation - with only the engine Core in the process: no WinUI assembly loads.
/// </summary>
public class AudioPlayerEngineTests
{
	[Fact]
	public void When_A_Source_Is_Loaded_Played_Scrubbed_And_Ends_Then_The_Transport_Reports_It_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		AudioTestPlatform.EnsureRegistered();
		var player = new RecordingPlayer();
		var timers = new List<TestTickSource>();
		var queued = new List<Action>();
		var transport = new AudioTransport(player, () =>
		{
			var timer = new TestTickSource();
			timers.Add(timer);
			return timer;
		}, action =>
		{
			queued.Add(action);
			return true;
		});
		var playing = new List<bool>();
		var positions = new List<TimeSpan>();
		var durations = new List<TimeSpan>();
		var ended = 0;
		transport.IsPlayingChanged += playing.Add;
		transport.PositionUpdated += positions.Add;
		transport.DurationChanged += durations.Add;
		transport.PlaybackEnded += () => ended++;

		//Act
		transport.Play(); // nothing loaded: ignored
		transport.Load(p => p.Load("song.wav"), "song.wav", volume: 2.0, isLooping: true, autoPlay: true);
		var pollTimer = timers[0];
		player.Position = TimeSpan.FromSeconds(1);
		pollTimer.RaiseTick();
		transport.QueueSeek(TimeSpan.FromSeconds(99)); // a slider drag: debounced, clamped to the duration
		transport.QueueSeek(TimeSpan.FromSeconds(2));
		var debounce = timers[1];
		var seeksBeforeRelease = player.Seeks.Count;
		debounce.RaiseTick();
		transport.Seek(TimeSpan.FromSeconds(-5)); // immediate, clamped to zero
		transport.Pause();
		transport.Play();
		player.RaiseEnded();
		var endQueuedOnly = ended;
		queued.ForEach(a => a());
		transport.Stop();

		//Assert
		player.Calls.Should().StartWith(new[] { "Load(song.wav)", "Play" });
		player.Volume.Should().Be(1f);
		player.IsLooping.Should().BeTrue();
		durations.Should().Equal(TimeSpan.FromSeconds(3));
		positions.Should().Contain(TimeSpan.FromSeconds(1));
		pollTimer.Interval.Should().Be(TimeSpan.FromMilliseconds(150));
		pollTimer.IsRunning.Should().BeFalse(); // stopped at the end
		debounce.IsRepeating.Should().BeFalse();
		debounce.Interval.Should().Be(AudioTransport.SeekDebounceInterval);
		seeksBeforeRelease.Should().Be(0);
		player.Seeks.Should().Equal(TimeSpan.FromSeconds(2), TimeSpan.Zero);
		endQueuedOnly.Should().Be(0); // the natural end arrives on the audio thread and is marshalled
		ended.Should().Be(1);
		playing.Should().Equal(false, true, false, true, false, false);

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.AudioPlayer.Core");
		EngineIsolation.AssertNoWinUILoaded("AudioPlayer (transport)");
	}

	[Fact]
	public void When_A_Load_Fails_Then_The_Transport_Reports_The_Platform_Explanation_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		AudioTestPlatform.EnsureRegistered();
		var player = new RecordingPlayer { FailLoad = true };
		var transport = new AudioTransport(player, () => new TestTickSource(), _ => true);
		var failures = new List<(string Message, Exception Error)>();
		transport.Failed += (message, error) => failures.Add((message, error));

		//Act
		transport.Load(p => p.Load("missing.opus"), "missing.opus", 1.0, false, autoPlay: true);

		//Assert
		transport.IsSourceLoaded.Should().BeFalse();
		failures.Should().ContainSingle();
		failures[0].Message.Should().Be("explained: The audio source 'missing.opus' could not be loaded.");
		failures[0].Error.Should().BeOfType<FileNotFoundException>();
		player.Calls.Should().NotContain("Play");

		EngineIsolation.AssertNoWinUILoaded("AudioPlayer (failure)");
	}

	[Fact]
	public void When_Sources_Are_Resolved_Through_The_Asset_Location_Then_Paths_And_Resources_Are_Found_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var assets = AudioTestPlatform.EnsureRegistered();
		var file = Path.Combine(Path.GetTempPath(), "wpe1-3-audio-" + Guid.NewGuid().ToString("N") + ".bin");
		File.WriteAllBytes(file, new byte[] { 7, 8, 9 });

		try
		{
			//Act
			var asset = AudioSourceResolver.ResolveLocalPathOrNull("ms-appx:///Assets/My%20Song.wav");
			var fileUri = AudioSourceResolver.ResolveLocalPathOrNull(new Uri(file).AbsoluteUri);
			var bytes = AudioSourceResolver.ReadAllBytes(file);
			var (embeddedPath, embeddedStream) = AudioSourceResolver.Resolve("embedded://./(assembly).Assets.themed.json");
			using var _ = embeddedStream;
			SoundEffect.Preload(file);

			//Assert
			asset.Should().Be(Path.Join(assets.InstalledPath, "Assets/My Song.wav"));
			fileUri.Should().Be(file);
			bytes.Should().Equal(7, 8, 9);
			embeddedPath.Should().BeNull();
			embeddedStream.Should().NotBeNull();
			assets.ApplicationAssemblyReads.Should().BeGreaterThan(0);
		}
		finally
		{
			SoundEffect.ClearCache();
			File.Delete(file);
		}

		EngineIsolation.AssertNoWinUILoaded("AudioPlayer (source resolver)");
	}

	private sealed class TestTickSource : ITickSource
	{
		public TimeSpan Interval { get; set; }

		public bool IsRepeating { get; set; } = true;

		public event Action? Tick;

		internal bool IsRunning { get; private set; }

		public void Start() => IsRunning = true;

		public void Stop() => IsRunning = false;

		internal void RaiseTick() => Tick?.Invoke();
	}

	private sealed class RecordingPlayer : IAudioPlayerPlatform
	{
		internal List<string> Calls { get; } = new();

		internal List<TimeSpan> Seeks { get; } = new();

		internal bool FailLoad { get; init; }

		public void Load(string filePath)
		{
			Calls.Add($"Load({filePath})");
			if (FailLoad)
			{
				throw new FileNotFoundException("no such file", filePath);
			}
		}

		public void Load(Stream stream) => Calls.Add("Load(stream)");

		public void Play() => Calls.Add("Play");

		public void Pause() => Calls.Add("Pause");

		public void Stop() => Calls.Add("Stop");

		public void Seek(TimeSpan position)
		{
			Seeks.Add(position);
			Position = position;
		}

		public float Volume { get; set; }

		public bool IsLooping { get; set; }

		public TimeSpan Duration => TimeSpan.FromSeconds(3);

		public TimeSpan Position { get; set; }

		public event EventHandler? PlaybackEnded;

		internal void RaiseEnded() => PlaybackEnded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>The audio contracts a platform registers: the shared output and the asset location.</summary>
	private sealed class AudioTestPlatform : IAudioOutputPlatform, IAssetLocation
	{
		private static readonly object _gate = new();
		private static AudioTestPlatform? _instance;
		private int _assemblyReads;

		internal static AudioTestPlatform EnsureRegistered()
		{
			lock (_gate)
			{
				if (_instance is null)
				{
					var platform = new AudioTestPlatform();
					ApiExtensibility.Register(typeof(IAudioOutputPlatform), _ => platform);
					ApiExtensibility.Register(typeof(IAssetLocation), _ => platform);
					_instance = platform;
				}

				return _instance;
			}
		}

		internal int ApplicationAssemblyReads => _assemblyReads;

		public string InstalledPath => Path.Combine(Path.GetTempPath(), "wpe1-3-package");

		public Assembly ApplicationAssembly
		{
			get
			{
				_assemblyReads++;
				return typeof(AudioPlayerEngineTests).Assembly;
			}
		}

		public IDisposable LoadSoundEffect(byte[] data) => new MemoryStream(data);

		public void PlaySoundEffect(IDisposable soundEffect, float volume)
		{
		}

		public void PlaySoundEffectOnce(Stream stream, float volume)
		{
		}

		public string ExplainFailure(string message, string source) => "explained: " + message;
	}
}
