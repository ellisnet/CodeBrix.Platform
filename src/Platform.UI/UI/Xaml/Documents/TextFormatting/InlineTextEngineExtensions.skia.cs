#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.UI.Dispatching;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// The text engine's per-inline font data. The font is cached in the inline's opaque
/// <see cref="Inline.PlatformFontDetails"/> slot, which the inline clears when a font property changes.
/// </summary>
/// <remarks>This is the engine code that lived in <c>Inline.skia.cs</c>, moved verbatim.</remarks>
internal static class InlineTextEngineExtensions
{
	/// <summary>
	/// Returns the font of <paramref name="inline"/>, loading it on first use. While the font is still loading, the
	/// fallback font is returned and the inline is invalidated when the font arrives.
	/// </summary>
	/// <param name="inline">The inline.</param>
	/// <returns>The font details.</returns>
	internal static FontDetails GetFontInfo(this Inline inline)
	{
		if (inline.PlatformFontDetails is null)
		{
			var (details, task) = FontDetailsCache.GetFont(inline.FontFamily?.Source, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle);
			if (task.IsCompletedSuccessfully)
			{
				inline.PlatformFontDetails = task.Result;
			}
			else
			{
				task.ContinueWith(_ =>
				{
					NativeDispatcher.Main.Enqueue(inline.OnFontLoaded);
				});
				inline.PlatformFontDetails = details;
			}
		}

		Debug.Assert(inline.PlatformFontDetails is FontDetails);
		return Unsafe.As<FontDetails>(inline.PlatformFontDetails);
	}

	/// <summary>
	/// Returns the line height of the font of <paramref name="inline"/>.
	/// </summary>
	/// <param name="inline">The inline.</param>
	/// <returns>The line height.</returns>
	internal static float GetLineHeight(this Inline inline) => inline.GetFontInfo().LineHeight;

	/// <summary>
	/// Returns the height of the font of <paramref name="inline"/> above the baseline.
	/// </summary>
	/// <param name="inline">The inline.</param>
	/// <returns>The height above the baseline.</returns>
	internal static float GetAboveBaselineHeight(this Inline inline) => -inline.GetFontInfo().SKFontMetrics.Ascent;

	/// <summary>
	/// Returns the height of the font of <paramref name="inline"/> below the baseline.
	/// </summary>
	/// <param name="inline">The inline.</param>
	/// <returns>The height below the baseline.</returns>
	internal static float GetBelowBaselineHeight(this Inline inline) => inline.GetFontInfo().SKFontMetrics.Descent;
}
