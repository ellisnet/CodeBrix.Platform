using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace CodeBrix.Platform.PlayTest.Hosting;

// Adapted from the CodeBrix.Platform FrameBuffer.Emulated window and display extensions.
internal sealed class VirtualWindow : NativeWindowWrapperBase, INativeWindowFactoryExtension
{
    private readonly VirtualHost _host;
    internal VirtualWindow(VirtualHost host) => _host = host;
    public override object? NativeWindow => null;
    public override string Title { get; set; } = "PlayTest";
    public bool SupportsClosingCancellation => false;
    public bool SupportsMultipleWindows => false;
    internal UIElement? Root => Window?.RootElement;
    internal Window? ManagedWindow => Window;

    public INativeWindowWrapper CreateWindow(Window window, XamlRoot root)
    {
        if (Window != null && Window != window)
            throw new NotSupportedException("PlayTest supports one application window per process.");
        SetWindow(window, root);
        XamlRootMap.Register(root, _host);
        RasterizationScale = 1;
        UpdateSize();
        return this;
    }

    internal void UpdateSize()
    {
        var bounds = new Rect(0, 0, _host.Width, _host.Height);
        SetBoundsAndVisibleBounds(bounds, bounds);
        var size = new SizeInt32(_host.Width, _host.Height);
        SetSizes(size, size);
    }

    protected internal override void Activate() => ActivationState = CoreWindowActivationState.CodeActivated;
}

internal sealed class VirtualDisplay : IDisplayInformationExtension
{
    private readonly VirtualHost _host;
    internal VirtualDisplay(VirtualHost host) => _host = host;
    public DisplayOrientations CurrentOrientation => _host.Width > _host.Height ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
    public uint ScreenWidthInRawPixels => (uint)_host.Width;
    public uint ScreenHeightInRawPixels => (uint)_host.Height;
    public float LogicalDpi => 96;
    public double RawPixelsPerViewPixel => 1;
    public ResolutionScale ResolutionScale => ResolutionScale.Scale100Percent;
    public double? DiagonalSizeInInches => null;
    public void StartDpiChanged() { }
    public void StopDpiChanged() { }
}

internal sealed class VirtualApplicationView : IApplicationViewExtension
{
    public string Title { get; set; } = "PlayTest";
    public void ExitFullScreenMode() { }
    public bool TryEnterFullScreenMode() => true;
    public bool TryResizeView(Size size) => false;
}
