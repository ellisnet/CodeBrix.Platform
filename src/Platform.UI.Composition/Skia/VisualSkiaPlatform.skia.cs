#nullable enable
//#define TRACE_COMPOSITION

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Composition.Composition;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="IVisualPlatform"/> for a plain <see cref="Visual"/>, and the base of the
/// per-type implementations. It renders the visual and its subtree onto an <see cref="SKCanvas"/>, records the
/// visual's own content (and, for large unchanged subtrees, the content of its children) into
/// <c>SKPicture</c>s that are replayed on later frames, and computes the clip path of native elements.
/// </summary>
/// <remarks>
/// This is the code that lived in <c>Visual.skia.cs</c> before the Composition seam, moved verbatim; the state that
/// the platform-neutral tree also needs (dirty flags, the total matrix, the picture-collapsing settings) stays on
/// <see cref="Visual"/>.
/// </remarks>
internal class VisualSkiaPlatform : IVisualPlatform
{
	private static readonly ObjectPool<SKPath> _pathPool = new(() => new SKPath());
	private static readonly SKPath _spareRenderPath = new SKPath();

	private static readonly IPrivateSessionFactory _factory = new PaintingSession.SessionFactory();

	private static SKPictureRecorder _recorder = new();

	private IntPtr _picture;
	private IntPtr _childrenPicture;

	// The drop-shadow paint, built for the ShadowState instance it was built from.
	private ShadowState? _shadowOnlyPaintState;
	private SKPaint? _shadowOnlyPaint;

	private const int SK_MaxS32FitsInFloat = 2147483520;
	// Skia uses SafeEdge = SK_MaxS32FitsInFloat / 2 - 1, but that causes clipping bounds issues in SKCanvasElement when used with LottieVisualSourceBase
	private const int SafeEdge = SK_MaxS32FitsInFloat / 4 - 1;
	// if we use float.Min/MaxValue, weird overflows happen and clipping breaks badly.
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRecorder.cpp#L79
	// https://github.com/mono/skia/blob/927041a58f130e0dd0562ba86cb4170989ad39e9/src/core/SkRectPriv.h#L38
	internal static SKRect InfiniteClipRect { get; } = new(-SafeEdge, -SafeEdge, SafeEdge, SafeEdge);

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal VisualSkiaPlatform(Visual owner)
	{
		Owner = owner;
	}

	/// <summary>
	/// Gets the visual this object renders.
	/// </summary>
	internal Visual Owner { get; }

	/// <summary>
	/// Returns the Skia platform state of <paramref name="visual"/>: one field read, no lookup, no type check
	/// in release builds. Every visual's platform state is a <see cref="VisualSkiaPlatform"/> when the Skia
	/// platform is registered.
	/// </summary>
	/// <param name="visual">The visual.</param>
	/// <returns>The visual's platform state.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VisualSkiaPlatform Of(Visual visual)
	{
		Debug.Assert(visual.Platform is VisualSkiaPlatform);
		return Unsafe.As<VisualSkiaPlatform>(visual.Platform);
	}

	/// <inheritdoc />
	public void DiscardPaintCache()
	{
		if (_picture != IntPtr.Zero)
		{
			CodeBrixSkiaApi.sk_refcnt_safe_unref(_picture);
			_picture = IntPtr.Zero;
		}
	}

	/// <inheritdoc />
	public void DiscardChildrenCache()
	{
		if (_childrenPicture != IntPtr.Zero)
		{
			CodeBrixSkiaApi.sk_refcnt_safe_unref(_childrenPicture);
			_childrenPicture = IntPtr.Zero;
		}
	}

	/// <inheritdoc cref="IVisualPlatform.CanPaint" />
	public virtual bool CanPaint() => false;

	// this is for effect brushes that apply an effect on an already-drawn area, so these need to be painted every frame.
	/// <inheritdoc cref="IVisualPlatform.RequiresRepaintOnEveryFrame" />
	public virtual bool RequiresRepaintOnEveryFrame => false;

	/// <inheritdoc cref="IVisualPlatform.HitTest" />
	public virtual bool HitTest(Point point) => false;

