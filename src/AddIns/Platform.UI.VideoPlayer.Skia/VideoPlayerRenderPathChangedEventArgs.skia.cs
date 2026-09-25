using System;
using CodeBrix.VideoPlayback.Rendering;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia;

/// <summary>
/// Event args for <see cref="VideoPlayer.RenderPathChanged"/>: which render path settled, and
/// whether the configured effect chain is being applied on it.
/// </summary>
public sealed class VideoPlayerRenderPathChangedEventArgs : EventArgs
{
	internal VideoPlayerRenderPathChangedEventArgs(VideoRenderBackend activeRenderPath, bool effectsActive)
	{
		ActiveRenderPath = activeRenderPath;
		EffectsActive = effectsActive;
	}

	/// <summary>Which render path is now running.</summary>
	public VideoRenderBackend ActiveRenderPath { get; }

	/// <summary>Whether the configured effects are actually being applied on it.</summary>
	public bool EffectsActive { get; }
}
