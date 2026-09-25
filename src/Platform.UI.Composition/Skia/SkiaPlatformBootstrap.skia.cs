using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Composition.Contracts;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// Registers this assembly's Skia implementations of its platform contracts with
/// <see cref="ApiExtensibility"/>, and fills the per-type factories of the composition objects' platform state.
/// Runs as a module initializer, so the registrations exist before any code of this assembly runs.
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
	/// Registers every Skia contract implementation of this assembly. Safe to call more than once.
	/// </summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			// Resolves the native Skia library on iOS/tvOS (was run by the Compositor's static constructor).
			CodeBrixSkiaApi.Initialize();

			var composition = new CompositionSkiaPlatform();

			composition.Visuals.Register<Visual>(static visual => new VisualSkiaPlatform(visual));
			composition.Visuals.Register<ContainerVisual>(static visual => new ContainerVisualSkiaPlatform(visual));
			composition.Visuals.Register<ShapeVisual>(static visual => new ShapeVisualSkiaPlatform(visual));
			composition.Visuals.Register<SpriteVisual>(static visual => new SpriteVisualSkiaPlatform(visual));
			composition.Visuals.Register<RedirectVisual>(static visual => new RedirectVisualSkiaPlatform(visual));
			composition.Visuals.Register<BorderVisual>(static visual => new BorderVisualSkiaPlatform(visual));

			composition.Brushes.Register<CompositionBrush>(static brush => new CompositionBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionColorBrush>(static brush => new CompositionColorBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionLinearGradientBrush>(static brush => new CompositionLinearGradientBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionRadialGradientBrush>(static brush => new CompositionRadialGradientBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionGradientBrush>(static brush => new CompositionGradientBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionMaskBrush>(static brush => new CompositionMaskBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionNineGridBrush>(static brush => new CompositionNineGridBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionSurfaceBrush>(static brush => new CompositionSurfaceBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionEffectBrush>(static brush => new CompositionEffectBrushSkiaPlatform(brush));
			composition.Brushes.Register<CompositionBrushWrapper>(static brush => new CompositionBrushWrapperSkiaPlatform(brush));

			composition.Clips.Register<CompositionClip>(static clip => new CompositionClipSkiaPlatform(clip));
			composition.Clips.Register<InsetClip>(static clip => new InsetClipSkiaPlatform(clip));
			composition.Clips.Register<RectangleClip>(static clip => new RectangleClipSkiaPlatform(clip));
			composition.Clips.Register<CompositionGeometricClip>(static clip => new CompositionGeometricClipSkiaPlatform(clip));

			composition.Shapes.Register<CompositionShape>(static shape => new CompositionShapeSkiaPlatform(shape));
			composition.Shapes.Register<CompositionSpriteShape>(static shape => new CompositionSpriteShapeSkiaPlatform(shape));

			ApiExtensibility.Register(typeof(ICompositionPlatform), _ => composition);

			var geometry = new CompositionGeometrySkiaPlatform();
			ApiExtensibility.Register(typeof(ICompositionGeometryPlatform), _ => geometry);

			_registered = true;
		}
	}
}
