using System;
using System.IO;
using CodeBrix.Audio.Playback;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IAudioOutputPlatform"/>: sound effects are CodeBrix.Audio
/// <see cref="SoundEffectClip"/> voices in the application's single shared output, and a load failure is explained by
/// <see cref="AudioFailureExplanation"/> (CodeBrix.Audio's Ogg codec sniffer).
/// </summary>
internal sealed class AudioOutputSkiaPlatform : IAudioOutputPlatform
{
	/// <inheritdoc/>
	public IDisposable LoadSoundEffect(byte[] data) => SoundEffectClip.Load(data);

	/// <inheritdoc/>
	public void PlaySoundEffect(IDisposable soundEffect, float volume) => ((SoundEffectClip)soundEffect).Play(volume);

	/// <inheritdoc/>
	public void PlaySoundEffectOnce(Stream stream, float volume) => SoundEffectClip.PlayOnce(stream, volume);

	/// <inheritdoc/>
	public string ExplainFailure(string message, string source) => AudioFailureExplanation.Amend(message, source);
}
