using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;
using CodeBrix.Audio.Wave;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// Fire-and-forget playback of short WAV and MP3 sound effects. Each call to
/// <see cref="Play(string, double)"/> plays one voice in the application's single shared audio
/// output, so many effects can overlap cheaply and can play alongside an
/// <see cref="AudioPlayer"/>. Sources are the same forms <see cref="AudioPlayer.Source"/>
/// accepts: a file path, an ms-appx:/// asset URI, or an embedded://Assembly/Resource.Name
/// embedded-resource URI.
///
/// Effect bytes are cached in memory after the first play (or an explicit
/// <see cref="Preload"/>), so no disk I/O ever happens on the real-time audio thread. One
/// sharp edge inherited from the audio engine: the shared output adopts the sample rate of
/// the first sound played (or a rate pinned with CodeBrix.Audio's SharedAudioOutput.Configure),
/// and a WAV or MP3 whose sample rate differs is rejected rather than played at the wrong
/// pitch - standardize your effects on one sample rate, or pin the output rate at startup.
/// </summary>
public static class SoundEffect
{
	private static readonly ConcurrentDictionary<string, byte[]> _cache = new(StringComparer.Ordinal);

	/// <summary>
	/// Loads the effect's bytes into the in-memory cache ahead of time, so the first
	/// <see cref="Play(string, double)"/> starts without any file access.
	/// </summary>
	public static void Preload(string source) => _cache.GetOrAdd(source, AudioSourceResolver.ReadAllBytes);

	/// <summary>Removes all preloaded/cached effect bytes.</summary>
	public static void ClearCache() => _cache.Clear();

	/// <summary>
	/// Plays a sound effect (fire and forget): the effect starts immediately as its own voice
	/// in the shared audio output and cleans itself up when it ends. Load or resolution
	/// failures are logged and reported through the returned value rather than thrown, so a
	/// missing effect never crashes the app.
	/// </summary>
	/// <param name="source">A WAV/MP3 file path, ms-appx:/// URI, or embedded:// URI.</param>
	/// <param name="volume">Volume for this play, 0.0 to 1.0 (default 1.0).</param>
	/// <returns>True when playback started; false when the effect could not be played.</returns>
	public static bool Play(string source, double volume = 1.0)
	{
		try
		{
			var bytes = _cache.GetOrAdd(source, AudioSourceResolver.ReadAllBytes);
			PlayCore(new MemoryStream(bytes, writable: false), volume);
			return true;
		}
		catch (Exception e)
		{
			if (typeof(SoundEffect).Log().IsEnabled(LogLevel.Error))
			{
				typeof(SoundEffect).Log().Error($"The sound effect '{source}' could not be played.", e);
			}
			return false;
		}
	}

	/// <summary>
	/// Plays a sound effect from a stream containing a WAV or MP3 file (fire and forget). The
	/// stream is fully buffered into memory first and can be disposed by the caller as soon as
	/// this method returns.
	/// </summary>
	/// <param name="stream">A readable stream positioned at the start of a WAV or MP3 file.</param>
	/// <param name="volume">Volume for this play, 0.0 to 1.0 (default 1.0).</param>
	/// <returns>True when playback started; false when the effect could not be played.</returns>
	public static bool Play(Stream stream, double volume = 1.0)
	{
		if (stream is null)
		{
			throw new ArgumentNullException(nameof(stream));
		}

		try
		{
			var buffer = new MemoryStream();
			stream.CopyTo(buffer);
			buffer.Position = 0;
			PlayCore(buffer, volume);
			return true;
		}
		catch (Exception e)
		{
			if (typeof(SoundEffect).Log().IsEnabled(LogLevel.Error))
			{
				typeof(SoundEffect).Log().Error("The sound effect stream could not be played.", e);
			}
			return false;
		}
	}

	private static void PlayCore(MemoryStream stream, double volume)
	{
		WaveStream reader = IsWavStream(stream)
			? new WaveFileReader(stream)
			: new Mp3FileReader(stream);

		var voice = new WaveOutEvent();
		try
		{
			voice.Volume = (float)Math.Clamp(volume, 0.0, 1.0);
			voice.Init(reader);
			voice.PlaybackStopped += (_, _) =>
			{
				voice.Dispose();
				reader.Dispose();
				stream.Dispose();
			};
			voice.Play();
		}
		catch (Exception)
		{
			voice.Dispose();
			reader.Dispose();
			stream.Dispose();
			throw;
		}
	}

	private static bool IsWavStream(MemoryStream stream)
	{
		Span<byte> header = stackalloc byte[4];
		var read = stream.Read(header);
		stream.Position = 0;
		return read == 4 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F';
	}
}
