#nullable enable

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition.Contracts;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionBrushPlatform"/> for a brush that paints nothing (the base
/// <see cref="CompositionBrush"/>, <see cref="CompositionBackdropBrush"/>), and the base of the per-type
/// implementations.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionBrushSkiaPlatform : ICompositionBrushPlatform
{
	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionBrushSkiaPlatform(CompositionBrush owner)
	{
		Owner = owner;
	}

	/// <summary>
	/// Gets the brush this object paints.
	/// </summary>
	internal CompositionBrush Owner { get; }

	/// <summary>
	/// Returns the Skia platform state of <paramref name="brush"/> (creating it on first use).
	/// </summary>
	/// <param name="brush">The brush.</param>
	/// <returns>The brush's platform state.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CompositionBrushSkiaPlatform Of(CompositionBrush brush)
	{
		var platform = brush.Platform;
		Debug.Assert(platform is CompositionBrushSkiaPlatform);
		return Unsafe.As<CompositionBrushSkiaPlatform>(platform);
	}

	/// <summary>
	/// Paints the brush over <paramref name="bounds"/>.
	/// </summary>
	/// <param name="canvas">The canvas to paint on.</param>
	/// <param name="opacity">The accumulated opacity to paint with.</param>
	/// <param name="bounds">The area to paint.</param>
	internal virtual void Paint(SKCanvas canvas, float opacity, SKRect bounds) { }

	/// <inheritdoc />
	public virtual bool CanPaint() => false;

	/// <inheritdoc />
	public virtual bool RequiresRepaintOnEveryFrame => false;

	/// <inheritdoc />
	public virtual Vector2? Size => null;

	/// <inheritdoc />
	public virtual void OnPropertyChanged(string? propertyName) { }

	/// <inheritdoc />
	public virtual void Dispose() { }
}
