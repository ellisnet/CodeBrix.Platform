using System;
using System.Globalization;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandDisplayInformationExtension : IDisplayInformationExtension
{
	private const string EnvironmentCodeBrixDisplayScaleOverride = "CODEBRIX_DISPLAY_SCALE_OVERRIDE";

	private readonly float? _scaleOverride;
	private readonly DisplayInformation _owner;
	private readonly WaylandXamlRootHost _host;
	private DisplayInformationDetails _details;

	private record DisplayInformationDetails(
		uint ScreenWidthInRawPixels,
		uint ScreenHeightInRawPixels,
		float LogicalDpi,
		double RawPixelsPerViewPixel,
		ResolutionScale ResolutionScale,
		double? DiagonalSizeInInches);

	public WaylandDisplayInformationExtension(object owner)
	{
		_owner = (DisplayInformation)owner;

		if (float.TryParse(
			Environment.GetEnvironmentVariable(EnvironmentCodeBrixDisplayScaleOverride),
			NumberStyles.Any,
			CultureInfo.InvariantCulture,
			out var environmentScaleOverride))
		{
			_scaleOverride = environmentScaleOverride;
		}

		// Same owner->window->host indirection as the X11 head: the extension is per window,
		// so the scale can follow the window's own surface (fractional scale is per surface).
		if (AppWindow.GetFromWindowId(_owner.WindowId) is not { } appWindow ||
			Window.GetFromAppWindow(appWindow) is not { } window ||
			WaylandXamlRootHost.GetHostFromWindow(window) is not { } host)
		{
			throw new InvalidOperationException($"{nameof(WaylandDisplayInformationExtension)} couldn't find a {nameof(WaylandXamlRootHost)}.");
		}
		_host = host;
		_host.SetDisplayInformationExtension(this);

		_details = ComputeDetails();

		if (WaylandConnection.Instance is { } connection)
		{
			// Output metrics/scale changes are a scale source for windows without a
			// per-surface fractional scale; the host re-enters UpdateDetails from its
			// scale-changed path on the UI thread.
			connection.OutputsChanged += _host.OnScaleSourceChanged;
		}
	}

	internal void UpdateDetails()
	{
		var oldDetails = _details;
		_details = ComputeDetails();
		if (_details != oldDetails)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"DisplayInformation changed: scale={_details.RawPixelsPerViewPixel}, screen={_details.ScreenWidthInRawPixels}x{_details.ScreenHeightInRawPixels}.");
			}
			_owner.NotifyDpiChanged();
		}
	}

	private DisplayInformationDetails ComputeDetails()
	{
		var (widthPx, heightPx, _) = WaylandConnection.Instance.PrimaryOutput;

		// Per-window effective scale: the surface's preferred fractional scale when the
		// compositor delivers one (wp_fractional_scale_v1), else the integer output scale.
		var scale = _scaleOverride ?? _host.EffectiveScale;
		if (scale <= 0)
		{
			scale = 1;
		}

		return new DisplayInformationDetails(
			(uint)Math.Max(0, widthPx),
			(uint)Math.Max(0, heightPx),
			(float)(scale * DisplayInformation.BaseDpi),
			scale,
			(ResolutionScale)(int)(scale * 100.0),
			null);
	}

	public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

	public uint ScreenHeightInRawPixels => _details.ScreenHeightInRawPixels;

	public uint ScreenWidthInRawPixels => _details.ScreenWidthInRawPixels;

	public float LogicalDpi => _details.LogicalDpi;

	public double RawPixelsPerViewPixel => _details.RawPixelsPerViewPixel;

	public ResolutionScale ResolutionScale => _details.ResolutionScale;

	public double? DiagonalSizeInInches => _details.DiagonalSizeInInches;

	public void StartDpiChanged()
	{
	}

	public void StopDpiChanged()
	{
	}
}
