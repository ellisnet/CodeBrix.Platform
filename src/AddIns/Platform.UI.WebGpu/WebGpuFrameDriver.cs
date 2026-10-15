namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// What decides when a <c>WebGpuRenderView</c> renders a frame. Every driver uses the same frame protocol
/// (<c>BeginFrame</c> / <c>EndFrame</c>); the two built-in drivers simply call it for you, on the user-interface
/// thread.
/// </summary>
public enum WebGpuFrameDriver
{
	/// <summary>
	/// The default: the view renders frame after frame at its <c>TargetFrameRate</c>, on the user-interface thread,
	/// raising <c>Render</c> for each one, until <c>IsPaused</c> is set or the view leaves the visual tree. A frame
	/// that runs late is never "caught up": the next one simply starts as soon as possible, so a slow frame costs
	/// frame rate, never a backlog.
	/// </summary>
	Continuous = 0,

	/// <summary>
	/// The view renders one frame for each <c>Invalidate()</c> (several calls before that frame runs make one
	/// frame), plus one when it is loaded and one when its pixel size changes, on the user-interface thread. For
	/// pictures that change only when something happens.
	/// </summary>
	OnDemand = 1,

	/// <summary>
	/// The view never renders by itself: the application (or a native module) calls <c>BeginFrame</c> and
	/// <c>EndFrame</c> whenever it likes, from whichever ONE thread it likes - its own render thread included.
	/// </summary>
	Manual = 2,
}
