#nullable enable

using SkiaSharp;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition //Was previously: Uno.UI.Composition
{
	internal interface ISkiaCompositionSurfaceProvider
	{
		SkiaCompositionSurface? SkiaCompositionSurface { get; }
	}
}
