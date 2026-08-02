using System;

namespace SkiaSharp.Views
{
	// NOTE (CodeBrix, SkiaSharp 4.151.0): the probe below no longer works, upstream included.
	// As of SkiaSharp 4.151.0 SKPMColor.PreMultiply is implemented entirely in managed code and
	// no longer P/Invokes into libSkiaSharp, so it can never throw DllNotFoundException and
	// IsValidEnvironment always returns true — even in a designer with no native library loaded.
	// Nothing in CodeBrix.Platform calls IsValidEnvironment, so this is currently harmless, but do
	// not wire it up believing it detects a missing native library. Anything still native-backed
	// (e.g. SKColorSpace.CreateSrgb() or new SKBitmap(1, 1)) would be needed for a working probe.
	// Kept as-is to stay byte-aligned with the vendored upstream source, which has the same defect.
	internal static class EnvironmentExtensions
	{
		private static readonly Lazy<bool> isValidEnvironment = new Lazy<bool>(() =>
		{
			try
			{
				// test an operation that requires the native library
				SKPMColor.PreMultiply(SKColors.Black);
				return true;
			}
			catch (DllNotFoundException)
			{
				// If we can't load the native library,
				// we may be in some designer.
				// We can make this assumption since any other member will fail
				// at some point in the draw operation.
				return false;
			}
		});

		internal static bool IsValidEnvironment => isValidEnvironment.Value;
	}
}
