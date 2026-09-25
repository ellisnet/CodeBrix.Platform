using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// Creates <see cref="PaintingSession"/> instances. Only <see cref="PaintingSession.SessionFactory"/> implements it,
/// so only the Skia visual platform (which holds a factory) can open a drawing session.
/// </summary>
internal interface IPrivateSessionFactory
{
	/// <summary>
	/// Opens a drawing session on <paramref name="canvas"/>.
	/// </summary>
	/// <param name="visual">The visual that draws in the session.</param>
	/// <param name="canvas">The canvas to draw on.</param>
	/// <param name="rootTransform">The transform to the root visual of the drawing.</param>
	/// <param name="opacity">The accumulated opacity.</param>
	/// <param name="session">The new session.</param>
	void CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session);
}

/// <summary>
/// Represents the "context" in which a visual draws.
/// </summary>
/// <remarks>
/// This type was nested in <see cref="Visual"/> (as <c>Visual.PaintingSession</c>) before the Core/Skia split; it is a
/// Skia type, so it now lives beside the Skia visual platform that creates it.
/// </remarks>
internal readonly ref struct PaintingSession
{
	/// <summary>
	/// The only <see cref="IPrivateSessionFactory"/>: this dance is done to make it so that only the visual platform
	/// can create a <see cref="PaintingSession"/>.
	/// </summary>
	public readonly struct SessionFactory : IPrivateSessionFactory
	{
		void IPrivateSessionFactory.CreateInstance(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity, out PaintingSession session)
		{
			session = new PaintingSession(visual, canvas, ref rootTransform, opacity);
		}
	}

	private PaintingSession(Visual visual, SKCanvas canvas, ref Matrix4x4 rootTransform, float opacity)
	{
		Canvas = canvas;
		RootTransform = ref rootTransform;
		Opacity = opacity;

		_saveCount = canvas.Save();
	}

	/// <summary>
	/// Restores the canvas to the state it had when the session was opened.
	/// </summary>
	public void Dispose() => Canvas.RestoreToCount(_saveCount);

	/// <summary>
	/// The canvas to draw on.
	/// </summary>
	public readonly SKCanvas Canvas;

	/// <summary>The transform matrix to the root visual of this drawing session (which isn't necessarily the identity matrix due to scaling (DPI) and/or RenderTargetBitmap.</summary>
	public readonly ref Matrix4x4 RootTransform;

	/// <summary>
	/// The accumulated opacity.
	/// </summary>
	public readonly float Opacity;

	private readonly int _saveCount;
}
