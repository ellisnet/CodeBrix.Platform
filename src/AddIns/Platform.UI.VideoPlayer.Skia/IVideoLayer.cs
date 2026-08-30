// Ported from CodeBrix.VideoPlayback.Skia (commit a3f3051, MIT, same author) on 2026-08-30;
// compiled against the Platform family's SkiaSharp.

using CodeBrix.VideoPlayback.Rendering;
using SkiaSharp;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia; //was previously: CodeBrix.VideoPlayback.Skia.Composition;

/// <summary>
/// Something that draws on top of the video, on the off-screen composition surface, before it
/// reaches the screen.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam that makes drawing on video, subtitle rendering, heads-up overlays and
/// picture-over-picture compositing possible without the player knowing anything about them. Layers
/// are drawn in list order, after the video base layer and before the picture is presented, so what
/// a layer draws is part of the picture: it is scaled and letterboxed with it, and
/// <see cref="VideoPlayer.CapturePresentedFrame"/> captures it too.
/// </para>
/// <para>
/// A layer that draws another video source - a camera, say - is just a layer that draws the latest
/// image it has; there is nothing video-specific to implement.
/// </para>
/// <para>
/// <see cref="Draw"/> is called on the user-interface thread, inside the composition of one frame,
/// and it must not block: it is on the display's critical path.
/// </para>
/// </remarks>
public interface IVideoLayer
{
	/// <summary>Draws this layer over the video.</summary>
	/// <param name="canvas">
	/// The composition surface's canvas, in video pixels. Its state is saved and restored around the
	/// call, so a layer may transform or clip it freely.
	/// </param>
	/// <param name="context">Where the video is, which frame it is, and what composed it.</param>
	void Draw(SKCanvas canvas, VideoCompositionContext context);
}
