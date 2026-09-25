#nullable enable

using System;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Internal;

/// <summary>
/// Converts the text engine's rectangles to SkiaSharp's, exactly as the conversion through Windows.Foundation.Rect did
/// before the text-engine home change (WPE1 C5): the same float edges (Right and Bottom summed in float) and the same
/// negative-size check, which that type's constructor made when the application turned it on.
/// </summary>
internal static class EngineGeometry
{
	private const string NegativeErrorMessage = "Non-negative number required.";

	/// <summary>The SkiaSharp rectangle of an engine rectangle.</summary>
	/// <param name="rect">The engine rectangle.</param>
	/// <returns>The rectangle by its edges.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The rectangle has a negative size and the application disallows
	/// that (FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight is false).</exception>
	internal static SKRect ToSKRect(EngineRect rect)
	{
		if (!CodeBrix.Platform.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight
			&& ((float)rect.Width < 0 || (float)rect.Height < 0))
		{
			// The same exception (and parameter name) Windows.Foundation.Rect's constructor throws.
			throw new ArgumentOutOfRangeException("width", NegativeErrorMessage);
		}

		return new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);
	}
}
