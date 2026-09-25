#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform state of one <see cref="CompositionBrush"/>: it paints the brush and keeps whatever the platform
/// caches for it (paints, shaders, color arrays, effect filters). One instance per brush, created by
/// <see cref="ICompositionPlatform.CreateBrushPlatform"/> the first time the brush needs it and kept in a field.
/// Disposed when the brush is disposed.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The Skia implementations are <c>CodeBrix.Platform.UI.Composition.Skia.CompositionBrushSkiaPlatform</c> and its
/// per-type subclasses (color, linear and radial gradient, mask, nine-grid, surface, effect, wrapper), created by
/// <c>CompositionSkiaPlatform</c>.
/// </para>
/// </remarks>
internal interface ICompositionBrushPlatform : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the brush paints anything at all (a transparent color brush does not).
	/// </summary>
	/// <returns><see langword="true"/> when the brush has something to paint.</returns>
	bool CanPaint();

	/// <summary>
	/// Gets a value indicating whether what the brush paints must be painted again on every frame (for example
	/// because it samples what is already drawn behind it).
	/// </summary>
	bool RequiresRepaintOnEveryFrame { get; }

	/// <summary>
	/// Gets the natural size of the brush's content, when it has one (a surface brush showing an image);
	/// <see langword="null"/> otherwise.
	/// </summary>
	Vector2? Size { get; }

	/// <summary>
	/// Tells the platform state that a property of the brush that it caches something for has changed.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	void OnPropertyChanged(string? propertyName);
}
