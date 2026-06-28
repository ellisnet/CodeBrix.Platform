using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Windows")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Tizen")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.AppleUIKit")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Windows")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics2DSK")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]

[assembly: InternalsVisibleTo("CodeBrix.PlatformIslandsSamplesApp")]
[assembly: InternalsVisibleTo("CodeBrix.PlatformIslandsSamplesApp.Skia")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
