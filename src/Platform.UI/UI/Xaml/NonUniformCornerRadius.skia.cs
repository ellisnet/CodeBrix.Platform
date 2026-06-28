using SkiaSharp;

#if IS_CODEBRIX_COMPOSITION
namespace CodeBrix.Platform.UI.Composition; //Was previously: Uno.UI.Composition
#else
namespace Microsoft.UI.Xaml;
#endif

partial record struct NonUniformCornerRadius
{
	unsafe internal void GetRadii(SKPoint* radiiStore)
	{
		*(radiiStore++) = new(TopLeft.X, TopLeft.Y);
		*(radiiStore++) = new(TopRight.X, TopRight.Y);
		*(radiiStore++) = new(BottomRight.X, BottomRight.Y);
		*radiiStore = new(BottomLeft.X, BottomLeft.Y);
	}
}
