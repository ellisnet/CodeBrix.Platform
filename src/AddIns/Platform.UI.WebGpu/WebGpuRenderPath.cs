namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// How the frames a <c>WebGpuRenderView</c> renders reach the screen.
/// </summary>
public enum WebGpuRenderPath
{
	/// <summary>
	/// Nothing is rendering: the view has no device yet (it is created on the first frame after the view is loaded),
	/// none could be created on this machine, the device was lost, or the launch said there is none (a test head's
	/// option). The view shows what is behind it.
	/// </summary>
	None = 0,

	/// <summary>
	/// The default on every head: the view owns its WebGPU device, renders into an off-screen texture the size of
	/// the element, reads each finished frame back once and draws it through the head's own Skia canvas, so it
	/// composites, clips and overlaps exactly like any other XAML content.
	/// </summary>
	Readback = 1,

	/// <summary>
	/// A direct path: the frames are rendered on the head's own Vulkan device and composited by the head without a
	/// copy to processor memory. Taken only where the application opted in to direct GPU rendering and the head
	/// offers it.
	/// </summary>
	DirectVulkan = 2,

	/// <summary>
	/// A direct path: the frames are rendered on the head's own Metal device and composited by the head without a
	/// copy to processor memory. Taken only where the application opted in to direct GPU rendering and the head
	/// offers it.
	/// </summary>
	DirectMetal = 3,

	/// <summary>
	/// A direct path: the frames are presented straight to a native window surface the head places under the
	/// element. Taken only where the application opted in to direct GPU rendering and the head offers it.
	/// </summary>
	DirectSurface = 4,
}
