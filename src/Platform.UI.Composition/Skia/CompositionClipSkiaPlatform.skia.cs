#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition.Contracts;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionClipPlatform"/> for a clip that clips nothing (the base
/// <see cref="CompositionClip"/>), and the base of the per-type implementations: it applies the clip to an
/// <see cref="SKCanvas"/> as a rectangle, a rounded rectangle or a path, whichever the clip can provide first.
/// </summary>
/// <remarks>This is the clipping code that lived in <c>CompositionClip.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionClipSkiaPlatform : ICompositionClipPlatform
{
	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The clip this object applies.</param>
	internal CompositionClipSkiaPlatform(CompositionClip owner)
	{
		Owner = owner;
	}

	/// <summary>
	/// Gets the clip this object applies.
	/// </summary>
	internal CompositionClip Owner { get; }

	/// <summary>
	/// Returns the Skia platform state of <paramref name="clip"/> (creating it on first use).
	/// </summary>
	/// <param name="clip">The clip.</param>
	/// <returns>The clip's platform state.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CompositionClipSkiaPlatform Of(CompositionClip clip)
	{
		var platform = clip.Platform;
		Debug.Assert(platform is CompositionClipSkiaPlatform);
		return Unsafe.As<CompositionClipSkiaPlatform>(platform);
	}

	internal virtual SKPath? GetClipPath(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rounded rect.
	/// </summary>
	private protected virtual SKRoundRect? GetClipRoundedRect(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rect.
	/// </summary>
	private protected virtual SKRect? GetClipRect(Visual visual) => null;

	internal void ApplyClip(Visual visual, SKCanvas canvas)
	{
		if (GetClipRect(visual) is { } clipRect)
		{
			canvas.ClipRect(clipRect, antialias: true);
		}
		else if (GetClipRoundedRect(visual) is { } roundedRect)
		{
			canvas.ClipRoundRect(roundedRect, antialias: true);
		}
		else if (GetClipPath(visual) is { } clipPath)
		{
			canvas.ClipPath(clipPath, antialias: true);
		}
	}
}
