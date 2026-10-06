using SkiaSharp.Views.Windows.Contracts;

namespace SkiaSharp.Views.Windows;

//was previously: the constructor and the three surface members were in SKSwapChainPanel.Skia.cs, which made the panel
//unsupported on Skia. That is a statement about the platform and moved to the Skia assembly behind
//ISKSwapChainPanelPlatform (Skia/SKSwapChainPanelSkiaPlatform.skia.cs); this file connects the vendored shared half
//to it.
public partial class SKSwapChainPanel
{
	private readonly ISKSwapChainPanelPlatform _platform;

	/// <summary>Initializes a new instance of the <see cref="SKSwapChainPanel" /> class.</summary>
	/// <remarks>The Skia-based platforms have no GPU swap chain: there the panel is not supported (see
	/// <see cref="RaiseOnUnsupported" />).</remarks>
	/// <exception cref="System.NotSupportedException">The running platform does not support the panel and
	/// <see cref="RaiseOnUnsupported" /> is <see langword="true" /> (the default).</exception>
	public SKSwapChainPanel()
	{
		_platform = PlatformContract.Create<ISKSwapChainPanelPlatform>(this);

		_platform.OnConstructed();
	}

	private SKSize GetCanvasSize()
	{
		var size = _platform.GetCanvasSize();
		return new SKSize((float)size.Width, (float)size.Height);
	}

	private GRContext GetGRContext() => (GRContext)_platform.GetGRContext();

	private void DoInvalidate() => _platform.Invalidate();
}
