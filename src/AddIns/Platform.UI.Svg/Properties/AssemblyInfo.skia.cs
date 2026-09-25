using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Svg;
using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;

// The composition hookup (the Skia side of the Svg add-in since the Core/Skia split): an application's XAML source
// generator finds this attribute in the referenced assemblies and emits the registration of SvgProvider (in
// CodeBrix.Platform.UI.Svg.Core) as the framework's ISvgProvider, which SvgImageSource resolves.
[assembly: ApiExtension(typeof(ISvgProvider), typeof(SvgProvider))]
