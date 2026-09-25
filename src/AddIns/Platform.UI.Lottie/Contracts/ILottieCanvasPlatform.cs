using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Lottie.Contracts;

/// <summary>
/// The Lottie add-in's canvas supply: the element an animation source puts into its AnimatedVisualPlayer to draw the
/// animation on. The source (in the Core assembly) owns the playback - the decoded Skottie animation, the frame clock
/// and the drawing of each frame - and asks the platform for the element that gives it a canvas.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once, into a static field (<c>LottieVisualSourceBase.CanvasPlatform</c>), through
/// <see cref="PlatformContract.Resolve{TContract}"/>. Platform (Skia):
/// <c>CodeBrix.Platform.UI.Lottie.Skia.LottieCanvasSkiaPlatform</c> in CodeBrix.Platform.UI.Lottie, which supplies a
/// Graphics2DSK SKCanvasElement composited by the framework's Skia renderer.
/// </para>
/// </remarks>
internal interface ILottieCanvasPlatform
{
	/// <summary>
	/// Creates the element that shows the animation.
	/// </summary>
	/// <param name="render">Draws one frame: the platform's drawing canvas (on every current platform an SKCanvas,
	/// passed as an object - see SkiaSharp.Views.Windows.SKCanvasHost) and the size of the area to draw in
	/// device-independent pixels.</param>
	/// <returns>The element, to be added to the player.</returns>
	UIElement CreateRenderSurface(Action<object, Size> render);

	/// <summary>
	/// Requests a repaint of an element <see cref="CreateRenderSurface"/> created: its render callback runs again on
	/// the next frame. Called once per animation tick.
	/// </summary>
	/// <param name="renderSurface">The element to repaint.</param>
	void Invalidate(UIElement renderSurface);
}
