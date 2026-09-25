using System;
using SkiaSharp.Views.Windows.Contracts;
using Windows.Foundation;

namespace SkiaSharp.Views.Windows.Skia;

//was previously: the Skia partial of SKSwapChainPanel (SkiaSharp.Views.Platform.WinUI.Skia/SKSwapChainPanel.Skia.cs),
//moved here verbatim as the Skia implementation of the element's platform hook. As before, the constructor never runs
//the shared Initialize(): the panel has nothing to present on Skia.

/// <summary>
/// The Skia platform's surface for one <see cref="SKSwapChainPanel"/>: the Skia heads have no GPU swap chain, so the
/// panel is not supported. While <see cref="SKSwapChainPanel.RaiseOnUnsupported"/> is true, constructing the panel
/// and reading its surface properties throw <see cref="NotSupportedException"/>; otherwise the panel is inert.
/// </summary>
internal sealed class SKSwapChainPanelSkiaPlatform : ISKSwapChainPanelPlatform
{
	/// <inheritdoc />
	public void OnConstructed()
	{
		if (SKSwapChainPanel.RaiseOnUnsupported)
		{
			throw new NotSupportedException($"SKSwapChainPanel is not supported for Skia based platforms");
		}
	}

	/// <inheritdoc />
	public Size GetCanvasSize()
	{
		if (SKSwapChainPanel.RaiseOnUnsupported)
		{
			throw new NotSupportedException($"SKSwapChainPanel is not supported for Skia based platforms");
		}

		return new Size();
	}

	/// <inheritdoc />
	public object GetGRContext()
	{
		if (SKSwapChainPanel.RaiseOnUnsupported)
		{
			throw new NotSupportedException($"SKSwapChainPanel is not supported for Skia based platforms");
		}

		return null;
	}

	/// <inheritdoc />
	public void Invalidate() { }
}
