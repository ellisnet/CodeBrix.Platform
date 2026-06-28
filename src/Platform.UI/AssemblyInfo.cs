using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Diagnostics.CodeAnalysis;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Foldable")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RemoteControl")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Skia")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg.Skia")]
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
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebView.Skia.X11")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.HotDesign.Client")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics2DSK")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(CodeBrix.Platform.UI.RuntimeTypeMetadataUpdateHandler))]

[assembly: AdditionalLinkerHint("System.Dynamic.ExpandoObject")]
[assembly: AdditionalLinkerHint("System.Dynamic.DynamicObject")]
