#nullable enable

using System.Numerics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition //Was previously: Uno.UI.Composition
{
	internal interface ISkiaSurface
	{
		internal void Paint(SKCanvas canvas, float opacity);
		internal Vector2 Size { get; }
	}
}
