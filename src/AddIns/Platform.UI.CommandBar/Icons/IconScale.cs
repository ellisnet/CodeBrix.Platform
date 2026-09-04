using System;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Where an icon's rasterization scale comes from.
/// </summary>
internal static class IconScale
{
	/// <summary>
	/// The scale to rasterise <paramref name="element"/>'s icon for.
	/// </summary>
	/// <param name="element">The element the icon is on.</param>
	/// <returns>
	/// The element's XamlRoot rasterization scale where there is one, the display's scale where
	/// there is not, and 1.0 in a process with neither.
	/// </returns>
	internal static double Of(FrameworkElement element)
	{
		if (element.XamlRoot is { RasterizationScale: > 0 } root)
		{
			return root.RasterizationScale;
		}

		try
		{
			//96 is the framework's own base dpi (DisplayInformation.BaseDpi, which is internal to
			//it); a display reporting 120 is therefore at 125%.
			const float BaseDpi = 96f;
			var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
			return dpi > 0 ? dpi / BaseDpi : 1d;
		}
		catch (Exception)
		{
			//No display information at all - a host-free process with nothing registered. One
			//logical pixel is one device pixel is the only sensible answer.
			return 1d;
		}
	}
}
