@needs-webgpu
Feature: WebGpuRenderView frame drivers and lifetime
	Three drivers decide when a WebGpuRenderView renders: Continuous (the default) renders frame after frame at its
	target rate, OnDemand renders one frame per Invalidate, and Manual renders only when the application opens a frame
	itself - from any thread. A view that leaves the visual tree gives its device up (DeviceLost) and a view that comes
	back gets a new one (DeviceReady) and renders again, with nothing for the application to do.

	Deliberately out of scope: the exact rate (a fact about the machine) and pausing, which the add-in's own suite
	covers.

Scenario: The Continuous driver renders frame after frame
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view"
	Then "view" has rendered 10 frames within 8000 milliseconds

Scenario: The driver switches from Continuous to OnDemand to Manual
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
	And "view" has rendered 3 frames within 8000 milliseconds
	When the FrameDriver of "view" is set to "OnDemand"
	Then "view" renders no new frame for 500 milliseconds
	When "view" is invalidated
	Then "view" renders exactly 1 more frame within 3000 milliseconds
	When the FrameDriver of "view" is set to "Manual"
	And "view" is invalidated
	Then "view" renders no new frame for 500 milliseconds
	When "view" renders one frame cleared to "#00FF00" from a worker thread
	Then the region of "view" shows at least 99 percent "#00FF00" within 5000 milliseconds

Scenario: A view taken out of the tree and put back renders again
	Given the application shows a Grid named "cell" 320 by 240
	And the layout "cell" holds a WebGpuRenderView named "view" with:
		| Property   | Value   |
		| ClearColor | #FF0000 |
	And "view" has rendered 3 frames within 8000 milliseconds
	When "view" is taken out of its layout
	Then the DeviceLost of "view" is raised within 3000 milliseconds
	And the RenderPath of "view" is "None"
	When "view" is put back into its layout
	Then "view" has had 2 devices within 8000 milliseconds
	And the region of "view" shows at least 99 percent "#FF0000" within 8000 milliseconds
