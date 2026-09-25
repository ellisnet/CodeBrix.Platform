using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Contracts;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// Registers this assembly's Skia implementations of its platform contracts with <see cref="ApiExtensibility"/>, after
/// those of the other Skia assemblies of the framework (Dispatching, the WinRT surface, Composition, Toolkit), and
/// applies the Skia renderer's defaults to <see cref="FeatureConfiguration"/>. Since the Core/Skia split the Core
/// assemblies run before any Skia assembly is touched, so the Skia host (<c>SkiaHost</c>) and the host-free test
/// processes call <see cref="EnsureRegistered"/> explicitly, first thing; the module initializer stays as a fallback.
/// </summary>
internal static class SkiaPlatformBootstrap
{
	private static readonly object _gate = new();
	private static bool _registered;

	//A module initializer is the one place that runs before the first use of any type in this assembly,
	//which is exactly when the contracts must be in place (CA2255 is aimed at application-level code).
#pragma warning disable CA2255
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Initialize() => EnsureRegistered();

	/// <summary>
	/// Registers every Skia contract implementation of this assembly and applies the Skia defaults. Safe to call more
	/// than once.
	/// </summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			// The contracts of the assemblies this one builds on come first: this bootstrap extends the composition
			// platform's visual factories below, and the framework needs all of them from its first object on.
			// Each call is idempotent; each of those assemblies also registers from its own module initializer.
			CodeBrix.Platform.UI.Dispatching.Skia.SkiaPlatformBootstrap.EnsureRegistered();
			CodeBrix.Platform.Skia.SkiaPlatformBootstrap.EnsureRegistered();
			CodeBrix.Platform.UI.Composition.Skia.SkiaPlatformBootstrap.EnsureRegistered();
			CodeBrix.Platform.UI.Toolkit.Skia.SkiaPlatformBootstrap.EnsureRegistered();

			// Platform defaults of FeatureConfiguration: an application's own setting always wins over these.
			FeatureConfiguration.Popup.ConstrainByVisibleBoundsPlatformDefault = false;
			FeatureConfiguration.Frame.UseWinUIBehaviorPlatformDefault = true;
			FeatureConfiguration.ToolTip.UseToolTipsPlatformDefault = true;

			var application = new ApplicationSkiaPlatform();
			ApiExtensibility.Register(typeof(IApplicationPlatform), _ => application);

			var fonts = new FontSkiaPlatform();
			ApiExtensibility.Register(typeof(IFontPlatform), _ => fonts);

			var focus = new FocusSkiaPlatform();
			ApiExtensibility.Register(typeof(IFocusPlatform), _ => focus);

			var rendering = new RenderingSkiaPlatform();
			ApiExtensibility.Register(typeof(IRenderingPlatform), _ => rendering);

			var geometry = new GeometrySkiaPlatform();
			ApiExtensibility.Register(typeof(IGeometryPlatform), _ => geometry);

			var imaging = new ImagingSkiaPlatform();
			ApiExtensibility.Register(typeof(IImagingPlatform), _ => imaging);

			var text = new TextSkiaPlatform();
			ApiExtensibility.Register(typeof(ITextPlatform), _ => text);

			// The shared text engine's font source (WPE1 C5): the engine copy in this assembly uses it directly; the copy in
			// CodeBrix.Platform.UI.TextLayout.Core resolves it from here, so both share one typeface cache.
			ApiExtensibility.Register(typeof(CodeBrix.Platform.Foundation.Contracts.IFontSourcePlatform<SkiaSharp.SKTypeface>), _ => FontSourceSkiaPlatform.Instance);

			// A text block's visual paints its text, and an SKCanvasVisual paints its render callback: register their
			// platform states with the composition platform, whose visual factories this assembly's own visual types extend.
			var composition = (CodeBrix.Platform.UI.Composition.Skia.CompositionSkiaPlatform)CodeBrix.Platform.UI.Composition.Contracts.CompositionPlatformServices.Composition;
			composition.Visuals.Register<Microsoft.UI.Composition.TextVisual>(static visual => new TextVisualSkiaPlatform(visual));
			composition.Visuals.Register<CodeBrix.Platform.UI.Graphics.SKCanvasVisual>(static visual => new SKCanvasVisualSkiaPlatform(visual));

			_registered = true;
		}
	}
}
