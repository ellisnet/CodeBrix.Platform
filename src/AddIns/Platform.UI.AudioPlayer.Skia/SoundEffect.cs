using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;
using CodeBrix.Audio.Playback;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// Fire-and-forget playback of short sound effects - WAV, MP3, Ogg Vorbis and FLAC. Each call to
/// <see cref="Play(string, double)"/> plays one voice in the application's single shared audio
/// output, so many effects can overlap cheaply and can play alongside an
/// <see cref="AudioPlayer"/>. Sources are the same forms <see cref="AudioPlayer.Source"/>
/// accepts: a file path, an ms-appx:/// asset URI, or an embedded://Assembly/Resource.Name
/// embedded-resource URI.
///
/// An effect is decoded ONCE, on its first play, and the decoded audio is kept - so a sound
/// triggered repeatedly costs nothing but mixing, and no file access or decoding ever happens on
/// the real-time audio thread. <see cref="Preload"/> reads the bytes ahead of time;
/// <see cref="ClearCache"/> releases everything.
///
/// Effects do NOT have to share a sample rate: each is converted to the output's format when it
/// is decoded, so an asset pack that mixes 22 kHz and 44.1 kHz files just works. (That is a
/// property of this class; feeding a mismatched source directly to CodeBrix.Audio's WaveOutEvent
/// still fails, by design.)
///
/// Formats beyond the four built in - Opus, say - become playable here as soon as an add-on
/// codec package is registered with CodeBrix.Audio; this class needs no change and no
/// dependency on it.
/// </summary>
public static class SoundEffect
{
	private static readonly ConcurrentDictionary<string, byte[]> _cache = new(StringComparer.Ordinal);

	// Decoded, ready-to-mix audio, keyed by the same source string. Populated on first play rather
	// than by Preload, because decoding starts the shared output device and Preload historically
	// did not - an app that preloads and then pins the output format must keep working.
	private static readonly ConcurrentDictionary<string, SoundEffectClip> _clips = new(StringComparer.Ordinal);

	/// <summary>
	/// Loads the effect's bytes into the in-memory cache ahead of time, so the first
	/// <see cref="Play(string, double)"/> starts without any file access.
	/// </summary>
	public static void Preload(string source) => _cache.GetOrAdd(source, AudioSourceResolver.ReadAllBytes);

	/// <summary>Removes all preloaded/cached effect bytes and releases all decoded audio.</summary>
	public static void ClearCache()
	{
		foreach (var clip in _clips.Values)
		{
			try { clip.Dispose(); } catch (Exception) { /* best effort */ }
		}

		_clips.Clear();
		_cache.Clear();
	}

	/// <summary>
	/// Plays a sound effect (fire and forget): the effect starts immediately as its own voice
	/// in the shared audio output and cleans itself up when it ends. Load or resolution
	/// failures are logged and reported through the returned value rather than thrown, so a
	/// missing effect never crashes the app.
	/// </summary>
	/// <param name="source">An audio file path, ms-appx:/// URI, or embedded:// URI.</param>
	/// <param name="volume">Volume for this play, 0.0 to 1.0 (default 1.0).</param>
	/// <returns>True when playback started; false when the effect could not be played.</returns>
	public static bool Play(string source, double volume = 1.0)
	{
		try
		{
			var clip = _clips.GetOrAdd(source, static key =>
				SoundEffectClip.Load(_cache.GetOrAdd(key, AudioSourceResolver.ReadAllBytes)));

			clip.Play((float)Math.Clamp(volume, 0.0, 1.0));
			return true;
		}
		catch (Exception e)
		{
			if (typeof(SoundEffect).Log().IsEnabled(LogLevel.Error))
			{
				typeof(SoundEffect).Log().Error(
					AudioFailureExplanation.Amend($"The sound effect '{source}' could not be played.", source),
					e);
			}
			return false;
		}
	}

	/// <summary>
	/// Plays a sound effect from a stream (fire and forget). The stream is read in full before this
	/// returns and can be disposed by the caller immediately afterwards.
	/// </summary>
	/// <remarks>
	/// A stream has no identity to cache under, so this decodes every time. For an effect played
	/// repeatedly, use <see cref="Play(string, double)"/>, which decodes once and keeps the result.
	/// </remarks>
	/// <param name="stream">A readable stream positioned at the start of an audio file.</param>
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
			using var buffer = new MemoryStream();
			stream.CopyTo(buffer);
			buffer.Position = 0;

			// PlayOnce takes over the decoded audio's lifetime and releases it when the sound ends.
			SoundEffectClip.PlayOnce(buffer, (float)Math.Clamp(volume, 0.0, 1.0));
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
}
