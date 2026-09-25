using CodeBrix.Platform.UI.AudioPlayer.Skia.Contracts;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

/// <summary>
/// The add-in's shared audio output service (<see cref="IAudioOutputPlatform"/>), resolved once for the process on
/// first use.
/// </summary>
internal static class AudioPlatform
{
	private static IAudioOutputPlatform? _output;

	/// <summary>The platform's shared audio output.</summary>
	internal static IAudioOutputPlatform Output => _output ??= PlatformContract.Resolve<IAudioOutputPlatform>();
}
