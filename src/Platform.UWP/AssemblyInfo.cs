using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.PlayTest")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.GooglePlay")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Foldable")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.VideoPlayer.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.VideoPlayer.Core")]
// WPE1-21: SoundEffect reads an ms-appx sound through the package-files contract (IApplicationPackageFilesPlatform)
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AudioPlayer.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebView.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL.Core")]

// Lets SKXamlCanvas reach the fast raw-buffer accessor (Windows.Storage.Streams.Buffer's internal
// Cast/ApplyActionOnRawBufferPtr) for the opt-in UseDirectSkiaCanvasMode() one-copy present path.
[assembly: InternalsVisibleTo("CodeBrix.Platform.SkiaSharp.Views")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.SkiaSharp.Views.Core")]

// The SkiaSharp.Views add-in's unit-test suite reads back the pixels the control just presented
// (the same internal Buffer accessor as above) and registers a fake IDisplayInformationExtension so
// a host-free test can measure the control at a display scale other than 1.
[assembly: InternalsVisibleTo("CodeBrix.Platform.SkiaSharp.Views.Tests")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.PlatformIslandsSamplesApp.Skia")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Tizen")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.AppleUIKit")]

// CodeBrix.Platform.Core (the platform-neutral Core assembly this file is compiled into since the Core/Skia split):
// its Skia twin, which implements the platform contracts and reaches Core internals,
[assembly: InternalsVisibleTo("CodeBrix.Platform")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Android.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.Tests")]

// The grants the pre-split assembly carried in AssemblyInfo.skia.cs (the heads and Skia-side test hosts), which
// reach Core internals too; the Core assembly keeps the union of the old lists (decision P4).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wayland")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost.Skia.Wpf")]

// The host-free Core suite (src/Platform.UI.Core.Tests) registers test doubles for the platform contracts.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core.Tests")]

// Element handler seam (WPH1, decisions D-P3 and the AP1-B cross-assembly rule): the CodeBrix.Android and CodeBrix.Mobile
// counterparts (and their tests) of every Skia-side assembly granted above - the framework Skia twins and the add-ins'
// platform assemblies - so that a platform implementation of a Core contract, or a platform handler, compiles against the
// same internals the Skia side uses. This Core assembly also grants every add-in's Android and Mobile name (D-P3).
[assembly: InternalsVisibleTo("CodeBrix.Android.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Toolkit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Toolkit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Composition")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Composition.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Composition")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Composition.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Lottie.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Lottie.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Svg.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Svg.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.MediaPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.MediaPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.MediaPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.MediaPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.VideoPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.VideoPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.VideoPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.VideoPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.WebView")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.WebView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.WebView")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.WebView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Graphics3DGL")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Graphics3DGL.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Graphics3DGL")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Graphics3DGL.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.SkiaSharp.Views")]
[assembly: InternalsVisibleTo("CodeBrix.Android.SkiaSharp.Views.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.SkiaSharp.Views")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.SkiaSharp.Views.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.AppSettings")]
[assembly: InternalsVisibleTo("CodeBrix.Android.AppSettings.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.AppSettings")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.AppSettings.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AdvancedTextEdit")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AdvancedTextEdit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AdvancedTextEdit")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AdvancedTextEdit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AudioPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AudioPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AudioPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AudioPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.CommandBar")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.CommandBar.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.CommandBar")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.CommandBar.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FlexPanel")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FlexPanel.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FlexPanel")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FlexPanel.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.PlotterView")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.PlotterView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.PlotterView")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.PlotterView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TerminalView")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TerminalView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TerminalView")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TerminalView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TextLayout")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TextLayout.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TextLayout")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TextLayout.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Graphics2DSK")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Graphics2DSK.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Graphics2DSK")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Graphics2DSK.Tests")]

[assembly: Microsoft.UI.Xaml.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Windows" /* Keep to avoid renaming */ + ".UI")]

namespace Microsoft.UI.Xaml;

// This attribute is aligned with https://github.com/dotnet/maui/blob/312948086267cf6c529dfeb2ec0eeae7e7aa57ae/src/Graphics/src/Graphics/XmlnsDefinitionAttribute.cs#L8
// Visual studio now expects this attribute to be present in order to provide intellisense for the types
// in the namespace, and must not have the `Assembly` property.
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[DebuggerDisplay("{XmlNamespace}, {ClrNamespace}")]
internal sealed class XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace) : Attribute
{
	public string XmlNamespace { get; } = xmlNamespace ?? throw new ArgumentNullException(nameof(clrNamespace));
	public string ClrNamespace { get; } = clrNamespace ?? throw new ArgumentNullException(nameof(xmlNamespace));
}
