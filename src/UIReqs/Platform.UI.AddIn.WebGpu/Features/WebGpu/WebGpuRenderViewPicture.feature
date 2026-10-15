@needs-webgpu
Feature: WebGpuRenderView picture, render path and overlay
	A WebGpuRenderView owns a WebGPU device, renders each frame into a texture the size of the view - cleared to its
	ClearColor before the renderer draws - and shows the finished frame through the head's own canvas. On the
	Readback path, which every head runs, that picture composites like any other XAML content, and an overlay drawn
	with SkiaSharp lands on top of it.

	Green and white are the renderer's colours (its triangle and its overlay square); no clear colour here uses them,
	so a region that shows them shows the renderer's work and nothing else.

	Deliberately out of scope: the direct render paths (a head-specific opt-in), display scale, and the exact frame
	rate (a fact about the machine; the add-in's own suite measures it).

Scenario: A view shows a frame cleared to its clear colour
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
	Then the region of "view" shows at least 99 percent "#FF0000" within 8000 milliseconds

Scenario: A triangle a renderer records reaches the screen over the clear colour
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value    |
		| ClearColor | #0000FF  |
		| Renderer   | Triangle |
	Then the region of "view" shows at least 15 percent "#00FF00" within 8000 milliseconds
	And the region of "view" contains at least 40 percent "#0000FF"

Scenario: The view reports the Readback render path once its device is ready
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view"
	When the DeviceReady of "view" is raised within 8000 milliseconds
	Then the RenderPath of "view" is "Readback"
	And the RenderPathChanged of "view" is raised within 1000 milliseconds

Scenario: The overlay is drawn over the frame
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value               |
		| ClearColor | #0000FF             |
		| Renderer   | TriangleWithOverlay |
	Then the region of "view" shows at least 15 percent "#00FF00" within 8000 milliseconds
	And the region of "view" contains at least 3 percent "#FFFFFF"
