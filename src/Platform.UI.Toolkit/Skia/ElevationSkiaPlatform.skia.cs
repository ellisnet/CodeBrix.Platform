using CodeBrix.Platform.UI.Composition.Composition;
using CodeBrix.Platform.UI.Toolkit.Contracts;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace CodeBrix.Platform.UI.Toolkit.Skia;

/// <summary>
/// The Skia implementation of <see cref="IElevationPlatform"/>: the element's composition visual carries a
/// <see cref="ShadowState"/> that the Skia compositor paints as a blurred drop shadow.
/// </summary>
internal sealed class ElevationSkiaPlatform : IElevationPlatform
{
	/// <inheritdoc />
	public void SetElevation(UIElement element, double elevation, Color shadowColor)
	{
		var visual = element.Visual;
		const float x = 0.28f;
		const float y = 0.92f * 0.5f;
		const float blur = 0.18f;

		var dx = (float)elevation * x;
		var dy = (float)elevation * y;
		var sigmaX = (float)(blur * elevation);
		var sigmaY = (float)(blur * elevation);
		var shadow = new ShadowState(dx, dy, sigmaX, sigmaY, shadowColor);
		visual.ShadowState = shadow;
	}
}
