using System;
using System.IO;
using CodeBrix.Audio.Playback;
using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IAudioPlayerPlatform"/>: one CodeBrix.Audio
/// <see cref="AudioFilePlayer"/> per <see cref="AudioPlayer"/> element, playing through the shared device output.
/// </summary>
internal sealed class AudioPlayerSkiaPlatform : IAudioPlayerPlatform
{
	private readonly AudioFilePlayer _player = new();

	/// <inheritdoc/>
	public void Load(string filePath) => _player.Load(filePath);

	/// <inheritdoc/>
	public void Load(Stream stream) => _player.Load(stream);

	/// <inheritdoc/>
	public void Play() => _player.Play();

	/// <inheritdoc/>
	public void Pause() => _player.Pause();

	/// <inheritdoc/>
	public void Stop() => _player.Stop();

	/// <inheritdoc/>
	public void Seek(TimeSpan position) => _player.Seek(position);

	/// <inheritdoc/>
	public float Volume
	{
		get => _player.Volume;
		set => _player.Volume = value;
	}

	/// <inheritdoc/>
	public bool IsLooping
	{
		get => _player.IsLooping;
		set => _player.IsLooping = value;
	}

	/// <inheritdoc/>
	public TimeSpan Duration => _player.Duration;

	/// <inheritdoc/>
	public TimeSpan Position => _player.Position;

	/// <inheritdoc/>
	public event EventHandler? PlaybackEnded
	{
		add => _player.PlaybackEnded += value;
		remove => _player.PlaybackEnded -= value;
	}
}
