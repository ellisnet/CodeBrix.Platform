using Microsoft.UI.Composition;
using CodeBrix.Platform.UI.Composition.Contracts;
using CodeBrix.Platform.UI.Composition.Skia;
using CodeBrix.Platform.WinUI.Graphics3DGL.Contracts;

namespace CodeBrix.Platform.WinUI.Graphics3DGL.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IGLCanvasPlatform"/>: a <see cref="GLCanvasElement"/> is drawn
/// with a border visual (its background brush carries the bitmap the element reads the pixels back into), whose Skia
/// platform state queues the render the element asked for before it paints.
/// </summary>
/// <remarks>
/// Moved verbatim from GLCanvasElement.cs at the Core/Skia split (the nested GLVisual and GLVisualSkiaPlatform); the
/// only change is that the pre-paint step now calls <see cref="GLCanvasElement.OnVisualPainting"/>, which holds the
/// same code.
/// </remarks>
internal sealed class GLCanvasSkiaPlatform : IGLCanvasPlatform
{
	/// <inheritdoc/>
	public BorderVisual CreateVisual(GLCanvasElement owner, Compositor compositor)
	{
		GLVisualSkiaPlatform.EnsureRegistered();
		return new GLVisual(owner, compositor);
	}

	private sealed class GLVisual(GLCanvasElement owner, Compositor compositor) : BorderVisual(compositor)
	{
		/// <summary>Gets the element this visual draws.</summary>
		internal GLCanvasElement CanvasElement => owner;
	}

	/// <summary>
	/// The Skia platform state of a <see cref="GLVisual"/>: the border visual's painting, preceded by queuing the render the
	/// element asked for. It is registered for <see cref="GLVisual"/> in the composition platform's visual factories
	/// the first time an element creates its visual (the pattern the framework's TextVisual and SKCanvasVisual follow).
	/// </summary>
	private sealed class GLVisualSkiaPlatform(GLVisual visual) : BorderVisualSkiaPlatform(visual)
	{
		private static readonly object _gate = new();
		private static bool _registered;

		private GLCanvasElement CanvasElement => ((GLVisual)Owner).CanvasElement;

		/// <summary>Registers this platform state for <see cref="GLVisual"/>, once, before the first one is created.</summary>
		internal static void EnsureRegistered()
		{
			lock (_gate)
			{
				if (!_registered)
				{
					((CompositionSkiaPlatform)CompositionPlatformServices.Composition).Visuals.Register<GLVisual>(static v => new GLVisualSkiaPlatform(v));
					_registered = true;
				}
			}
		}

		internal override void Paint(in PaintingSession session)
		{
			// ONLY when a render was actually asked for: see GLCanvasElement.OnVisualPainting.
			CanvasElement.OnVisualPainting();

			base.Paint(in session);
		}
	}
}
