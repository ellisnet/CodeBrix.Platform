using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Interim stand-in until native element hosting is built on Wayland subsurfaces (parity
/// plan P7): it never claims content as a native element — so ContentPresenter behaves
/// exactly as if no extension were registered — but the first attempt logs a one-time
/// Warning instead of the content being SILENTLY ignored.
/// </summary>
internal partial class WaylandNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	public WaylandNativeElementHostingExtension(ContentPresenter presenter)
	{
	}

	public bool IsNativeElement(object content)
	{
		if (content != null)
		{
			WaylandNotSupported.WarnOnce(typeof(WaylandNativeElementHostingExtension),
				"Native element hosting (native content in a ContentPresenter)",
				"it requires Wayland subsurface support, which is not built yet; the content is ignored. " +
				"The shipping WebView (offscreen WPE) and MediaPlayer (vmem) add-ins do not need it.");
		}
		return false;
	}

	// Unreachable while IsNativeElement always returns false; benign implementations keep
	// the interface honest if that ever changes.
	public void AttachNativeElement(object content)
	{
	}

	public void DetachNativeElement(object content)
	{
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => childMeasuredSize;

	// Dev-tools-only API; unreachable while IsNativeElement always returns false.
	public object CreateSampleComponent(string text)
		=> throw new NotSupportedException("Native element hosting is not built on the Wayland head yet (parity plan P7).");

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
	}
}
