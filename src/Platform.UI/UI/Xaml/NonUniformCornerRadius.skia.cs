using SkiaSharp;

#if IS_CODEBRIX_COMPOSITION
namespace CodeBrix.Platform.UI.Composition; //Was previously: Uno.UI.Composition
#else
namespace Microsoft.UI.Xaml;
#endif

/// <summary>
/// Skia conversions of <see cref="NonUniformCornerRadius"/>. (An extension class rather than a partial of the struct, so
/// the platform-neutral struct carries no Skia type.)
/// </summary>
internal static class NonUniformCornerRadiusSkiaExtensions
{
	unsafe internal static void GetRadii(this in NonUniformCornerRadius radius, SKPoint* radiiStore)
	{
		*(radiiStore++) = new(radius.TopLeft.X, radius.TopLeft.Y);
		*(radiiStore++) = new(radius.TopRight.X, radius.TopRight.Y);
		*(radiiStore++) = new(radius.BottomRight.X, radius.BottomRight.Y);
		*radiiStore = new(radius.BottomLeft.X, radius.BottomLeft.Y);
	}
}
