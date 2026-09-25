using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition.Core")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]

[assembly: InternalsVisibleTo("CodeBrix.Platform")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.Core")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
