using CodeBrix.Platform.UI.Lottie.Engine;

namespace CodeBrix.Platform.UI.Lottie.Contracts;

/// <summary>
/// The platform's frame clock for the Lottie engine (WPE1-13): creates the <see cref="ITickSource"/> that drives an
/// animation's frames, for a platform with a better clock than a dispatcher timer (Android's Choreographer vsync
/// callbacks, for one).
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered - the engine keeps its DispatcherQueueTimer
/// (Internal/DispatcherQueueTickSource.ForCurrentThread), as before.
/// <para>
/// OPTIONAL contract, looked up once per animation source when the source creates its engine
/// (<see cref="PlatformContract.SelectTickSourceFactory"/>; when it is not registered, the platform assemblies'
/// bootstraps are run once per process and the lookup is repeated). The engine calls <see cref="CreateTickSource"/>
/// on the source's UI thread each time playback starts, and the tick source must raise
/// <see cref="ITickSource.Tick"/> on that thread.
/// </para>
/// </remarks>
internal interface ILottieTickSourcePlatform
{
	/// <summary>
	/// Creates a stopped tick source for the calling (UI) thread.
	/// </summary>
	/// <returns>A new tick source; the engine sets its interval and starts and stops it.</returns>
	ITickSource CreateTickSource();
}
