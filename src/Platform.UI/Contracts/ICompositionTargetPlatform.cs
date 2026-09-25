#nullable enable

using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The platform side of one <see cref="CompositionTarget"/>: records a frame of the root's visual tree for the native
/// window to draw. The render scheduling (when a frame is requested, rendered ahead of time, or skipped) and the
/// <see cref="CompositionTarget.Rendering"/> event stay platform-neutral.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// One instance per composition target, created by <see cref="IRenderingPlatform.CreateCompositionTargetPlatform"/>
/// in the target's constructor and kept in a field. All members are called on the UI thread. The Skia
/// implementation is <c>CodeBrix.Platform.UI.Skia.CompositionTargetSkiaPlatform</c> (an <c>SKPicture</c> per
/// frame, drawn by the head when the native window asks for pixels).
/// </para>
/// </remarks>
internal interface ICompositionTargetPlatform
{
	/// <summary>
	/// Gets a value indicating whether the root's visual tree can be recorded now.
	/// </summary>
	/// <returns><see langword="true"/> when a frame can be recorded.</returns>
	bool CanRecordFrame();

	/// <summary>
	/// Records a frame of the root's visual tree, replacing the previous one.
	/// </summary>
	void RecordFrame();

	/// <summary>
	/// Called after a frame was recorded and the host was asked to draw it: brings the native elements hosted in the
	/// visual tree into the order the recorded frame placed them in.
	/// </summary>
	void UpdateNativeElementsOrder();
}
