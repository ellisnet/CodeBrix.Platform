#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Composition;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Helpers;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionTargetPlatform"/>: records each frame of the root's visual tree
/// into an <see cref="SKPicture"/> (with the clip path of the native elements it hosts), and draws the last recorded
/// frame when the head's native window asks for pixels (<see cref="Draw"/>).
/// </summary>
internal sealed class CompositionTargetSkiaPlatform : ICompositionTargetPlatform
{
	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	private static SKPath? _lastNativeClipPath;
	private static SKPath? _lastScaledNativeClipPath;

	private readonly CompositionTarget _owner;
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly Lock _frameGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	private float _lastRasterizationScale = 1;

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IntPtr frame, SKPath nativeElementClipPath)? _lastRenderedFrame;
	// only set and read on the UI thread
	private List<Visual> _nativeVisualsInZOrder = new();
	// only set and read on the UI thread: the order of the native elements in the last recorded frame
	private List<Visual> _recordedNativeVisualsInZOrder = new();

	/// <summary>
	/// Creates the Skia side of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The composition target.</param>
	internal CompositionTargetSkiaPlatform(CompositionTarget owner)
	{
		_owner = owner;
	}

	/// <summary>
	/// Returns the Skia side of <paramref name="target"/>.
	/// </summary>
	/// <param name="target">The composition target.</param>
	/// <returns>The Skia side of the target.</returns>
	internal static CompositionTargetSkiaPlatform Of(CompositionTarget target) => (CompositionTargetSkiaPlatform)target.Platform;

	/// <summary>
	/// This method is called from each platform's rendering logic in response to the native windowing/composition
	/// engine's signal requesting the CodeBrix app to draw something _right now_, usually synced to the refresh rate
	/// of the screen (e.g. Android's IRenderer.OnDrawFrame). This class does not assume that this method will only
	/// be called once per <see cref="CodeBrix.Platform.UI.Hosting.IXamlRootHost.InvalidateRender"/> call, but the contract allows any number
	/// of repeated calls, even if no new invalidations are requested.
	/// </summary>
	/// <remarks>
	/// Before the Core/Skia split this was the <c>CompositionTarget.OnNativePlatformFrameRequested(SKCanvas?, Func&lt;Size, SKCanvas&gt;)</c>
	/// overload (a Skia partial of the Core type); the heads call it here now.
	/// </remarks>
	/// <param name="target">The composition target of the window to draw.</param>
	/// <param name="canvas">The canvas to draw on, or <see langword="null"/> to get one from <paramref name="resizeFunc"/>.</param>
	/// <param name="resizeFunc">Creates a canvas of the given size when the size changed or no canvas was given.</param>
	/// <returns>The clip path of the native elements in the drawn frame.</returns>
	internal static SKPath OnNativePlatformFrameRequested(CompositionTarget target, SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		target.OnNativePlatformFrameRequested();

		return Of(target).Draw(canvas, resizeFunc);
	}

	/// <inheritdoc />
	public bool CanRecordFrame() => SkiaRenderHelper.CanRecordPicture(_owner.ContentRoot.VisualTree.RootElement);

	/// <inheritdoc />
	public void RecordFrame()
	{
		var rootElement = _owner.ContentRoot.VisualTree.RootElement;
		var bounds = _owner.ContentRoot.VisualTree.Size;

		var (picture, path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath);
		var renderedFrame = (picture, path);
		var previousFrame = default((IntPtr frame, SKPath path)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			_lastRenderedFrame = renderedFrame;
		}

		// Delete previous SKPicture now since we are swapping it
		if (previousFrame != null)
		{
			CodeBrixSkiaApi.sk_refcnt_safe_unref(previousFrame.Value.frame);
		}

		_recordedNativeVisualsInZOrder = nativeVisualsInZOrder;
	}

	/// <inheritdoc />
	public void UpdateNativeElementsOrder()
	{
		var nativeVisualsInZOrder = _recordedNativeVisualsInZOrder;

		var nativeVisualsZOrderChanged = _nativeVisualsInZOrder.Count != nativeVisualsInZOrder.Count;
		if (!nativeVisualsZOrderChanged)
		{
			for (int i = 0; i < nativeVisualsInZOrder.Count; i++)
			{
				if (nativeVisualsInZOrder[i] != _nativeVisualsInZOrder[i])
				{
					nativeVisualsZOrderChanged = true;
					break;
				}
			}
		}

		if (nativeVisualsZOrderChanged)
		{
			_nativeVisualsInZOrder = nativeVisualsInZOrder;
			ContentPresenter.OnNativeHostsRenderOrderChanged(nativeVisualsInZOrder);
		}
	}

	/// <summary>
	/// Draws the last recorded frame onto <paramref name="canvas"/> (resized first through
	/// <paramref name="resizeFunc"/> when the root's size or scale changed), raises
	/// <see cref="CompositionTarget.Rendering"/>, and returns the clip path of the native elements, scaled to the
	/// canvas. Called by the head on its rendering thread.
	/// </summary>
	/// <param name="canvas">The head's canvas, or <see langword="null"/> when it has none yet.</param>
	/// <param name="resizeFunc">Creates a canvas of the given pixel size.</param>
	/// <returns>The clip path of the native elements.</returns>
	internal SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		_owner.LogTrace()?.Trace($"CompositionTarget#{_owner.GetHashCode()}: {nameof(Draw)}");

		(IntPtr frame, SKPath nativeElementClipPath)? lastRenderedFrameNullable;
		lock (_frameGate)
		{
			lastRenderedFrameNullable = _lastRenderedFrame;

			// Borrow frame temporarily
			_lastRenderedFrame = null;
		}

		if (lastRenderedFrameNullable is not { } lastRenderedFrame)
		{
			return new SKPath();
		}
		else
		{
			_owner.GetXamlRootBounds(out var xamlRootBounds, out var rasterizationScale);
			if (xamlRootBounds.Width <= 0 || xamlRootBounds.Height <= 0)
			{
				ReturnFrame(lastRenderedFrame);

				// Besides being an optimization step, returning early here also avoids resizing
				// the canvas to 0x0 which may crash on some targets
				return lastRenderedFrame.nativeElementClipPath;
			}
			if (canvas is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale)
			{
				canvas = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;
			}

			canvas.Save();
			if (rasterizationScale != 1)
			{
				canvas.Scale(rasterizationScale, rasterizationScale);
			}
			SkiaRenderHelper.RenderPicture(
				canvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper);
			canvas.Restore();

			ReturnFrame(lastRenderedFrame);

			CompositionTarget.InvokeRendering();

			if (FrameRenderingOptions.applyScalingToNativeElementClipPath && rasterizationScale != 1)
			{
				if (_lastNativeClipPath != lastRenderedFrame.nativeElementClipPath || _lastScaledNativeClipPath == null)
				{
					_lastScaledNativeClipPath = new();

					lastRenderedFrame
						.nativeElementClipPath
						.Transform(SKMatrix.CreateScale(rasterizationScale, rasterizationScale), _lastScaledNativeClipPath);

					_lastNativeClipPath = lastRenderedFrame.nativeElementClipPath;
				}

				return _lastScaledNativeClipPath;
			}

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	private void ReturnFrame((IntPtr picture, SKPath path) frame)
	{
		var pictureToDelete = IntPtr.Zero;

		lock (_frameGate)
		{
			// Put the frame back unless it has changed
			if (_lastRenderedFrame == null)
			{
				_lastRenderedFrame = frame;
			}
			else
			{
				pictureToDelete = frame.picture;
			}
		}

		// Delete it then
		if (pictureToDelete != IntPtr.Zero)
		{
			CodeBrixSkiaApi.sk_refcnt_safe_unref(pictureToDelete);
		}
	}
}
