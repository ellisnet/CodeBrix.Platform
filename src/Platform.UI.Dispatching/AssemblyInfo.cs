using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Wpf")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Tizen")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.AppleUIKit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost.Skia.Wpf")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.Win32")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]

[assembly: InternalsVisibleTo("CodeBrix.Platform")]

[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
