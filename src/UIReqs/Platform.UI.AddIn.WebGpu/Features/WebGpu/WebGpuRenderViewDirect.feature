@needs-webgpu
Feature: WebGpuRenderView direct render path
	An application that opts in to direct GPU rendering (UseDirectGpuRenderingMode()) lets the view render on the
	head's OWN GPU device where the head offers one - the X11 and Wayland heads on their Vulkan renderer - and hand the
	head GPU-resident frames with no processor readback (render path DirectVulkan). Where the head offers no device the
	view uses Readback, silently, and shows the same picture.

	The harness runs on the software frame-buffer head, which offers no GPU device and cannot composite a GPU image:
	the direct scenarios therefore offer, as the window's device, the X11 and Wayland heads' own Vulkan device code
	(created headless), and read the picture through the view's own capture of the frame it draws. The window-level
	proof on a real X11 Vulkan window is a live run, not a scenario.

	Those direct views are given Opacity 0, so the frame-buffer head never draws them. A software head that draws a
	GPU image reads it back through the image's GPU context on the head's own render thread, outside the device lock
	the view and its capture hold; that race crashed the graphics driver now and then in a busy solution run. On a
	real Vulkan head the head draws the image on its own GPU surface, inside that lock, so this is a harness-only
	precaution and the view's own rendering and capture are unchanged by it.

	Deliberately out of scope: Metal (macOS only) and the exact frame rate.

@needs-head-vulkan
Scenario: With direct rendering on and a head Vulkan device, the view renders DirectVulkan
	Given the application uses direct GPU rendering
	And the window offers the head's Vulkan device
	And the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
		| Opacity    | 0       |
	When the DeviceReady of "view" is raised within 8000 milliseconds
	Then the RenderPath of "view" is "DirectVulkan"
	And the picture of "view" shows at least 99 percent "#FF0000" within 8000 milliseconds

@needs-head-vulkan
Scenario: A triangle reaches the direct picture over the clear colour
	Given the application uses direct GPU rendering
	And the window offers the head's Vulkan device
	And the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value    |
		| ClearColor | #0000FF  |
		| Renderer   | Triangle |
		| Opacity    | 0        |
	Then the RenderPath of "view" is "DirectVulkan"
	And the picture of "view" shows at least 15 percent "#00FF00" within 8000 milliseconds
	And the picture of "view" shows at least 40 percent "#0000FF" within 1000 milliseconds

@needs-head-vulkan
Scenario: A direct view taken out of the tree and put back adopts the head device again
	Given the application uses direct GPU rendering
	And the window offers the head's Vulkan device
	And the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
		| Opacity    | 0       |
	And "view" has rendered 3 frames within 8000 milliseconds
	When "view" is taken out of its layout
	Then the DeviceLost of "view" is raised within 3000 milliseconds
	And the RenderPath of "view" is "None"
	When "view" is put back into its layout
	Then "view" has had 2 devices within 8000 milliseconds
	And the RenderPath of "view" is "DirectVulkan"
	And the picture of "view" shows at least 99 percent "#FF0000" within 8000 milliseconds

Scenario: With direct rendering on and no head device, the view falls back to Readback and shows the same picture
	Given the application uses direct GPU rendering
	And the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
	When the DeviceReady of "view" is raised within 8000 milliseconds
	Then the RenderPath of "view" is "Readback"
	And the region of "view" shows at least 99 percent "#FF0000" within 8000 milliseconds
