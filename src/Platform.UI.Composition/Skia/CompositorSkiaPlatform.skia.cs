// #define PRINT_FRAME_TIMES
#nullable enable

using System;
using System.Diagnostics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// Renders a frame of a compositor's tree onto an <see cref="SKCanvas"/>: the frame begins on the compositor
/// (running animations advance), the tree is rendered from the root visual, and the frame ends on the compositor
/// (background transitions advance, another frame is requested while anything is animating).
/// </summary>
/// <remarks>This is the frame code that lived in <c>Compositor.skia.cs</c>, moved verbatim.</remarks>
internal static class CompositorSkiaPlatform
{
#if PRINT_FRAME_TIMES
	private static int _frameNumber;
#endif

	/// <summary>
	/// Renders one frame of <paramref name="rootVisual"/>'s tree onto <paramref name="canvas"/>.
	/// </summary>
	/// <param name="compositor">The compositor that owns the tree.</param>
	/// <param name="canvas">The canvas to render on.</param>
	/// <param name="rootVisual">The root visual.</param>
	internal static void RenderRootVisual(Compositor compositor, SKCanvas canvas, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		compositor.BeginFrame();

#if PRINT_FRAME_TIMES
		var start = Stopwatch.GetTimestamp();
#endif
		VisualSkiaPlatform.Of(rootVisual).RenderRootVisual(canvas, null);
#if PRINT_FRAME_TIMES
		var span = Stopwatch.GetElapsedTime(start);
		Console.WriteLine($"Rendered frame {_frameNumber++} in {span.TotalMilliseconds}ms");
#endif

		compositor.EndFrame(rootVisual);
	}
}
