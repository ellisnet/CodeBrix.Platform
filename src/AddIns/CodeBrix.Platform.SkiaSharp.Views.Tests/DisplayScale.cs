using System;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.Graphics.Display;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// Puts a display scale under the suite's control, so a host-free test can measure the control at
/// something other than 100%.
/// </summary>
/// <remarks>
/// <para>
/// With no application head the Skia <c>DisplayInformation</c> finds no
/// <c>IDisplayInformationExtension</c> through the platform's extensibility registry, logs one
/// warning and falls back to 96 logical dpi, so every canvas reports <c>Dpi</c> 1.0 and the scaled
/// half of the control is never exercised. This registers a MUTABLE fake extension instead, from a
/// module initializer - which runs before any test, and therefore before the first
/// <c>DisplayInformation</c> is created and caches the instance the registry hands it. Because the
/// fake is a single mutable object, changing <see cref="LogicalDpi"/> afterwards is seen live by
/// that cached instance.
/// </para>
/// <para>
/// What is NOT live is the control: <c>SKXamlCanvas</c> reads <c>LogicalDpi</c> once, in
/// <c>Initialize</c>, so a test that wants a different scale must set it and then construct a NEW
/// canvas. Use <see cref="At"/>, which does that and restores the default in a finally.
/// </para>
/// </remarks>
internal static class DisplayScale
{
	/// <summary>The dpi a display at 100% reports; the framework's own base.</summary>
	internal const float BaseDpi = 96f;

	private static readonly MutableDisplayInformationExtension Extension = new();

	[ModuleInitializer]
	internal static void Initialize()
	{
		//Register once, before anything creates a DisplayInformation. ApiExtensibility.Register
		//throws on a duplicate key, which is the behaviour wanted here: if a head ever registered
		//one first, the suite must fail loudly rather than measure the wrong display.
		ApiExtensibility.Register(
			typeof(IDisplayInformationExtension),
			_ => Extension);
	}

	/// <summary>The logical dpi every <c>DisplayInformation</c> in this process now reports.</summary>
	internal static float LogicalDpi
	{
		get => Extension.LogicalDpi;
		set => Extension.LogicalDpi = value;
	}

	/// <summary>
	/// Runs <paramref name="body"/> with the display reporting <paramref name="scale"/> times 96
	/// dpi, then restores 96 dpi whatever happens.
	/// </summary>
	/// <param name="scale">The display scale, e.g. 2.0 for a 200% display.</param>
	/// <param name="body">What to measure. Construct the canvas INSIDE it.</param>
	internal static void At(double scale, Action body)
	{
		LogicalDpi = (float)(BaseDpi * scale);
		try
		{
			body();
		}
		finally
		{
			LogicalDpi = BaseDpi;
		}
	}

	private sealed class MutableDisplayInformationExtension : IDisplayInformationExtension
	{
		public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels => 1080;

		public uint ScreenWidthInRawPixels => 1920;

		public float LogicalDpi { get; set; } = BaseDpi;

		public double RawPixelsPerViewPixel => LogicalDpi / BaseDpi;

		public ResolutionScale ResolutionScale => ResolutionScale.Scale100Percent;

		public double? DiagonalSizeInInches => 15.0;
	}
}

/// <summary>
/// The collection for tests that change the display scale. The scale is process-wide, and so is the
/// one <c>DisplayInformation</c> instance the framework caches per window, so these tests must not
/// run beside anything that constructs a canvas.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DisplayScaleCollection
{
	/// <summary>The collection's name.</summary>
	public const string Name = "Display scale";
}
