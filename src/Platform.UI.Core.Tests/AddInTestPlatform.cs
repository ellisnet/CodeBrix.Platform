#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.AppSettings.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using CodeBrix.Platform.WinUI.Graphics3DGL.Contracts;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Test doubles for the platform contracts of the add-ins whose Core assembly holds no platform code at all
/// (AppSettings, AudioPlayer) or none of its visual (Graphics3DGL), registered the way a platform library registers its own - so the host-free suite
/// drives the add-ins' Core logic exactly as CodeBrix.Android or CodeBrix.Mobile would, with no database engine and no
/// audio engine in the process.
/// </summary>
internal static class AddInTestPlatform
{
	private static readonly object _gate = new();
	private static bool _registered;

	/// <summary>The most recently created AudioPlayer output hook.</summary>
	internal static RecordingAudioPlayerPlatform? LastAudioPlayer { get; private set; }

	/// <summary>The shared audio output service.</summary>
	internal static RecordingAudioOutputPlatform AudioOutput { get; } = new();

	/// <summary>The GLCanvasElement visual platform.</summary>
	internal static RecordingGLCanvasPlatform GLCanvas { get; } = new();

#pragma warning disable CA2255 // A test process's platform must exist before the first add-in object is created.
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Initialize() => EnsureRegistered();

	/// <summary>Registers the add-in test platform once.</summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			var storage = new InMemoryAppSettingsStoragePlatform();
			ApiExtensibility.Register(typeof(IAppSettingsStoragePlatform), _ => storage);

			ApiExtensibility.Register(typeof(IAudioPlayerPlatform), _ => LastAudioPlayer = new RecordingAudioPlayerPlatform());
			ApiExtensibility.Register(typeof(IAudioOutputPlatform), _ => AudioOutput);

			ApiExtensibility.Register(typeof(IGLCanvasPlatform), _ => GLCanvas);

			_registered = true;
		}
	}

	/// <summary>A storage platform that keeps every "database" in memory, keyed by folder.</summary>
	private sealed class InMemoryAppSettingsStoragePlatform : IAppSettingsStoragePlatform
	{
		private readonly Dictionary<string, Dictionary<string, string>> _databases = new(StringComparer.Ordinal);

		public string GetDefaultDirectory(string appName) => "/memory/" + appName;

		public IAppSettingsStorage Open(string appName, string directoryPath, IDictionary<string, string> values, Func<DateTime> clock)
		{
			var fresh = !_databases.TryGetValue(directoryPath, out var rows);
			rows ??= _databases[directoryPath] = new Dictionary<string, string>(StringComparer.Ordinal);
			values.Clear();
			foreach (var row in rows)
			{
				values[row.Key] = row.Value;
			}
			return new InMemoryStorage(directoryPath, rows, fresh);
		}
	}

	/// <summary>One in-memory settings "database".</summary>
	private sealed class InMemoryStorage : IAppSettingsStorage
	{
		private readonly Dictionary<string, string> _rows;
		private bool _closed;

		public InMemoryStorage(string directoryPath, Dictionary<string, string> rows, bool fresh)
		{
			DirectoryPath = directoryPath;
			DatabaseFilePath = directoryPath + "/settings.sqlite";
			WasCreatedFresh = fresh;
			_rows = rows;
		}

		public string DirectoryPath { get; }
		public string DatabaseFilePath { get; }
		public bool WasCreatedFresh { get; }
		public bool WasRestoredFromBackup => false;
		public bool WasReplacedByImport => false;
		public int AutoBackups { get; private set; }

		public void Write(string key, string json) => Open()[key] = json;
		public void Delete(string key) => Open().Remove(key);
		public void CreateAutoBackupAndPrune(int retainCount) => AutoBackups++;
		public string GetExportDestination(string destinationFilePath) => destinationFilePath;
		public void ExportTo(string fullPath) => Open();
		public void StageIncomingFile(string sourceFilePath) => throw new NotSupportedException();
		public void Dispose() => _closed = true;

		private Dictionary<string, string> Open() => _closed ? throw new ObjectDisposedException("AppSettingsStore") : _rows;
	}

	/// <summary>An AudioPlayer output that records what the element asked of it and plays nothing.</summary>
	internal sealed class RecordingAudioPlayerPlatform : IAudioPlayerPlatform
	{
		internal List<string> Calls { get; } = new();

		public void Load(string filePath) => Calls.Add("Load(file)");
		public void Load(Stream stream) => Calls.Add("Load(stream)");
		public void Play() => Calls.Add("Play");
		public void Pause() => Calls.Add("Pause");
		public void Stop() => Calls.Add("Stop");
		public void Seek(TimeSpan position)
		{
			Calls.Add($"Seek({position.TotalSeconds})");
			Position = position;
		}

		public float Volume { get; set; } = 1f;
		public bool IsLooping { get; set; }
		public TimeSpan Duration => TimeSpan.FromSeconds(3);
		public TimeSpan Position { get; private set; }

		public event EventHandler? PlaybackEnded { add { } remove { } }
	}

	/// <summary>A shared audio output that counts the effects it was asked to play.</summary>
	internal sealed class RecordingAudioOutputPlatform : IAudioOutputPlatform
	{
		internal int OneShotEffects { get; private set; }

		public IDisposable LoadSoundEffect(byte[] data) => throw new NotSupportedException();
		public void PlaySoundEffect(IDisposable soundEffect, float volume) => throw new NotSupportedException();
		public void PlaySoundEffectOnce(Stream stream, float volume) => OneShotEffects++;
		public string ExplainFailure(string message, string source) => message;
	}

	/// <summary>A GLCanvasElement visual platform that hands out plain border visuals and records their elements.</summary>
	internal sealed class RecordingGLCanvasPlatform : IGLCanvasPlatform
	{
		/// <summary>The elements a visual was created for, in order.</summary>
		internal List<GLCanvasElement> Owners { get; } = new();

		public BorderVisual CreateVisual(GLCanvasElement owner, Compositor compositor)
		{
			Owners.Add(owner);
			return new BorderVisual(compositor);
		}
	}
}
