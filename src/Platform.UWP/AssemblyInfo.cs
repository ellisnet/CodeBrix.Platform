using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Composition")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.GooglePlay")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Foldable")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.Skia.Win32")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.MediaPlayer.WebAssembly")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebView.Skia.X11")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.XamlHost")]

[assembly: InternalsVisibleTo("CodeBrix.Platform.WinUI.Graphics3DGL")]

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
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.AppleUIKit")]

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
