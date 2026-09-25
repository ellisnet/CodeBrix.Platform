using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.UI.Composition; //Was previously: Uno.UI.Composition

internal static class CompositionMathHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseReal(float a, float b, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseRealZero(float a, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a) < epsilon;

	/// <summary>
	/// Returns the 2D affine part of <paramref name="m"/> (the same conversion as the public
	/// <c>SkiaExtensions.ToMatrix3x2</c>, which is Skia-only). Called with static syntax on purpose, so it never
	/// competes with that extension method.
	/// </summary>
	/// <param name="m">The 3D matrix.</param>
	/// <returns>The 2D matrix.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Matrix3x2 ToMatrix3x2(Matrix4x4 m)
		=> new Matrix3x2(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42);
}
