using System;
using System.IO;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;

/// <summary>
/// The audio output behind one <see cref="AudioPlayer"/> element: decodes the loaded source and plays it on the
/// device. The element keeps the control API and the playback state (its dependency properties, the position timer,
/// the debounced seek, the failure reporting); this hook does the sound.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// One instance per element, created in the element's constructor through <see cref="PlatformContract.Create{TContract}"/>
/// with the element as owner, and kept in a field. The Skia implementation is
/// <c>CodeBrix.Platform.UI.AudioPlayer.Skia.AudioPlayerSkiaPlatform</c> (assembly CodeBrix.Platform.UI.AudioPlayer.Skia,
/// over CodeBrix.Audio's AudioFilePlayer), registered by that assembly's <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IAudioPlayerPlatform
{
	/// <summary>Loads an audio file from disk, replacing whatever was loaded. Throws when it cannot be read.</summary>
	/// <param name="filePath">The file's path.</param>
	void Load(string filePath);

	/// <summary>Loads audio from a stream, replacing whatever was loaded; the output takes ownership of the stream.</summary>
	/// <param name="stream">A readable (preferably seekable) stream positioned at the start of an audio file.</param>
	void Load(Stream stream);

	/// <summary>Starts or resumes playback. Throws when playback cannot start.</summary>
	void Play();

	/// <summary>Pauses playback, keeping the position.</summary>
	void Pause();

	/// <summary>Stops playback and rewinds to the beginning.</summary>
	void Stop();

	/// <summary>Moves playback to <paramref name="position"/>.</summary>
	/// <param name="position">The new position, already clamped by the caller.</param>
	void Seek(TimeSpan position);

	/// <summary>The output volume, 0.0 to 1.0.</summary>
	float Volume { get; set; }

	/// <summary>Whether playback restarts from the beginning at the end of the audio.</summary>
	bool IsLooping { get; set; }

	/// <summary>The length of the loaded audio.</summary>
	TimeSpan Duration { get; }

	/// <summary>The current playback position.</summary>
	TimeSpan Position { get; }

	/// <summary>
	/// Raised when playback reaches the natural end of the audio (not when looping). Raised on the audio thread.
	/// </summary>
	event EventHandler? PlaybackEnded;
}
