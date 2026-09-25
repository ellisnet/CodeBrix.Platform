using System;
using System.IO;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;

/// <summary>
/// The shared audio output of the AudioPlayer add-in: the fire-and-forget voices behind <see cref="SoundEffect"/>,
/// and the explanation of a load failure in terms the application can act on (which needs to look inside the audio
/// data with the platform's codec knowledge).
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once, lazily, into <c>AudioPlatform.Output</c> through <see cref="PlatformContract.Resolve{TContract}"/>.
/// The Skia implementation is <c>CodeBrix.Platform.UI.AudioPlayer.Skia.AudioOutputSkiaPlatform</c> (assembly
/// CodeBrix.Platform.UI.AudioPlayer.Skia, over CodeBrix.Audio's SoundEffectClip and Ogg codec sniffer), registered by
/// that assembly's <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IAudioOutputPlatform
{
	/// <summary>
	/// Decodes a sound effect once, converted to the shared output's format, ready to be played any number of times.
	/// Throws when the audio cannot be decoded.
	/// </summary>
	/// <param name="data">The complete bytes of an audio file.</param>
	/// <returns>An opaque handle to the decoded effect; disposing it releases the decoded audio.</returns>
	IDisposable LoadSoundEffect(byte[] data);

	/// <summary>Plays one voice of an effect returned by <see cref="LoadSoundEffect"/>.</summary>
	/// <param name="soundEffect">The handle returned by <see cref="LoadSoundEffect"/>.</param>
	/// <param name="volume">The volume for this voice, 0.0 to 1.0.</param>
	void PlaySoundEffect(IDisposable soundEffect, float volume);

	/// <summary>
	/// Decodes and plays an effect once; the decoded audio is released when the sound ends. Throws when the audio
	/// cannot be decoded.
	/// </summary>
	/// <param name="stream">A readable stream positioned at the start of an audio file.</param>
	/// <param name="volume">The volume, 0.0 to 1.0.</param>
	void PlaySoundEffectOnce(Stream stream, float volume);

	/// <summary>
	/// Returns <paramref name="message"/>, with an explanation appended when the source turns out to be a format
	/// that needs something of the application. Never throws.
	/// </summary>
	/// <param name="message">The failure message as it stands.</param>
	/// <param name="source">The source that failed, in any form the add-in accepts.</param>
	/// <returns>The message, possibly amended.</returns>
	string ExplainFailure(string message, string source);
}
