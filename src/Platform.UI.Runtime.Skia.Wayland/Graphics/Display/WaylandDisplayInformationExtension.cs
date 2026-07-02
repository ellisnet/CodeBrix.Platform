using System;
using System.Globalization;
using Windows.Graphics.Display;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandDisplayInformationExtension : IDisplayInformationExtension
{
	private const string EnvironmentCodeBrixDisplayScaleOverride = "CODEBRIX_DISPLAY_SCALE_OVERRIDE";

	private readonly float? _scaleOverride;
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
		if (float.TryParse(
			Environment.GetEnvironmentVariable(EnvironmentCodeBrixDisplayScaleOverride),
			NumberStyles.Any,
			CultureInfo.InvariantCulture,
			out var environmentScaleOverride))
		{
			_scaleOverride = environmentScaleOverride;
		}

		_details = ComputeDetails();

		if (WaylandConnection.Instance is { } connection)
		{
			connection.OutputsChanged += UpdateDetails;
		}
	}

	internal void UpdateDetails()
	{
		_details = ComputeDetails();
	}

	private DisplayInformationDetails ComputeDetails()
	{
		var (widthPx, heightPx, outputScale) = WaylandConnection.Instance.PrimaryOutput;

		// Integer wl_output scale for now; fractional-scale-v1 refinement is a later phase.
		var scale = _scaleOverride ?? outputScale;
		if (scale <= 0)
		{
			scale = 1;
		}

		return new DisplayInformationDetails(
			(uint)Math.Max(0, widthPx),
			(uint)Math.Max(0, heightPx),
			scale * DisplayInformation.BaseDpi,
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
