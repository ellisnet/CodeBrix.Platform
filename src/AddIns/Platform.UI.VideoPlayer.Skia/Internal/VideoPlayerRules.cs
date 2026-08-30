using System;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;

/// <summary>
/// The <see cref="VideoPlayer"/> element's decisions that are pure arithmetic or pure policy, kept
/// apart from the element so they can be exercised without a window.
/// </summary>
internal static class VideoPlayerRules
{
	/// <summary>
	/// Whether a write to the render path may stand.
	/// </summary>
	/// <param name="isSourceLoaded">Whether a source is currently open.</param>
	/// <param name="oldValue">The value the property held.</param>
	/// <param name="newValue">The value being written.</param>
	/// <returns>
	/// True when the write is allowed - which it always is while nothing is open, and while the
	/// value is not actually changing.
	/// </returns>
	/// <remarks>
	/// The render path is chosen once, before anything is opened: the presenter's surface, its
	/// shaders and the graphics context are all built around it, exactly as the game engine's
	/// canvas chooses its render tier before its pipeline exists.
	/// </remarks>
	public static bool IsRenderPathChangeAllowed<TRenderPath>(bool isSourceLoaded, TRenderPath oldValue, TRenderPath newValue)
		where TRenderPath : struct, Enum =>
		!isSourceLoaded || Equals(oldValue, newValue);

	/// <summary>
	/// The message a refused render-path change carries: what the rule is and what to do about it.
	/// </summary>
	/// <param name="propertyName">The name of the property being written.</param>
	/// <param name="sourcePropertyName">The name of the source property to clear first.</param>
	/// <returns>The message.</returns>
	public static string RenderPathChangeRefusal(string propertyName, string sourcePropertyName) =>
		$"{propertyName} must be set before a source is opened; close the source (set {sourcePropertyName} " +
		"to an empty string) before changing it.";

	/// <summary>
	/// Brings a requested position inside the media.
	/// </summary>
	/// <param name="position">The position asked for.</param>
	/// <param name="duration">How long the media is, or <see cref="TimeSpan.Zero"/> when unknown.</param>
	/// <returns>
	/// The position to seek to: never negative, and never past the end when the end is known. An
	/// unknown duration clamps only at zero, because a container that does not state its length is
	/// not evidence that a position is out of range.
	/// </returns>
	public static TimeSpan ClampToDuration(TimeSpan position, TimeSpan duration)
	{
		if (position < TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}

		return duration > TimeSpan.Zero && position > duration ? duration : position;
	}
}