	private SKPaint GetShadowOnlyPaint(ShadowState shadowState)
	{
		if (!ReferenceEquals(_shadowOnlyPaintState, shadowState) || _shadowOnlyPaint is null)
		{
			_shadowOnlyPaint = new SKPaint()
			{
				// Equivalent (I think) to SKImageFilter.CreateDropShadow(Dx, Dy, SigmaX, SigmaY, Color.ToSKColor()) but much much faster
				// Writing our own shader that does the same (basically takes the alpha value of the given pixel
				// adjust by (Dx, Dy) and multiplies it by ShadowState.Color and "modulates" it with the original
				// pixel) did not improve the numbers one bit.
				ImageFilter = SKImageFilter.CreateOffset(shadowState.Dx, shadowState.Dy, SKImageFilter.CreateCompose(SKImageFilter.CreateBlur(shadowState.SigmaX, shadowState.SigmaY), SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(shadowState.Color.ToSKColor(), SKBlendMode.Modulate))))
			};
			_shadowOnlyPaintState = shadowState;
		}

		return _shadowOnlyPaint;
	}

	/// <summary>
	/// Render a visual as if it's the root visual.
	/// </summary>
	/// <param name="canvas">The canvas on which this visual should be rendered.</param>
	/// <param name="offsetOverride">The offset (from the origin) to render the Visual at. If null, the offset properties on the Visual like <see cref="Visual.Offset"/> and <see cref="Visual.AnchorPoint"/> are used.</param>
	internal void RenderRootVisual(SKCanvas canvas, Vector2? offsetOverride)
	{
		var owner = Owner;
		if (owner is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		// Since we're acting as if this visual is a root visual, we undo the parent's TotalMatrix
		// so that when concatenated with this visual's TotalMatrix, the result is only the transforms
		// from this visual.
		// It's important to set the default to canvas.TotalMatrix not SKMatrix.Identity in case there's
		// an initial global transformation set (e.g. if the renderer sets scaling for dpi or we're rendering from a VisualSurface)
		var initialTransform = canvas.TotalMatrix.ToMatrix4x4();
		if (owner.Parent?.TotalMatrix is { } parentTotalMatrix)
		{
			Matrix4x4.Invert(parentTotalMatrix, out var invertedParentTotalMatrix);
			initialTransform = invertedParentTotalMatrix * initialTransform;
		}

		if (offsetOverride is { } offset)
		{
			var totalOffset = owner.GetTotalOffset();
			var translation = Matrix4x4.Identity with { M41 = -(offset.X + totalOffset.X + owner.AnchorPoint.X), M42 = -(offset.Y + totalOffset.Y + owner.AnchorPoint.Y) };
			initialTransform = translation * initialTransform;
		}

		_factory.CreateInstance(owner,
						  canvas,
						  ref initialTransform.IsIdentity ? ref Unsafe.NullRef<Matrix4x4>() : ref initialTransform,
						  opacity: 1.0f,
						  out var session);

		using (session)
		{
			// we set the matrix here similarly to CreateLocalMatrix in case the SetMatrix call there is
			// omitted.
			canvas.SetMatrix(initialTransform.IsIdentity ? owner.TotalMatrix : owner.TotalMatrix * initialTransform);
			Render(session);
		}
	}

	/// <summary>
	/// Position a sub visual on the canvas and draw its content.
	/// </summary>
	/// <param name="parentSession">The drawing session of the <see cref="Visual.Parent"/> visual.</param>
	/// <param name="applyChildOptimization">Whether a large unchanged subtree may be recorded once and replayed.</param>
	private void Render(in PaintingSession parentSession, bool applyChildOptimization = true)
	{
		var owner = Owner;
#if TRACE_COMPOSITION
		var indent = int.TryParse(owner.Comment?.Split(new char[] { '-' }, 2, StringSplitOptions.TrimEntries).FirstOrDefault(), out var depth)
			? new string(' ', depth * 2)
			: string.Empty;
		global::System.Diagnostics.Debug.WriteLine($"{indent}{owner.Comment} (Opacity:{parentSession.Opacity:F2}x{owner.Opacity:F2} | IsVisible:{owner.IsVisible})");
#endif

		if (owner is { Opacity: 0 } or { IsVisible: false })
		{
			return;
		}

		if ((owner._flags & Visual.VisualFlags.ChildrenSKPictureInvalid) == 0)
		{
			owner._framesSinceSubtreeNotChanged++;
		}
		else
		{
			owner._framesSinceSubtreeNotChanged = 0;
			owner._flags &= ~Visual.VisualFlags.ChildrenSKPictureInvalid;
		}

		CreateLocalSession(in parentSession, out var session);

		using (session)
		{
			var canvas = session.Canvas;

			var preClip = _spareRenderPath;

			preClip.Reset();

			if (GetPrePaintingClipping(preClip))
			{
				canvas.ClipPath(preClip, antialias: true);
			}

			if (owner.ShadowState is not { } shadowState)
			{
				PaintStep(this, session);
				PostPaintingClipStep(this, canvas);
				RenderChildrenStep(this, session, applyChildOptimization);
			}
			else
			{
				var recorder = new SKPictureRecorder();
				var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(owner.TotalMatrix, out var rootTransform);
				_factory.CreateInstance(owner, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
				using (childSession)
				{
					PaintStep(this, childSession);
					PostPaintingClipStep(this, recordingCanvas);
					RenderChildrenStep(this, childSession, applyChildOptimization);
				}

				unsafe
				{
					var childrenPicture = CodeBrixSkiaApi.sk_picture_recorder_end_recording(recorder.Handle);

					CodeBrixSkiaApi.sk_canvas_draw_picture(canvas.Handle, childrenPicture, null, GetShadowOnlyPaint(shadowState).Handle);
					CodeBrixSkiaApi.sk_canvas_draw_picture(canvas.Handle, childrenPicture, null, IntPtr.Zero);

					CodeBrixSkiaApi.sk_refcnt_safe_unref(childrenPicture);
				}
			}
		}

		static void PaintStep(VisualSkiaPlatform platform, in PaintingSession session)
		{
			var visual = platform.Owner;
			// Rendering shouldn't depend on matrix or clip adjustments happening in a visual's Paint. That should
			// be specific to that visual and should not affect the rendering of any other visual.
#if DEBUG
			var saveCount = session.Canvas.SaveCount;
#endif
			if (platform.RequiresRepaintOnEveryFrame)
			{
				// why bother with a recorder when it's going to get repainted next frame? just paint directly
				platform.Paint(session);
			}
			else
			{
				if ((visual._flags & Visual.VisualFlags.PaintDirty) != 0)
				{
					visual._flags &= ~Visual.VisualFlags.PaintDirty;

					var recordingCanvas = _recorder.BeginRecording(InfiniteClipRect);
					_factory.CreateInstance(visual, recordingCanvas, ref session.RootTransform, session.Opacity, out var recorderSession);
					// To debug what exactly gets repainted, replace the following line with `Paint(in session);`
					platform.Paint(in recorderSession);

					var picture = CodeBrixSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);

					if (platform._picture != IntPtr.Zero)
					{
						CodeBrixSkiaApi.sk_refcnt_safe_unref(platform._picture);
					}

					platform._picture = picture;
				}

				if (platform._picture != IntPtr.Zero)
				{
					unsafe
					{
						CodeBrixSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, platform._picture, null, IntPtr.Zero);
					}
				}
			}
#if DEBUG
			Debug.Assert(saveCount == session.Canvas.SaveCount);
#endif
		}

		static void PostPaintingClipStep(VisualSkiaPlatform platform, SKCanvas canvas)
		{
#if DEBUG
			canvas.Save();
			if (platform.GetPostPaintingClipping() is { } postClip)
			{
				canvas.ClipPath(postClip, antialias: true);
			}

			var nonOptimizedClip = (canvas.DeviceClipBounds, canvas.IsClipRect);
			canvas.Restore();
#endif
			platform.ApplyPostPaintingClipping(canvas);
#if DEBUG
			Debug.Assert(nonOptimizedClip.IsClipRect == canvas.IsClipRect && nonOptimizedClip.DeviceClipBounds == canvas.DeviceClipBounds);
#endif
		}

		static void RenderChildrenStep(VisualSkiaPlatform platform, PaintingSession session, bool applyChildOptimization)
		{
			var visual = platform.Owner;
			if (platform._childrenPicture != IntPtr.Zero)
			{
				unsafe
				{
					CodeBrixSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, platform._childrenPicture, null, IntPtr.Zero);
				}
			}
			else if (!visual._enablePictureCollapsingOptimization
					 || visual._framesSinceSubtreeNotChanged < visual._pictureCollapsingOptimizationFrameThreshold
					 || !applyChildOptimization
					 || visual.GetSubTreeVisualCount() < visual._pictureCollapsingOptimizationVisualCountThreshold)
			{
				foreach (var child in visual.GetChildrenInRenderOrder())
				{
					Of(child).Render(in session, applyChildOptimization);
				}
			}
			else
			{
				var recorder = new SKPictureRecorder();
				var recordingCanvas = recorder.BeginRecording(InfiniteClipRect);
				// child.Render will reapply the total transform matrix, so we need to invert ours.
				Matrix4x4.Invert(visual.TotalMatrix, out var rootTransform);
				_factory.CreateInstance(visual, recordingCanvas, ref rootTransform, session.Opacity, out var childSession);
				using (childSession)
				{
					foreach (var child in visual.GetChildrenInRenderOrder())
					{
						Of(child).Render(in childSession, applyChildOptimization: false);
					}
				}

				var picture = IntPtr.Zero;

				unsafe
				{
					picture = CodeBrixSkiaApi.sk_picture_recorder_end_recording(recorder.Handle);
					CodeBrixSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, picture, null, IntPtr.Zero);
				}

				// The visual can be set on a ChildrenSKPictureInvalid path after the render has started.
				// In such case, we should not cache this picture. Not only it is outdated, it will also lead to a corrupted state,
				// where subtree rendering is skipped with the cached picture,
				// and its descendant can't invalidate the cached picture since they area already on a ChildrenSKPictureInvalid path.
				if ((visual._flags & Visual.VisualFlags.ChildrenSKPictureInvalid) == 0)
				{
					if (platform._childrenPicture != IntPtr.Zero)
					{
						CodeBrixSkiaApi.sk_refcnt_safe_unref(platform._childrenPicture);
					}

					platform._childrenPicture = picture;
				}
				else
				{
					CodeBrixSkiaApi.sk_refcnt_safe_unref(picture);
				}
			}
		}
	}

	internal void GetNativeViewPathAndZOrder(SKPath clipFromParent, SKPath clipPath, List<Visual> nativeVisualsInZOrder)
	{
		var owner = Owner;
		if (owner is { Opacity: 0 } or { IsVisible: false } || clipFromParent.IsEmpty)
		{
			return;
		}

		var localClipCombinedByClipFromParent = _pathPool.Allocate();
		using var rentedArrayDisposable = new DisposableStruct<SKPath>(static path => _pathPool.Free(path), localClipCombinedByClipFromParent);
		localClipCombinedByClipFromParent.Reset();

		if (GetPrePaintingClipping(_spareRenderPath))
		{
			localClipCombinedByClipFromParent.Op(_spareRenderPath, SKPathOp.Union, localClipCombinedByClipFromParent);
		}
		else
		{
			using var sizeRectBuilder = new SKPathBuilder();
			sizeRectBuilder.AddRect(new SKRect(0, 0, owner.Size.X, owner.Size.Y));
			using var sizeRectPath = sizeRectBuilder.Snapshot();
			localClipCombinedByClipFromParent.Op(sizeRectPath, SKPathOp.Union, localClipCombinedByClipFromParent);
		}
		localClipCombinedByClipFromParent.Transform(owner.TotalMatrix.ToSKMatrix(), localClipCombinedByClipFromParent);
		localClipCombinedByClipFromParent.Op(clipFromParent, SKPathOp.Intersect, localClipCombinedByClipFromParent);

		if (owner.IsNativeHostVisual || owner.CanPaint())
		{
			clipPath.Op(localClipCombinedByClipFromParent, owner.IsNativeHostVisual ? SKPathOp.Union : SKPathOp.Difference, clipPath);
		}

		if (owner.IsNativeHostVisual && !localClipCombinedByClipFromParent.IsEmpty)
		{
			nativeVisualsInZOrder.Add(owner);
		}

		if (GetPostPaintingClipping() is { } postClip)
		{
			postClip.Transform(owner.TotalMatrix.ToSKMatrix(), postClip);
			localClipCombinedByClipFromParent.Op(postClip, SKPathOp.Intersect, localClipCombinedByClipFromParent);
		}
		foreach (var child in owner.GetChildrenInRenderOrder())
		{
			Of(child).GetNativeViewPathAndZOrder(localClipCombinedByClipFromParent, clipPath, nativeVisualsInZOrder);
		}
	}

	/// <summary>
	/// Draws the content of this visual.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal virtual void Paint(in PaintingSession session) { }

	internal virtual bool GetPrePaintingClipping(SKPath dst)
	{
		var owner = Owner;
		// Apply the clipping defined on the element
		// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ContainerVisual through the LayoutClip)
		// Note: The Clip is applied after the transformation matrix, so it's also transformed.
		if (owner.Clip is not null)
		{
			dst.Reset();
			dst.Op(owner.Clip is { } clip ? CompositionClipSkiaPlatform.Of(clip).GetClipPath(owner) : null, SKPathOp.Union, dst);
			return true;
		}
		return false;
	}

	/// <summary>This clipping won't affect the visual itself, but its children.</summary>
	private protected virtual SKPath? GetPostPaintingClipping() => null;
	/// <summary>This can be overriden if some Visuals can apply the clipping more optimally than generating a path
	/// and then applying the clip. Specifically, if the clipping is a simple rectangle, creating an SKPath with the
	/// rectangle might be a lot more overhead than just calling SKCanvas.ClipRect, specifically on WASM.</summary>
	private protected virtual void ApplyPostPaintingClipping(SKCanvas canvas)
	{
		if (GetPostPaintingClipping() is { } postClip)
		{
			canvas.ClipPath(postClip, antialias: true);
		}
	}

	/// <summary>
	/// Creates a new <see cref="PaintingSession"/> set up with the local coordinates and opacity.
	/// </summary>
	private unsafe void CreateLocalSession(in PaintingSession parentSession, out PaintingSession session)
	{
		var owner = Owner;
		var canvas = parentSession.Canvas;

		ref var rootTransform = ref parentSession.RootTransform;

		var opacity = owner.Opacity == 1.0f ? parentSession.Opacity : parentSession.Opacity * owner.Opacity;

		_factory.CreateInstance(owner, canvas, ref rootTransform, opacity, out session);

		if ((owner._flags & Visual.VisualFlags.MatrixDirty) != 0 || !owner._totalMatrix.isLocalMatrixIdentity)
		{
			Matrix4x4 totalMatrix;

			if (Unsafe.IsNullRef(ref rootTransform))
			{
				totalMatrix = owner.TotalMatrix;
			}
			else
			{
				totalMatrix = owner.TotalMatrix * rootTransform;
			}

			if (!owner._totalMatrix.isLocalMatrixIdentity)
			{
				// this avoids the matrix copying in canvas.SetMatrix()
				CodeBrixSkiaApi.sk_canvas_set_matrix(canvas.Handle, (SKMatrix44*)&totalMatrix);
			}
		}
#if DEBUG
		else
		{
			Debug.Assert(Unsafe.IsNullRef(ref rootTransform)
				? canvas.TotalMatrix == owner.TotalMatrix.ToSKMatrix()
				// Due to the limit precision of doubles, instead of comparing the two matrices directly we compare the Frobenius norm of their difference to zero
				: CompositionMathHelpers.IsCloseRealZero((canvas.TotalMatrix.ToMatrix4x4() - owner.TotalMatrix * rootTransform).ToSKMatrix().Values.Sum(i => i * i), 1e-5f));
		}
#endif
	}
}
