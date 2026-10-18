using global::System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Graphics;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Tizen")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wayland")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.HRApp")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("CodeBrix.PlatformIslandsSamplesApp.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.AppleUIKit")]
[assembly: InternalsVisibleTo("SamplesApp.Skia.Generic")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.HRApp.Skia")]

// Since the Core/Skia split this assembly holds only the Skia implementations; it keeps every grant the
// single pre-split assembly had (the list below is the one of the shared AssemblyInfo.cs, which now goes to
// the Core assembly), so every head, add-in and test that reached these internals still reaches them.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Foldable")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RemoteControl")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Headless.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.TextLayout.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AdvancedTextEdit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AdvancedTextEdit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.TerminalView")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.TerminalView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.PlotterView")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.PlotterView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.CommandBar")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Maps")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("SamplesApp.netcoremobile")]
[assembly: InternalsVisibleTo("CodeBrix.PlatformIslandsSamplesApp.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.FluentTheme")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.FluentTheme.v1")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.FluentTheme.v2")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.VideoPlayer.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebView.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.HotDesign.Client")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics2DSK")]

[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.PlayTest")]

// The WebGpu add-in's test suite: its direct-path GPU tests run a head's own Vulkan device (VulkanContext, initialized
// headless) behind the provider the X11 and Wayland heads register (Vulkan/VulkanNativeGpuDeviceProvider.skia.cs).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebGpu.Skia.Tests")]

// The WebGpu add-in itself: it registers the engine's adopt-requirements source the heads' Vulkan devices are created
// with while direct GPU rendering is on (Vulkan/IVulkanAdoptRequirementsSource.skia.cs, Vulkan/VulkanAdoptionProfile.skia.cs).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebGpu.Skia")]

// The WebGpu add-in's UIReqs pair: the direct-path scenarios offer a head's Vulkan device (VulkanContext, initialized
// headless, behind VulkanNativeGpuDeviceProvider) as the window's provider on the software frame-buffer harness.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Portrait")]
